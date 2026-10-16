@echo off
REM Script para hashear contraseñas existentes en la base de datos (Windows)
REM Uso: hash_existing_passwords.bat

echo === Hash de contraseñas existentes ===
echo.

REM Verificar variables de entorno requeridas
if "%DB_HOST%"=="" (
    echo Error: Variables de entorno de base de datos no configuradas
    echo Requeridas: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD
    exit /b 1
)

set CONNECTION_STRING=Host=%DB_HOST%;Port=%DB_PORT%;Database=%DB_NAME%;Username=%DB_USER%;Password=%DB_PASSWORD%;SSL Mode=Require;Trust Server Certificate=true

echo Conectando a la base de datos...

REM Obtener usuarios existentes con contraseñas en texto plano
for /f "tokens=1,2,3 delims=|" %%i in ('psql "%CONNECTION_STRING%" -t -c "SELECT id, username, password FROM app_users WHERE password_hash IS NULL OR password_hash = '';"') do (
    set "id=%%i"
    set "username=%%j"
    set "password=%%k"
    
    REM Limpiar espacios
    for /f "tokens=*" %%a in ("%id%") do set "id=%%a"
    for /f "tokens=*" %%a in ("%username%") do set "username=%%a"
    for /f "tokens=*" %%a in ("%password%") do set "password=%%a"
    
    if not "%id%"=="" if not "%username%"=="" if not "%password%"=="" (
        echo Procesando usuario: %username% (ID: %id%)
        
        REM Generar hash BCrypt usando dotnet
        for /f "tokens=*" %%h in ('dotnet run --project C:\tmp\hash_tool -- "%password%" 2^>nul') do set "HASH=%%h"
        
        if not "%HASH%"=="" (
            psql "%CONNECTION_STRING%" -c "UPDATE app_users SET password_hash = '%HASH%' WHERE id = %id%;"
            echo   Hash actualizado para %username%
        ) else (
            echo   Error generando hash para %username%
        )
    )
)

echo.
echo === Migración completada ===