# CivicConnect web application (ADR-007: Render runs this image; the database is on Neon).
# Build context is the repository root:  docker build -t civicconnect .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from the project files only, so this layer is cached until a package changes.
COPY app/src/CivicConnect.Domain/CivicConnect.Domain.csproj app/src/CivicConnect.Domain/
COPY app/src/CivicConnect.Application/CivicConnect.Application.csproj app/src/CivicConnect.Application/
COPY app/src/CivicConnect.Infrastructure/CivicConnect.Infrastructure.csproj app/src/CivicConnect.Infrastructure/
COPY app/src/CivicConnect.Web/CivicConnect.Web.csproj app/src/CivicConnect.Web/
RUN dotnet restore app/src/CivicConnect.Web/CivicConnect.Web.csproj

COPY app/src app/src
RUN dotnet publish app/src/CivicConnect.Web/CivicConnect.Web.csproj \
    --configuration Release --no-restore --output /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /out .

# Render sends traffic to the port in its PORT variable, which defaults to 10000.
# Render ends TLS in front of the container, so the app must trust the forwarded headers.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=10000 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 10000

# The base image provides a non-root user; never run the app as root.
USER $APP_UID
ENTRYPOINT ["dotnet", "CivicConnect.Web.dll"]
