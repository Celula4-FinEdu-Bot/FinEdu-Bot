# Auditoría de seguridad actualizada — HU-45

**Historia de usuario:** HU-45 — *Externalizar y proteger las credenciales de la plataforma*  
**Repositorio:** https://github.com/Celula4-FinEdu-Bot/FinEdu-Bot  
**Rama:** `Arquitectura-Lezma-HU-45`  
**Fecha:** 30 de octubre de 2026  
**Estado:** Implementación parcial; automatización Gitleaks retirada por decisión del responsable.

## 1. Objetivo y alcance

Esta actualización incorpora la decisión comunicada por el usuario: por indicación de su jefe, se eliminó `.github/workflows/gitleaks.yml`, porque el workflow generaba errores relacionados con `main` y se ejecutaba en ramas donde no debía hacerlo.

Las ejecuciones exitosas anteriores de Gitleaks son evidencia histórica, no evidencia de una protección automática actualmente activa. No se ha confirmado si el ruleset `main-3` fue modificado después de eliminar el workflow.

## 2. Resumen del estado actualizado

| Elemento | Estado |
|---|---|
| Rama | `Arquitectura-Lezma-HU-45` |
| Commit reportado | `2ccf3b3` — `security(HU-45): validate controlled secret detection` |
| `.github/workflows/gitleaks.yml` | Eliminado por instrucción del jefe |
| Gitleaks CLI v8.30.1 | Implementación anterior; no activa mediante el workflow eliminado |
| Prueba controlada con canary ficticio | Ejecutada anteriormente; evidencia histórica |
| GitHub Actions `Detect secrets` | Ejecuciones anteriores exitosas; workflow retirado posteriormente |
| Ruleset `main-3` | Estado actual no confirmado; revisar si aún exige `Detect secrets` |
| PR #218 | Se reportó con 15/15 checks verdes y `Review required`; sin merge |
| Estado global | HU-45 parcialmente implementada; cierre pendiente |

## 3. Hallazgos y evolución

### H-01. Posibles credenciales en el historial
**Estado:** Abierto — remediación no demostrada.

El diagnóstico inicial registró ocho coincidencias `generic-api-key` en 130 commits. La prueba controlada validó la detección, pero no determinó si los valores históricos eran reales o seguían activos. Confirmar con el responsable, rotar o revocar las credenciales activas y acordar el tratamiento del historial.

### H-02. Configuraciones rastreadas por Git
**Estado:** Pendiente de evidencia.

Inicialmente se identificaron `appsettings.json` y `appsettings.Development.json` rastreados. Falta una salida posterior de `git ls-files` que demuestre su retirada del índice o su sustitución por configuraciones seguras.

### H-03. Artefactos de compilación versionados
**Estado:** Pendiente de evidencia.

Se identificaron archivos bajo `bin/Debug/net9.0`. Las reglas de `.gitignore` no dejan de rastrear archivos ya versionados; falta comprobar el índice actual.

### H-04. Filtro de rama y ejecución del workflow
**Estado:** Workflow retirado por decisión del responsable.

La auditoría inicial identificó un posible error tipográfico en el filtro de rama. Posteriormente se reportó que Gitleaks se ejecutaba en ramas donde no debía hacerlo y generaba errores relacionados con `main`. Por indicación del jefe, se eliminó `.github/workflows/gitleaks.yml`. La eliminación detiene ese workflow, pero también significa que Gitleaks ya no proporciona una comprobación automática mediante ese archivo.

### H-05. Externalización de conexión
**Estado:** Parcial.

`Program.cs` utiliza `GetConnectionString("DefaultConnection")`, compatible con configuración externa de .NET. Falta demostrar ejecución satisfactoria con `ConnectionStrings__DefaultConnection` y sin dependencia de valores sensibles versionados.

### H-06. Plantilla de configuración
**Estado:** No verificado.

No hay evidencia suficiente sobre `.env.example` o una guía equivalente completa y libre de secretos. Se debe confirmar su existencia y contenido.

### H-07. Detección automática de secretos
**Estado:** Retirada temporalmente / no activa mediante el workflow eliminado.

La implementación anterior instalaba Gitleaks CLI v8.30.1, escaneaba archivos actuales con `--redact` y ejecutaba una prueba temporal con un canary ficticio y una regla específica. Las ejecuciones fueron exitosas antes de retirar el archivo. Son evidencia histórica, no protección activa actual.

### H-08. Protección de `main`
**Estado:** Parcial; configuración actual por verificar.

Se reportó que el ruleset `main-3` exigía Pull Request y el status check `Detect secrets`. Como el workflow que generaba ese check fue eliminado, es necesario revisar el ruleset y retirar o sustituir ese requisito si todavía está configurado y ya no existe un check equivalente. No se debe desactivar la protección de `main` en general.

## 4. Implementación posterior documentada

- Se reportó el commit `2ccf3b3`: `security(HU-45): validate controlled secret detection`.
- El workflow anterior instalaba Gitleaks CLI v8.30.1 y escaneaba archivos actuales con salida redactada.
- Se añadió una prueba controlada con un canary ficticio en una ubicación temporal.
- GitHub Actions para `Detect secrets` fue reportado en verde en ejecuciones anteriores.
- Posteriormente, por instrucción del jefe, se eliminó `.github/workflows/gitleaks.yml` debido a ejecuciones en ramas no previstas y errores relacionados con `main`.
- No debe considerarse activo el escaneo automático mediante ese workflow eliminado.
- Se reportó que el PR #218 tenía 15/15 checks verdes y `Review required`; no se realizó merge.
- El estado actual del ruleset `main-3` debe confirmarse.

## 5. Matriz actualizada de componentes

| Elemento | Estado actualizado | Siguiente verificación |
|---|---|---|
| `Program.cs` / `GetConnectionString` | Parcial | Probar ejecución con variable externa |
| `appsettings.json` | Pendiente | Comprobar seguimiento Git y contenido |
| `appsettings.Development.json` | Pendiente | Comprobar seguimiento Git y contenido |
| `.env.example` / guía equivalente | No verificado | Confirmar existencia y ausencia de secretos |
| `.gitignore` | Reglas existentes; eficacia pendiente | Comprobar `git ls-files` y `git check-ignore -v` |
| `bin/Debug/net9.0` | Hallazgo inicial abierto | Confirmar retiro de artefactos del índice |
| `FixAdminHash.cs` | Hallazgo histórico pendiente | Validar coincidencia sin exponer el valor |
| `.github/workflows/gitleaks.yml` | **Eliminado** | Confirmar commit de eliminación |
| Gitleaks CLI v8.30.1 | Implementación anterior; no activa | Definir alternativa aprobada si se requiere detección |
| Canary ficticio | Prueba controlada histórica | No presentarlo como prueba activa |
| Ruleset `main-3` | Estado actual no confirmado | Verificar si exige `Detect secrets` |
| Ruleset `main-2` | Activo según reporte anterior | Confirmar restricciones vigentes |
| PR #218 | Estado actual por confirmar | Se reportó abierto y sin merge |
| Rotación/revocación | No verificado | Confirmación del responsable |
| Saneamiento del historial | Pendiente de decisión | Coordinar con el equipo |

## 6. Evaluación de criterios de aceptación

| Criterio | Estado actualizado | Brecha / evidencia |
|---|---|---|
| **CA-01 — Externalizar credenciales y configuración** | Parcial | Existe `GetConnectionString`; falta probar variable externa y verificar archivos versionados. |
| **CA-02 — Excluir archivos sensibles y artefactos** | Pendiente de evidencia | Falta comprobar que archivos ya rastreados fueron retirados del índice. |
| **CA-03 — Detección automática de secretos** | No activa mediante el workflow retirado | La implementación y prueba controlada funcionaron anteriormente, pero el workflow fue eliminado. |
| **CA-04 — Proteger rama principal** | Parcial; requiere revisión | Verificar si `main-3` conserva `Detect secrets` como check obligatorio. |
| **CA-05 — Gestionar credenciales históricas** | Pendiente | Validar valores reales/activos, rotar o revocar y acordar tratamiento del historial. |

## 7. Evidencias disponibles

- Diagnóstico inicial: `git log -5 --oneline`, `git ls-files`, `.gitignore`, workflow inicial, búsqueda en `Program.cs` y registro Gitleaks con ocho hallazgos.
- Commit reportado: `2ccf3b3` — `security(HU-45): validate controlled secret detection`.
- Ejecuciones anteriores de GitHub Actions reportadas exitosas para `Detect secrets`, con escaneo actual y prueba controlada.
- Decisión comunicada: eliminación de `.github/workflows/gitleaks.yml` por indicación del jefe, debido a ejecuciones en ramas no previstas y errores relacionados con `main`.
- PR #218: se reportaron 15/15 checks verdes y `Review required`; no fusionado.
- Configuración anterior reportada del ruleset `main-3`; falta confirmar su estado actual.

Las capturas y registros deben redactarse para no mostrar tokens, contraseñas ni cadenas de conexión.

## 8. Acciones pendientes para el cierre

1. Registrar `git status --short` y `git ls-files`.
2. Confirmar que `.github/workflows/gitleaks.yml` ya no existe en la rama y registrar el commit que contiene la eliminación.
3. Revisar el ruleset `main-3` y comprobar si todavía exige `Detect secrets`. Si el check ya no se genera, coordinar la actualización del requisito con el jefe o administrador.
4. Validar si las coincidencias históricas eran credenciales reales; rotar o revocar las que sigan activas.
5. Confirmar que los `appsettings` versionados no contienen secretos y retirar del índice los archivos que deban ser locales.
6. Confirmar `.env.example` o una guía equivalente con variables necesarias y valores ficticios.
7. Si se decide restablecer la detección automática, seleccionar una solución aprobada y limitarla a las ramas/eventos previstos.
8. Probar la aplicación con `ConnectionStrings__DefaultConnection` configurada externamente.
9. Coordinar cualquier saneamiento del historial Git con el equipo.
10. Actualizar el informe con capturas y enlaces a las ejecuciones y configuraciones.

## 9. Conclusión

HU-45 avanzó desde un diagnóstico inicial con ocho coincidencias Gitleaks hasta una implementación que incluía escaneo de archivos actuales y una prueba controlada con un canary ficticio. También se reportó la configuración de protección de `main`.

Posteriormente, por instrucción del jefe, se eliminó `.github/workflows/gitleaks.yml` debido a ejecuciones en ramas no previstas y errores relacionados con `main`. Por ello, la detección automática mediante ese workflow ya no está activa. Se debe revisar el ruleset `main-3` para comprobar que no siga exigiendo un status check que dejó de generarse.

**Estado recomendado:** HU-45 parcialmente implementada; automatización Gitleaks retirada por decisión del responsable; cierre pendiente de verificar la configuración de `main`, la gestión de credenciales históricas, la limpieza de archivos rastreados y la configuración externa.
