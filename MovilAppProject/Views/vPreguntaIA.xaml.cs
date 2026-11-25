using MovilAppProject.Models;
using MovilAppProject.Services;
using MovilAppProject.Utils;

namespace MovilAppProject.Views;

// Pregunta a la IA (PB23). Componente que va dentro de vRecomendaciones.
public partial class vPreguntaIA : ContentView
{
    // V25: límites de la pregunta
    const int minimoCaracteres = 5;
    const int maximoCaracteres = 200;

    public vPreguntaIA()
    {
        InitializeComponent();
    }

    private async void btnEnviar_Clicked(object sender, EventArgs e)
    {
        // V26: si ya se está esperando una respuesta no se envía otra vez
        if (!btnEnviar.IsEnabled)
        {
            return;
        }

        try
        {
            string pregunta = (txtPregunta.Text ?? "").Trim();

            // V25: entre 5 y 200 caracteres
            if (pregunta.Length < minimoCaracteres || pregunta.Length > maximoCaracteres)
            {
                await MostrarError("Escribe una pregunta de 5 a 200 caracteres.");
                return;
            }

            // V26: se deshabilita Enviar mientras espera
            btnEnviar.IsEnabled = false;
            txtPregunta.IsEnabled = false;
            slEsperando.IsVisible = true;
            brdRespuesta.IsVisible = false;

            // Totales del mes seleccionado, con el perfil de la regla elegido
            DateTime mes = FiltroMes.Mes;
            PerfilPresupuesto perfil = App.ReglaService.ObtenerPerfilActual();
            ResultadoRegla regla = await Task.Run(() => App.ReglaService.CalcularRegla(mes.Year, mes.Month, perfil));

            // Consulta a la IA
            ResultadoIA respuesta = await App.IAService.PreguntarAsync(pregunta, regla);

            if (!respuesta.Exito)
            {
                // Sin conexión (V24), sin respuesta en 30 s (V26), falta la clave, etc.
                await MostrarError(respuesta.Mensaje);
                return;
            }

            // La respuesta aparece en la misma pantalla
            lblPreguntaHecha.Text = pregunta;
            lblRespuesta.Text = respuesta.Texto;
            brdRespuesta.IsVisible = true;
            txtPregunta.Text = "";
        }
        catch (Exception ex)
        {
            await MostrarError(ex.Message);
        }
        finally
        {
            // Se vuelve a habilitar Enviar
            slEsperando.IsVisible = false;
            btnEnviar.IsEnabled = true;
            txtPregunta.IsEnabled = true;
        }
    }

    // Muestra el error con DisplayAlert("Error", mensaje, "Aceptar")
    private async Task MostrarError(string mensaje)
    {
        Page? pagina = Shell.Current;
        if (pagina != null)
        {
            await pagina.DisplayAlertAsync("Error", mensaje, "Aceptar");
        }
    }
}
