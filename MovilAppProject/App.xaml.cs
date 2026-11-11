using System.Globalization;
using MovilAppProject.Repositories;

namespace MovilAppProject
{
    // Clase principal de la aplicación: es lo primero que se ejecuta.
    public partial class App : Application
    {
        // Repositorio de SQLite disponible para todas las vistas (PB03 / PB06)
        public static MovimientoRepository MovimientoRepo { get; private set; } = null!;

        public App(MovimientoRepository repo)
        {
            MovimientoRepo = repo;
            // PB04 · RNF09: textos en español, moneda USD y fechas dd/MM/aaaa.
            // La cultura es-EC (Ecuador) usa dólar ($), coma decimal (25,50)
            // y punto de miles ($99.999,99), igual que los mensajes del Backlog.
            var cultura = new CultureInfo("es-EC");
            cultura.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;
            CultureInfo.CurrentCulture = cultura;
            CultureInfo.CurrentUICulture = cultura;

            // Los mockups están en tema claro: se fuerza para que el modo
            // oscuro del teléfono no cambie los colores.
            UserAppTheme = AppTheme.Light;

            // Carga los recursos definidos en App.xaml (estilos globales)
            InitializeComponent();
        }

        // Crea la ventana principal: AppShell con las pestañas de navegación.
        // Se arranca en el Shell (PB04) y no en una sola vista para que vInicio sea la
        // pantalla principal. Entre pantallas se sigue usando Navigation.PushAsync/PopAsync.
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
