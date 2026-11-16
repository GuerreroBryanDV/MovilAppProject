using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using MovilAppProject.Repositories;
using MovilAppProject.Services;
using MovilAppProject.Utils;

namespace MovilAppProject
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLocalNotification()   // PB20: notificaciones locales (Plugin.LocalNotification)
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Ruta de la base de datos SQLite de GastoFácil (PB03)
            string dbPath = FileAccessHelper.GetLocalFilePath("gastofacil.db3");
            builder.Services.AddSingleton<MovimientoRepository>(s => ActivatorUtilities.CreateInstance<MovimientoRepository>(s, dbPath));

            // Servicio de cálculo de la regla (PB11): recibe el repositorio por inyección
            builder.Services.AddSingleton<ReglaPresupuestoService>();

            // Servicio de recordatorios locales (PB20)
            builder.Services.AddSingleton<RecordatorioService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
