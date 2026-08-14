import cors from 'cors';
import express from 'express';
import helmet from 'helmet';

import { env } from '../config/env';
import { errorHandler } from '../infrastructure/http/middlewares/error-handler';
import { notFoundHandler } from '../infrastructure/http/middlewares/not-found';
import { buildRoutes } from '../infrastructure/http/routes';

export function createApp() {
  const app = express();

  app.use(helmet());
  app.use(cors());
  app.use(express.json());

  app.use(env.apiPrefix, buildRoutes());
  app.use(notFoundHandler);
  app.use(errorHandler);

  return app;
}
