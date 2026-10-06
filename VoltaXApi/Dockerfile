# Use the official Microsoft .NET SDK 9 image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy the CSPROJ file and restore any dependencies (via NUGET)
COPY *.csproj ./
RUN dotnet restore

# Copy the project files and build the application
COPY . ./

# Production defaults (no secrets: those come from environment variables at run time,
# e.g. -e AppSettings__Token=... -e ConnectionStrings__DefaultConnection=...).
COPY appsettings.production.json ./appsettings.json

RUN dotnet publish -c Release -o out

# Generate the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build-env /app/out .

# Run as the image's unprivileged user instead of root.
USER $APP_UID

# Railway injects PORT; fall back to 8080 elsewhere
ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT} exec dotnet VoltaXApi.dll"]
