import cors from 'cors';
import express from 'express';
import helmet from 'helmet';
import path from 'node:path';

import { env } from '../config/env';
import { errorHandler } from '../infrastructure/http/middlewares/error-handler';
import { notFoundHandler } from '../infrastructure/http/middlewares/not-found';
import { buildRoutes } from '../infrastructure/http/routes';

export function createApp() {
  const app = express();
  const frontendDir = path.resolve(__dirname, '../../../src/frontend');

  app.use(
    helmet({
      contentSecurityPolicy: {
        directives: {
          defaultSrc: ["'self'"],
          scriptSrc: ["'self'", "'unsafe-inline'"],
          styleSrc: ["'self'", "'unsafe-inline'"],
          imgSrc: ["'self'", 'data:'],
          connectSrc: ["'self'", 'http:', 'https:']
        }
      }
    })
  );
  app.use(cors());
  app.use(express.json());
  app.use(express.static(frontendDir));

  app.use(env.apiPrefix, buildRoutes());
  app.use(notFoundHandler);
  app.use(errorHandler);

  return app;
}
