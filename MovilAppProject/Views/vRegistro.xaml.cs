using MovilAppProject.Models;
using MovilAppProject.Utils;

namespace MovilAppProject.Views;

// Registro del único usuario del dispositivo. Se muestra solo cuando no hay un usuario activo
// (primer uso, o después de inactivar la cuenta desde Ajustes).
public partial class vRegistro : ContentPage
{
    public vRegistro()
    {
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        return true;
    }

    private async void btnCrear_Clicked(object sender, EventArgs e)
    {
        try
        {
            string nombre = (txtUsuario.Text ?? "").Trim();
            string contrasena = txtPassword.Text ?? "";
            string confirmar = txtConfirmarPassword.Text ?? "";
            string pin = (txtPin.Text ?? "").Trim();
            string confirmarPin = (txtConfirmarPin.Text ?? "").Trim();

            if (nombre.Length < 3 || nombre.Length > 20)
            {
                await DisplayAlertAsync("Error", "El usuario debe tener de 3 a 20 caracteres.", "Aceptar");
                return;
            }
            if (!ContrasenaValida(contrasena))
            {
                await DisplayAlertAsync("Error", "La contraseña debe tener al menos 6 caracteres, con letras y números.", "Aceptar");
                return;
            }
            if (contrasena != confirmar)
            {
                await DisplayAlertAsync("Error", "Las contraseñas no coinciden.", "Aceptar");
                return;
            }
            // V16: exactamente 4 dígitos
            if (!PinValido(pin))
            {
                await DisplayAlertAsync("Error", "El PIN debe tener exactamente 4 dígitos.", "Aceptar");
                return;
            }
            // V17: los dos PIN deben ser iguales
            if (pin != confirmarPin)
            {
                await DisplayAlertAsync("Error", "Los PIN no coinciden.", "Aceptar");
                return;
            }

            btnCrear.IsEnabled = false;
            Usuario? usuario = await Task.Run(() => App.MovimientoRepo.RegistrarUsuario(nombre, contrasena));
            if (usuario == null)
            {
                btnCrear.IsEnabled = true;
                string mensaje = App.MovimientoRepo.StatusMessage;
                await DisplayAlertAsync("Error", mensaje.StartsWith("Error: ") ? mensaje.Substring(7) : mensaje, "Aceptar");
                return;
            }
            // El PIN va a SecureStorage (cifrado)
            await PinStorage.GuardarAsync(pin);
            Sesion.Iniciar(usuario.Id, usuario.Nombre);
            // PB27: respalda el usuario y le asigna en el servidor los movimientos anteriores al login
            _ = Services.ServicioWeb.SincronizarAsync();
            Sesion.BloqueoVisible = false;
            await DisplayAlertAsync("Éxito", "Cuenta creada. Bienvenido, " + usuario.Nombre + ".", "Aceptar");
            // Se reconstruye la app para que todas las pantallas lean los datos de este usuario
            Application.Current!.Windows[0].Page = new AppShell();
        }
        catch (Exception ex)
        {
            btnCrear.IsEnabled = true;
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // Al menos 6 caracteres, con una letra y un número
    private static bool ContrasenaValida(string contrasena)
    {
        return contrasena.Length >= 6
            && contrasena.Any(char.IsLetter)
            && contrasena.Any(char.IsDigit);
    }

    private static bool PinValido(string pin)
    {
        return pin.Length == 4 && pin.All(char.IsDigit);
    }
}