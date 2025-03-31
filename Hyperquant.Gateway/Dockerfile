FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HyperquanTest.Gateway/HyperquanTest.Gateway.csproj", "HyperquanTest.Gateway/"]
RUN dotnet restore "HyperquanTest.Gateway/HyperquanTest.Gateway.csproj"
COPY . .
WORKDIR "/src/HyperquanTest.Gateway"
RUN dotnet build "HyperquanTest.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HyperquanTest.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HyperquanTest.Gateway.dll"]
