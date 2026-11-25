using MovilAppProject.Utils;

namespace MovilAppProject
{
    public partial class AppShell : Shell
    {
        // Cada cuánto se revisa si pasó el tiempo de inactividad
        const int segundosRevision = 15;

        public AppShell()
        {
            InitializeComponent();
            // Las rutas de páginas que NO son pestañas (ej. vMovimiento)
            // se registran aquí con Routing.RegisterRoute en sus propias HU.

            // Cuando el Shell ya está cargado se exige el login
            Loaded += AppShell_Loaded;
            // Cambiar de pestaña cuenta como actividad (útil también fuera de Android)
            Navigated += (s, e) => Sesion.Tocar();
        }

        // Login tipo banco: al abrir la app siempre se pide (registro si no hay usuario)
        // y se vuelve a pedir tras Sesion.MinutosInactividad minutos sin actividad.
        private async void AppShell_Loaded(object? sender, EventArgs e)
        {
            IniciarTemporizador();
            await ExigirLoginAsync();
        }

        private void IniciarTemporizador()
        {
            IDispatcherTimer temporizador = Dispatcher.CreateTimer();
            temporizador.Interval = TimeSpan.FromSeconds(segundosRevision);
            temporizador.Tick += (s, e) => RevisarInactividad();
            temporizador.Start();
        }

        // Lo llama App cuando la app vuelve de segundo plano
        public void AlReanudar()
        {
            RevisarInactividad();
        }

        // true mientras se está abriendo la pantalla de login (evita abrirla dos veces)
        bool abriendoLogin = false;

        private async void RevisarInactividad()
        {
            // Pasó el tiempo: la sesión se cierra aunque la app esté en segundo plano
            if (Sesion.Expirada())
            {
                Sesion.Bloquear();
            }
            // Android no deja mostrar pantallas en segundo plano: el login se abre al volver (OnResume)
            if (!Sesion.EnPrimerPlano)
            {
                return;
            }
            // Sesión cerrada de un usuario que ya había entrado: se pide el login otra vez
            if (!Sesion.Autenticado && Sesion.UsuarioId != 0)
            {
                await ExigirLoginAsync();
            }
        }

        // true si el login o el registro ya están en pantalla
        private bool LoginVisible()
        {
            return Navigation.ModalStack.Any(p => p is Views.vDesbloqueo || p is Views.vRegistro);
        }

        // Muestra el login (o el registro si todavía no hay usuario) encima de todo
        private async Task ExigirLoginAsync()
        {
            if (Sesion.Autenticado || abriendoLogin || LoginVisible())
            {
                return;
            }
            abriendoLogin = true;
            Sesion.BloqueoVisible = true;
            try
            {
                bool hayUsuario = App.MovimientoRepo.ObtenerUsuarioActivo() != null;
                Page pagina = hayUsuario ? new Views.vDesbloqueo() : new Views.vRegistro();
                await Navigation.PushModalAsync(pagina, false);
            }
            catch (Exception ex)
            {
                // Si no se pudo abrir, se vuelve a intentar en la siguiente revisión
                Sesion.BloqueoVisible = false;
                System.Diagnostics.Debug.WriteLine("Login: " + ex);
            }
            finally
            {
                abriendoLogin = false;
            }
        }    }
}