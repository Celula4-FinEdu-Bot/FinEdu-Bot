import { Router } from 'express';

import { chatController } from './controllers/chat.controller';
import { healthController } from './controllers/health.controller';

export function buildRoutes() {
  const router = Router();

  router.get('/health', healthController.getStatus);
  router.post('/chat', chatController.handle);

  return router;
}
