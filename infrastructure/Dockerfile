# ============================================================
# BASE
# ============================================================

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app

EXPOSE 8080


# ============================================================
# BUILD
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /source

# Directorio donde NuGet almacenará los paquetes
ENV NUGET_PACKAGES=/root/.nuget/packages


# ============================================================
# LIMPIEZA
# ============================================================

# Evitar que archivos bin/obj provenientes del repositorio
# interfieran con el restore de Docker.
RUN rm -rf /source/src/frontend/src/bin \
           /source/src/frontend/src/obj


# ============================================================
# COPIAR SOLO EL PROYECTO
# ============================================================

COPY src/frontend/src/src.csproj \
     ./src/frontend/src/src.csproj


# ============================================================
# LIMPIAR CACHE DE NUGET
# ============================================================

RUN dotnet nuget locals all --clear


# ============================================================
# RESTORE
# ============================================================

RUN dotnet restore \
    ./src/frontend/src/src.csproj \
    --force \
    --no-cache


# ============================================================
# COPIAR EL CÓDIGO DEL FRONTEND
# ============================================================

COPY src/frontend/src/ \
     ./src/frontend/src/


# ============================================================
# LIMPIAR BIN/OBJ COPIADOS
# ============================================================

RUN rm -rf ./src/frontend/src/bin \
           ./src/frontend/src/obj


# ============================================================
# RESTORE NUEVAMENTE
# ============================================================

RUN dotnet restore \
    ./src/frontend/src/src.csproj \
    --force \
    --no-cache


# ============================================================
# BUILD
# ============================================================

WORKDIR /source/src/frontend/src

RUN dotnet build src.csproj \
    -c Release \
    -o /app/build


# ============================================================
# PUBLISH
# ============================================================

FROM build AS publish

WORKDIR /source/src/frontend/src

RUN dotnet publish src.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ============================================================
# FINAL
# ============================================================

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .


# ============================================================
# CONFIGURACIÓN ASP.NET CORE / RENDER
# ============================================================

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080


# ============================================================
# EJECUTAR
# ============================================================

ENTRYPOINT ["dotnet", "src.dll"]