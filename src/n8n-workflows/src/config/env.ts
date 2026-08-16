import dotenv from 'dotenv';

dotenv.config();

const requiredEnvVars = ['PORT', 'API_PREFIX'] as const;

for (const key of requiredEnvVars) {
  if (!process.env[key]) {
    throw new Error(`Missing required environment variable: ${key}`);
  }
}

export const env = {
  nodeEnv: process.env.NODE_ENV ?? 'development',
  port: Number(process.env.PORT ?? 3001),
  apiPrefix: process.env.API_PREFIX ?? '/api/v1',
  databaseUrl: process.env.DATABASE_URL ?? '',
  n8nWebhookBaseUrl: process.env.N8N_WEBHOOK_BASE_URL ?? ''
};
