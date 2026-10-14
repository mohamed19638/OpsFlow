# OpsFlow on AWS: Kubernetes orchestration + Terraform

This folder describes a small, reproducible AWS deployment for the existing OpsFlow API and Worker. Terraform creates the AWS network, an EKS cluster using EKS Auto Mode, and two ECR image repositories. Kubernetes then runs the API, Worker, PostgreSQL, Redis, and RabbitMQ.

This is a learning/demo deployment, not a claim of production high availability. PostgreSQL and RabbitMQ each run as a single in-cluster StatefulSet with persistent EBS volumes; Redis is a single, non-persistent cache. There is no monitoring stack here. Kubernetes health probes are only used to decide whether a container should receive traffic or restart.

## What orchestration means here

Kubernetes keeps the declared workloads running and connects them through in-cluster Services. For example, the API addresses PostgreSQL as `postgres:5432`, Redis as `redis:6379`, and RabbitMQ as `rabbitmq:5672`; Kubernetes DNS resolves those names. Deployments manage the API, Worker, and Redis pods. StatefulSets and persistent volume claims are used for PostgreSQL and RabbitMQ. The API and Worker wait for dependencies to accept TCP connections before starting.

Terraform creates the AWS foundation that Kubernetes needs: a dedicated VPC with public/private subnets and NAT, EKS, and ECR. Terraform does not build or push application images, create application secrets, or deploy the Kubernetes manifests.

## Important safety notes

- No AWS resources were created by preparing these files. `terraform apply` is a separate, billable action that you run yourself.
- Limit the EKS public API endpoint to your current public IP as a `/32` in `cluster_admin_cidrs`; do not use `0.0.0.0/0`.
- Keep real credentials in a local `secrets.env` file and Kubernetes Secret. Never commit it.
- The current application seeder inserts known demo accounts when the database has no organizations (see `OpsFlow.Infrastructure/Data/OpsFlowDbSeeder.cs`). Do not expose this API publicly until those credentials are changed/removed through an application/data change.
- The API Service is `ClusterIP` by default. Test it with `kubectl port-forward`. An optional TLS/NLB example is provided separately and is not included in the default Kustomize deployment.
- The sample stores PostgreSQL and RabbitMQ data on EBS, but does not configure backups, multi-AZ database failover, or managed services. Deleting the cluster can delete its storage depending on the EBS/PVC lifecycle.

## Prerequisites

Install and authenticate the AWS CLI, Terraform, kubectl, and Docker. The AWS identity needs permissions for VPC, EKS, ECR, IAM roles/policies, and related resources. Terraform module/provider downloads require network access.

## 1. Configure and create AWS infrastructure

Run these commands in PowerShell from the repository root:

```powershell
Copy-Item deploy/terraform/aws/terraform.tfvars.example deploy/terraform/aws/terraform.tfvars
```

Edit `deploy/terraform/aws/terraform.tfvars`: set `cluster_admin_cidrs` to your public IPv4 address followed by `/32`, for example `198.51.100.24/32`. The value in the example file is reserved documentation IP space and must be replaced.

```powershell
Set-Location deploy/terraform/aws
terraform init
terraform fmt -check
terraform validate
terraform plan -out opsflow.tfplan
terraform apply opsflow.tfplan
```

Review the plan before applying. The cluster, EKS Auto Mode compute, NAT Gateway, and EBS volumes can incur AWS charges. Save the Terraform state: it records which cloud resources Terraform created.

Configure kubectl for the newly-created cluster:

```powershell
$cluster = terraform output -raw cluster_name
$region = terraform output -raw aws_region
aws eks update-kubeconfig --name $cluster --region $region
kubectl get nodes
```

Return to the repository root before building Docker images and applying the manifests:

```powershell
Set-Location ..\..\..
```

## 2. Build and push the two application images

The image definitions are `OpsFlow.Api/Dockerfile` and `OpsFlow.Worker/Dockerfile`. Use the ECR URLs created by Terraform and an immutable source revision tag:

```powershell
$apiRepo = terraform -chdir=deploy/terraform/aws output -raw api_ecr_repository_url
$workerRepo = terraform -chdir=deploy/terraform/aws output -raw worker_ecr_repository_url
$accountId = (aws sts get-caller-identity --query Account --output text)
$region = terraform -chdir=deploy/terraform/aws output -raw aws_region
$tag = (git rev-parse --short HEAD)
aws ecr get-login-password --region $region | docker login --username AWS --password-stdin "$accountId.dkr.ecr.$region.amazonaws.com"
docker build -f OpsFlow.Api/Dockerfile -t "${apiRepo}:$tag" .
docker push "${apiRepo}:$tag"
docker build -f OpsFlow.Worker/Dockerfile -t "${workerRepo}:$tag" .
docker push "${workerRepo}:$tag"
```

Replace the placeholder `image:` values in `deploy/kubernetes/api.yaml` and `deploy/kubernetes/worker.yaml` with the respective ECR URL and the same `$tag`, such as `123456789012.dkr.ecr.us-east-1.amazonaws.com/opsflow-demo-api:abc1234`.

## 3. Create runtime secrets and deploy Kubernetes resources

Copy the template and replace every value with unique generated values. Use passwords consisting of letters/numbers because the app's Redis connection string is assembled from a password value.

```powershell
Copy-Item deploy/kubernetes/secrets.env.example deploy/kubernetes/secrets.env
notepad deploy/kubernetes/secrets.env
kubectl apply -f deploy/kubernetes/namespace.yaml
kubectl create secret generic opsflow-runtime-secrets `
  --namespace opsflow `
  --from-env-file deploy/kubernetes/secrets.env `
  --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -k deploy/kubernetes
```

`JWT_SECRET_KEY` must contain at least 32 random characters. The Postgres, Redis, and RabbitMQ credentials must match between the Secret and their workloads; the manifests wire these values into the containers. Keep `secrets.env` local.

The API calls EF Core migrations and the demo seeder at startup. It is intentionally configured with one replica to avoid multiple API pods trying to initialize/seed the same empty database at once. The current seeder includes demo user accounts; review `OpsFlow.Infrastructure/Data/OpsFlowDbSeeder.cs` before any real/public deployment.

Check Kubernetes rollout state:

```powershell
kubectl get pods,services,pvc -n opsflow
kubectl rollout status deployment/opsflow-api -n opsflow
kubectl rollout status deployment/opsflow-worker -n opsflow
```

The API Service is private to the cluster. To test from your machine without making it public:

```powershell
kubectl port-forward service/opsflow-api 8080:8080 -n opsflow
```

In a second terminal, call the health endpoint:

```powershell
Invoke-RestMethod http://localhost:8080/api/health
```

## Optional public HTTPS entry point

`deploy/kubernetes/api-public-service.example.yaml` is deliberately excluded from `kustomization.yaml`. It demonstrates an internet-facing AWS Network Load Balancer with TLS termination using an ACM certificate. Replace the certificate ARN with a certificate in the same AWS region, configure DNS, and apply only after the demo credentials have been rotated/removed and the application has been reviewed for public use:

```powershell
kubectl apply -f deploy/kubernetes/api-public-service.example.yaml
kubectl get service opsflow-api-public -n opsflow
```

EKS Auto Mode provisions a load balancer for a `LoadBalancer` Service. This example does not create an ACM certificate or DNS record.

## Cleanup

Delete application workloads first, then destroy AWS resources from the Terraform directory:

```powershell
kubectl delete -k deploy/kubernetes
Set-Location deploy/terraform/aws
terraform destroy
```

Review the destroy plan. Confirm that any data you need has been backed up before deleting persistent storage. Keep the Terraform state until the destroy finishes successfully; deleting state first would make resource cleanup harder. ECR images and AWS resources may have separate lifecycle/billing implications, so verify the resulting AWS account state after cleanup.

## File map

- `terraform/aws/*.tf`: AWS provider, network, EKS Auto Mode, ECR, outputs, and configurable inputs.
- `kubernetes/kustomization.yaml`: default application deployment bundle.
- `kubernetes/api.yaml`, `worker.yaml`: application Deployments and internal API Service.
- `kubernetes/postgres.yaml`, `rabbitmq.yaml`: single-replica StatefulSets and persistent claims.
- `kubernetes/redis.yaml`: single Redis cache Deployment.
- `kubernetes/configmap.yaml`, `secrets.env.example`: non-secret settings and local secret template.
- `kubernetes/storage.yaml`: encrypted `gp3` EBS StorageClass for EKS Auto Mode.
- `kubernetes/api-public-service.example.yaml`: optional public TLS entry point; not applied by default.
