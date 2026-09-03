import dotenv from 'dotenv';

dotenv.config();

export const env = {
  nodeEnv: process.env.NODE_ENV ?? 'development',

  port: Number(process.env.PORT ?? 3000),

  apiPrefix: process.env.API_PREFIX ?? '/api',

  databaseUrl: process.env.DATABASE_URL ?? '',

  n8nWebhookUrl: process.env.N8N_WEBHOOK_URL ?? '',

  mefApiBaseUrl:
    process.env.MEF_API_BASE_URL ?? 'https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/',

  mefResourceId: process.env.MEF_RESOURCE_ID ?? '5f3b3cbe-3955-41cc-8662-1757ebb5cf53'
};
