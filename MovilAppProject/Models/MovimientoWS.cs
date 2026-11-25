namespace MovilAppProject.Models
{
    // Movimiento tal como lo devuelve el servicio web; los nombres van en minúscula como las columnas de MySQL.
    public class MovimientoWS
    {
        public int codigo { get; set; }
        public string tipo { get; set; } = string.Empty;
        public decimal monto { get; set; }
        public int categoria_id { get; set; }
        public string fecha { get; set; } = string.Empty;
        public string? nota { get; set; }
        // Uid del usuario dueño (null = movimiento anterior al login)
        public string? usuario_uid { get; set; }
    }
}
