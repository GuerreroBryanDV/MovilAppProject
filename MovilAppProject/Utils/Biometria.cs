using Plugin.Maui.Biometric;

namespace MovilAppProject.Utils
{
    // PB18 · Huella, Face ID o Windows Hello con Plugin.Maui.Biometric (D16).
    // Lo usan vDesbloqueo (al abrir la app) y vAjustes (para desactivar el bloqueo, D19),
    // así las dos pantallas verifican la identidad de la misma forma.
    public static class Biometria
    {
        // V19: true si el dispositivo tiene biometría y hay una huella o rostro registrado
        public static async Task<bool> DisponibleAsync()
        {
            try
            {
                BiometricHwStatus estado = await BiometricAuthenticationService.Default
                    .GetAuthenticationStatusAsync(AuthenticatorStrength.Strong);
                return estado == BiometricHwStatus.Success;
            }
            catch
            {
                // Si el dispositivo no responde se usa solo el PIN
                return false;
            }
        }

        // true si el usuario se verificó con su huella o rostro.
        // Si falla, cancela o toca "Usar PIN" devuelve false y la pantalla pide el PIN.
        public static async Task<bool> AutenticarAsync(string descripcion)
        {
            try
            {
                AuthenticationRequest solicitud = new()
                {
                    Title = "GastoFácil",
                    Description = descripcion,
                    NegativeText = "Usar PIN",
                    AllowPasswordAuth = false
                };

                AuthenticationResponse respuesta = await BiometricAuthenticationService.Default
                    .AuthenticateAsync(solicitud, CancellationToken.None);

                return respuesta.Status == BiometricResponseStatus.Success;
            }
            catch
            {
                // Cualquier falla del lector se trata igual que una huella no reconocida
                return false;
            }
        }
    }
}
