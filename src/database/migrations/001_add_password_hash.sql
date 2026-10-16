-- Migración: Agregar columna password_hash a la tabla app_users
-- Esta migración agrega soporte para contraseñas hasheadas con BCrypt

-- Agregar columna password_hash si no existe
ALTER TABLE app_users 
ADD COLUMN IF NOT EXISTS password_hash TEXT;

-- Comentario para documentar el propósito
COMMENT ON COLUMN app_users.password_hash IS 'Hash BCrypt de la contraseña del usuario (SHA384, work factor 12)';

-- Índice para búsquedas por username (si no existe)
CREATE INDEX IF NOT EXISTS idx_app_users_username ON app_users(username);