# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first so the dependency layer is cached between builds.
COPY global.json ./
COPY src/PhotoGallery.Web/PhotoGallery.Web.csproj src/PhotoGallery.Web/
RUN dotnet restore src/PhotoGallery.Web/PhotoGallery.Web.csproj

COPY src/ src/
RUN dotnet publish src/PhotoGallery.Web/PhotoGallery.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# The database, uploaded photos and data protection keys live in App_Data.
# Mount a volume there to keep data between restarts.
RUN mkdir -p /app/App_Data && chown -R $APP_UID /app/App_Data
VOLUME /app/App_Data

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PhotoGallery.Web.dll"]
