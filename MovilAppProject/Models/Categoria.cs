using SQLite;

namespace MovilAppProject.Models
{
    // Categoría predefinida (Comida, Vivienda, Restaurantes...). La categoría "Ingreso" no tiene grupo.
    [Table("categoria")]
    public class Categoria
    {
        public const string NombreIngreso = "Ingreso";

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull, Unique, MaxLength(40)]
        public string Nombre { get; set; } = string.Empty;

        // Id del grupo de la regla; null solo en la categoría "Ingreso"
        [Indexed]
        public int? GrupoId { get; set; }

        [Ignore]
        public bool EsIngreso => Nombre == NombreIngreso;

        // El Picker muestra este texto
        public override string ToString() => Nombre;
    }
}
