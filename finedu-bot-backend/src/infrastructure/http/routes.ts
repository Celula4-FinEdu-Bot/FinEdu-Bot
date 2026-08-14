import { Router } from 'express';

import { healthController } from './controllers/health.controller';

export function buildRoutes() {
  const router = Router();

  router.get('/health', healthController.getStatus);

  return router;
}
