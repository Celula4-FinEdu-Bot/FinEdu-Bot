-- ============================================================
-- FinEdu-Bot: vista "¿Qué pasó hoy?" (MEF, años 2017-2021)
-- La tabla la crea y la llena el flujo de n8n. Este archivo es
-- solo documentación del esquema. Se puede repetir sin duplicar nada.
--
-- El frontend muestra las filas de la fecha más reciente (MAX(fecha)).
-- Unidad de "valor":
--   - indicador terminado en "_pct": porcentaje en puntos (85.3 = 85,3 %)
--   - cualquier otro indicador: monto en soles (S/)
-- ============================================================

CREATE TABLE IF NOT EXISTS que_paso_hoy (
  fecha date NOT NULL,
  indicador text NOT NULL,
  titulo text NOT NULL,
  valor numeric,
  detalle text,
  destacado boolean NOT NULL DEFAULT false,
  rango_anios text NOT NULL DEFAULT '2017-2021',
  actualizado_en timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (fecha, indicador)
);
