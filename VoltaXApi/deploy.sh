#!/usr/bin/env bash
# Build and push the API image, apply the database migrations, then replace the container on a Docker
# host reached over SSH (the EC2 host). Railway does not use this script: it builds the Dockerfile itself.
#
#   cd VoltaXApi
#   IMAGE=docker.io/<account>/voltax-api DEPLOY_HOST=ec2-user@<host> SSH_KEY=~/.ssh/eveon.pem ./deploy.sh
#
# Required
#   IMAGE         image repository to push and pull, e.g. docker.io/<account>/voltax-api or ghcr.io/<owner>/eveon-api
#   DEPLOY_HOST   SSH target, user@host
# Optional
#   TAG           image tag (default: short git commit, "-dirty" when the tree has uncommitted changes)
#   PUSH_LATEST   also push and tag :latest (default true)
#   PLATFORM      docker build platform (default linux/amd64)
#   SSH_KEY       private key file for ssh (default: ssh config / agent)
#   SSH_PORT      default 22
#   CONTAINER     container name on the host (default voltax-api)
#   ENV_FILE      settings file on the host, see deploy/api.env.example (default /etc/eveon/api.env)
#   PUBLISH       docker -p value (default 127.0.0.1:8080:8080: Caddy, deploy/Caddyfile, terminates TLS)
#   VOLUME_PREFIX prefix of the named volumes that keep uploads across deploys (default eveon)
#   SKIP_BUILD=1  deploy an already pushed TAG without building
#   SKIP_MIGRATE=1  do not run the migrations first (only if they were applied another way)
#
# The database is never dropped or reset here. Migrations go forward only, through the image's --migrate
# (docs/database.md); take a backup first when a release contains migrations.
set -Eeuo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; BLUE='\033[0;34m'; YELLOW='\033[1;33m'; NC='\033[0m'
step() { echo -e "${BLUE}==> $*${NC}"; }
warn() { echo -e "${YELLOW}$*${NC}" >&2; }
fail() { echo -e "${RED}ERROR: $*${NC}" >&2; exit 1; }
trap 'fail "deploy failed at line $LINENO"' ERR

cd "$(dirname "$0")"

: "${IMAGE:?set IMAGE, e.g. IMAGE=docker.io/<account>/voltax-api}"
: "${DEPLOY_HOST:?set DEPLOY_HOST, e.g. DEPLOY_HOST=ec2-user@<host>}"
PUSH_LATEST="${PUSH_LATEST:-true}"
PLATFORM="${PLATFORM:-linux/amd64}"
SSH_PORT="${SSH_PORT:-22}"
CONTAINER="${CONTAINER:-voltax-api}"
ENV_FILE="${ENV_FILE:-/etc/eveon/api.env}"
PUBLISH="${PUBLISH:-127.0.0.1:8080:8080}"
VOLUME_PREFIX="${VOLUME_PREFIX:-eveon}"

if [[ -z "${TAG:-}" ]]; then
    TAG="$(git rev-parse --short HEAD)"
    if [[ -n "$(git status --porcelain -- .)" ]]; then
        TAG="$TAG-dirty"
        warn "Uncommitted changes in VoltaXApi: the image is tagged $TAG"
    fi
fi
REF="$IMAGE:$TAG"

command -v docker >/dev/null || fail "docker is not installed"
command -v ssh >/dev/null || fail "ssh is not installed"
ssh_args=(-p "$SSH_PORT" -o BatchMode=yes -o ConnectTimeout=15)
if [[ -n "${SSH_KEY:-}" ]]; then
    ssh_args+=(-i "$SSH_KEY")
fi

step "Checking SSH access to $DEPLOY_HOST"
ssh "${ssh_args[@]}" "$DEPLOY_HOST" true || fail "cannot reach $DEPLOY_HOST over SSH"

if [[ "${SKIP_BUILD:-}" != 1 ]]; then
    step "Building $REF ($PLATFORM)"
    docker build --platform "$PLATFORM" --pull --tag "$REF" .
    step "Pushing $REF"
    docker push "$REF"
    if [[ "$PUSH_LATEST" == true ]]; then
        docker tag "$REF" "$IMAGE:latest"
        docker push "$IMAGE:latest"
    fi
fi

step "Deploying $REF to $DEPLOY_HOST"
# The remote script is sent as is (quoted heredoc); its inputs are passed as quoted positional arguments.
remote_args=$(printf '%q ' "$REF" "$CONTAINER" "$ENV_FILE" "$PUBLISH" "$VOLUME_PREFIX" "${SKIP_MIGRATE:-0}")
ssh "${ssh_args[@]}" "$DEPLOY_HOST" "bash -s -- $remote_args" <<'REMOTE'
set -Eeuo pipefail
ref=$1 container=$2 env_file=$3 publish=$4 prefix=$5 skip_migrate=$6

[[ -f "$env_file" ]] || { echo "ERROR: $env_file is missing on $(hostname) (template: deploy/api.env.example)" >&2; exit 1; }

docker pull "$ref"

if [[ "$skip_migrate" != 1 ]]; then
    echo "==> Applying database migrations"
    # Same image and settings as the API; exits non-zero (and stops the deploy) if a migration fails.
    docker run --rm --name "$container-migrate" --env-file "$env_file" "$ref" --migrate
fi

previous=$(docker inspect --format '{{.Config.Image}}' "$container" 2>/dev/null || true)
echo "==> Replacing container $container (previous image: ${previous:-none})"
docker stop --time 30 "$container" >/dev/null 2>&1 || true
docker rm "$container" >/dev/null 2>&1 || true
docker run --detach --name "$container" --restart unless-stopped \
    --env-file "$env_file" \
    --publish "$publish" \
    --volume "$prefix-profile-pictures:/app/wwwroot/ProfilePictures" \
    --volume "$prefix-partner-logos:/app/wwwroot/PartnersLogos" \
    --volume "$prefix-station-images:/app/wwwroot/images/charging-stations" \
    --volume "$prefix-app-data:/app/App_Data" \
    "$ref" >/dev/null

echo "==> Waiting for the container health check"
for _ in $(seq 1 40); do
    status=$(docker inspect --format '{{.State.Health.Status}}' "$container" 2>/dev/null || echo missing)
    case "$status" in
        healthy) echo "==> $container is healthy"; docker image prune --force >/dev/null; exit 0 ;;
        unhealthy|missing) break ;;
    esac
    sleep 5
done
echo "ERROR: $container did not become healthy (status: ${status:-unknown}). Last log lines:" >&2
docker logs --tail 50 "$container" >&2 || true
if [[ -n "$previous" ]]; then
    echo "To roll back, run deploy.sh again with SKIP_BUILD=1 SKIP_MIGRATE=1 and the previous TAG ($previous)" >&2
fi
exit 1
REMOTE

trap - ERR
echo -e "${GREEN}Deployed $REF to $DEPLOY_HOST${NC}"
