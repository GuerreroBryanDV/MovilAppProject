using Plugin.Maui.Biometric;

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

    // V19: "Usar huella" solo se muestra si el dispositivo tiene biometría con datos registrados.
    // Se detecta con Plugin.Maui.Biometric (D15) al aparecer la pantalla.
    bool hayBiometria = false;

    public vDesbloqueo()
    {
        InitializeComponent();

        // Hasta saber si hay biometría se muestra solo el PIN (siempre disponible)
        MostrarSoloPin();
    }

    // Sin huella: se muestra directamente el ingreso del PIN
    private void MostrarSoloPin()
    {
        btnHuella.IsVisible = false;
        lblInstruccion.Text = "Ingresa tu PIN para continuar.";
        btnUsarPin.IsVisible = false;
        slPin.IsVisible = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // V19: la huella solo aparece si el dispositivo la tiene y hay una registrada
            BiometricHwStatus estado = await BiometricAuthenticationService.Default
                .GetAuthenticationStatusAsync(AuthenticatorStrength.Strong);
            hayBiometria = estado == BiometricHwStatus.Success;
        }
        catch
        {
            // Si el dispositivo no responde se usa solo el PIN
            hayBiometria = false;
        }

        if (hayBiometria)
        {
            lblInstruccion.Text = "Desbloquea con tu huella o Face ID";
            btnHuella.IsVisible = true;
            btnUsarPin.IsVisible = true;
            slPin.IsVisible = false;
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
        try
        {
            AuthenticationRequest solicitud = new()
            {
                Title = "GastoFácil",
                Description = "Verifica tu identidad para abrir la app.",
                NegativeText = "Usar PIN",
                AllowPasswordAuth = false
            };

            AuthenticationResponse respuesta = await BiometricAuthenticationService.Default
                .AuthenticateAsync(solicitud, CancellationToken.None);

            // Huella correcta: se cierra esta pantalla y se ve vInicio
            if (respuesta.Status == BiometricResponseStatus.Success)
            {
                await Navigation.PopModalAsync();
                return;
            }
        }
        catch
        {
            // Cualquier falla del lector se trata igual que una huella no reconocida
        }

        // V19: si la huella falla se pide el PIN
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

            // El PIN está en SecureStorage (RNF05)
            string pinGuardado = await Utils.PinStorage.ObtenerAsync() ?? "";

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
