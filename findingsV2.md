# Auditoría de seguridad actualizada — HU-45

**Historia de usuario:** HU-45 — *Externalizar y proteger las credenciales de la plataforma*  
**Repositorio:** https://github.com/Celula4-FinEdu-Bot/FinEdu-Bot  
**Rama:** `Arquitectura-Lezma-HU-45`  
**Fecha de actualización:** 30 de octubre de 2026  
**Estado:** Implementación avanzada; validaciones de cierre pendientes.

## 1. Objetivo y alcance

Esta actualización incorpora los avances posteriores al diagnóstico inicial: actualización del workflow de Gitleaks, prueba controlada con un secreto ficticio, ejecuciones de GitHub Actions y configuración de protección de la rama `main`. Se separan los avances reportados de las comprobaciones que aún no tienen evidencia concluyente.

No se afirma que las credenciales históricas hayan sido rotadas, revocadas o eliminadas del historial, ni que la aplicación haya sido probada con una variable externa: esos resultados no están acreditados en la información disponible.

## 2. Resumen del estado actualizado

| Elemento | Estado reportado |
|---|---|
| Rama | `Arquitectura-Lezma-HU-45` |
| Commit | `2ccf3b3` — `security(HU-45): validate controlled secret detection` |
| Workflow | Actualizado: escaneo de archivos actuales y prueba controlada con canary ficticio |
| GitHub Actions | `Detect secrets` reportado en verde |
| PR #218 | 15/15 checks verdes; `Review required`; sin merge |
| Ruleset `main-3` | Guardado para `main`; exige Pull Request y status check `Detect secrets` |
| Ruleset `main-2` | Activo según lo reportado; restricciones de borrado y force-push |
| Estado global | Implementación avanzada; cierre pendiente de validaciones |

## 3. Hallazgos y evolución

### H-01. Posibles credenciales en el historial

**Estado:** Abierto — remediación no demostrada.

El diagnóstico inicial registró ocho coincidencias `generic-api-key` en 130 commits. La prueba controlada posterior valida la detección, pero no determina si los valores históricos eran reales o siguen activos.

**Acciones pendientes:** confirmar con el responsable, rotar o revocar las credenciales activas y acordar el tratamiento del historial.

### H-02. Configuraciones rastreadas por Git

**Estado:** Pendiente de evidencia.

Inicialmente se identificaron `appsettings.json` y `appsettings.Development.json` rastreados. Falta una salida posterior de `git ls-files` que demuestre su retirada del índice o su sustitución por configuraciones seguras.

### H-03. Artefactos de compilación versionados

**Estado:** Pendiente de evidencia.

Se identificaron archivos bajo `bin/Debug/net9.0`. Las reglas de `.gitignore` no dejan de rastrear archivos ya versionados; falta comprobar el índice actual.

### H-04. Filtro de rama de Gitleaks

**Estado:** Corregido según avance reportado; conservar evidencia.

El filtro inicial contenía un nombre de rama aparentemente incorrecto. El workflow posterior se ejecutó y el commit `2ccf3b3` fue reportado.

**Verificación recomendada:** conservar el YAML final y una ejecución `push` en la rama correcta.

### H-05. Externalización de conexión

**Estado:** Parcial.

`Program.cs` utiliza `GetConnectionString("DefaultConnection")`, compatible con configuración externa de .NET. Falta demostrar ejecución satisfactoria con `ConnectionStrings__DefaultConnection` y sin dependencia de valores sensibles versionados.

### H-06. Plantilla de configuración

**Estado:** No verificado.

No hay evidencia posterior suficiente sobre `.env.example` o guía equivalente completa y libre de secretos. Se debe confirmar su existencia y contenido.

### H-07. Detección automática

**Estado:** Implementado; prueba controlada reportada.

El workflow instala Gitleaks CLI v8.30.1, escanea archivos actuales con `--redact` y ejecuta una prueba temporal con un canary ficticio y una regla específica. GitHub Actions fue reportado exitoso.

Esto no demuestra que el historial esté libre de secretos.

### H-08. Protección de `main`

**Estado:** Configurada; prueba negativa pendiente.

`main-3` exige Pull Request y `Detect secrets` como status check. El PR #218 mostró 15/15 checks y `Review required`.

Falta demostrar, en una prueba segura, que un check fallido bloquea el merge.

## 4. Implementación posterior documentada

- Se actualizó `.github/workflows/gitleaks.yml` y se reportó el commit `2ccf3b3`: `security(HU-45): validate controlled secret detection`.
- El workflow instala Gitleaks CLI v8.30.1 y ejecuta un escaneo de archivos actuales con salida redactada.
- Se añadió una prueba controlada con un canary ficticio en una ubicación temporal, una regla temporal y verificación del hallazgo esperado. No se deben usar credenciales reales.
- GitHub Actions para `Detect secrets` fue reportado en verde, incluidos los pasos de escaneo y detección controlada.
- El PR #218 mostró 15/15 checks verdes y `Review required`. No se realizó merge.
- El ruleset `main-3` fue guardado para `main`, exigiendo Pull Request y el check `Detect secrets`.

## 5. Matriz actualizada de componentes

| Elemento | Estado actualizado | Siguiente verificación |
|---|---|---|
| `Program.cs` / `GetConnectionString` | Parcialmente implementado | Probar ejecución con variable externa |
| `appsettings.json` | Pendiente de verificación | Comprobar seguimiento Git y contenido |
| `appsettings.Development.json` | Pendiente de verificación | Comprobar seguimiento Git y contenido |
| `.env.example` / guía equivalente | No verificado | Confirmar existencia y ausencia de secretos |
| `.gitignore` | Reglas existentes; eficacia pendiente | Comprobar `git ls-files` y `git check-ignore -v` |
| `bin/Debug/net9.0` | Hallazgo inicial abierto | Confirmar retiro de artefactos del índice |
| `FixAdminHash.cs` | Hallazgo histórico pendiente | Validar coincidencia sin exponer el valor |
| `.github/workflows/gitleaks.yml` | Actualizado | Guardar YAML final y evidencia de push |
| Gitleaks v8.30.1 | Implementado | Escaneo actual y prueba controlada reportados exitosos |
| Canary ficticio | Prueba controlada implementada | Confirmar limpieza del archivo temporal |
| Ruleset `main-3` | Configurado según lo reportado | Prueba negativa segura del status check |
| Ruleset `main-2` | Activo según lo reportado | Conservar captura de restricciones |
| PR #218 | Abierto; revisión requerida | Sin merge; conservar evidencia de checks |
| Rotación/revocación | No verificado | Confirmación del responsable y evidencia segura |
| Saneamiento del historial | Pendiente de decisión | Coordinar con el equipo; no reescribir unilateralmente |

## 6. Evaluación de criterios de aceptación

| Criterio | Estado | Brecha / evidencia |
|---|---|---|
| **CA-01 — Externalizar credenciales y configuración** | Parcial | Existe `GetConnectionString`; falta probar el despliegue con variable externa y verificar archivos versionados. |
| **CA-02 — Excluir archivos sensibles y artefactos** | Pendiente de evidencia | Falta comprobar que archivos ya rastreados fueron retirados del índice. |
| **CA-03 — Detección automática de secretos** | Implementado; prueba controlada reportada | Workflow y canary ficticio; Actions en verde. Falta escaneo limpio actual e investigación histórica. |
| **CA-04 — Proteger rama principal** | Parcialmente implementado | Ruleset exige PR y `Detect secrets`; falta prueba negativa de bloqueo ante check fallido. |
| **CA-05 — Gestionar credenciales históricas** | Pendiente | Validar valores reales/activos, rotar o revocar y acordar tratamiento del historial. |

## 7. Evidencias disponibles

- Diagnóstico inicial: `git log -5 --oneline`, `git ls-files`, `.gitignore`, workflow inicial, búsqueda en `Program.cs` y registro Gitleaks con ocho hallazgos.
- Commit reportado: `2ccf3b3` — `security(HU-45): validate controlled secret detection`.
- Ejecución de GitHub Actions reportada exitosa para `Detect secrets`, con escaneo actual y prueba controlada.
- PR #218: 15/15 checks verdes y `Review required`; no fusionado.
- Ruleset `main-3` guardado con requerimiento de PR y `Detect secrets`; `main-2` activo según lo reportado.

Las capturas y registros deben redactarse para no mostrar tokens, contraseñas ni cadenas de conexión.

## 8. Acciones pendientes para el cierre

1. Registrar `git status --short` y `git ls-files` para conocer el estado actual de configuraciones y artefactos.
2. Validar con el responsable si las coincidencias históricas eran credenciales reales; rotar o revocar las que sigan activas.
3. Confirmar que los `appsettings` versionados no contienen secretos y retirar del índice los archivos que deban ser locales.
4. Confirmar `.env.example` o una guía equivalente con variables necesarias y valores ficticios.
5. Ejecutar Gitleaks sobre el estado actual e investigar toda coincidencia sin desactivar la herramienta ni añadir excepciones injustificadas.
6. Probar la aplicación con `ConnectionStrings__DefaultConnection` configurada externamente.
7. Validar de forma segura que un status check fallido impide completar un PR, sin fusionar ni debilitar `main`.
8. Coordinar con el equipo cualquier saneamiento del historial Git.
9. Actualizar el informe con capturas y enlaces a las ejecuciones y configuraciones.

## 9. Conclusión

HU-45 avanzó de un diagnóstico con ocho coincidencias Gitleaks a una automatización que escanea archivos actuales y prueba la detección mediante un canary ficticio. También se reportó la protección de `main` mediante un ruleset que exige Pull Request y el check `Detect secrets`. La ejecución de Actions y los checks del PR #218 fueron reportados en verde.

No corresponde declarar HU-45 completamente cerrada todavía. Faltan evidencias concluyentes sobre rotación o revocación de credenciales históricas reales, limpieza de configuraciones y artefactos previamente rastreados, ejecución con configuración externa y bloqueo efectivo ante un check fallido.

**Estado recomendado:** implementación avanzada; cierre pendiente de validaciones y evidencias.
