import dotenv from 'dotenv';

dotenv.config();

export const env = {
  nodeEnv: process.env.NODE_ENV ?? 'development',
  port: Number(process.env.PORT ?? 3000),
  apiPrefix: process.env.API_PREFIX ?? '/api',
  databaseUrl: process.env.DATABASE_URL ?? '',
  n8nWebhookUrl: process.env.N8N_WEBHOOK_URL ?? ''
};
