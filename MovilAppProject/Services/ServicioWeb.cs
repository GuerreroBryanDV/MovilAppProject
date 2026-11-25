using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using MovilAppProject.Models;
using Newtonsoft.Json;

namespace MovilAppProject.Services
{
    // PB25 · MAP-25 · Respaldo de la base SQLite en MySQL (XAMPP + PHP).
    // Mismo patrón que la Semana 6 (vEstudiante / vAgregarEstudiante / vActualizarEliminar):
    //   GET    -> HttpClient.GetStringAsync + JsonConvert
    //   POST   -> WebClient.UploadValues(url, "POST", parametros)
    //   PUT    -> WebClient.UploadValues(url + "?codigo=...", "PUT", parametros)
    //   DELETE -> WebClient.UploadValues(url + "?codigo=...", "DELETE", parametros)
    // SQLite sigue siendo la base principal: si XAMPP está apagado o no hay red,
    // la app funciona igual y lo pendiente se envía en la siguiente sincronización.
    public static class ServicioWeb
    {
        // IP de la computadora que tiene XAMPP, para probar en un celular Android real.
        // Se ve en la PC con el comando ipconfig ("Dirección IPv4" del Wi-Fi).
        // El celular y la PC deben estar conectados al mismo Wi-Fi.
        private const string IpComputadora = "192.168.1.100";

        // Windows: localhost
        // Emulador de Android: 10.0.2.2 (es el "localhost" de la computadora)
        // Celular Android real: la IP de la computadora
        private static string Url
        {
            get
            {
                if (DeviceInfo.Platform != DevicePlatform.Android)
                    return "http://localhost/ws_gastofacil/movimiento_ws.php";

                if (DeviceInfo.DeviceType == DeviceType.Virtual)
                    return "http://10.0.2.2/ws_gastofacil/movimiento_ws.php";

                return "http://" + IpComputadora + "/ws_gastofacil/movimiento_ws.php";
            }
        }

        private static readonly HttpClient cliente = new() { Timeout = TimeSpan.FromSeconds(5) };

        // Una sola operación a la vez, para que no se crucen envíos y sincronización
        private static readonly SemaphoreSlim candado = new(1, 1);

        // Último resultado (para mostrar o depurar)
        public static string StatusMessage { get; private set; } = string.Empty;

        // ===== Envíos de una operación (se llaman desde vMovimiento) =====

        public static Task AgregarAsync(Movimiento m) =>
            EjecutarAsync(() => { Enviar(m, "POST"); });

        public static Task ActualizarAsync(Movimiento m) =>
            EjecutarAsync(() => { Enviar(m, "PUT"); });

        public static Task EliminarAsync(int id) =>
            EjecutarAsync(() => { Eliminar(id); });

        // ===== Sincronización completa (se llama al abrir vInicio) =====
        // Deja MySQL igual que SQLite: inserta lo que falta, actualiza lo que
        // cambió y borra lo que ya no existe en el teléfono.
        public static async Task SincronizarAsync()
        {
            await candado.WaitAsync();
            try
            {
                List<Movimiento> locales = App.MovimientoRepo.ListarTodos();
                List<MovimientoWS>? servidor = await ListarServidorAsync();

                // XAMPP apagado o sin red: se intenta en la siguiente sincronización
                if (servidor == null)
                    return;

                Dictionary<int, MovimientoWS> enServidor = servidor.ToDictionary(s => s.codigo);
                HashSet<int> idsLocales = locales.Select(l => l.Id).ToHashSet();

                await Task.Run(() =>
                {
                    foreach (Movimiento m in locales)
                    {
                        if (!enServidor.TryGetValue(m.Id, out MovimientoWS? s))
                            Enviar(m, "POST");
                        else if (!Igual(m, s))
                            Enviar(m, "PUT");
                    }

                    foreach (MovimientoWS s in servidor)
                    {
                        if (!idsLocales.Contains(s.codigo))
                            Eliminar(s.codigo);
                    }
                });

                StatusMessage = "Sincronizado con el servidor";
            }
            catch (Exception ex)
            {
                // Sin conexión: no se muestra error, la app sigue con SQLite
                StatusMessage = "Error: " + ex.Message;
            }
            finally
            {
                candado.Release();
            }
        }

        // GET: lista de movimientos guardados en MySQL
        // Devuelve null si no se pudo conectar
        public static async Task<List<MovimientoWS>?> ListarServidorAsync()
        {
            try
            {
                string contenido = await cliente.GetStringAsync(Url);
                return JsonConvert.DeserializeObject<List<MovimientoWS>>(contenido) ?? new List<MovimientoWS>();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return null;
            }
        }

        // ---------------- Auxiliares ----------------

        private static async Task EjecutarAsync(Action accion)
        {
            await candado.WaitAsync();
            try
            {
                await Task.Run(accion);
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            finally
            {
                candado.Release();
            }
        }

        // Datos del movimiento con el formato de MySQL (punto decimal y fecha aaaa-MM-dd)
        private static NameValueCollection Parametros(Movimiento m)
        {
            NameValueCollection parametros = new();
            parametros.Add("codigo", m.Id.ToString());
            parametros.Add("tipo", m.Tipo.ToString());
            parametros.Add("monto", m.Monto.ToString("0.00", CultureInfo.InvariantCulture));
            parametros.Add("categoria_id", m.CategoriaId.ToString());
            parametros.Add("fecha", m.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            parametros.Add("nota", m.Nota ?? "");
            return parametros;
        }

#pragma warning disable SYSLIB0014 // WebClient: es el que se usó en la Semana 6
        // Los errores (XAMPP apagado, sin red) se atrapan aquí mismo para que
        // Visual Studio no detenga la app y para que SQLite siga funcionando.
        private static bool Enviar(Movimiento m, string metodo)
        {
            try
            {
                NameValueCollection parametros = Parametros(m);
                using WebClient web = new();

                if (metodo == "POST")
                {
                    web.UploadValues(Url, "POST", parametros);
                }
                else
                {
                    // PUT: igual que en vActualizarEliminar, los datos van en la URL
                    string query = string.Join("&", parametros.AllKeys.Select(k =>
                        k + "=" + Uri.EscapeDataString(parametros[k] ?? "")));
                    web.UploadValues(Url + "?" + query, "PUT", new NameValueCollection());
                }

                StatusMessage = "Enviado al servidor";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return false;
            }
        }

        private static bool Eliminar(int id)
        {
            try
            {
                using WebClient web = new();
                web.UploadValues(Url + "?codigo=" + id, "DELETE", new NameValueCollection());
                StatusMessage = "Eliminado en el servidor";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return false;
            }
        }
#pragma warning restore SYSLIB0014

        // true si el movimiento de SQLite y el de MySQL tienen los mismos datos
        private static bool Igual(Movimiento m, MovimientoWS s)
        {
            return m.Tipo.ToString() == s.tipo
                && m.Monto == s.monto
                && m.CategoriaId == s.categoria_id
                && m.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) == s.fecha
                && (m.Nota ?? "") == (s.nota ?? "");
        }
    }
}
