namespace MovilAppProject.Views;

// PB17 · MAP-17 · Pantalla de ajustes
// Validaciones V16 (PIN de 4 dígitos) y V17 (los PIN deben coincidir).
// Los valores se guardan en Preferences, así se conservan al cerrar la app.
// PB18 (vDesbloqueo) leerá las mismas claves: "BloqueoActivo" y "Pin".
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
    }

    // Cada vez que se abre la pantalla se leen los valores guardados
    protected override void OnAppearing()
    {
        base.OnAppearing();

        cargando = true;
        swBloqueo.IsToggled = Preferences.Get(ClaveBloqueo, false);
        swRecordatorios.IsToggled = Preferences.Get(ClaveRecordatorios, false);
        cargando = false;

        OcultarFormularioPin();
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

    // ===== Recordatorios =====
    private void swRecordatorios_Toggled(object sender, ToggledEventArgs e)
    {
        if (cargando)
        {
            return;
        }

        // Se guarda la preferencia; la programación de los avisos es PB20
        Preferences.Set(ClaveRecordatorios, e.Value);
    }

    // ===== Regla 50/40/10 =====
    private async void btnPorcentajes_Clicked(object sender, EventArgs e)
    {
        // PENDIENTE PB15: abrir vPorcentajes cuando esa pantalla exista
        // await Navigation.PushAsync(new vPorcentajes());
        await DisplayAlertAsync("Regla 50/40/10", "Esta opción estará disponible próximamente.", "Aceptar");
    }

    // Vuelve a apagar el interruptor sin disparar el evento Toggled
    private void ApagarBloqueo()
    {
        cargando = true;
        swBloqueo.IsToggled = Preferences.Get(ClaveBloqueo, false);
        cargando = false;

        OcultarFormularioPin();
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
