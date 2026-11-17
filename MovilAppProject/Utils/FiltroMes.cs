namespace MovilAppProject.Utils
{
    // PB14 · Mes que se muestra en vInicio y vRegla (RF12).
    // Es estático para que las dos pantallas compartan el mismo mes al cambiar de pestaña.
    public static class FiltroMes
    {
        // Siempre el día 1 del mes seleccionado
        public static DateTime Mes { get; private set; } =
            new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        // V10: no se permiten meses futuros, por eso › se deshabilita en el mes actual
        public static bool EsMesActual =>
            Mes.Year == DateTime.Today.Year && Mes.Month == DateTime.Today.Month;

        public static void Anterior()
        {
            Mes = Mes.AddMonths(-1);
        }

        public static void Siguiente()
        {
            if (!EsMesActual)
            {
                Mes = Mes.AddMonths(1);
            }
        }

        // "Noviembre 2026"
        public static string Texto()
        {
            string texto = Mes.ToString("MMMM yyyy");
            return texto.Substring(0, 1).ToUpper() + texto.Substring(1);
        }
    }
}
