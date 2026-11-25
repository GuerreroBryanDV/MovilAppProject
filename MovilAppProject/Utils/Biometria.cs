using System.Diagnostics;
using Plugin.Maui.Biometric;

namespace MovilAppProject.Utils
{
    // PB18 · Huella, rostro o Windows Hello con Plugin.Maui.Biometric (D16).
    // Lo usan vDesbloqueo (login) y vAjustes (confirmar identidad), así las dos pantallas
    // verifican la identidad de la misma forma.
    public static class Biometria
    {
        // Nivel con el que el dispositivo respondió bien (se prueba Strong y luego Weak,
        // porque el rostro de muchos teléfonos solo llega a Weak)
        private static AuthenticatorStrength _fuerza = AuthenticatorStrength.Strong;

        // V19: Disponible = true si hay biometría con una huella o rostro registrado.
        // Motivo explica por qué no (para mostrarlo en pantalla en vez de ocultar el botón sin avisar).
        public static async Task<(bool Disponible, string Motivo)> EstadoAsync()
        {
            try
            {
                BiometricHwStatus estado = await BiometricAuthenticationService.Default
                    .GetAuthenticationStatusAsync(AuthenticatorStrength.Strong);
                if (estado == BiometricHwStatus.Success && await HayRegistradaAsync())
                {
                    _fuerza = AuthenticatorStrength.Strong;
                    return (true, "");
                }
                BiometricHwStatus debil = await BiometricAuthenticationService.Default
                    .GetAuthenticationStatusAsync(AuthenticatorStrength.Weak);
                if (debil == BiometricHwStatus.Success && await HayRegistradaAsync())
                {
                    _fuerza = AuthenticatorStrength.Weak;
                    return (true, "");
                }
                if (estado == BiometricHwStatus.Success || debil == BiometricHwStatus.Success)
                {
                    // El sensor responde pero no hay ninguna huella o rostro registrado
                    return (false, Motivo(BiometricHwStatus.NotEnrolled));
                }
                Debug.WriteLine("Biometria: Strong=" + estado + ", Weak=" + debil);
                return (false, Motivo(estado == BiometricHwStatus.NoHardware ? debil : estado));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Biometria: " + ex);
                return (false, "No se pudo consultar la biometría del dispositivo.");
            }
        }

        // Algunos dispositivos (como el emulador) dicen que el sensor está listo aunque no haya
        // ninguna huella o rostro registrado; por eso se confirma con la lista de registrados.
        private static async Task<bool> HayRegistradaAsync()
        {
            try
            {
                BiometricType[] tipos = await BiometricAuthenticationService.Default.GetEnrolledBiometricTypesAsync();
                return tipos != null && tipos.Any(t => t != BiometricType.None);
            }
            catch
            {
                // Si no se puede consultar la lista, se confía en el estado del sensor
                return true;
            }
        }

        public static async Task<bool> DisponibleAsync()
        {
            return (await EstadoAsync()).Disponible;
        }

        private static string Motivo(BiometricHwStatus estado)
        {
            return estado switch
            {
                BiometricHwStatus.NoHardware => "Este dispositivo no tiene lector de huella ni de rostro.",
                BiometricHwStatus.NotEnrolled or BiometricHwStatus.PresentButNotEnrolled =>
                    "No hay huella ni rostro registrado. Regístralo en los ajustes de seguridad del teléfono.",
                BiometricHwStatus.LockedOut => "El lector está bloqueado temporalmente. Usa tu PIN o contraseña.",
                _ => "La biometría no está disponible (" + estado + ")."
            };
        }

        // true si el usuario se verificó con su huella o rostro.
        // Si falla, cancela o toca "Usar PIN" devuelve false y la pantalla pide el PIN.
        // Mensaje de Android de la última falla (vacío si fue cancelada o no hubo detalle)
        public static string UltimoError { get; private set; } = string.Empty;

        public static async Task<bool> AutenticarAsync(string descripcion)
        {
            UltimoError = string.Empty;
            try
            {
                AuthenticationRequest solicitud = new()
                {
                    Title = "GastoFácil",
                    Description = descripcion,
                    NegativeText = "Usar PIN",
                    AllowPasswordAuth = false,
                    AuthStrength = _fuerza
                };
                AuthenticationResponse respuesta = await BiometricAuthenticationService.Default
                    .AuthenticateAsync(solicitud, CancellationToken.None);
                if (respuesta.Status != BiometricResponseStatus.Success)
                {
                    UltimoError = respuesta.ErrorMsg ?? string.Empty;
                    if (UltimoError.Contains("enrolled", StringComparison.OrdinalIgnoreCase))
                    {
                        UltimoError = "no hay huellas registradas en el dispositivo";
                    }
                    Debug.WriteLine("Biometria: falla " + UltimoError);
                }
                return respuesta.Status == BiometricResponseStatus.Success;
            }
            catch (Exception ex)
            {
                // Cualquier falla del lector se trata igual que una huella no reconocida
                UltimoError = ex.Message;
                Debug.WriteLine("Biometria: " + ex);
                return false;
            }
        }
    }
}