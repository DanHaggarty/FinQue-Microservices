# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files first for optimized layer caching
COPY ["ValidationService/ValidationService.csproj", "ValidationService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "ValidationService/ValidationService.csproj"

# Copy the rest of the source
COPY . .
WORKDIR "/src/ValidationService"
RUN dotnet build "ValidationService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ValidationService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ValidationService.dll"]
