using MovilAppProject.Services;

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

    // ===== Carga la regla del mes actual =====
    private async void CargarRegla()
    {
        try
        {
            DateTime hoy = DateTime.Today;

            // Mes actual: "Noviembre 2026"
            string mes = hoy.ToString("MMMM yyyy");
            lblMes.Text = mes.Substring(0, 1).ToUpper() + mes.Substring(1);

            ResultadoRegla resultado = App.ReglaService.CalcularRegla(hoy.Year, hoy.Month);

            lblTitulo.Text = "Regla " + resultado.Perfil.Nombre;
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
