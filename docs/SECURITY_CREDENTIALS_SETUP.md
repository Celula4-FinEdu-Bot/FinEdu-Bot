# Guía de Configuración de Credenciales Seguras

## Resumen de Cambios de Seguridad

Este documento describe los cambios implementados para proteger credenciales y secretos en el proyecto FinEdu-Bot.

---

## 1. Contraseñas de Usuarios (Frontend - Blazor)

### Antes (Inseguro)
- Contraseñas almacenadas en texto plano en la base de datos
- Función SQL `authenticate_app_user($1, $2)` recibía contraseña en texto plano
- Sin hashing ni salt

### Después (Seguro)
- **BCrypt con SHA384** (work factor 12) para hashing de contraseñas
- Nuevo servicio `IPasswordHasher` / `PasswordHasher` en `src/Services/`
- Columna `password_hash` en tabla `app_users` (migración `001_add_password_hash.sql`)
- Login verifica contraseña con `BCrypt.EnhancedVerify()`
- Protección contra timing attacks (tiempo constante en fallos)

### Migración de Contraseñas Existentes
```bash
# Configurar variables de entorno
export DB_HOST=...
export DB_PORT=...
export DB_NAME=...
export DB_USER=...
export DB_PASSWORD=...

# Ejecutar script de migración
./src/database/migrations/hash_existing_passwords.sh
```

---

## 2. Variables de Entorno (Archivos .env)

### Archivos Creados/Actualizados
| Archivo | Propósito |
|---------|-----------|
| `infrastructure/.env.example` | Plantilla con todas las variables necesarias |
| `infrastructure/.env` | **NO commitear** - valores reales para desarrollo local |
| `src/frontend/src/appsettings.json` | Usa placeholders `${VAR}` |
| `src/frontend/src/appsettings.Development.json` | Usa placeholders `${VAR}` |

### Variables Requeridas
```bash
# Base de datos
DB_HOST=...
DB_PORT=5432
DB_NAME=...
DB_USER=...
DB_PASSWORD=...

# Backend API
N8N_WEBHOOK_URL=http://localhost:5678/webhook/ai-agent-orchestrator

# APIs Externas
GOOGLE_GEMINI_API_KEY=...
MEF_API_BASE_URL=https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/
MEF_RESOURCE_ID=5f3b3cbe-3955-41cc-8662-1757ebb5cf53

# Opcional (n8n)
SLACK_WEBHOOK_URL=...
GROQ_API_KEY=...
```

### Configuración Local
```bash
cp infrastructure/.env.example infrastructure/.env
# Editar .env con valores reales
```

---

## 3. Credenciales de n8n

### Cambios en Workflow `FinEdu-Caso5-Cedula4.json`
- **Eliminado**: Nodo Slack Alert (credenciales hardcoded)
- **Eliminado**: Nodo Groq Chat Model Fallback (credenciales hardcoded)
- **Mantenido**: Solo Google Gemini API (configurable en n8n Credentials)

### Configuración en n8n UI
1. Abrir n8n en `http://localhost:5678`
2. Ir a **Credentials** → **New Credential**
3. Crear credenciales:
   - **Google Gemini API**: API Key de Google AI Studio
   - **PostgreSQL**: Host, puerto, base de datos, usuario, contraseña
4. En el workflow, actualizar las referencias de credenciales (IDs placeholder: `GEMINI_CRED_ID`, `POSTGRES_CRED_ID`)

### Seguridad en n8n
- n8n encripta credenciales en su base de datos interna (`/home/node/.n8n`)
- No almacenar API keys en el JSON del workflow
- Usar variables de entorno de n8n para configuración sensible

---

## 4. Docker Compose

### Actualizaciones en `infrastructure/docker-compose.yml`
- Servicios: `n8n`, `frontend`, `backend`
- Uso de `env_file: .env` para cargar variables
- Variables de entorno referenciadas con `${VAR:-default}`
- Contraseñas **nunca** en el archivo YAML

---

## 5. Base de Datos

### Migración: `001_add_password_hash.sql`
```sql
ALTER TABLE app_users 
ADD COLUMN IF NOT EXISTS password_hash TEXT;

COMMENT ON COLUMN app_users.password_hash 
IS 'Hash BCrypt de la contraseña del usuario (SHA384, work factor 12)';

CREATE INDEX IF NOT EXISTS idx_app_users_username ON app_users(username);
```

---

## 6. Verificación de Seguridad

### Checklist Pre-Deploy
- [ ] No hay contraseñas en texto plano en código
- [ ] No hay API keys en archivos commiteados
- [ ] `.env` está en `.gitignore`
- [ ] `appsettings.json` usa placeholders `${VAR}`
- [ ] Credenciales n8n configuradas en UI (no en JSON)
- [ ] BCrypt work factor ≥ 12
- [ ] HTTPS habilitado en producción
- [ ] Cookies seguras (`SecurePolicy = Always` en producción)

### Comandos de Verificación
```bash
# Verificar que no hay secretos en git
git log --all --full-history -- "**/.env" "**/appsettings.json" | grep -i password

# Verificar hashes en BD
psql "$DATABASE_URL" -c "SELECT username, CASE WHEN password_hash IS NULL THEN 'SIN HASH' ELSE 'HASHEADO' END FROM app_users;"
```

---

## 7. Rotación de Credenciales

Si se detecta exposición de credenciales:
1. Revocar inmediatamente la clave expuesta
2. Generar nueva clave en el proveedor (Google AI Studio, Render, etc.)
3. Actualizar en `.env` local y en n8n Credentials
4. Re-desplegar servicios afectados
5. Verificar logs de acceso anómalo

---

## 8. Referencias

- [BCrypt.Net-Next Documentation](https://github.com/BcryptNet/bcrypt.net)
- [n8n Credentials Management](https://docs.n8n.io/credentials/)
- [ASP.NET Core Configuration](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/)
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)