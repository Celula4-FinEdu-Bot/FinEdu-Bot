import { Request, Response } from 'express';

import { env } from '../../../config/env';

export const healthController = {
  getStatus(_request: Request, response: Response) {
    response.status(200).json({
      service: 'finedu-bot-backend',
      status: 'ok',
      environment: env.nodeEnv,
      timestamp: new Date().toISOString()
    });
  }
};
