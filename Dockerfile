FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
RUN apt-get update && apt-get install -y curl \
    && curl -Ls https://cli.doppler.com/install.sh | sh
WORKDIR /src
COPY ["AurionCal.Api.csproj", "./"]
RUN dotnet restore "AurionCal.Api.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "./AurionCal.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AurionCal.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /usr/bin/doppler /usr/bin/doppler
COPY --from=publish /app/publish .

ENTRYPOINT ["doppler", "run", "--", "dotnet", "AurionCal.Api.dll"]
