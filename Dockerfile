# The hotel server (Habbo.Host: the Orleans silo, the game gateway and the web API in one process), with the seed
# content it imports on start. The asset mirror, photos and session keys live on volumes (/data); the database
# connection, the RSA key and the first administrator come from the deployment's secret.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# the project files alone first: the restore is a layer of its own, cached until a package changes, rather than
# downloaded again for every change to the code
COPY Directory.Build.props Habbo.slnx ./
COPY src/Habbo.Camera/Habbo.Camera.csproj src/Habbo.Camera/
COPY src/Habbo.Domain/Habbo.Domain.csproj src/Habbo.Domain/
COPY src/Habbo.Gateway/Habbo.Gateway.csproj src/Habbo.Gateway/
COPY src/Habbo.Host/Habbo.Host.csproj src/Habbo.Host/
COPY src/Habbo.Infrastructure/Habbo.Infrastructure.csproj src/Habbo.Infrastructure/
COPY src/Habbo.Protocol/Habbo.Protocol.csproj src/Habbo.Protocol/
COPY src/Habbo.Protocol.Generator/Habbo.Protocol.Generator.csproj src/Habbo.Protocol.Generator/
COPY src/Habbo.Protocol.Wire/Habbo.Protocol.Wire.csproj src/Habbo.Protocol.Wire/
COPY src/Habbo.Silo/Habbo.Silo.csproj src/Habbo.Silo/
COPY src/Habbo.Silo.Abstractions/Habbo.Silo.Abstractions.csproj src/Habbo.Silo.Abstractions/
COPY src/Habbo.Web/Habbo.Web.csproj src/Habbo.Web/
RUN dotnet restore src/Habbo.Host/Habbo.Host.csproj

COPY src/ src/
# four build nodes at most: the runner builds the web image on the same machine
RUN dotnet publish src/Habbo.Host/Habbo.Host.csproj -c Release -o /app --no-restore -maxcpucount:4

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# the seed first: it changes far less often than the code, so its layer is kept and not pushed again
COPY seed/ seed/
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# web API and gamedata; the game (TCP and WebSocket)
EXPOSE 8080 30000
USER app
ENTRYPOINT ["dotnet", "Habbo.Host.dll"]
