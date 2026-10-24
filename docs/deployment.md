# Deployment

| Piece | Where | Driven by |
| --- | --- | --- |
| API | Railway (root directory `VoltaXApi`) | `VoltaXApi/railway.json` + `VoltaXApi/Dockerfile`; variables from `VoltaXApi/deploy/railway.env.example` |
| API, alternative | A Docker host (EC2) behind Caddy | `VoltaXApi/deploy.sh`; settings in `/etc/eveon/api.env` from `VoltaXApi/deploy/api.env.example`; TLS by `VoltaXApi/deploy/Caddyfile` |
| Several API instances | Docker host | `VoltaXApi/deploy/docker-compose.scaleout.yml`, see [scale-out.md](scale-out.md) |
| Dashboard | Vercel (root directory `VoltaXDashboardSpa`) | `VoltaXDashboardSpa/vercel.json` |
| Dashboard, alternative | Hostinger static hosting | `VoltaXDashboardSpa/deploy.sh` |
| Database | Hostinger MariaDB | migrations, see [database.md](database.md) |

## The API image

`VoltaXApi/Dockerfile` builds one image for every host:

- **No configuration baked in** besides `appsettings.json` (development defaults, no secrets). Every
  production value is an environment variable ([configuration.md](configuration.md)).
- **Port:** `PORT` (Railway sets it, default 8080) becomes `ASPNETCORE_HTTP_PORTS`. Set `ASPNETCORE_URLS`
  to listen differently (e.g. Kestrel TLS for OCPP security profile 3 without a proxy).
- **User:** uid 1654 (`app`), never root. The application files are read only.
- **Health:** a Docker `HEALTHCHECK` calls `/health/live` every 30 s. Host filtering applies, so it sends the
  first `AllowedHosts` entry as Host header; set `HEALTHCHECK_HOST` if that entry is a wildcard.
  Railway uses its own probe (`healthcheckPath` in `railway.json`).
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
- **Railway** (one replica, no pre-deploy step configured): set `Database__MigrateOnStartup=true`. The new
  deployment migrates, then starts; Railway only switches traffic once `/health/live` answers, so a failed
  migration leaves the previous deployment serving.
- Take a database backup before deploying a release that contains migrations.

## Docker host (EC2) with deploy.sh

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
- **Railway:** a service has a single volume, so these four paths cannot all persist; uploads made on
  Railway are lost at each deploy. Mount the volume on the folder that matters most, or give the API a
  single configurable storage root or object storage (code change, not done yet).
- The repository still contains some uploaded files under `VoltaXApi/wwwroot` (profile pictures, station
  photos, logos) that the live data may reference; move them to the storage above and remove them from git.

## Dashboard

- **Vercel** builds on every push to the configured branch with `vercel.json` (`npm ci`, production build,
  security headers and CSP). The API address is in `src/environments/environment.prod.ts`.
- **Hostinger:**

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
