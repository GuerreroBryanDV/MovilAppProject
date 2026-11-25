namespace MovilAppProject.Models
{
    // Porcentajes de la regla. No es una tabla: se guarda en Preferences.
    public class PerfilPresupuesto
    {
        public const string NombrePersonalizado = "Personalizado";

        public string Nombre { get; set; } = string.Empty;
        public int PorcentajeNecesidades { get; set; }
        public int PorcentajeAhorroInversion { get; set; }
        public int PorcentajeLujos { get; set; }
        public bool EsFijo { get; set; }

        public int Suma => PorcentajeNecesidades + PorcentajeAhorroInversion + PorcentajeLujos;
        public bool EsValido => Suma == 100; // V15

        public static PerfilPresupuesto Regla50_40_10 => new()
        {
            Nombre = "50/40/10", PorcentajeNecesidades = 50, PorcentajeAhorroInversion = 40, PorcentajeLujos = 10, EsFijo = true
        };

        public static PerfilPresupuesto Clasica50_20_30 => new()
        {
            Nombre = "Clásica 50/20/30", PorcentajeNecesidades = 50, PorcentajeAhorroInversion = 20, PorcentajeLujos = 30, EsFijo = true
        };

        // Perfil con los porcentajes que escribe el usuario
        public static PerfilPresupuesto Personalizado(int necesidades, int ahorroInversion, int lujos) => new()
        {
            Nombre = NombrePersonalizado, PorcentajeNecesidades = necesidades, PorcentajeAhorroInversion = ahorroInversion, PorcentajeLujos = lujos, EsFijo = false
        };
    }
}
