import { Request, Response } from 'express';

import { env } from '../../../config/env';

type OeceRequestBody = {
  chatInput?: string;
  sessionId?: string;
};

const MAX_ATTEMPTS = 3;
const REQUEST_TIMEOUT_MS = 30_000;

function failure(message: string, intent = 'oece_error') {
  return {
    success: false,
    intent,
    message,
    source: 'OECE',
    total: 0,
    records: [],
    fallbackUsed: true
  };
}

export const oeceController = {
  async query(
    request: Request<unknown, unknown, OeceRequestBody>,
    response: Response
  ) {
    const { chatInput, sessionId } = request.body ?? {};

    if (typeof chatInput !== 'string' || chatInput.trim().length === 0) {
      response.status(400).json(failure('Debe enviar una pregunta OECE en chatInput.', 'invalid_request'));
      return;
    }

    if (typeof sessionId !== 'string' || sessionId.trim().length === 0) {
      response.status(400).json(failure('Debe enviar sessionId.', 'invalid_request'));
      return;
    }

    if (!env.n8nOeceWebhookUrl) {
      response.status(503).json(
        failure('La URL del webhook OECE no está configurada en el backend.', 'oece_not_configured')
      );
      return;
    }

    let lastError: unknown;

    for (let attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
      try {
        const webhookResponse = await fetch(env.n8nOeceWebhookUrl, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json'
          },
          body: JSON.stringify({
            chatInput: chatInput.trim(),
            sessionId: sessionId.trim()
          }),
          signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS)
        });

        const responseText = await webhookResponse.text();
        const retryable =
          webhookResponse.status === 408 ||
          webhookResponse.status === 429 ||
          webhookResponse.status >= 500;

        if (retryable && attempt < MAX_ATTEMPTS) {
          console.warn(
            `Webhook OECE respondió HTTP ${webhookResponse.status}; reintento ${attempt + 1}/${MAX_ATTEMPTS}.`
          );
          await new Promise((resolve) => setTimeout(resolve, 250 * attempt));
          continue;
        }

        if (responseText.trim().length === 0) {
          response.status(502).json(
            failure('El workflow OECE devolvió una respuesta vacía.', 'oece_empty_response')
          );
          return;
        }

        const contentType = webhookResponse.headers.get('content-type');
        response.status(webhookResponse.status);
        if (contentType) response.setHeader('content-type', contentType);
        response.send(responseText);
        return;
      } catch (error) {
        lastError = error;
        console.warn(`Error al llamar al webhook OECE (intento ${attempt}/${MAX_ATTEMPTS}).`, error);
        if (attempt < MAX_ATTEMPTS) {
          await new Promise((resolve) => setTimeout(resolve, 250 * attempt));
        }
      }
    }

    console.error('No fue posible completar la consulta OECE después de reintentos:', lastError);
    response.status(502).json(
      failure(
        'No fue posible completar la consulta OECE. Comprueba que el workflow esté activo y vuelve a intentarlo.',
        'oece_unavailable'
      )
    );
  }
};

