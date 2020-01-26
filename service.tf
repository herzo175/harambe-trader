// Variables
variable "do_token" {
  type = string
}

variable "version_tag" {
  type    = string
  default = "latest"
}

variable "broker_api_key_id" {
  type = string
}

variable "broker_api_key_secret" {
  type = string
}

variable "datasource_token" {
  type = string
}

variable "database_connection_string" {
  type = string
}

variable "predictor_endpoint" {
  type    = string
  default = "harambe-6.default.svc.cluster.local:80"
}

terraform {
  backend "s3" {
    bucket = "harambe-assets"
    key    = "terraform/harambe-trader.tfstate"
    region = "us-east-1"
    endpoint = "https://nyc3.digitaloceanspaces.com"
    skip_credentials_validation = true
    skip_get_ec2_platforms = true
    skip_requesting_account_id = true
    skip_metadata_api_check = true
  }
}

// Providers
provider "digitalocean" {
  token = var.do_token
}

// TODO: get from remote backend (host, token, cluster ca certificate)
data "digitalocean_kubernetes_cluster" "harambe" {
  name = "harambe-dev-1"
}

provider "kubernetes" {
  host  = data.digitalocean_kubernetes_cluster.harambe.endpoint
  token = data.digitalocean_kubernetes_cluster.harambe.kube_config[0].token
  cluster_ca_certificate = base64decode(
    data.digitalocean_kubernetes_cluster.harambe.kube_config[0].cluster_ca_certificate
  )
}

// Deploy service
resource "kubernetes_cron_job" "harambe_trader" {
  metadata {
    name = "harambe-trader"
  }
  spec {
    concurrency_policy            = "Forbid"
    failed_jobs_history_limit     = 5
    schedule                      = "0 17 * * 1-5"
    starting_deadline_seconds     = 10
    suspend                       = true
    job_template {
      metadata {}  // NOTE: required (can be empty)
      spec {
        template {
          metadata {}
          spec {
            container {
              name  = "harambe-trader"
              image = "jeremyaherzog/harambe-trader:${var.version_tag}"

              env {
                name  = "BROKER_API_KEY_ID"
                value = var.broker_api_key_id
              }

              env {
                name  = "BROKER_API_KEY_SECRET"
                value = var.broker_api_key_secret
              }

              env {
                name  = "DATASOURCE_TOKEN"
                value = var.broker_api_key_secret
              }

              env {
                name  = "DATABASE_CONNECTION_STRING"
                value = var.database_connection_string
              }

              env {
                name  = "PREDICTOR_ENDPOINT"
                value = var.predictor_endpoint
              }
            }
            restart_policy = "OnFailure"
          }
        }
      }
    }
  }
}
