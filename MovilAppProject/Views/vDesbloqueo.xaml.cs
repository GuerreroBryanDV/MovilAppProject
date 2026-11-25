using MovilAppProject.Models;
using MovilAppProject.Utils;

namespace MovilAppProject.Views;

// Login tipo banco (PB18 ampliado). Se muestra al abrir la app y cuando pasan
// Sesion.MinutosInactividad minutos sin actividad. Solo entra el usuario registrado en el
// dispositivo, con su contraseña, su PIN o su huella/rostro.
// Validaciones V16 (formato del PIN), V18 (3 intentos y espera de 30 s) y V19 (biometría).
public partial class vDesbloqueo : ContentPage
{
    // V18: intentos permitidos y segundos de espera (PIN y contraseña comparten el contador)
    const int maximoIntentos = 3;
    const int segundosEspera = 30;

    int intentosFallidos = 0;

    bool hayBiometria = false;

    public vDesbloqueo()
    {
        InitializeComponent();
        Usuario? usuario = App.MovimientoRepo.ObtenerUsuarioActivo();
        lblUsuario.Text = usuario == null ? "" : "Hola, " + usuario.Nombre;
        // Hasta saber si hay biometría se muestra solo el PIN (siempre disponible)
        MostrarPin();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // V19: la huella solo aparece si el dispositivo la tiene y hay una registrada
        (bool disponible, string motivo) = await Biometria.EstadoAsync();
        hayBiometria = disponible;
        if (hayBiometria)
        {
            lblInstruccion.Text = "Desbloquea con tu huella o rostro";
            btnHuella.IsVisible = true;
            slPin.IsVisible = false;
            slPassword.IsVisible = false;
            lblAvisoBiometria.IsVisible = false;
        }
        else
        {
            // Se explica por qué no hay biometría en vez de ocultar el botón sin avisar
            lblAvisoBiometria.Text = motivo;
            lblAvisoBiometria.IsVisible = true;
        }
    }

    // El botón "atrás" de Android no puede saltarse el login
    protected override bool OnBackButtonPressed()
    {
        return true;
    }

    // Login correcto: se cierra esta pantalla y la app queda donde estaba
    private async Task EntrarAsync()
    {
        Usuario? usuario = App.MovimientoRepo.ObtenerUsuarioActivo();
        if (usuario == null)
        {
            await DisplayAlertAsync("Error", "No hay un usuario registrado en este dispositivo.", "Aceptar");
            return;
        }
        // Al abrir la app todavía no había usuario en memoria: las pantallas cargaron sin datos.
        // Se reconstruye la app para que lean los del usuario. Tras una inactividad el usuario
        // ya estaba en memoria y solo se cierra esta pantalla (no se pierde lo que se estaba haciendo).
        bool primerLogin = Sesion.UsuarioId == 0;
        Sesion.Iniciar(usuario.Id, usuario.Nombre);
        Sesion.BloqueoVisible = false;
        if (primerLogin)
        {
            Application.Current!.Windows[0].Page = new AppShell();
            return;
        }
        await Navigation.PopModalAsync();
    }

    // ===== Usar huella o rostro (V19) =====
    private async void btnHuella_Clicked(object sender, EventArgs e)
    {
        if (await Biometria.AutenticarAsync("Verifica tu identidad para abrir la app."))
        {
            await EntrarAsync();
            return;
        }
        // V19: si la biometría falla se pide el PIN
        string detalle = string.IsNullOrWhiteSpace(Biometria.UltimoError) ? "" : " (" + Biometria.UltimoError + ")";
        await DisplayAlertAsync("Error", "No se pudo verificar tu identidad" + detalle + ". Usa tu PIN o contraseña.", "Aceptar");
        MostrarPin();
    }

    private void btnUsarPin_Clicked(object sender, EventArgs e)
    {
        MostrarPin();
    }

    private void btnUsarPassword_Clicked(object sender, EventArgs e)
    {
        slPin.IsVisible = false;
        slPassword.IsVisible = true;
        lblInstruccion.Text = "Ingresa tu contraseña para continuar.";
        txtPassword.Focus();
    }

    private void MostrarPin()
    {
        slPassword.IsVisible = false;
        slPin.IsVisible = true;
        lblInstruccion.Text = "Ingresa tu PIN para continuar.";
        txtPin.Focus();
    }

    // ===== Desbloquear con PIN =====
    private async void btnDesbloquear_Clicked(object sender, EventArgs e)
    {
        try
        {
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
            string pinGuardado = await PinStorage.ObtenerAsync() ?? "";
            if (pin == pinGuardado)
            {
                await EntrarAsync();
                return;
            }
            txtPin.Text = "";
            await IntentoFallidoAsync("PIN incorrecto.");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Desbloquear con contraseña =====
    private async void btnIngresarPassword_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!btnIngresarPassword.IsEnabled)
            {
                return;
            }
            string contrasena = txtPassword.Text ?? "";
            if (contrasena.Length == 0)
            {
                await DisplayAlertAsync("Error", "Ingresa tu contraseña.", "Aceptar");
                return;
            }
            bool correcta = await Task.Run(() => App.MovimientoRepo.VerificarContrasena(contrasena));
            if (correcta)
            {
                await EntrarAsync();
                return;
            }
            txtPassword.Text = "";
            await IntentoFallidoAsync("Contraseña incorrecta.");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // V18: cuenta el error; al tercero se espera 30 segundos
    private async Task IntentoFallidoAsync(string mensaje)
    {
        intentosFallidos = intentosFallidos + 1;
        int restantes = maximoIntentos - intentosFallidos;
        if (restantes > 0)
        {
            await DisplayAlertAsync("Error", mensaje + " Te quedan " + restantes + " intentos.", "Aceptar");
            return;
        }
        await DisplayAlertAsync("Error", "Demasiados intentos. Espera 30 segundos.", "Aceptar");
        await EsperarBloqueo();
    }

    // V18: bloquea el ingreso durante 30 segundos con cuenta regresiva
    private async Task EsperarBloqueo()
    {
        SetEntradaHabilitada(false);
        lblEspera.IsVisible = true;
        for (int segundos = segundosEspera; segundos > 0; segundos--)
        {
            lblEspera.Text = "Espera " + segundos + " segundos.";
            await Task.Delay(1000);
        }
        intentosFallidos = 0;
        lblEspera.IsVisible = false;
        SetEntradaHabilitada(true);
    }

    private void SetEntradaHabilitada(bool habilitada)
    {
        txtPin.IsEnabled = habilitada;
        btnDesbloquear.IsEnabled = habilitada;
        txtPassword.IsEnabled = habilitada;
        btnIngresarPassword.IsEnabled = habilitada;
        btnHuella.IsEnabled = habilitada;
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