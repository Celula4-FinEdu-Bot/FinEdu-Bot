import { Router } from 'express';

import { chatController } from './controllers/chat.controller';
import { healthController } from './controllers/health.controller';
import { mefController } from './controllers/mef.controller';

export function buildRoutes() {
  const router = Router();

  router.get('/health', healthController.getStatus);

  router.post('/chat', chatController.handle);

  router.post('/mef/query', mefController.query);

  return router;
}
