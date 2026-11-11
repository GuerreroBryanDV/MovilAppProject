using SQLite;

namespace MovilAppProject.Models
{
    // Tipo de movimiento (V01): solo Gasto o Ingreso
    public enum TipoMovimiento
    {
        Gasto = 0,
        Ingreso = 1
    }

    // Un gasto o ingreso registrado por el usuario (tabla "movimiento")
    [Table("movimiento")]
    public class Movimiento
    {
        private decimal _monto;
        private DateTime _fecha = DateTime.Today;
        private string? _nota;

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public TipoMovimiento Tipo { get; set; } = TipoMovimiento.Gasto;

        // Monto en USD, siempre con 2 decimales (V03)
        [NotNull]
        public decimal Monto
        {
            get => _monto;
            set => _monto = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        // Categoría del movimiento; en un ingreso es la categoría "Ingreso" (V05)
        [Indexed, NotNull]
        public int CategoriaId { get; set; }

        // Fecha sin hora (indexada para filtrar rápido por mes)
        [Indexed, NotNull]
        public DateTime Fecha
        {
            get => _fecha;
            set => _fecha = value.Date;
        }

        // Nota opcional, máximo 100 caracteres, sin espacios al inicio y al final (V07)
        [MaxLength(100)]
        public string? Nota
        {
            get => _nota;
            set
            {
                string? limpia = value?.Trim();
                _nota = string.IsNullOrEmpty(limpia) ? null : limpia;
            }
        }

        // --- Solo para mostrar en pantalla (no se guardan en la base) ---
        [Ignore]
        public string NombreCategoria { get; set; } = string.Empty;

        // Grupo de la regla al que pertenece la categoría (vacío en un ingreso)
        [Ignore]
        public string NombreGrupo { get; set; } = string.Empty;

        [Ignore]
        public decimal MontoConSigno => Tipo == TipoMovimiento.Gasto ? -Monto : Monto;
    }
}
