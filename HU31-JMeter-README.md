# HU-31 - Prueba de carga con Apache JMeter

## Herramienta
Apache JMeter 5.6.3

## Configuración
- Usuarios concurrentes: 5
- Ramp-up: 10 segundos
- Iteraciones: 5
- Método HTTP: GET
- Endpoint: http://localhost:3000/api/health

## Verificación del backend
El endpoint /api/health respondió con:
- Service: finedu-bot-backend
- Status: ok
- Environment: development

## Objetivo
Realizar una prueba inicial para comprobar la disponibilidad del backend de FinEdu-Bot.
