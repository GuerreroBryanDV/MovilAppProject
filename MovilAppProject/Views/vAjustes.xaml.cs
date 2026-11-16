namespace MovilAppProject.Views;

// PB17 · MAP-17 · Pantalla de ajustes
// Validaciones V16 (PIN de 4 dígitos) y V17 (los PIN deben coincidir).
// Los valores se guardan en Preferences, así se conservan al cerrar la app.
// PB18 (vDesbloqueo) leerá las mismas claves: "BloqueoActivo" y "Pin".
// PB20 · Recordatorios locales: V20 (texto), V21 (hora 20:00), V22 (frecuencia), V23 (permiso).
public partial class vAjustes : ContentPage
{
    // Claves de Preferences (las usa también PB18)
    public const string ClaveBloqueo = "BloqueoActivo";
    public const string ClavePin = "Pin";
    public const string ClaveRecordatorios = "RecordatoriosActivos";

    // Evita que el evento Toggled se ejecute cuando el código mueve el Switch
    bool cargando = false;

    public vAjustes()
    {
        InitializeComponent();

        // PB20: frecuencias del recordatorio (V22)
        pkFrecuencia.ItemsSource = Services.RecordatorioService.Frecuencias;
    }

    // Cada vez que se abre la pantalla se leen los valores guardados
    protected override void OnAppearing()
    {
        base.OnAppearing();

        cargando = true;
        swBloqueo.IsToggled = Preferences.Get(ClaveBloqueo, false);
        // PB20: el interruptor queda encendido solo si hay un recordatorio programado
        swRecordatorios.IsToggled = Preferences.Get(ClaveRecordatorios, false)
                                    && App.RecordatorioService.HayRecordatorio;
        cargando = false;

        OcultarFormularioPin();
        OcultarFormularioRecordatorio();
        MostrarRecordatorioActual();
    }

    // ===== Bloqueo de la app =====
    private async void swBloqueo_Toggled(object sender, ToggledEventArgs e)
    {
        if (cargando)
        {
            return;
        }

        try
        {
            if (e.Value)
            {
                // Al activar se piden Nuevo PIN y Confirmar PIN
                txtPin.Text = "";
                txtConfirmarPin.Text = "";
                slPin.IsVisible = true;
                txtPin.Focus();
            }
            else
            {
                // Al desactivar se borra el PIN guardado
                OcultarFormularioPin();

                if (Preferences.Get(ClaveBloqueo, false))
                {
                    Preferences.Set(ClaveBloqueo, false);
                    Preferences.Remove(ClavePin);
                    await DisplayAlertAsync("Ajustes", "Bloqueo desactivado.", "Aceptar");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Guardar PIN =====
    private async void btnGuardarPin_Clicked(object sender, EventArgs e)
    {
        try
        {
            string pin = (txtPin.Text ?? "").Trim();
            string confirmarPin = (txtConfirmarPin.Text ?? "").Trim();

            // V16: exactamente 4 dígitos ("12a" o "123" no son válidos)
            if (!PinValido(pin) || !PinValido(confirmarPin))
            {
                await DisplayAlertAsync("Error", "El PIN debe tener exactamente 4 dígitos.", "Aceptar");
                txtPin.Text = "";
                txtConfirmarPin.Text = "";
                return;
            }

            // V17: los dos PIN deben ser iguales; si no, el interruptor vuelve a apagado
            if (pin != confirmarPin)
            {
                await DisplayAlertAsync("Error", "Los PIN no coinciden.", "Aceptar");
                ApagarBloqueo();
                return;
            }

            // Todo válido: se guarda en Preferences
            Preferences.Set(ClavePin, pin);
            Preferences.Set(ClaveBloqueo, true);

            OcultarFormularioPin();
            await DisplayAlertAsync("Ajustes", "Bloqueo activado.", "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Cancelar: no se guarda nada y el interruptor vuelve a apagado =====
    private void btnCancelarPin_Clicked(object sender, EventArgs e)
    {
        ApagarBloqueo();
    }

    // ===== Recordatorios (PB20) =====
    private async void swRecordatorios_Toggled(object sender, ToggledEventArgs e)
    {
        if (cargando)
        {
            return;
        }

        try
        {
            if (e.Value)
            {
                // Al activar se piden texto, hora y frecuencia
                AbrirFormularioRecordatorio();
            }
            else
            {
                // Al desactivar se cancela la notificación programada
                OcultarFormularioRecordatorio();
                Preferences.Set(ClaveRecordatorios, false);

                if (App.RecordatorioService.HayRecordatorio)
                {
                    App.RecordatorioService.CancelarRecordatorio();
                    await DisplayAlertAsync("Ajustes", "Recordatorio desactivado.", "Aceptar");
                }

                MostrarRecordatorioActual();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Guardar recordatorio =====
    private async void btnGuardarRecordatorio_Clicked(object sender, EventArgs e)
    {
        try
        {
            string? frecuencia = pkFrecuencia.SelectedItem as string;
            TimeSpan hora = tpHora.Time ?? Services.RecordatorioService.HoraPorDefecto;

            // Valida V20 y V22, pide el permiso (V23) y programa la notificación
            bool guardado = await App.RecordatorioService.GuardarRecordatorioAsync(txtRecordatorio.Text, hora, frecuencia);

            if (!guardado)
            {
                await DisplayAlertAsync("Error", App.RecordatorioService.StatusMessage, "Aceptar");

                // V23: si se negó el permiso, el recordatorio no se guarda y el interruptor se apaga
                if (App.RecordatorioService.StatusMessage == Services.RecordatorioService.MensajePermiso)
                {
                    ApagarRecordatorios();
                }
                return;
            }

            Preferences.Set(ClaveRecordatorios, true);
            OcultarFormularioRecordatorio();
            MostrarRecordatorioActual();
            await DisplayAlertAsync("Ajustes", App.RecordatorioService.StatusMessage, "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Cancelar: no se guarda nada y el interruptor vuelve a como estaba =====
    private void btnCancelarRecordatorio_Clicked(object sender, EventArgs e)
    {
        ApagarRecordatorios();
    }

    // ===== Regla 50/40/10 =====
    private async void btnPorcentajes_Clicked(object sender, EventArgs e)
    {
        // PB15: abre la pantalla de perfiles de porcentajes
        try
        {
            await Navigation.PushAsync(new vPorcentajes());
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // Vuelve a apagar el interruptor sin disparar el evento Toggled
    private void ApagarBloqueo()
    {
        cargando = true;
        swBloqueo.IsToggled = Preferences.Get(ClaveBloqueo, false);
        cargando = false;

        OcultarFormularioPin();
    }

    // PB20: muestra el formulario con el recordatorio guardado (o 20:00 por defecto, V21)
    private void AbrirFormularioRecordatorio()
    {
        txtRecordatorio.Text = App.RecordatorioService.ObtenerTexto();
        tpHora.Time = App.RecordatorioService.ObtenerHora();

        string frecuencia = App.RecordatorioService.ObtenerFrecuencia();
        pkFrecuencia.SelectedIndex = Services.RecordatorioService.Frecuencias.IndexOf(frecuencia);

        slRecordatorio.IsVisible = true;
        txtRecordatorio.Focus();
    }

    // PB20: esconde el formulario del recordatorio
    private void OcultarFormularioRecordatorio()
    {
        slRecordatorio.IsVisible = false;
    }

    // PB20: el interruptor vuelve a su estado guardado sin disparar el evento Toggled
    private void ApagarRecordatorios()
    {
        cargando = true;
        swRecordatorios.IsToggled = Preferences.Get(ClaveRecordatorios, false)
                                    && App.RecordatorioService.HayRecordatorio;
        cargando = false;

        OcultarFormularioRecordatorio();
        MostrarRecordatorioActual();
    }

    // PB20: "Registrar ahorro · 20:00 · Semanal"
    private void MostrarRecordatorioActual()
    {
        if (!App.RecordatorioService.HayRecordatorio)
        {
            lblRecordatorioActual.IsVisible = false;
            return;
        }

        TimeSpan hora = App.RecordatorioService.ObtenerHora();
        lblRecordatorioActual.Text = App.RecordatorioService.ObtenerTexto() + " · "
                                   + hora.ToString(@"hh\:mm") + " · "
                                   + App.RecordatorioService.ObtenerFrecuencia();
        lblRecordatorioActual.IsVisible = true;
    }

    // Esconde el formulario y limpia los PIN escritos
    private void OcultarFormularioPin()
    {
        txtPin.Text = "";
        txtConfirmarPin.Text = "";
        slPin.IsVisible = false;
    }

    // V16: true si el texto tiene exactamente 4 números
    private bool PinValido(string pin)
    {
        if (pin.Length != 4)
        {
            return false;
        }

        foreach (char c in pin)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
