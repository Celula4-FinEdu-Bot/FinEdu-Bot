# Auditoría inicial de seguridad --- HU-45

**Historia de usuario:** HU-45 --- *Externalizar y proteger las
credenciales de la plataforma*\
**Repositorio:** https://github.com/Celula4-FinEdu-Bot/FinEdu-Bot\
**Rama:** `Arquitectura-Lezma-HU-45`\
**Fecha:** 28 de octubre de 2026\
**Estado:** Diagnóstico inicial; acciones correctivas pendientes.

## 1. Objetivo y alcance

Evaluar la gestión inicial de credenciales, la configuración de entorno,
los archivos rastreados por Git y la detección automática de secretos
antes de modificar el repositorio. Este documento se basa en los
comandos y registros compartidos durante la conversación; no implica que
se hayan aplicado correcciones.

## 2. Estado inicial

-   Rama reportada: `Arquitectura-Lezma-HU-45`.
-   Último commit reportado: `ab13e36` (`cambio lezma 2`).
-   La rama local y `origin/Arquitectura-Lezma-HU-45` apuntaban al mismo
    commit.
-   **Pendiente:** registrar `git status --short` para confirmar si
    existen cambios locales.

## 3. Hallazgos

### H-01. Posibles credenciales expuestas en el historial

**Prioridad preliminar: Alta, pendiente de validar si las credenciales
son reales y siguen activas.**

Gitleaks informó ocho hallazgos con la regla `generic-api-key`. Los
registros mencionan cadenas de conexión y configuraciones de base de
datos en varios commits, entre ellas:

-   `src/frontend/src/appsettings.json`, línea 3.
-   `src/frontend/src/bin/Debug/net9.0/appsettings.json`, línea 3.
-   `src/database/migrations/FixAdminHash.cs`, línea 10.
-   `FinEduBot.Frontend/appsettings.json`, línea 3.
-   `FinEduBot.Frontend/bin/Debug/net9.0/appsettings.json`, línea 3.

Los ocho hallazgos no implican necesariamente ocho credenciales únicas.
La clasificación de Gitleaks requiere validar los valores de forma
segura.

**Acciones propuestas:** confirmar con el responsable de la base de
datos si los valores son reales; rotar y revocar cualquier credencial
activa potencialmente expuesta; retirar los secretos de la configuración
versionada; evaluar un saneamiento coordinado del historial sin
reescribirlo unilateralmente.

### H-02. Configuraciones rastreadas por Git

La salida de `git ls-files` mostró:

-   `src/frontend/src/appsettings.json`
-   `src/frontend/src/appsettings.Development.json`
-   `src/frontend/src/bin/Debug/net9.0/appsettings.json`
-   `src/frontend/src/bin/Debug/net9.0/appsettings.Development.json`

Se confirmó que `appsettings.json` contiene las claves
`ConnectionStrings` y `DefaultConnection`; el archivo de desarrollo
también contiene esas claves.

**Riesgo:** `.gitignore` no deja de rastrear archivos ya versionados. Se
debe crear una plantilla segura y decidir qué archivos de configuración
deben ser locales antes de retirar archivos del índice de Git.

### H-03. Artefactos de compilación versionados

Git rastrea numerosos archivos bajo
`src/frontend/src/bin/Debug/net9.0/`, incluidos `.dll`, `.exe`, `.pdb` y
copias de `appsettings`.

El `.gitignore` ya contiene `**/bin/` y `**/obj/`, pero esos archivos
siguen apareciendo en `git ls-files`.

**Acción propuesta:** retirar del seguimiento los artefactos generados
que corresponda, conservar los archivos locales cuando sea necesario y
comprobar que la aplicación compile después del cambio.

### H-04. Posible error en el filtro de rama de Gitleaks

En `.github/workflows/gitleaks.yml` aparece:

``` yaml
push:
  branches:
    - main
    - ArquitecArquitectura-Lezma-HU-45
```

La rama reportada se llama `Arquitectura-Lezma-HU-45`; el nombre
configurado no coincide exactamente. Por tanto, el evento `push` a esa
rama podría no activar el workflow por ese filtro. `pull_request` y
`workflow_dispatch` están configurados por separado.

**Acción propuesta:** corregir el nombre y verificar el workflow con un
`push` a la rama correcta.

### H-05. Externalización de la conexión

La búsqueda en código encontró en `src/frontend/src/Program.cs`:

``` csharp
builder.Configuration.GetConnectionString("DefaultConnection");
configuration.GetConnectionString("DefaultConnection");
```

También se encontró un mensaje que indica que en Render se puede
configurar `ConnectionStrings__DefaultConnection`.

Esto es compatible con la configuración de .NET mediante variables de
entorno, pero no demuestra por sí solo que todos los secretos estén
externalizados ni que el despliegue esté correctamente configurado.

**Acciones propuestas:** configurar la variable en el entorno de
despliegue, evitar contraseñas reales en archivos versionados y probar
la aplicación con la configuración externa.

### H-06. Plantilla de configuración

En la lista compartida de archivos rastreados no apareció
`.env.example`. Esto no demuestra que no exista localmente ni que la
aplicación .NET necesite obligatoriamente un archivo `.env`.

**Acción propuesta:** documentar las variables necesarias en una
plantilla o guía sin secretos reales. Para la conexión de .NET, se puede
documentar `ConnectionStrings__DefaultConnection` con un valor ficticio.

## 4. Resultado del escaneo Gitleaks

  Métrica              Resultado reportado
  -------------------- --------------------------
  Commits escaneados   130
  Datos escaneados     Aproximadamente 56,28 MB
  Hallazgos            8
  Regla                `generic-api-key`
  Resultado            Fallido
  Código de salida     `1`

**Interpretación:** el escaneo encontró valores que requieren
investigación. No debe resolverse desactivando Gitleaks ni agregando
excepciones injustificadas.

## 5. Revisión de `.gitignore`

El archivo contiene reglas para excluir `**/bin/`, `**/obj/`, `.env`,
`.env.*`, `appsettings.json` y `appsettings.Development.json`, además de
permitir `!.env.example`.

Las reglas son un punto de partida útil, pero no eliminan del índice los
archivos ya rastreados. Se debe comprobar el resultado de
`git check-ignore -v` y distinguir entre archivos ignorados y archivos
que Git ya sigue.

## 6. Plan de corrección propuesto

1.  Registrar el estado local con `git status --short`.
2.  Validar si las credenciales detectadas son reales, de prueba o
    revocadas; rotar las activas potencialmente expuestas.
3.  Preparar una plantilla de configuración sin secretos.
4.  Retirar del índice de Git los archivos sensibles y artefactos
    generados que no deban versionarse, sin borrar innecesariamente las
    copias locales.
5.  Corregir el filtro de rama en el workflow.
6.  Probar Gitleaks con el repositorio corregido y con una credencial
    ficticia de prueba, eliminando el archivo temporal al terminar.
7.  Coordinar la decisión sobre el saneamiento del historial con el
    equipo.
8.  Compilar y probar la aplicación usando la variable de entorno.
9.  Guardar evidencias redactadas de las pruebas y del workflow.

## 7. Evidencias disponibles

-   Salida de `git log -5 --oneline`.
-   Salida de `git ls-files` con configuraciones y artefactos generados
    rastreados.
-   Contenido compartido de `.gitignore`.
-   Contenido compartido de `.github/workflows/gitleaks.yml`.
-   Búsqueda de `GetConnectionString("DefaultConnection")` en
    `Program.cs`.
-   Registro de GitHub Actions con ocho hallazgos.

Las capturas y registros deben redactarse para no incluir contraseñas,
tokens ni cadenas de conexión completas.

## 8. Evaluación preliminar de criterios

  ------------------------------------------------------------------------
  Criterio propuesto      Estado                  Evidencia o pendiente
  ----------------------- ----------------------- ------------------------
  No almacenar            Pendiente de corrección Gitleaks detectó
  credenciales en                                 posibles credenciales
  archivos versionados                            históricas y los
                                                  archivos de
                                                  configuración están
                                                  rastreados.

  Leer la conexión desde  Parcial                 Existe
  configuración externa                           `GetConnectionString`;
                                                  falta validar ejecución
                                                  con la variable externa.

  Excluir archivos        Pendiente de corrección Las reglas existen, pero
  sensibles y artefactos                          Git sigue rastreando
                                                  archivos.

  Detección automática de Parcialmente verificado Gitleaks detectó
  secretos                                        hallazgos y el workflow
                                                  falló; falta prueba
                                                  controlada y escaneo
                                                  limpio.

  Escaneo en la rama de   Pendiente               El filtro de `push`
  trabajo                                         tiene un nombre de rama
                                                  aparentemente
                                                  incorrecto.

  Historial libre de      Pendiente               Los hallazgos requieren
  secretos activos                                validación y posible
                                                  saneamiento.
  ------------------------------------------------------------------------

  # Matriz de estado de componentes — FinEdu-Bot (HU-45)

| N.º | Elemento / componente | Estado real | Clasificación técnica | Observación |
|---:|---|---|---|---|
| 1 | `Program.cs` | Parcialmente implementado | En revisión | Usa `GetConnectionString("DefaultConnection")`, pero falta probar la configuración externa en ejecución. |
| 2 | `appsettings.json` | Requiere corrección | Riesgo de configuración | Está rastreado por Git y contiene la configuración de conexión. |
| 3 | `appsettings.Development.json` | Requiere revisión | Riesgo de configuración | Está rastreado por Git; se debe revisar qué valores deben permanecer locales. |
| 4 | `.env.example` | No verificado | Pendiente de comprobación | Falta confirmar su existencia y que no contenga credenciales reales. |
| 5 | `.gitignore` | Parcialmente implementado | Configuración existente | Tiene reglas de exclusión, pero estas no dejan de rastrear archivos ya versionados. |
| 6 | Artefactos `bin/Debug/net9.0/` | Requiere corrección | Versionado indebido | Se encontraron configuraciones y archivos generados de compilación rastreados por Git. |
| 7 | `FixAdminHash.cs` | Requiere revisión | Hallazgo de seguridad | Gitleaks informó una coincidencia histórica; falta confirmar si es un secreto real o un falso positivo. |
| 8 | `.github/workflows/gitleaks.yml` | Implementado con observaciones | Automatización existente | El workflow existe y ha detectado hallazgos; se identificó un posible error en el nombre de la rama. |
| 9 | Gitleaks | Parcialmente verificado | Detección activa | Se registraron ocho hallazgos históricos. Falta corregirlos o justificarlos y repetir el escaneo. |
| 10 | Credenciales de base de datos | Pendiente de validación | Riesgo potencial | No se ha confirmado si los valores reportados son reales, activos o revocados. |
| 11 | Prueba con variables de entorno | Pendiente | Validación funcional | Falta evidencia de una ejecución satisfactoria usando la configuración externa. |
| 12 | Documentación de auditoría | Parcialmente implementada | Evidencia inicial disponible | Existe una auditoría inicial; falta documentar las correcciones y sus resultados. |

## Resumen

| Clasificación | Cantidad |
|---|---:|
| Parcialmente implementado o verificado | 4 |
| Requiere corrección o revisión | 5 |
| Pendiente de verificación | 3 |
| **Total de elementos** | **12** |

**Estado global de HU-45:** En proceso. No debe considerarse terminada hasta completar las correcciones, verificar la configuración externa y registrar las pruebas finales.

## 9. Conclusión

La auditoría inicial identifica riesgos relevantes para la HU-45. El
proyecto utiliza una API de configuración de .NET compatible con
variables de entorno y dispone de un workflow de Gitleaks; sin embargo,
se reportaron posibles credenciales en el historial, configuraciones y
artefactos de compilación rastreados, además de un probable error
tipográfico en el filtro de rama.

La HU-45 no debe considerarse completada hasta externalizar y proteger
las credenciales, corregir el seguimiento de archivos, validar la
aplicación, gestionar los hallazgos históricos y demostrar con nuevas
ejecuciones que el escaneo funciona correctamente.

**Nota:** actualizar este informe después de ejecutar las comprobaciones
pendientes y aplicar las correcciones.
