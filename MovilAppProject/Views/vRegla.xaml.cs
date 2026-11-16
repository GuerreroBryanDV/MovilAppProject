using MovilAppProject.Services;
using MovilAppProject.Utils;

namespace MovilAppProject.Views;

// PB12 · Panel de la regla y PB13 · Semáforo por grupo (V11, V12), según el mockup.
// Todos los cálculos los hace ReglaPresupuestoService (PB11); aquí solo se muestran.
public partial class vRegla : ContentPage
{
    public vRegla()
    {
        InitializeComponent();
    }

    // Se ejecuta cada vez que la pantalla aparece (al entrar a la pestaña Regla),
    // así el panel se actualiza después de registrar un gasto o un ingreso.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarRegla();
    }

    // ===== PB14: mes anterior / siguiente (nunca pasa del mes actual, V10) =====
    private void btnMesAnterior_Clicked(object sender, EventArgs e)
    {
        FiltroMes.Anterior();
        CargarRegla();
    }

    private void btnMesSiguiente_Clicked(object sender, EventArgs e)
    {
        FiltroMes.Siguiente();
        CargarRegla();
    }

    // ===== Carga la regla del mes seleccionado =====
    private async void CargarRegla()
    {
        try
        {
            // PB14: mes seleccionado (compartido con vInicio): "Noviembre 2026"
            lblMes.Text = FiltroMes.Texto();

            // V10: el botón › se deshabilita en el mes actual (no hay meses futuros)
            btnMesSiguiente.IsEnabled = !FiltroMes.EsMesActual;
            btnMesSiguiente.Opacity = FiltroMes.EsMesActual ? 0.3 : 1;

            ResultadoRegla resultado = App.ReglaService.CalcularRegla(FiltroMes.Mes.Year, FiltroMes.Mes.Month);

            // Título con los porcentajes del perfil elegido en vPorcentajes (PB15): "Regla 60/30/10"
            lblTitulo.Text = "Regla " + resultado.Perfil.PorcentajeNecesidades + "/"
                           + resultado.Perfil.PorcentajeAhorroInversion + "/"
                           + resultado.Perfil.PorcentajeLujos;
            lblIngresos.Text = "Ingresos del mes: $" + resultado.Ingresos.ToString("N2");

            // Error al leer la base de datos
            if (resultado.Mensaje.StartsWith("Error"))
            {
                await DisplayAlertAsync("Error", resultado.Mensaje, "Aceptar");
                return;
            }

            // V11: sin ingresos se muestra el aviso dentro de la vista y no hay tarjetas
            if (!resultado.HayIngresos)
            {
                lblSinIngresos.Text = resultado.Mensaje;
                brdSinIngresos.IsVisible = true;
                slGrupos.IsVisible = false;
                brdCumplimiento.IsVisible = false;
                BindableLayout.SetItemsSource(slGrupos, null);
                return;
            }

            // Una tarjeta por grupo con su % real, meta, barra y semáforo
            brdSinIngresos.IsVisible = false;
            slGrupos.IsVisible = true;
            brdCumplimiento.IsVisible = true;
            BindableLayout.SetItemsSource(slGrupos, resultado.Grupos);

            // "1 de 3 grupos" en meta (semáforo verde)
            lblCumplimiento.Text = resultado.GruposEnMeta + " de " + resultado.Grupos.Count + " grupos";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }
}
