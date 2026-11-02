import { env } from '../../config/env';

let missingSecretWarned = false;

//agrega la cabecera de autenticación de servicio (HU-44) que n8n valida con Header Auth
export function buildN8nHeaders(base: Record<string, string> = {}): Record<string, string> {
  const headers: Record<string, string> = { ...base };

  if (env.n8nWebhookAuthSecret) {
    headers[env.n8nWebhookAuthHeader] = env.n8nWebhookAuthSecret;
  } else if (!missingSecretWarned) {
    missingSecretWarned = true;
    console.warn(
      'N8N_WEBHOOK_AUTH_SECRET no está configurado: las peticiones a n8n se enviarán sin cabecera de autenticación'
    );
  }

  return headers;
}
