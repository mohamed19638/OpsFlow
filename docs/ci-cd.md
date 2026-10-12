# CI/CD pipeline

The GitHub Actions workflow at `.github/workflows/ci-cd.yml` runs on pushes to `main` and `master`, pull requests, and manual dispatches.

## What it does

1. Restores the solution, then builds and runs its tests on .NET 8.
2. Scans the repository's dependencies and configuration with Trivy. Unfixed Critical findings are ignored; other Critical findings fail the workflow.
3. Builds the API and Worker Docker images and scans both images for Critical vulnerabilities.
4. On a push to `main` only, authenticates to AWS through GitHub OIDC and publishes both images to ECR. Images receive the commit SHA tag and the `latest` tag.

The ECR publish job intentionally does not run on pull requests, manual dispatch, or `master` pushes. The repository currently uses `master`, so the first three jobs can run there; ECR publishing starts after the deployment branch is `main` and AWS is configured.

## Configure AWS ECR publishing

Create two ECR repositories, one for the API and one for the Worker. Configure GitHub repository **Actions variables** (Settings → Secrets and variables → Actions → Variables):

| Variable | Value |
| --- | --- |
| `AWS_REGION` | AWS region containing the ECR repositories, such as `eu-west-1` |
| `AWS_ROLE_TO_ASSUME` | ARN of an IAM role configured for GitHub OIDC |
| `ECR_REPOSITORY_API` | ECR repository name for the API image |
| `ECR_REPOSITORY_WORKER` | ECR repository name for the Worker image |

The IAM role trust policy should use the GitHub OIDC provider, audience `sts.amazonaws.com`, and restrict the subject to this repository's `main` branch (`repo:mohamed19638/OpsFlow:ref:refs/heads/main`). Give the role only the ECR permissions required to obtain an authorization token and push image layers/manifests to the two repositories. Do not add AWS access keys to GitHub; OIDC issues short-lived credentials for the job. See [GitHub's AWS OIDC guide](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-aws).

The workflow reports a clear error if an ECR variable is missing when the `main` publish job runs. AWS role trust, IAM permissions, and repository names still need to be configured in AWS/GitHub before the first image can publish.
