namespace MovilAppProject.Views;

// PB17 · MAP-17 · Pantalla de ajustes
// Validaciones V16 (PIN de 4 dígitos) y V17 (los PIN deben coincidir).
// El interruptor se guarda en Preferences y el PIN en SecureStorage (RNF05),
// así se conservan al cerrar la app. PB18 (vDesbloqueo) lee ambos.
// PB20 · Recordatorios locales: V20 (texto), V21 (hora 20:00), V22 (frecuencia), V23 (permiso).
public partial class vAjustes : ContentPage
{
    // Claves de Preferences (las usa también PB18)
    public const string ClaveBloqueo = "BloqueoActivo";
    public const string ClaveRecordatorios = "RecordatoriosActivos";

    // Evita que el evento Toggled se ejecute cuando el código mueve el Switch
    bool cargando = false;

    // D20: true si el formulario del PIN se abrió para cambiarlo (no para activar el bloqueo)
    bool cambiandoPin = false;

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
        lblUsuarioActual.Text = "Usuario: " + Utils.Sesion.NombreUsuario;
        // PB20: el interruptor queda encendido solo si hay un recordatorio programado
        swRecordatorios.IsToggled = Preferences.Get(ClaveRecordatorios, false)
                                    && App.RecordatorioService.HayRecordatorio;
        cargando = false;

        OcultarFormularioPin();
        OcultarFormularioRecordatorio();
        MostrarRecordatorioActual();
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

            // V17: los dos PIN deben ser iguales; si no, el interruptor vuelve a como estaba
            // (apagado si se estaba activando; encendido y con el PIN anterior si se estaba cambiando)
            if (pin != confirmarPin)
            {
                await DisplayAlertAsync("Error", "Los PIN no coinciden.", "Aceptar");
                ApagarBloqueo();
                return;
            }

            // Todo válido: el PIN va a SecureStorage (cifrado) y solo el interruptor a Preferences
            bool eraCambio = cambiandoPin;
            await Utils.PinStorage.GuardarAsync(pin);
            Preferences.Set(ClaveBloqueo, true);

            OcultarFormularioPin();
            await DisplayAlertAsync("Ajustes", eraCambio ? "PIN actualizado." : "Bloqueo activado.", "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Cancelar: no se guarda nada y el interruptor vuelve a como estaba =====
    // (al cambiar el PIN, se conserva el PIN anterior)
    private void btnCancelarPin_Clicked(object sender, EventArgs e)
    {
        ApagarBloqueo();
    }

    // ===== D20: Cambiar PIN =====
    // Solo con el bloqueo activo. Pide el PIN actual antes de dejar escribir uno nuevo,
    // así nadie con el teléfono desbloqueado puede reemplazarlo.
    private async void btnCambiarPin_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!await VerificarPinActualAsync("Cambiar PIN", "Ingresa tu PIN actual."))
            {
                return;
            }

            cambiandoPin = true;
            MostrarFormularioPin();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Cuenta: inactivar (borrado lógico) =====
    // La cuenta queda inactiva con fecha de baja y sus datos se conservan en el historial.
    // Después se muestra el registro para crear otro usuario en este dispositivo.
    private async void btnInactivarCuenta_Clicked(object sender, EventArgs e)
    {
        try
        {
            bool confirmar = await DisplayAlertAsync("¿Inactivar tu cuenta?",
                "Se cerrará tu sesión y podrás registrar otro usuario. Tus datos se conservan en el historial.",
                "Inactivar", "Cancelar");
            if (!confirmar || !await ConfirmarIdentidadAsync())
            {
                return;
            }
            if (!App.MovimientoRepo.InactivarUsuarioActivo())
            {
                await DisplayAlertAsync("Error", App.MovimientoRepo.StatusMessage, "Aceptar");
                return;
            }
            // El PIN pertenece al usuario anterior
            Utils.PinStorage.Borrar();
            Preferences.Set(ClaveBloqueo, false);
            Utils.Sesion.Cerrar();
            await DisplayAlertAsync("Ajustes", "Cuenta inactivada.", "Aceptar");
            // Se reconstruye la app: sin usuario activo muestra el registro
            Application.Current!.Windows[0].Page = new AppShell();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
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

    // D19: verifica que quien desactiva el bloqueo es el dueño.
    // Primero la huella (si el dispositivo la tiene); si no, o si falla, el PIN guardado.
    private async Task<bool> ConfirmarIdentidadAsync()
    {
        if (await Utils.Biometria.DisponibleAsync()
            && await Utils.Biometria.AutenticarAsync("Confirma tu identidad para inactivar la cuenta."))
        {
            return true;
        }

        return await VerificarPinActualAsync("Inactivar cuenta", "Ingresa tu PIN para inactivar la cuenta.");
    }

    // D19 / D20: pide el PIN guardado y devuelve true solo si es correcto.
    // Cancelar, un PIN con otro formato (V16) o un PIN distinto devuelven false.
    private async Task<bool> VerificarPinActualAsync(string titulo, string mensaje)
    {
        string? pin = await DisplayPromptAsync(titulo, mensaje,
                                               "Aceptar", "Cancelar",
                                               "4 dígitos", 4, Keyboard.Numeric);

        // Canceló: no se cambia nada
        if (pin is null)
        {
            return false;
        }

        pin = pin.Trim();

        // V16: exactamente 4 dígitos
        if (!PinValido(pin))
        {
            await DisplayAlertAsync("Error", "El PIN debe tener exactamente 4 dígitos.", "Aceptar");
            return false;
        }

        string pinGuardado = await Utils.PinStorage.ObtenerAsync() ?? "";
        if (pin != pinGuardado)
        {
            await DisplayAlertAsync("Error", "PIN incorrecto.", "Aceptar");
            return false;
        }

        return true;
    }

    // Deja el interruptor como está guardado (encendido o apagado) sin disparar el evento Toggled
    private void ApagarBloqueo()
    {
        cargando = true;
        lblUsuarioActual.Text = "Usuario: " + Utils.Sesion.NombreUsuario;
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

    // Muestra Nuevo PIN / Confirmar PIN (al activar el bloqueo o al cambiar el PIN)
    private void MostrarFormularioPin()
    {
        txtPin.Text = "";
        txtConfirmarPin.Text = "";
        btnCambiarPin.IsVisible = false;
        slPin.IsVisible = true;
        txtPin.Focus();
    }

    // Esconde el formulario y limpia los PIN escritos.
    // "Cambiar PIN" solo se ve si el bloqueo está activo (D20).
    private void OcultarFormularioPin()
    {
        txtPin.Text = "";
        txtConfirmarPin.Text = "";
        slPin.IsVisible = false;
        cambiandoPin = false;
        btnCambiarPin.IsVisible = true;
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
