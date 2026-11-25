using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace MovilAppProject
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // Cada toque en la pantalla cuenta como actividad (bloqueo por inactividad)
        public override bool DispatchTouchEvent(MotionEvent? ev)
        {
            MovilAppProject.Utils.Sesion.Tocar();
            return base.DispatchTouchEvent(ev);
        }

        // Al volver de segundo plano se revisa si pasó el tiempo de inactividad.
        // Se usa el ciclo de vida de Android porque es más fiable que Window.Resumed.
        protected override void OnResume()
        {
            base.OnResume();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page as AppShell)?.AlReanudar();
            });
        }
    }
}
