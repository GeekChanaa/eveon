#!/usr/bin/env bash
# Build the dashboard and upload it to a static host over SSH (Hostinger). The Vercel deployment does not
# use this script: Vercel builds from vercel.json.
#
#   cd VoltaXDashboardSpa
#   DEPLOY_HOST=<user>@<host> DEPLOY_DIR=/home/<user>/domains/<domain>/public_html/dashboard SSH_PORT=65002 ./deploy.sh
#
# Required
#   DEPLOY_HOST   SSH target, user@host
#   DEPLOY_DIR    absolute directory the dashboard is served from; only this directory is written to
# Optional
#   SSH_PORT      default 22 (Hostinger uses 65002)
#   SSH_KEY       private key file for ssh (default: ssh config / agent)
#   BUILD_CONFIGURATION  Angular configuration (default production)
#   SKIP_INSTALL=1  reuse node_modules instead of npm ci
#   SKIP_BUILD=1    upload the existing dist/ as is
#   DELETE_STALE=1  also delete files in DEPLOY_DIR that the new build no longer has (old hashed bundles)
#   DRY_RUN=1       show what rsync would change, change nothing
set -Eeuo pipefail

fail() { echo "ERROR: $*" >&2; exit 1; }
trap 'fail "deploy failed at line $LINENO"' ERR

cd "$(dirname "$0")"

: "${DEPLOY_HOST:?set DEPLOY_HOST, e.g. DEPLOY_HOST=<user>@<host>}"
: "${DEPLOY_DIR:?set DEPLOY_DIR, the absolute directory the dashboard is served from}"
SSH_PORT="${SSH_PORT:-22}"
BUILD_CONFIGURATION="${BUILD_CONFIGURATION:-production}"
BUILD_DIR="dist/volta-xdashboard-spa"
EXTRA_DIR="deployment-files"   # .htaccess: HTTPS, SPA fallback, security headers

# A wrong DEPLOY_DIR with DELETE_STALE=1 would wipe someone else's files: insist on a specific path.
case "$DEPLOY_DIR" in
    /*/*) ;;
    *) fail "DEPLOY_DIR must be an absolute path at least two levels deep, got '$DEPLOY_DIR'" ;;
esac
if [[ ! "$DEPLOY_DIR" =~ ^[A-Za-z0-9._/-]+$ || "$DEPLOY_DIR" == *..* ]]; then
    fail "DEPLOY_DIR may only contain letters, digits, '.', '_', '-' and '/', and no '..'"
fi

ssh_args=(ssh -p "$SSH_PORT" -o BatchMode=yes -o ConnectTimeout=15)
if [[ -n "${SSH_KEY:-}" ]]; then
    ssh_args+=(-i "$SSH_KEY")
fi

if [[ "${SKIP_BUILD:-}" != 1 ]]; then
    if [[ "${SKIP_INSTALL:-}" != 1 ]]; then
        echo "==> Installing dependencies (npm ci)"
        npm ci
    fi
    echo "==> Building ($BUILD_CONFIGURATION)"
    npm run build -- --configuration "$BUILD_CONFIGURATION"
fi
[[ -f "$BUILD_DIR/index.html" ]] || fail "$BUILD_DIR/index.html not found: the build did not run or failed"

rsync_args=(--archive --compress --human-readable --itemize-changes -e "$(printf '%q ' "${ssh_args[@]}")")
if [[ "${DELETE_STALE:-}" == 1 ]]; then
    rsync_args+=(--delete-after)
fi
if [[ "${DRY_RUN:-}" == 1 ]]; then
    rsync_args+=(--dry-run)
fi

echo "==> Uploading to $DEPLOY_HOST:$DEPLOY_DIR"
"${ssh_args[@]}" "$DEPLOY_HOST" "mkdir -p '$DEPLOY_DIR'"
# One rsync for both sources, so DELETE_STALE never removes the .htaccess.
rsync "${rsync_args[@]}" "$BUILD_DIR/" "$EXTRA_DIR/" "$DEPLOY_HOST:$DEPLOY_DIR/"

trap - ERR
echo "==> Deployed the dashboard to $DEPLOY_HOST:$DEPLOY_DIR"
