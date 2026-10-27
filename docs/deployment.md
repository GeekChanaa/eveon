# Deployment

| Piece | Where | Driven by |
| --- | --- | --- |
| API | EC2 test host (`eu-north-1`, t3.micro, `api-v2.eveon.ma`) behind Caddy | `VoltaXApi/deploy-ec2.sh`; settings in `/etc/eveon/api.env` from `VoltaXApi/deploy/api.env.example`; TLS and plain OCPP `ws` (port 8081) by `VoltaXApi/deploy/Caddyfile.ec2` |
| API, any Docker host | A Docker host behind Caddy | `VoltaXApi/deploy.sh`; settings in `/etc/eveon/api.env`; TLS by `VoltaXApi/deploy/Caddyfile` |
| Several API instances | Docker host | `VoltaXApi/deploy/docker-compose.scaleout.yml`, see [scale-out.md](scale-out.md) |
| Dashboard | Hostinger static hosting (`dashboard-v2.eveon.ma`) | `.github/workflows/deploy-dashboard.yml` running `VoltaXDashboardSpa/deploy.sh` |
| Database | Hostinger MariaDB | migrations, see [database.md](database.md) |

## The API image

`VoltaXApi/Dockerfile` builds one image for every host:

- **No configuration baked in** besides `appsettings.json` (development defaults, no secrets). Every
  production value is an environment variable ([configuration.md](configuration.md)).
- **Port:** `PORT` (default 8080) becomes `ASPNETCORE_HTTP_PORTS`. Set `ASPNETCORE_URLS`
  to listen differently (e.g. Kestrel TLS for OCPP security profile 3 without a proxy).
- **User:** uid 1654 (`app`), never root. The application files are read only.
- **Health:** a Docker `HEALTHCHECK` calls `/health/live` every 30 s. Host filtering applies, so it sends the
  first `AllowedHosts` entry as Host header; set `HEALTHCHECK_HOST` if that entry is a wildcard.
- **Logs:** stdout (Serilog console sink). The file sinks write to `/app/logs`, inside the container only.
- **Arguments** after the image name reach the API: `--migrate` (apply the migrations and exit),
  `--create-admin` (first administrator, see [configuration.md](configuration.md)).

```bash
docker build -t eveon-api VoltaXApi
docker run --rm --env-file api.env eveon-api --migrate
docker run -d --name voltax-api --env-file api.env -p 127.0.0.1:8080:8080 eveon-api
```

## Migrations at deploy time

The API never changes the schema on its own unless told to, and refuses to start in Production while a
migration is pending ([database.md](database.md)).

- **Docker host:** `deploy.sh` runs `--migrate` in a one-off container from the new image before it
  replaces the running one; a failed migration stops the deploy and the old container keeps running.
- **Hosts without a pre-deploy step** (single instance): set `Database__MigrateOnStartup=true`. The API
  migrates, then starts.
- Take a database backup before deploying a release that contains migrations.

## EC2 test host with deploy-ec2.sh

The API runs on one t3.micro (AWS free plan, `eu-north-1`, Elastic IP `13.63.232.120`, 1 GB RAM + 2 GB swap,
Docker and Caddy installed at first boot). `api-v2.eveon.ma` is an A record to that IP in Hostinger's DNS.
The security group allows SSH from the administrator's address only, 80 and 443, and 8081 for chargers on
OCPP security profile 1 (`ws://api-v2.eveon.ma:8081/OCPP/<identity>`, needs `Ocpp__AllowInsecureProfile1=true`);
chargers with TLS use `wss://api-v2.eveon.ma/OCPP/<identity>`.

```bash
cd VoltaXApi
./deploy-ec2.sh                                  # build here, send over SSH, migrate, replace, health check
ENV_SOURCE=<local api.env> ./deploy-ec2.sh       # also replace /etc/eveon/api.env first
```

No registry: the image is streamed with `docker save | docker load` (`deploy.sh` with `TRANSFER=ssh`). The
script builds the working tree as it is, so deploy from a clean checkout of the commit to ship. It needs the
key `~/.ssh/eveon-ec2.pem`; other options are at the top of the script. A new administrator address must be
added to the security group's SSH rule.

## Docker host with deploy.sh

Prerequisites on the host: Docker, `/etc/eveon/api.env` (from `deploy/api.env.example`, readable by the
SSH user, never committed), Caddy with `deploy/Caddyfile` in front of `127.0.0.1:8080`.

```bash
cd VoltaXApi
IMAGE=docker.io/<account>/voltax-api DEPLOY_HOST=ec2-user@<host> SSH_KEY=~/.ssh/<key>.pem ./deploy.sh
```

It builds and pushes `IMAGE:<git commit>` (and `:latest`), pulls it on the host, migrates, replaces the
container with the upload folders on named volumes, and waits for the health check. Other options
(`TAG`, `SKIP_BUILD`, `SKIP_MIGRATE`, `PLATFORM`, `ENV_FILE`, `PUBLISH`, `VOLUME_PREFIX`) are described at the
top of the script. To roll back, deploy the previous tag with `SKIP_BUILD=1 SKIP_MIGRATE=1 TAG=<previous>`
(migrations only go forward: check that the previous version still runs on the newer schema).

Release images can also come from GitHub: pushing a `v1.2.3` tag runs `.github/workflows/release.yml`,
which publishes `ghcr.io/<owner>/eveon-api:1.2.3`; deploy it with
`IMAGE=ghcr.io/<owner>/eveon-api TAG=1.2.3 SKIP_BUILD=1 ./deploy.sh`.

## Versions and changelog

The API version is `<Version>` in `VoltaXApi/VoltaXApi.csproj`, the dashboard version is `version` in
`VoltaXDashboardSpa/package.json`. Raise one with Actions > **Bump version** > Run workflow
(`.github/workflows/bump-version.yml`): pick the component and patch, minor or major, and optionally the
changes separated by `;` (by default, the subjects of the commits that touched the component since its last
version). It runs `tools/release/bump-version.mjs`, which updates the version and adds the release to
`VoltaXDashboardSpa/src/assets/data/changelog.json`, then commits `Release <component> <version>` to the
branch and tags it `api-v<version>` or `dashboard-v<version>`. The changelog is public: anyone can read it at
`/changelog` on the dashboard, so write the changes for customers. The release commit is pushed with
`GITHUB_TOKEN`, so it starts neither CI, `release.yml` nor `deploy-dashboard.yml`: after a dashboard release,
run Actions > **Deploy dashboard** by hand so the public changelog shows it.

## Storage

The API writes files in four places. Inside a container they disappear when the container is replaced,
unless they are volumes:

| Path in the container | Content |
| --- | --- |
| `/app/wwwroot/ProfilePictures` | user profile pictures |
| `/app/wwwroot/PartnersLogos` | partner logos |
| `/app/wwwroot/images/charging-stations` | station photos |
| `/app/App_Data` | charger log uploads (`charger-logs`), GDPR exports, private files |

- **Docker host:** `deploy.sh` mounts the named volumes `<prefix>-profile-pictures`, `-partner-logos`,
  `-station-images` and `-app-data` (prefix `eveon`). A new named volume starts with the files the image
  carries for that path. Uploads made before the first `deploy.sh` run live in the old container: copy
  them out (`docker cp`) before it is replaced.
- The repository still contains some uploaded files under `VoltaXApi/wwwroot` (profile pictures, station
  photos, logos) that the live data may reference; move them to the storage above and remove them from git.

## Dashboard

The dashboard is static files on Hostinger. `.github/workflows/deploy-dashboard.yml` builds and uploads it
on every push to `master` that touches `VoltaXDashboardSpa/` (or by hand). `deployment-files/.htaccess`
forces HTTPS and sets the security headers and CSP. The API address is in
`src/environments/environment.prod.ts`. By hand:

```bash
cd VoltaXDashboardSpa
DEPLOY_HOST=<user>@<host> SSH_PORT=65002 DEPLOY_DIR=/home/<user>/domains/<domain>/public_html/dashboard ./deploy.sh
```

It runs `npm ci` and the production build, then rsyncs the build and `deployment-files/.htaccess` to
`DEPLOY_DIR` only. `DRY_RUN=1` shows the changes; `DELETE_STALE=1` also removes files the new build no
longer has.

## Continuous integration

`.github/workflows/ci.yml` builds and tests the API, applies every migration to empty MariaDB and SQL
Server databases, builds and tests the dashboard, analyzes and tests the mobile app (report only for now),
runs shellcheck, scans new commits for secrets (gitleaks, `.gitleaks.toml`) and audits dependencies. Once
the repository variable `EVEON_SIM_REPOSITORY` is set it also runs the simulator end-to-end matrix
([testing.md](testing.md)).
`.github/dependabot.yml` opens weekly update pull requests.
