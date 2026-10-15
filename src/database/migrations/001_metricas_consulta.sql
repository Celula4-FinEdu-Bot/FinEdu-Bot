-- ============================================================
-- FinEdu-Bot: metricas por consulta de IA (HU metricas)
-- Ejecutar en Auto-commit. Se puede repetir sin duplicar nada.
-- ============================================================

-- 1) Metricas de cada consulta (una fila por solicitud)
CREATE TABLE IF NOT EXISTS public.metricas_consulta (
    id                     bigserial PRIMARY KEY,
    correlation_id         uuid        NOT NULL UNIQUE,   -- CA-01
    session_id             varchar(100),                  -- sesion del chat
    execution_id           varchar(50),                   -- ejecucion de n8n
    created_at             timestamptz NOT NULL DEFAULT now(),

    latencia_total_ms      integer,                       -- CA-02 (end-to-end)
    latencia_proveedor_ms  integer,                       -- CA-02 (NULL si no se pudo medir)

    tokens_entrada         integer,                       -- CA-03 (NULL = el proveedor no los entrego)
    tokens_salida          integer,
    tokens_total           integer,
    tokens_origen          varchar(20) NOT NULL DEFAULT 'no_disponible',
        -- 'proveedor' = dato real | 'no_disponible' = no se inventa

    proveedor              varchar(50),                   -- CA-04 (quien respondio finalmente)
    modelo                 varchar(100),
    uso_failover           boolean     NOT NULL DEFAULT false,
    estado                 varchar(20) NOT NULL DEFAULT 'ok',
        -- ok | ok_failover | error | bloqueada
    error_mensaje          text,

    costo_usd              numeric(12,6),                 -- CA-05 (NULL si no es verificable)
    costo_origen           varchar(30) NOT NULL DEFAULT 'no_calculado',
        -- 'tarifa_verificada' | 'no_calculado'

    CONSTRAINT metricas_tokens_origen_chk CHECK (tokens_origen IN ('proveedor','no_disponible')),
    CONSTRAINT metricas_estado_chk        CHECK (estado IN ('ok','ok_failover','error','bloqueada')),
    CONSTRAINT metricas_costo_origen_chk  CHECK (costo_origen IN ('tarifa_verificada','no_calculado')),
    -- si no hay tokens reales, tampoco puede haber costo
    CONSTRAINT metricas_costo_requiere_tokens CHECK (costo_usd IS NULL OR tokens_origen = 'proveedor')
);
CREATE INDEX IF NOT EXISTS idx_metricas_created   ON public.metricas_consulta (created_at);
CREATE INDEX IF NOT EXISTS idx_metricas_proveedor ON public.metricas_consulta (proveedor);
CREATE INDEX IF NOT EXISTS idx_metricas_session   ON public.metricas_consulta (session_id);

-- 2) Tarifas verificadas (solo se llena con precios confirmados en la pagina oficial)
CREATE TABLE IF NOT EXISTS public.tarifas_modelo (
    id                 serial PRIMARY KEY,
    proveedor          varchar(50)  NOT NULL,
    modelo             varchar(100) NOT NULL,
    usd_por_m_entrada  numeric(12,6) NOT NULL,
    usd_por_m_salida   numeric(12,6) NOT NULL,
    fuente             text         NOT NULL,   -- URL donde se verifico
    verificado_el      date         NOT NULL,
    UNIQUE (proveedor, modelo)
);

-- 3) Vistas para el dashboard / observabilidad (CA-05)
CREATE OR REPLACE VIEW public.vw_metricas_consulta AS
SELECT id, correlation_id, session_id, execution_id, created_at,
       latencia_total_ms, latencia_proveedor_ms,
       tokens_entrada, tokens_salida, tokens_total, tokens_origen,
       proveedor, modelo, uso_failover, estado, costo_usd, costo_origen
FROM public.metricas_consulta;

CREATE OR REPLACE VIEW public.vw_metricas_diarias AS
SELECT date_trunc('day', created_at)::date AS dia,
       proveedor, modelo,
       count(*)                                         AS consultas,
       count(*) FILTER (WHERE uso_failover)             AS con_failover,
       count(*) FILTER (WHERE estado = 'error')         AS con_error,
       round(avg(latencia_total_ms))                    AS latencia_total_prom_ms,
       percentile_cont(0.95) WITHIN GROUP (ORDER BY latencia_total_ms) AS latencia_total_p95_ms,
       sum(tokens_total) FILTER (WHERE tokens_origen = 'proveedor')    AS tokens_reales,
       count(*) FILTER (WHERE tokens_origen = 'no_disponible')         AS sin_tokens,
       sum(costo_usd)                                   AS costo_usd_verificado
FROM public.metricas_consulta
GROUP BY 1, 2, 3;