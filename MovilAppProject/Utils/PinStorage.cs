namespace MovilAppProject.Utils
{
    // PB17 / PB18 · El PIN se guarda en SecureStorage (Keystore en Android, Keychain en iOS)
    // y no en Preferences, porque Preferences es texto plano (RNF05, Analisis_BD 2.3).
    public static class PinStorage
    {
        // Nombre de la clave dentro de SecureStorage
        const string ClavePin = "Pin";

        public static async Task GuardarAsync(string pin)
        {
            await SecureStorage.Default.SetAsync(ClavePin, pin);
        }

        // Devuelve null si no hay PIN guardado
        public static async Task<string?> ObtenerAsync()
        {
            await MigrarDePreferencesAsync();
            return await SecureStorage.Default.GetAsync(ClavePin);
        }

        public static async Task<bool> ExisteAsync()
        {
            return !string.IsNullOrEmpty(await ObtenerAsync());
        }

        public static void Borrar()
        {
            SecureStorage.Default.Remove(ClavePin);
            Preferences.Remove(ClavePin);
        }

        // Versiones anteriores guardaban el PIN en Preferences con la misma clave.
        // Si existe, se pasa a SecureStorage y se borra el texto plano.
        private static async Task MigrarDePreferencesAsync()
        {
            if (!Preferences.ContainsKey(ClavePin))
            {
                return;
            }

            string pinAnterior = Preferences.Get(ClavePin, "");
            if (!string.IsNullOrEmpty(pinAnterior))
            {
                await SecureStorage.Default.SetAsync(ClavePin, pinAnterior);
            }
            Preferences.Remove(ClavePin);
        }
    }
}
