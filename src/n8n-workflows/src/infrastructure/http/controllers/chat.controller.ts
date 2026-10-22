import { Request, Response } from 'express';

import { env } from '../../../config/env';

//según el flujo de trabajo se usa $json.body.sessionId y esto se enviará a n8n
type ChatRequestBody = {
  chatInput?: string;
  sessionId?: string;
};

export const chatController = {
  async handle(request: Request<unknown, unknown, ChatRequestBody>, response: Response) {
    const { chatInput, sessionId } = request.body;

    if (typeof chatInput !== 'string' || chatInput.trim().length === 0) {
      response.status(400).json({
        success: false,
        error: 'chatInput must be a non-empty string'
      });
      return;
    }

    if (typeof sessionId !== 'string' || sessionId.trim().length === 0) {
      response.status(400).json({
        success: false,
        error: 'sessionId must be a non-empty string'
      });
      return;
    }

    if (!env.n8nWebhookUrl) {
      response.status(500).json({
        success: false,
        error: 'N8N_WEBHOOK_URL is not configured'
      });
      return;
    }

    try {
      const webhookResponse = await fetch(env.n8nWebhookUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          chatInput: chatInput.trim(),
          sessionId: sessionId.trim()
        })
      });

      const responseText = await webhookResponse.text();

      response.status(webhookResponse.status);

      const contentType = webhookResponse.headers.get('content-type');

      if (contentType) {
        response.setHeader('content-type', contentType);
      }

      response.send(responseText);
    } catch (error) {
      console.error('Error connecting to n8n:', error);

      response.status(502).json({
        success: false,
        error: 'Failed to connect to n8n webhook'
      });
    }
  }
};
