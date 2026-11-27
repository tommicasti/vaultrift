FROM node:22-alpine AS web
WORKDIR /src/web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY src/ src/
RUN dotnet publish src/Vaultrift.Api/Vaultrift.Api.csproj --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app ./
COPY --from=web /src/web/dist ./wwwroot
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vaultrift.Api.dll"]
