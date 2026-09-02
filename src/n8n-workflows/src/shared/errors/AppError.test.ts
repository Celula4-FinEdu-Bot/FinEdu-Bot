import { AppError } from './AppError';

function test(nombre: string, prueba: () => void) {
  try {
    prueba();
    console.log(`✅ PASS: ${nombre}`);
  } catch (error) {
    console.error(`❌ FAIL: ${nombre}`);
    throw error;
  }
}

test('HU-12: debe registrar un error de actualización', () => {
  const error = new AppError(
    'Error durante la actualización de la fuente',
    500,
    {
      fuente: 'MEF',
      proceso: 'actualizacion',
      detalle: 'No fue posible obtener los datos',
    }
  );

  if (error.name !== 'AppError') {
    throw new Error('El nombre del error no es correcto');
  }

  if (error.statusCode !== 500) {
    throw new Error('El código de estado no es 500');
  }

  if (error.details === undefined) {
    throw new Error('El error no contiene detalles');
  }
});

test('HU-12: debe usar 400 como código por defecto', () => {
  const error = new AppError('Datos inválidos');

  if (error.statusCode !== 400) {
    throw new Error('El código por defecto no es 400');
  }
});