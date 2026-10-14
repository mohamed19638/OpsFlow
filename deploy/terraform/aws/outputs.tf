output "cluster_name" {
  description = "EKS cluster name for aws eks update-kubeconfig."
  value       = module.eks.cluster_name
}

output "aws_region" {
  description = "AWS region used by this deployment."
  value       = var.aws_region
}

output "api_ecr_repository_url" {
  description = "ECR repository URL for the OpsFlow API image."
  value       = aws_ecr_repository.api.repository_url
}

output "worker_ecr_repository_url" {
  description = "ECR repository URL for the OpsFlow Worker image."
  value       = aws_ecr_repository.worker.repository_url
}

output "vpc_id" {
  description = "VPC created for the EKS cluster."
  value       = module.vpc.vpc_id
}
