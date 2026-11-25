using MovilAppProject.Models;
using MovilAppProject.Services;
using MovilAppProject.Utils;

namespace MovilAppProject.Views;

// Recomendaciones de la IA (PB22). Sin un ingreso y un gasto en el mes, o sin internet, se muestra el aviso (V24).
public partial class vRecomendaciones : ContentPage
{
    // Datos con los que se pidieron las últimas recomendaciones.
    // Si no cambiaron, no se vuelve a consultar a la IA al regresar a la pestaña
    // (ahorra consultas del plan gratuito de Gemini).
    string ultimaConsulta = "";

    // V26: evita consultas dobles mientras se espera la respuesta
    bool consultando = false;

    public vRecomendaciones()
    {
        InitializeComponent();
    }

    // Cada vez que se entra a la pestaña IA
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarRecomendaciones(false);
    }

    // El usuario pide recomendaciones nuevas
    private void btnActualizar_Clicked(object sender, EventArgs e)
    {
        CargarRecomendaciones(true);
    }

    // Carga las tarjetas del mes y las recomendaciones de la IA
    private async void CargarRecomendaciones(bool forzar)
    {
        if (consultando)
        {
            return;
        }

        try
        {
            // Mes seleccionado en Inicio / Regla
            DateTime mes = FiltroMes.Mes;
            PerfilPresupuesto perfil = App.ReglaService.ObtenerPerfilActual();

            // "Basadas en tus movimientos de noviembre y la regla 50/40/10."
            lblSubtitulo.Text = "Basadas en tus movimientos de " + mes.ToString("MMMM").ToLower()
                              + " y la regla " + perfil.PorcentajeNecesidades + "/"
                              + perfil.PorcentajeAhorroInversion + "/" + perfil.PorcentajeLujos + ".";

            // El cálculo lee la base fuera del hilo de la pantalla
            ResultadoRegla regla = await Task.Run(() => App.ReglaService.CalcularRegla(mes.Year, mes.Month, perfil));

            // Error al leer la base de datos
            if (regla.Mensaje.StartsWith("Error"))
            {
                await DisplayAlertAsync("Error", regla.Mensaje, "Aceptar");
                return;
            }

            // V24: hace falta al menos un ingreso y un gasto en el mes
            if (!IAService.HayDatosSuficientes(regla))
            {
                lblSinDatos.Text = IAService.MensajeSinDatos;
                brdSinDatos.IsVisible = true;
                slGrupos.IsVisible = false;
                brdIA.IsVisible = false;
                BindableLayout.SetItemsSource(slGrupos, null);
                ultimaConsulta = "";
                return;
            }

            // Tarjetas por grupo (se calculan en el teléfono, no necesitan internet)
            brdSinDatos.IsVisible = false;
            slGrupos.IsVisible = true;
            brdIA.IsVisible = true;
            BindableLayout.SetItemsSource(slGrupos, regla.Grupos);

            // Si los datos no cambiaron, se dejan las recomendaciones que ya están en pantalla
            string clave = ClaveConsulta(mes, regla);
            if (!forzar && clave == ultimaConsulta)
            {
                return;
            }

            // V26: el botón se deshabilita mientras espera la respuesta (máximo 30 s en IAService)
            consultando = true;
            MostrarCargando(true);

            ResultadoIA respuesta = await App.IAService.ObtenerRecomendacionesAsync(regla);

            if (respuesta.Exito)
            {
                lblRecomendaciones.Text = respuesta.Texto;
                lblRecomendaciones.IsVisible = true;
                lblErrorIA.IsVisible = false;
                ultimaConsulta = clave;
            }
            else
            {
                // V24 sin conexión, V26 sin respuesta, falta la clave, límite de consultas...
                lblErrorIA.Text = respuesta.Mensaje;
                lblErrorIA.IsVisible = true;
                lblRecomendaciones.IsVisible = false;
                ultimaConsulta = "";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
        finally
        {
            consultando = false;
            MostrarCargando(false);
        }
    }

    private async void Accion_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (e.Parameter is not int grupoId)
            {
                return;
            }

            if (grupoId == GrupoPresupuesto.AhorroInversionId)
            {
                // "Programar recordatorio" -> Ajustes
                await Shell.Current.GoToAsync("//vAjustes");
            }
            else if (grupoId == GrupoPresupuesto.LujosId)
            {
                // "Ver movimientos" -> lista de movimientos del mes en Inicio
                await Shell.Current.GoToAsync("//vInicio");
            }
            else
            {
                // "Registrar movimiento" -> Nuevo movimiento
                await Navigation.PushAsync(new vMovimiento());
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // Muestra u oculta el indicador de carga y bloquea el botón (V26)
    private void MostrarCargando(bool cargando)
    {
        slCargando.IsVisible = cargando;
        aiCargando.IsRunning = cargando;
        btnActualizar.IsEnabled = !cargando;
        btnActualizar.Text = cargando ? "Consultando..." : "Actualizar recomendaciones";
    }

    // Identifica los datos de la consulta: mes, perfil, ingresos y gasto de cada grupo
    private static string ClaveConsulta(DateTime mes, ResultadoRegla regla)
    {
        string clave = mes.ToString("yyyyMM") + "|" + regla.Perfil.PorcentajeNecesidades + "/"
                     + regla.Perfil.PorcentajeAhorroInversion + "/" + regla.Perfil.PorcentajeLujos
                     + "|" + regla.Ingresos;

        foreach (ResumenGrupo grupo in regla.Grupos)
        {
            clave = clave + "|" + grupo.GrupoId + ":" + grupo.Gastado;
        }

        return clave;
    }
}
