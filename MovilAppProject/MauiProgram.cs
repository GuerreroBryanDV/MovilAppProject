using Microsoft.Extensions.Logging;
using MovilAppProject.Repositories;
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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Ruta de la base de datos SQLite de GastoFácil (PB03)
            string dbPath = FileAccessHelper.GetLocalFilePath("gastofacil.db3");
            builder.Services.AddSingleton<MovimientoRepository>(s => ActivatorUtilities.CreateInstance<MovimientoRepository>(s, dbPath));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
