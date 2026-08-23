import { Request, Response } from 'express';

import { env } from '../../../config/env';

type ChatRequestBody = {
  chatInput?: string;
};

export const chatController = {
  async handle(request: Request<unknown, unknown, ChatRequestBody>, response: Response) {
    const { chatInput } = request.body;

    if (typeof chatInput !== 'string' || chatInput.trim().length === 0) {
      response.status(400).json({ error: 'chatInput must be a non-empty string' });
      return;
    }

    if (!env.n8nWebhookUrl) {
      response.status(500).json({ error: 'N8N_WEBHOOK_URL is not configured' });
      return;
    }

    try {
      const webhookResponse = await fetch(env.n8nWebhookUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ chatInput })
      });

      const responseText = await webhookResponse.text();
      const contentType = webhookResponse.headers.get('content-type');

      if (contentType) {
        response.setHeader('content-type', contentType);
      }

      response.status(webhookResponse.status).send(responseText);
    } catch (error) {
      void error;
      response.status(500).json({ error: 'Failed to connect to n8n webhook' });
    }
  }
};