# EVEON API image. Build context: this folder (Railway root directory VoltaXApi, or `docker build VoltaXApi`).
#
# Nothing environment specific is baked in: the image carries appsettings.json (defaults, no secrets) and
# every production value comes from environment variables at run time (deploy/railway.env.example,
# deploy/api.env.example). .dockerignore keeps appsettings.production.json, env files and local data out.
#
#   docker run --rm --env-file api.env <image> --migrate     apply the database migrations, then exit
#   docker run -d --env-file api.env -p 8080:8080 <image>    serve
#
# Storage: uploads are written inside the container and are lost when it is replaced, unless these paths
# are volumes (EC2: deploy.sh mounts them; Railway: see docs/deployment.md, "Storage"):
#   /app/wwwroot/ProfilePictures, /app/wwwroot/PartnersLogos, /app/wwwroot/images/charging-stations
#   /app/App_Data (charger log uploads, GDPR exports, private files)
# Logs go to stdout (Serilog console sink); the file sinks write to /app/logs, which is container scratch.

# Literal tags, so Dependabot can follow them (.github/dependabot.yml); 9.0 gets the .NET 9 patches.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Restore first, in its own layer, so code changes do not download the packages again.
COPY VoltaXApi.csproj ./
RUN dotnet restore VoltaXApi.csproj
COPY . ./
RUN dotnet publish VoltaXApi.csproj --configuration Release --no-restore --output /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
# curl only serves the HEALTHCHECK below.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
# The application files stay owned by root (read only for the app); only the data paths are writable.
COPY --from=build /app/publish ./
RUN mkdir -p logs App_Data wwwroot/ProfilePictures wwwroot/PartnersLogos wwwroot/images/charging-stations \
    && chown -R "$APP_UID" logs App_Data wwwroot/ProfilePictures wwwroot/PartnersLogos wwwroot/images/charging-stations

# The image's unprivileged user (uid 1654), never root.
USER $APP_UID

# Railway injects PORT; 8080 everywhere else. Set ASPNETCORE_URLS instead to listen elsewhere (e.g. https://+:8443).
ENV ASPNETCORE_ENVIRONMENT=Production \
    PORT=8080
EXPOSE 8080

# /health/live answers 200 once the API serves requests (no dependency checks; /health/ready checks the
# database). Host filtering applies, so the probe sends the first AllowedHosts entry (or HEALTHCHECK_HOST).
HEALTHCHECK --interval=30s --timeout=5s --start-period=90s --retries=3 \
    CMD host="${HEALTHCHECK_HOST:-${AllowedHosts%%;*}}"; case "$host" in ""|"*"|"*"*) host=localhost ;; esac; \
        curl --fail --silent --show-error --output /dev/null --header "Host: $host" "http://127.0.0.1:${PORT}/health/live" || exit 1

# Arguments after the image name reach the API (e.g. --migrate, --create-admin).
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=\"${PORT}\" exec dotnet VoltaXApi.dll \"$@\"", "--"]
