## CA-02 — Protección de credenciales expuestas

| N.º | Elemento | Estado real | Evidencia / observación |
|---:|---|---|---|
| 1 | Identificación de posibles credenciales expuestas | Completado parcialmente | Gitleaks reportó 8 hallazgos históricos relacionados con posibles secretos. |
| 2 | Validación de credenciales detectadas | Pendiente | Falta confirmar si los valores son reales, de prueba, revocados o falsos positivos. |
| 3 | Rotación o revocación de credenciales | Pendiente de evaluación | No existe evidencia registrada de que se hayan rotado o revocado credenciales. |
| 4 | Eliminación de credenciales de archivos versionados | Pendiente de corrección | Se identificaron archivos de configuración rastreados por Git que deben revisarse y sanearse. |
| 5 | Revisión del historial de Git | Pendiente | Los hallazgos históricos requieren evaluación y una decisión coordinada sobre el saneamiento del historial. |
| 6 | Verificación posterior con Gitleaks | Pendiente | Falta repetir el escaneo después de aplicar las correcciones y documentar el resultado. |
| 7 | Evidencias de seguridad | Parcialmente implementado | Existe una auditoría inicial, pero falta documentar la validación y las acciones correctivas finales. |

### Resultado de la CA-02

- **Estado general:** En proceso.
- **Elementos identificados:** 7.
- **Pendientes de validación o corrección:** 6.
- **Parcialmente implementados:** 1.

**Conclusión:** La CA-02 todavía no puede considerarse cumplida. Primero deben validarse los hallazgos de Gitleaks y, si se confirma que alguna credencial activa fue expuesta, rotarla o revocarla. Después se deben documentar las medidas aplicadas y los resultados del nuevo escaneo.