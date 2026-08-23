# ================================
# Stage 1: Build
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project files
COPY ["ECommerce.API/ECommerce.API.csproj", "ECommerce.API/"]
COPY ["ECommerce.Application/ECommerce.Application.csproj", "ECommerce.Application/"]
COPY ["ECommerce.Domain/ECommerce.Domain.csproj", "ECommerce.Domain/"]
COPY ["ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "ECommerce.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ECommerce.API/ECommerce.API.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/ECommerce.API"

RUN dotnet build "ECommerce.API.csproj" \
    -c Release \
    -o /app/build


# ================================
# Stage 2: Publish
# ================================
FROM build AS publish

RUN dotnet publish "ECommerce.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ================================
# Stage 3: Runtime
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ECommerce.API.dll"]