namespace MovilAppProject.Models
{
    // PB25 · Movimiento tal como lo devuelve el servicio web (MySQL).
    // Los nombres van en minúscula, igual que las columnas de la tabla
    // y que el modelo Estudiante del ejemplo de la Semana 6.
    public class MovimientoWS
    {
        public int codigo { get; set; }
        public string tipo { get; set; } = string.Empty;
        public decimal monto { get; set; }
        public int categoria_id { get; set; }
        public string fecha { get; set; } = string.Empty;
        public string? nota { get; set; }
    }
}
