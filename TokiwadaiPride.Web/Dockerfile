
FROM mcr.microsoft.com/dotnet/aspnet:8.0 as base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY ./ /src
WORKDIR /src
ENV BUILDING_DOCKER=1
RUN dotnet build -c Release TokiwadaiPride.Web/TokiwadaiPride.Web.csproj -o /app/build

FROM node:lts as build-node
WORKDIR /ClientApp
COPY ./TokiwadaiPride.Web/ClientApp/ .
RUN npm install
RUN npm run build

FROM build AS publish
RUN dotnet publish -c Release TokiwadaiPride.Web/TokiwadaiPride.Web.csproj -o /app/publish
COPY --from=build-node /ClientApp/build/ /app/publish/wwwroot

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5052
EXPOSE 5053
ENTRYPOINT [ "dotnet", "TokiwadaiPride.Web.dll" ]