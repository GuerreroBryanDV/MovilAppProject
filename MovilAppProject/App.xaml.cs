using System.Globalization;
using MovilAppProject.Repositories;
using MovilAppProject.Services;

namespace MovilAppProject
{
    public partial class App : Application
    {
        // Repositorio de SQLite disponible para todas las vistas
        public static MovimientoRepository MovimientoRepo { get; private set; } = null!;

        // Servicio de la regla disponible para todas las vistas
        public static ReglaPresupuestoService ReglaService { get; private set; } = null!;

        // Servicio de recordatorios locales
        public static RecordatorioService RecordatorioService { get; private set; } = null!;

        // Servicio de la IA disponible para vRecomendaciones
        public static IAService IAService { get; private set; } = null!;

        public App(MovimientoRepository repo, ReglaPresupuestoService reglaService, RecordatorioService recordatorioService, IAService iaService)
        {
            MovimientoRepo = repo;
            ReglaService = reglaService;
            RecordatorioService = recordatorioService;
            IAService = iaService;
            // Textos en español, moneda USD y fechas dd/MM/aaaa.
            // La cultura es-EC (Ecuador) usa dólar ($), coma decimal (25,50)
            // y punto de miles ($99.999,99), igual que los mensajes del Backlog.
            var cultura = new CultureInfo("es-EC");
            cultura.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;
            CultureInfo.CurrentCulture = cultura;
            CultureInfo.CurrentUICulture = cultura;

            // La app usa tema claro: se fuerza para que el modo
            // oscuro del teléfono no cambie los colores.
            UserAppTheme = AppTheme.Light;

            // Carga los recursos definidos en App.xaml (estilos globales)
            InitializeComponent();
        }

        // Crea la ventana principal: AppShell con las pestañas de navegación.
        // Se arranca en el Shell y no en una sola vista para que vInicio sea la
        // pantalla principal. Entre pantallas se sigue usando Navigation.PushAsync/PopAsync.
        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window window = new(new AppShell());
            // Al volver de segundo plano se revisa si pasó el tiempo de inactividad
            window.Resumed += (s, e) => (window.Page as AppShell)?.AlReanudar();
            return window;
        }
    }
}
