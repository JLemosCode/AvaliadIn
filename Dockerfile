FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AvaliadIN.sln .
COPY src/AvaliadIN.Core/AvaliadIN.Core.csproj src/AvaliadIN.Core/
COPY src/AvaliadIN.Api/AvaliadIN.Api.csproj src/AvaliadIN.Api/
RUN dotnet restore src/AvaliadIN.Api/AvaliadIN.Api.csproj

COPY src/ src/
RUN dotnet publish src/AvaliadIN.Api/AvaliadIN.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/playwright/dotnet:v1.50.0-noble AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AvaliadIN.Api.dll"]
