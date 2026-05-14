FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SpotIt.Domain/SpotIt.Domain.csproj src/SpotIt.Domain/
COPY src/SpotIt.Application/SpotIt.Application.csproj src/SpotIt.Application/
COPY src/SpotIt.Infrastructure/SpotIt.Infrastructure.csproj src/SpotIt.Infrastructure/
COPY src/SpotIt.API/SpotIt.API.csproj src/SpotIt.API/

RUN dotnet restore src/SpotIt.API/SpotIt.API.csproj

COPY src/ src/
RUN dotnet publish src/SpotIt.API/SpotIt.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SpotIt.API.dll"]
