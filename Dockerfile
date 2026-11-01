
# Compilar ScoutVision con .NET 8
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ScoutVision.Web.csproj ./

RUN dotnet restore ScoutVision.Web.csproj

COPY . .

RUN dotnet publish ScoutVision.Web.csproj -c Release -o /app/publish --no-restore

# Ejecutar ScoutVision
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "ScoutVision.Web.dll"]
