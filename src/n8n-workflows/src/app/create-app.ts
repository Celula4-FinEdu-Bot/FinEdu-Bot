import cors from 'cors';
import express from 'express';
import helmet from 'helmet';

import { env } from '../config/env';
import { errorHandler } from '../infrastructure/http/middlewares/error-handler';
import { notFoundHandler } from '../infrastructure/http/middlewares/not-found';
import { buildRoutes } from '../infrastructure/http/routes';

//const frontendDir = path.resolve(__dirname, '../../frontend');
//app.use(express.static(frontendDir));
//Según el flujo de trabajo node no debe intentar servir el blazor frontend ya que este es independiente.

export function createApp() {
  const app = express();

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

  app.use(
    cors({
      origin: true,
      credentials: true
    })
  );

  app.use(express.json());

  app.use(env.apiPrefix, buildRoutes());

  app.use(notFoundHandler);
  app.use(errorHandler);

  return app;
}
