import { NextFunction, Request, Response } from 'express';

import { AppError } from '../../../shared/errors/AppError';

export function errorHandler(
  error: Error,
  _request: Request,
  response: Response,
  _next: NextFunction
) {
  if (error instanceof AppError) {
    return response.status(error.statusCode).json({
      message: error.message,
      details: error.details ?? null
    });
  }

  return response.status(500).json({
    message: 'Internal server error'
  });
}
