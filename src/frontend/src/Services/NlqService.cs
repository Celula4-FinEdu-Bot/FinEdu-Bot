using src.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace src.Services;

public sealed class NlqService
{
    private readonly MefService _mefService;

    public NlqService(MefService mefService)
    {
        _mefService = mefService;
    }

    public async Task<NlqResponse> ProcesarAsync(
        string pregunta,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new NlqResponse
            {
                Success = false,
                Message = "Escribe una consulta. Ejemplo: ¿Cuál fue la evolución del presupuesto entre 2012 y 2016?"
            };
        }

        try 
        {
            using var client = new HttpClient();
            var payload = JsonSerializer.Serialize(new { mensaje = pregunta, accion = "consultar_agente" });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            // Se envía la petición a tu entorno local
            var n8nResponse = await client.PostAsync("http://host.docker.internal:5678/webhook-test/ai-agent-orchestrator", content, cancellationToken);
            
            if (n8nResponse.IsSuccessStatusCode) 
            {
                return new NlqResponse 
                { 
                    Success = true, 
                    Intent = "N8nOrquestador", 
                    Message = "¡Conexión exitosa! Los datos llegaron a n8n." 
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error conectando a n8n: " + ex.Message);
        }

        var texto = pregunta.Trim().ToLowerInvariant();

        if (ContieneAlguno(texto, "evolución",         
        "evolucion del presupuesto",
                "presupuesto por año",
                "presupuesto anual",
                "ejecución por año",
                "ejecucion por año"))
    {
        var entidad = ExtraerEntidad(texto);
        var datos = await _mefService.ObtenerEvolucionAsync(entidad, cancellationToken);
        
        // Convertir a PresupuestoResumen
        var resumen = _mefService.ConvertirEvolucionAResumen(datos);

        return new NlqResponse
        {
            Success = true,
            Intent = "EvolucionPresupuesto",
            Message = "...",
            Presupuestos = resumen  // Usa la propiedad Presupuestos en lugar de Evolucion
        };
    }

        if (ContieneAlguno(
                texto,
                "proyectos",
                "proyecto",
                "categorías",
                "categorias",
                "mayor presupuesto",
                "mayor ejecución",
                "mayor ejecucion"))
        {
            return new NlqResponse
            {
                Success = true,
                Intent = "Proyectos",
                Message =
                    "La consulta corresponde al análisis de proyectos y categorías presupuestarias."
            };
        }

        if (ContieneAlguno(
                texto,
                "contrataciones",
                "contratos",
                "licitaciones",
                "oece"))
        {
            return new NlqResponse
            {
                Success = true,
                Intent = "Contrataciones",
                Message =
                    "Esta consulta debe ser atendida por el microfrontend OECE."
            };
        }

        return new NlqResponse
        {
            Success = false,
            Intent = "NoReconocido",
            Message =
                "No pude identificar la consulta. Prueba con: " +
                "\"¿Cuál fue la evolución del presupuesto entre 2012 y 2016?\""
        };
    }

    private static bool ContieneAlguno(
        string texto,
        params string[] valores)
    {
        return valores.Any(texto.Contains);
    }

    private static string? ExtraerEntidad(string texto)
    {
        var marcadores = new[] { 
            "municipalidad ", 
            "entidad ",
            "ejecutora ",
            "pliego "
        };

        foreach (var marcador in marcadores)
        {
            var index = texto.IndexOf(
                marcador,
                StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                var entidad = texto[(index + marcador.Length)..];
                
                // Buscar hasta el final de la frase
                var endIndex = entidad.IndexOfAny(new[] { '.', '?', ';', ',' });
                if (endIndex > 0)
                {
                    entidad = entidad[..endIndex];
                }

                return string.IsNullOrWhiteSpace(entidad) 
                    ? null 
                    : entidad.Trim();
            }
        }

        // Si no encuentra marcador, intenta extraer palabras clave
        var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var palabra in palabras)
        {
            if (palabra.Length > 3 && !EsPalabraComun(palabra))
            {
                return palabra.Trim('.', '?', ',', ';');
            }
        }

        return null;
    }

    private static bool EsPalabraComun(string palabra)
    {
        var comunes = new[] { 
            "evolución", "evolucion", "presupuesto", "año", "años", "cual", "cuál",
            "fue", "son", "los", "las", "del", "de", "el", "la", "por", "para",
            "entre", "desde", "hasta", "municipal", "entidad", "pliego"
        };
        return comunes.Contains(palabra.ToLowerInvariant());
    }
}