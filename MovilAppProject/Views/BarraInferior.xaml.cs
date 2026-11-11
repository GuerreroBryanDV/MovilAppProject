namespace MovilAppProject.Views;

// Barra de navegación inferior. Marca la pestaña activa según la ruta del Shell.
public partial class BarraInferior : ContentView
{
    // Cada pestaña: su ruta en el AppShell y los controles que se pintan
    private readonly (string Ruta, Border Pill, Microsoft.Maui.Controls.Shapes.Path Icono, Label Texto)[] _pestanas;

    public BarraInferior()
    {
        InitializeComponent();

        _pestanas =
        [
            ("vInicio", pillInicio, iconoInicio, lblInicio),
            ("vRegla", pillRegla, iconoRegla, lblRegla),
            ("vEstadisticas", pillEstadisticas, iconoEstadisticas, lblEstadisticas),
            ("vAjustes", pillAjustes, iconoAjustes, lblAjustes)
        ];

        Loaded += (_, _) =>
        {
            if (Shell.Current is not null)
            {
                Shell.Current.Navigated += Shell_Navigated;
            }
            ActualizarPestanaActiva();
        };

        Unloaded += (_, _) =>
        {
            if (Shell.Current is not null)
            {
                Shell.Current.Navigated -= Shell_Navigated;
            }
        };
    }

    private void Shell_Navigated(object? sender, ShellNavigatedEventArgs e)
    {
        ActualizarPestanaActiva();
    }

    // Toca una pestaña: navega a su ruta (si no es la que ya está abierta)
    private async void Item_Tapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string ruta || Shell.Current is null)
        {
            return;
        }

        if (EsPestanaActiva(ruta))
        {
            return;
        }

        await Shell.Current.GoToAsync("//" + ruta);
    }

    private static bool EsPestanaActiva(string ruta)
    {
        string ubicacion = Shell.Current?.CurrentState?.Location?.OriginalString ?? "";
        return ubicacion.Contains(ruta);
    }

    // Pestaña activa: ícono y texto en verde oscuro, fondo suave y texto en negrita.
    // Las demás: gris.
    private void ActualizarPestanaActiva()
    {
        Color activo = (Color)Application.Current!.Resources["Primary"];
        Color inactivo = (Color)Application.Current.Resources["ColorTextoSecundario"];
        Color fondoActivo = (Color)Application.Current.Resources["Secondary"];

        foreach (var pestana in _pestanas)
        {
            bool esActiva = EsPestanaActiva(pestana.Ruta);

            pestana.Pill.BackgroundColor = esActiva ? fondoActivo : Colors.Transparent;
            pestana.Icono.Fill = new SolidColorBrush(esActiva ? activo : inactivo);
            pestana.Texto.TextColor = esActiva ? activo : inactivo;
            pestana.Texto.FontAttributes = esActiva ? FontAttributes.Bold : FontAttributes.None;
        }
    }
}
