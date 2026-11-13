namespace MovilAppProject.Views;

// PB18 · MAP-18 · Bloqueo con huella, Face ID o PIN
// Se abre desde AppShell al iniciar la app cuando el bloqueo está activo (vAjustes, PB17).
// Validaciones V16 (formato del PIN), V18 (3 intentos y espera de 30 s) y V19 (huella).
public partial class vDesbloqueo : ContentPage
{
    // V18: intentos permitidos y segundos de espera
    const int maximoIntentos = 3;
    const int segundosEspera = 30;

    // Errores de PIN acumulados
    int intentosFallidos = 0;

    // V19: "Usar huella" solo se muestra si el dispositivo tiene biometría.
    // PENDIENTE: la detección y el uso de la huella necesitan un paquete NuGet
    // (por ejemplo Plugin.Maui.Biometric) que el equipo aún no ha aprobado.
    // Mientras tanto se considera que no hay biometría y se usa solo el PIN.
    bool hayBiometria = false;

    public vDesbloqueo()
    {
        InitializeComponent();

        btnHuella.IsVisible = hayBiometria;

        if (!hayBiometria)
        {
            // Sin huella: se muestra directamente el ingreso del PIN
            lblInstruccion.Text = "Ingresa tu PIN para continuar.";
            btnUsarPin.IsVisible = false;
            slPin.IsVisible = true;
        }
    }

    // El botón "atrás" de Android no puede saltarse el desbloqueo
    protected override bool OnBackButtonPressed()
    {
        return true;
    }

    // ===== Usar huella (V19) =====
    private async void btnHuella_Clicked(object sender, EventArgs e)
    {
        // PENDIENTE: autenticación biométrica (ver comentario de hayBiometria).
        // Si la huella falla se pide el PIN:
        await DisplayAlertAsync("Error", "No se pudo verificar tu huella. Usa tu PIN.", "Aceptar");
        MostrarPin();
    }

    // ===== Usar PIN =====
    private void btnUsarPin_Clicked(object sender, EventArgs e)
    {
        MostrarPin();
    }

    private void MostrarPin()
    {
        slPin.IsVisible = true;
        btnUsarPin.IsVisible = false;
        txtPin.Focus();
    }

    // ===== Desbloquear con PIN =====
    private async void btnDesbloquear_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Mientras corre la espera de 30 s no se acepta nada
            if (!btnDesbloquear.IsEnabled)
            {
                return;
            }

            string pin = (txtPin.Text ?? "").Trim();

            // V16: el PIN tiene exactamente 4 dígitos (no cuenta como intento)
            if (!PinValido(pin))
            {
                await DisplayAlertAsync("Error", "El PIN debe tener exactamente 4 dígitos.", "Aceptar");
                txtPin.Text = "";
                return;
            }

            string pinGuardado = Preferences.Get(vAjustes.ClavePin, "");

            // PIN correcto: se cierra esta pantalla y se ve vInicio
            if (pin == pinGuardado)
            {
                await Navigation.PopModalAsync();
                return;
            }

            // V18: PIN incorrecto
            intentosFallidos = intentosFallidos + 1;
            txtPin.Text = "";
            int intentosRestantes = maximoIntentos - intentosFallidos;

            if (intentosRestantes > 0)
            {
                await DisplayAlertAsync("Error", "PIN incorrecto. Te quedan " + intentosRestantes + " intentos.", "Aceptar");
                return;
            }

            // Al tercer error: espera de 30 segundos
            await DisplayAlertAsync("Error", "Demasiados intentos. Espera 30 segundos.", "Aceptar");
            await EsperarBloqueo();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // V18: bloquea el ingreso del PIN durante 30 segundos con cuenta regresiva
    private async Task EsperarBloqueo()
    {
        txtPin.IsEnabled = false;
        btnDesbloquear.IsEnabled = false;
        lblEspera.IsVisible = true;

        for (int segundos = segundosEspera; segundos > 0; segundos--)
        {
            lblEspera.Text = "Espera " + segundos + " segundos.";
            await Task.Delay(1000);
        }

        intentosFallidos = 0;
        lblEspera.IsVisible = false;
        txtPin.IsEnabled = true;
        btnDesbloquear.IsEnabled = true;
        txtPin.Focus();
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
