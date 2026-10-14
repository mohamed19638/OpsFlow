variable "aws_region" {
  description = "AWS region in which the OpsFlow demo cluster and network are created."
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Short project prefix used in AWS resource names."
  type        = string
  default     = "opsflow"
}

variable "environment" {
  description = "Environment suffix used in AWS resource names and tags."
  type        = string
  default     = "demo"
}

variable "kubernetes_version" {
  description = "Amazon EKS Kubernetes minor version. Update when upgrading the cluster."
  type        = string
  default     = "1.35"
}

variable "vpc_cidr" {
  description = "CIDR range for the dedicated OpsFlow VPC."
  type        = string
  default     = "10.40.0.0/16"
}

variable "cluster_admin_cidrs" {
  description = "Public IPv4 CIDR ranges allowed to reach the EKS Kubernetes API endpoint. Set this to your current public IP as /32."
  type        = list(string)

  validation {
    condition     = length(var.cluster_admin_cidrs) > 0 && alltrue([for cidr in var.cluster_admin_cidrs : can(cidrhost(cidr, 0))])
    error_message = "Provide at least one valid CIDR range for EKS API access."
  }
}
