import { Request, Response } from 'express';

const MEF_BASE_URL =
  process.env.MEF_API_BASE_URL ?? 'https://api.datosabiertos.mef.gob.pe/DatosAbiertos/v1/';

const MEF_RESOURCE_ID = process.env.MEF_RESOURCE_ID ?? '5f3b3cbe-3955-41cc-8662-1757ebb5cf53';

export const mefController = {
  async query(request: Request, response: Response) {
    try {
      const { pregunta, entidad, anio, departamento, limit } = request.body ?? {};

      if (typeof pregunta !== 'string' && typeof entidad !== 'string') {
        response.status(400).json({
          success: false,
          error: 'Debe enviar pregunta o entidad.'
        });
        return;
      }

      const params = new URLSearchParams();

      params.set('resource', MEF_RESOURCE_ID);

      if (entidad) {
        params.set('entidad', String(entidad));
      }

      if (anio) {
        params.set('anio', String(anio));
      }

      if (departamento) {
        params.set('departamento', String(departamento));
      }

      params.set('limit', String(Math.min(Number(limit ?? 20), 100)));

      const url = `${MEF_BASE_URL.replace(/\/$/, '')}` + `?${params.toString()}`;

      const mefResponse = await fetch(url, {
        method: 'GET',
        headers: {
          Accept: 'application/json'
        }
      });

      const text = await mefResponse.text();

      if (!mefResponse.ok) {
        response.status(mefResponse.status).json({
          success: false,
          error: 'MEF respondió con error.',
          details: text
        });
        return;
      }

      let data: unknown;

      try {
        data = JSON.parse(text);
      } catch {
        response.status(502).json({
          success: false,
          error: 'La respuesta del MEF no es JSON válido.'
        });
        return;
      }

      response.json({
        success: true,
        source: 'MEF',
        query: {
          pregunta: pregunta ?? null,
          entidad: entidad ?? null,
          anio: anio ?? null,
          departamento: departamento ?? null
        },
        data
      });
    } catch (error) {
      console.error('Error consultando MEF:', error);

      response.status(502).json({
        success: false,
        error: 'No fue posible consultar la API del MEF.'
      });
    }
  }
};
