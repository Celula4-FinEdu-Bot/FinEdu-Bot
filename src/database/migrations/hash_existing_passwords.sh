#!/usr/bin/env bash
# Script para hashear contraseñas existentes en la base de datos
# Uso: ./hash_existing_passwords.sh

set -e

echo "=== Hash de contraseñas existentes ==="
echo ""

# Verificar variables de entorno requeridas
if [ -z "$DB_HOST" ] || [ -z "$DB_PORT" ] || [ -z "$DB_NAME" ] || [ -z "$DB_USER" ] || [ -z "$DB_PASSWORD" ]; then
    echo "Error: Variables de entorno de base de datos no configuradas"
    echo "Requeridas: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD"
    exit 1
fi

CONNECTION_STRING="Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"

echo "Conectando a la base de datos..."

# Obtener usuarios existentes con contraseñas en texto plano
USERS=$(psql "$CONNECTION_STRING" -t -c "SELECT id, username, password FROM app_users WHERE password_hash IS NULL OR password_hash = '';")

if [ -z "$USERS" ]; then
    echo "No hay usuarios con contraseñas en texto plano para migrar."
    exit 0
fi

echo "Usuarios encontrados para migrar:"
echo "$USERS"
echo ""

# Para cada usuario, hashear la contraseña y actualizar
echo "$USERS" | while IFS='|' read -r id username password; do
    # Limpiar espacios
    id=$(echo "$id" | xargs)
    username=$(echo "$username" | xargs)
    password=$(echo "$password" | xargs)
    
    if [ -z "$id" ] || [ -z "$username" ] || [ -z "$password" ]; then
        continue
    fi
    
    echo "Procesando usuario: $username (ID: $id)"
    
    # Generar hash BCrypt usando dotnet
    HASH=$(dotnet run --project /tmp/hash_tool -- "$password" 2>/dev/null || echo "")
    
    if [ -z "$HASH" ]; then
        echo "  Error generando hash para $username"
        continue
    fi
    
    # Actualizar en base de datos
    psql "$CONNECTION_STRING" -c "UPDATE app_users SET password_hash = '$HASH' WHERE id = $id;"
    
    echo "  ✓ Hash actualizado para $username"
done

echo ""
echo "=== Migración completada ==="