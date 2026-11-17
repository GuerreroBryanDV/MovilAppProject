using System.Text;
using System.Text.Json.Nodes;

namespace MovilAppProject.Services
{
    // Respuesta del servicio de IA para la vista
    public class ResultadoIA
    {
        // true si la IA respondió
        public bool Exito { get; set; }

        // Texto de la IA (solo si Exito)
        public string Texto { get; set; } = string.Empty;

        // Mensaje para el usuario cuando algo falla (V24, V26...)
        public string Mensaje { get; set; } = string.Empty;
    }

    // PB21 · MAP-21 · Servicio de conexión con la IA (RNF01, RNF06).
    // Usa la API de Google Gemini (modelo Flash-Lite, plan gratuito, D22).
    // V27 / RNF06: solo envía los totales por grupo y los porcentajes de la regla del mes;
    // nunca notas, montos individuales ni datos personales.
    // La clave NO está en el código: se lee del archivo Resources/Raw/.env, que no se sube a Git (D23).
    // Lo usan vRecomendaciones (PB22) y la pregunta a la IA (PB23).
    public class IAService
    {
        // Mensajes de la hoja Validaciones
        public const string MensajeSinConexion = "Sin conexión a internet: las recomendaciones necesitan conexión.";               // V24
        public const string MensajeSinDatos = "Registra al menos un ingreso y un gasto este mes para recibir recomendaciones."; // V24 (PB22)
        public const string MensajeSinRespuesta = "La IA no respondió. Inténtalo de nuevo.";                                       // V26

        // Mensajes nuevos (no están en la hoja Validaciones; documentados en D23)
        public const string MensajeSinClave = "Falta configurar la clave de la IA (archivo .env).";
        public const string MensajeClaveInvalida = "La clave de la IA no es válida. Revisa el archivo .env.";
        public const string MensajeLimite = "Se alcanzó el límite de consultas a la IA por hoy. Inténtalo más tarde.";

        // Claves del archivo .env
        const string ClaveApi = "GEMINI_API_KEY";
        const string ClaveModelo = "GEMINI_MODEL";

        // Modelo por defecto si el .env no indica otro (estable a nov. 2026)
        const string modeloPorDefecto = "gemini-3.5-flash-lite";
        const string urlBase = "https://generativelanguage.googleapis.com/v1beta/models/";

        // V26: si no hay respuesta en 30 s se cancela
        static readonly TimeSpan tiempoMaximo = TimeSpan.FromSeconds(30);

        // Un solo HttpClient para toda la app (el tiempo máximo se controla con CancellationToken)
        private readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };

        // Valores leídos del .env (se leen una sola vez)
        private Dictionary<string, string>? _config;

        // Detalle técnico del último error (para depurar; no se muestra al usuario)
        public string StatusMessage { get; set; } = string.Empty;

        // Instrucciones fijas para la IA (no llevan datos del usuario)
        const string instruccionesBase =
            "Eres el asistente de GastoFácil, una app de control de gastos personales en Ecuador (moneda USD). " +
            "Recibes solo totales agregados del mes por grupo de la regla de presupuesto. " +
            "Necesidades y Lujos son un tope máximo: pasarse es malo. Ahorro e inversión es un mínimo: quedarse corto es malo. " +
            "Responde en español, con frases cortas y concretas. No inventes datos que no estén en el resumen. " +
            "No des asesoría financiera profesional ni recomiendes bancos, inversiones o productos financieros específicos. " +
            "No uses Markdown ni asteriscos.";

        // ---------------- Uso desde las vistas ----------------

        // V24 (PB22): hacen falta al menos un ingreso y un gasto en el mes
        public static bool HayDatosSuficientes(ResultadoRegla regla)
        {
            return regla.HayIngresos && regla.Grupos.Any(g => g.Gastado > 0);
        }

        // PB22: recomendaciones del mes según la regla
        public Task<ResultadoIA> ObtenerRecomendacionesAsync(ResultadoRegla regla)
        {
            string instrucciones = instruccionesBase +
                " Da entre 3 y 4 recomendaciones para mejorar los hábitos de ahorro del mes, una por línea, cada una empezando con '• '.";

            return EnviarAsync(instrucciones, ResumenAgregado(regla));
        }

        // PB23: pregunta puntual del usuario (la validación V25 la hace la vista)
        public Task<ResultadoIA> PreguntarAsync(string pregunta, ResultadoRegla regla)
        {
            string instrucciones = instruccionesBase +
                " Responde la pregunta del usuario en máximo 120 palabras, usando el resumen del mes si sirve.";

            string mensaje = ResumenAgregado(regla) + "\n\nPregunta del usuario: " + pregunta.Trim();
            return EnviarAsync(instrucciones, mensaje);
        }

        // V27 / RNF06: lo único que sale del teléfono. Totales por grupo y porcentajes de la regla.
        // Ejemplo:
        //   Regla del mes: perfil 50/40/10.
        //   - Necesidades: meta 50 %, real 40 %, gastado USD 400,00, estado En meta.
        public static string ResumenAgregado(ResultadoRegla regla)
        {
            StringBuilder texto = new();
            texto.AppendLine("Regla del mes: perfil "
                             + regla.Perfil.PorcentajeNecesidades + "/"
                             + regla.Perfil.PorcentajeAhorroInversion + "/"
                             + regla.Perfil.PorcentajeLujos + ".");

            if (!regla.HayIngresos)
            {
                texto.AppendLine("Este mes no hay ingresos registrados, así que no se pueden calcular los porcentajes.");
                return texto.ToString();
            }

            foreach (ResumenGrupo grupo in regla.Grupos)
            {
                texto.AppendLine("- " + grupo.Nombre
                                 + ": meta " + grupo.PorcentajeMeta + " %"
                                 + ", real " + grupo.PorcentajeReal.ToString("N0") + " %"
                                 + ", gastado USD " + grupo.Gastado.ToString("N2")
                                 + ", estado " + grupo.TextoSemaforo + ".");
            }

            return texto.ToString();
        }

        // ---------------- Llamada a Gemini ----------------

        private async Task<ResultadoIA> EnviarAsync(string instrucciones, string mensajeUsuario)
        {
            // V24: sin internet no se intenta; el resto de la app sigue funcionando (RNF01)
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                return Fallo(MensajeSinConexion, "Sin conexión");
            }

            string clave = await LeerValorAsync(ClaveApi);
            if (string.IsNullOrWhiteSpace(clave))
            {
                return Fallo(MensajeSinClave, "No hay " + ClaveApi + " en el .env");
            }

            string modelo = await LeerValorAsync(ClaveModelo);
            if (string.IsNullOrWhiteSpace(modelo))
            {
                modelo = modeloPorDefecto;
            }

            JsonObject cuerpo = new()
            {
                ["system_instruction"] = new JsonObject
                {
                    ["parts"] = new JsonArray(new JsonObject { ["text"] = instrucciones })
                },
                ["contents"] = new JsonArray(new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray(new JsonObject { ["text"] = mensajeUsuario })
                }),
                ["generationConfig"] = new JsonObject { ["temperature"] = 0.4 }
            };

            try
            {
                using CancellationTokenSource limite = new(tiempoMaximo);
                using HttpRequestMessage solicitud = new(HttpMethod.Post, urlBase + modelo + ":generateContent");

                // La clave va en el encabezado, no en la URL (no queda en registros de la URL)
                solicitud.Headers.Add("x-goog-api-key", clave);
                solicitud.Content = new StringContent(cuerpo.ToJsonString(), Encoding.UTF8, "application/json");

                using HttpResponseMessage respuesta = await _http.SendAsync(solicitud, limite.Token);
                string json = await respuesta.Content.ReadAsStringAsync(limite.Token);

                if (!respuesta.IsSuccessStatusCode)
                {
                    int codigo = (int)respuesta.StatusCode;
                    string detalle = "HTTP " + codigo;

                    if (codigo == 400 || codigo == 401 || codigo == 403)
                    {
                        return Fallo(MensajeClaveInvalida, detalle);
                    }

                    if (codigo == 429)
                    {
                        return Fallo(MensajeLimite, detalle);
                    }

                    return Fallo(MensajeSinRespuesta, detalle);
                }

                string texto = LeerTexto(json);
                if (string.IsNullOrWhiteSpace(texto))
                {
                    return Fallo(MensajeSinRespuesta, "Respuesta vacía");
                }

                StatusMessage = string.Empty;
                return new ResultadoIA { Exito = true, Texto = texto.Trim() };
            }
            catch (OperationCanceledException)
            {
                // V26: pasaron 30 s sin respuesta
                return Fallo(MensajeSinRespuesta, "Tiempo agotado (30 s)");
            }
            catch (HttpRequestException ex)
            {
                // La red se cayó en medio de la consulta
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    return Fallo(MensajeSinConexion, ex.Message);
                }

                return Fallo(MensajeSinRespuesta, ex.Message);
            }
            catch (Exception ex)
            {
                return Fallo(MensajeSinRespuesta, ex.Message);
            }
        }

        // Une los textos de candidates[0].content.parts[]
        private static string LeerTexto(string json)
        {
            JsonNode? raiz = JsonNode.Parse(json);
            JsonArray? partes = raiz?["candidates"]?[0]?["content"]?["parts"]?.AsArray();
            if (partes is null)
            {
                return string.Empty;
            }

            StringBuilder texto = new();
            foreach (JsonNode? parte in partes)
            {
                string? fragmento = parte?["text"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(fragmento))
                {
                    texto.Append(fragmento);
                }
            }

            return texto.ToString();
        }

        private ResultadoIA Fallo(string mensaje, string detalle)
        {
            StatusMessage = detalle;
            return new ResultadoIA { Exito = false, Mensaje = mensaje };
        }

        // ---------------- Archivo .env (D23) ----------------

        // Lee una clave del .env empaquetado con la app (Resources/Raw/.env).
        // Formato: una línea por valor, CLAVE=valor; las líneas con # son comentarios.
        private async Task<string> LeerValorAsync(string nombre)
        {
            if (_config is null)
            {
                _config = await CargarEnvAsync();
            }

            return _config.TryGetValue(nombre, out string? valor) ? valor : string.Empty;
        }

        private static async Task<Dictionary<string, string>> CargarEnvAsync()
        {
            Dictionary<string, string> valores = new();

            try
            {
                using Stream archivo = await FileSystem.OpenAppPackageFileAsync(".env");
                using StreamReader lector = new(archivo);

                string? linea;
                while ((linea = await lector.ReadLineAsync()) is not null)
                {
                    linea = linea.Trim();
                    if (linea.Length == 0 || linea.StartsWith('#'))
                    {
                        continue;
                    }

                    int igual = linea.IndexOf('=');
                    if (igual <= 0)
                    {
                        continue;
                    }

                    string clave = linea.Substring(0, igual).Trim();
                    string valor = linea.Substring(igual + 1).Trim().Trim('"', '\'');
                    valores[clave] = valor;
                }
            }
            catch
            {
                // Sin .env la app funciona igual; la IA responde con MensajeSinClave
            }

            return valores;
        }
    }
}
