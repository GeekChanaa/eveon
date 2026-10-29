#!/usr/bin/env bash
# Deploy the API to the EC2 test host (eu-north-1, t3.micro, api-v2.eveon.ma) without an image registry:
# builds the image here, streams it over SSH, migrates, replaces the container (deploy.sh, TRANSFER=ssh).
#
#   cd VoltaXApi
#   ./deploy-ec2.sh                         deploy the current code and upload deploy/api.env to /etc/eveon/api.env
#   SKIP_ENV=1 ./deploy-ec2.sh              keep the settings file on the host as it is
#
# The settings live in deploy/api.env (copy deploy/api.env.example; *.env is gitignored, never commit it). It is
# the source of truth: each deploy replaces the host's file, after keeping a copy as /etc/eveon/api.env.bak-<date>.
#
# Optional
#   EC2_HOST      SSH target (default ubuntu@13.63.232.120, the host's Elastic IP)
#   SSH_KEY       private key (default ~/.ssh/eveon-ec2.pem)
#   ENV_SOURCE    local settings file to upload (default deploy/api.env)
#   SKIP_ENV=1    do not upload a settings file
#   SKIP_CADDY=1  do not install deploy/Caddyfile.ec2
# Any other deploy.sh option (TAG, SKIP_BUILD, SKIP_MIGRATE, ...) is passed through.
set -Eeuo pipefail
cd "$(dirname "$0")"

# Git Bash on Windows would turn /etc/... arguments into C:/Program Files/Git/etc/...
export MSYS_NO_PATHCONV=1

EC2_HOST="${EC2_HOST:-ubuntu@13.63.232.120}"
SSH_KEY="${SSH_KEY:-$HOME/.ssh/eveon-ec2.pem}"
ssh_args=(-i "$SSH_KEY" -o BatchMode=yes -o ConnectTimeout=15 -o StrictHostKeyChecking=accept-new)

ssh "${ssh_args[@]}" "$EC2_HOST" 'test -f /var/lib/eveon-bootstrap-done' \
    || { echo "ERROR: $EC2_HOST is unreachable or still installing Docker and Caddy (first boot)" >&2; exit 1; }

if [[ "${SKIP_ENV:-}" != 1 ]]; then
    ENV_SOURCE="${ENV_SOURCE:-deploy/api.env}"
    [[ -f "$ENV_SOURCE" ]] \
        || { echo "ERROR: $ENV_SOURCE is missing (copy deploy/api.env.example and fill it in, or SKIP_ENV=1)" >&2; exit 1; }
    if git ls-files --error-unmatch "$ENV_SOURCE" >/dev/null 2>&1; then
        echo "ERROR: $ENV_SOURCE is tracked by git; it holds secrets: git rm --cached it first" >&2; exit 1
    fi
    echo "==> Uploading $ENV_SOURCE to /etc/eveon/api.env"
    # shellcheck disable=SC2016 # the remote shell expands $f and $(date), not this one
    ssh "${ssh_args[@]}" "$EC2_HOST" \
        'f=/etc/eveon/api.env; if [ -f "$f" ]; then cp -p "$f" "$f.bak-$(date +%Y%m%d-%H%M%S)"; fi; umask 077 && cat > "$f"' \
        < "$ENV_SOURCE"
fi

if [[ "${SKIP_CADDY:-}" != 1 ]]; then
    echo "==> Installing deploy/Caddyfile.ec2"
    ssh "${ssh_args[@]}" "$EC2_HOST" \
        'sudo tee /etc/caddy/Caddyfile >/dev/null && sudo caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile && sudo systemctl reload caddy' \
        < deploy/Caddyfile.ec2
fi

TRANSFER=ssh DEPLOY_HOST="$EC2_HOST" SSH_KEY="$SSH_KEY" PLATFORM=linux/amd64 PUSH_LATEST=false ./deploy.sh
