using Microsoft.Extensions.Logging;
using LiveChartsCore.SkiaSharpView.Maui;
using Plugin.LocalNotification;
using SkiaSharp.Views.Maui.Controls.Hosting;
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
                .UseLocalNotification()   // Notificaciones locales (Plugin.LocalNotification)
                .UseSkiaSharp()           // LiveCharts2 dibuja el gráfico con SkiaSharp
                .UseLiveCharts()          // Gráfico de gastos por categoría
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Ruta de la base de datos SQLite de GastoFácil
            string dbPath = FileAccessHelper.GetLocalFilePath("gastofacil.db3");
            builder.Services.AddSingleton<MovimientoRepository>(s => ActivatorUtilities.CreateInstance<MovimientoRepository>(s, dbPath));

            // Servicio de cálculo de la regla: recibe el repositorio por inyección
            builder.Services.AddSingleton<ReglaPresupuestoService>();

            // Servicio de recordatorios locales
            builder.Services.AddSingleton<RecordatorioService>();
            // Servicio de conexión con la IA: la clave se lee de Resources/Raw/.env
            builder.Services.AddSingleton<IAService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
