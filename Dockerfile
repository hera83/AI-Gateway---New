# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, in its own layer, so dependency restore is cached across builds
# that only change application code.
COPY AiGateway.csproj .
RUN dotnet restore AiGateway.csproj

COPY . .
RUN dotnet publish AiGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The .NET 8+ ASP.NET Core base images listen on 8080 by default; set explicitly for clarity.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# App_dbs/ and App_files/ are created automatically at startup (see Program.cs) and are
# expected to be mounted as volumes — see docker-compose.yml — so they aren't created here.
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AiGateway.dll"]
