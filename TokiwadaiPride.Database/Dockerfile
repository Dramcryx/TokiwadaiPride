
FROM mcr.microsoft.com/dotnet/aspnet:8.0 as base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY ./ /src
WORKDIR /src
RUN dotnet build -c Release TokiwadaiPride.Database/TokiwadaiPride.Database.csproj -o /app/build

FROM build AS publish
RUN dotnet publish -c Release TokiwadaiPride.Database/TokiwadaiPride.Database.csproj -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [ "dotnet", "TokiwadaiPride.Database.dll" ]
