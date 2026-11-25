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
        private static string Servidor
        {
            get
            {
                if (DeviceInfo.Platform != DevicePlatform.Android)
                    return "http://localhost/ws_gastofacil/";
                if (DeviceInfo.DeviceType == DeviceType.Virtual)
                    return "http://10.0.2.2/ws_gastofacil/";
                return "http://" + IpComputadora + "/ws_gastofacil/";
            }
        }

        private static string Url => Servidor + "movimiento_ws.php";

        // PB27: usuarios del login (alta e inactivación = borrado lógico)
        private static string UrlUsuarios => Servidor + "usuario_ws.php";

        private static readonly HttpClient cliente = new() { Timeout = TimeSpan.FromSeconds(5) };

        // Una sola operación a la vez, para que no se crucen envíos y sincronización
        private static readonly SemaphoreSlim candado = new(1, 1);

        // Último resultado (para mostrar o depurar)
        public static string StatusMessage { get; private set; } = string.Empty;

        // ===== Envíos de una operación (se llaman desde vMovimiento) =====

        public static Task AgregarAsync(Movimiento m) =>
            EjecutarAsync(() => { EnviarDueno(m); Enviar(m, "POST", UidDe(m)); });

        public static Task ActualizarAsync(Movimiento m) =>
            EjecutarAsync(() => { EnviarDueno(m); Enviar(m, "PUT", UidDe(m)); });

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
                // PB27: uid de cada usuario local (los movimientos se envían con el uid de su dueño)
                List<Usuario> usuarios = App.MovimientoRepo.ListarUsuarios();
                Dictionary<int, string> uidPorId = usuarios.ToDictionary(u => u.Id, u => u.Uid);
                List<MovimientoWS>? servidor = await ListarServidorAsync();

                // XAMPP apagado o sin red: se intenta en la siguiente sincronización
                if (servidor == null)
                    return;

                Dictionary<int, MovimientoWS> enServidor = servidor.ToDictionary(s => s.codigo);
                HashSet<int> idsLocales = locales.Select(l => l.Id).ToHashSet();

                await Task.Run(() =>
                {
                    // PB27: primero los usuarios (activos e inactivos), porque los movimientos
                    // apuntan a ellos. Una cuenta inactivada queda en el servidor con activo = 0.
                    foreach (Usuario u in usuarios)
                        EnviarUsuario(u);
                    foreach (Movimiento m in locales)
                    {
                        string uid = uidPorId.TryGetValue(m.UsuarioId, out string? valor) ? valor : "";
                        if (!enServidor.TryGetValue(m.Id, out MovimientoWS? s))
                            Enviar(m, "POST", uid);
                        else if (!Igual(m, s, uid))
                            Enviar(m, "PUT", uid);
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
        private static NameValueCollection Parametros(Movimiento m, string uid)
        {
            NameValueCollection parametros = new();
            parametros.Add("codigo", m.Id.ToString());
            parametros.Add("tipo", m.Tipo.ToString());
            parametros.Add("monto", m.Monto.ToString("0.00", CultureInfo.InvariantCulture));
            parametros.Add("categoria_id", m.CategoriaId.ToString());
            parametros.Add("fecha", m.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            parametros.Add("nota", m.Nota ?? "");
            // PB27: vacío = movimiento anterior al login (en MySQL queda NULL)
            parametros.Add("usuario_uid", uid);
            return parametros;
        }

#pragma warning disable SYSLIB0014 // WebClient: es el que se usó en la Semana 6
        // Los errores (XAMPP apagado, sin red) se atrapan aquí mismo para que
        // Visual Studio no detenga la app y para que SQLite siga funcionando.
        private static bool Enviar(Movimiento m, string metodo, string uid)
        {
            try
            {
                NameValueCollection parametros = Parametros(m, uid);
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
        // PB27: POST del usuario (el servidor inserta o actualiza por uid).
        // No se envían la contraseña ni el PIN: se quedan cifrados en el teléfono.
        private static bool EnviarUsuario(Usuario u)
        {
            try
            {
                NameValueCollection parametros = new();
                parametros.Add("uid", u.Uid);
                parametros.Add("nombre", u.Nombre);
                parametros.Add("activo", u.Activo ? "1" : "0");
                parametros.Add("fecha_creacion", u.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                parametros.Add("fecha_baja", u.FechaBaja?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "");
                using WebClient web = new();
                web.UploadValues(UrlUsuarios, "POST", parametros);
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return false;
            }
        }
#pragma warning restore SYSLIB0014

        // PB27: uid del dueño de un movimiento ("" si es anterior al login)
        private static string UidDe(Movimiento m)
        {
            return App.MovimientoRepo.ListarUsuarios().FirstOrDefault(u => u.Id == m.UsuarioId)?.Uid ?? "";
        }

        // PB27: asegura que el dueño exista en el servidor antes de enviar su movimiento
        private static void EnviarDueno(Movimiento m)
        {
            Usuario? dueno = App.MovimientoRepo.ListarUsuarios().FirstOrDefault(u => u.Id == m.UsuarioId);
            if (dueno != null)
                EnviarUsuario(dueno);
        }

        // true si el movimiento de SQLite y el de MySQL tienen los mismos datos
        private static bool Igual(Movimiento m, MovimientoWS s, string uid)
        {
            return m.Tipo.ToString() == s.tipo
                && m.Monto == s.monto
                && m.CategoriaId == s.categoria_id
                && m.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) == s.fecha
                && (m.Nota ?? "") == (s.nota ?? "")
                && uid == (s.usuario_uid ?? "");
        }
    }
}
