# ==========================================
# BASE
# ==========================================

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app

EXPOSE 8080


# ==========================================
# BUILD
# ==========================================

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /source

# ==========================================
# COPIAR PROYECTO .NET
# ==========================================

COPY src/frontend/src/src.csproj src/frontend/src/

# Restaurar dependencias
RUN dotnet restore src/frontend/src/src.csproj


# ==========================================
# COPIAR CÓDIGO DEL PROYECTO
# ==========================================

COPY src/frontend/src/ src/frontend/src/


# ==========================================
# COMPILAR
# ==========================================

WORKDIR /source/src/frontend/src

RUN dotnet build src.csproj \
    -c Release \
    -o /app/build \
    --no-restore


# ==========================================
# PUBLISH
# ==========================================

FROM build AS publish

RUN dotnet publish src.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore


# ==========================================
# FINAL
# ==========================================

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .


# ==========================================
# CONFIGURACIÓN ASP.NET
# ==========================================

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENV ASPNETCORE_ENVIRONMENT=Production


# ==========================================
# PUERTO
# ==========================================

EXPOSE 8080


# ==========================================
# EJECUTAR
# ==========================================

ENTRYPOINT ["dotnet", "src.dll"]