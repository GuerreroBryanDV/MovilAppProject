namespace MovilAppProject.Utils
{
    // Sesión del usuario (login tipo banco). Vive solo en memoria: al cerrar la app hay que iniciar sesión otra vez.
    public static class Sesion
    {
        // Minutos sin usar la app antes de pedir el login de nuevo
        public const int MinutosInactividad = 2;

        // Id del usuario activo (0 = sin sesión)
        public static int UsuarioId { get; private set; }

        public static string NombreUsuario { get; private set; } = string.Empty;

        public static bool Autenticado { get; private set; }

        // true mientras se muestra la pantalla de login o de registro (evita abrirla dos veces)
        public static bool BloqueoVisible { get; set; }

        // false mientras la app está en segundo plano (lo actualiza MainActivity en Android)
        public static bool EnPrimerPlano { get; set; } = true;

        private static DateTime _ultimaActividad = DateTime.Now;

        public static void Iniciar(int usuarioId, string nombre)
        {
            UsuarioId = usuarioId;
            NombreUsuario = nombre;
            Autenticado = true;
            _ultimaActividad = DateTime.Now;
        }

        public static void Cerrar()
        {
            UsuarioId = 0;
            NombreUsuario = string.Empty;
            Autenticado = false;
        }

        // Se llama con cada toque en la pantalla
        public static void Tocar()
        {
            // Si la sesión ya expiró, un toque no la renueva: primero hay que volver a iniciar sesión
            if (Expirada())
            {
                return;
            }
            _ultimaActividad = DateTime.Now;
        }

        // Cierra la sesión (sin borrar al usuario) para exigir el login otra vez
        public static void Bloquear()
        {
            Autenticado = false;
        }

        // true si pasó el tiempo permitido sin actividad
        public static bool Expirada()
        {
            return Autenticado && DateTime.Now - _ultimaActividad > TimeSpan.FromMinutes(MinutosInactividad);
        }
    }
}