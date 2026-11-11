using SQLite;

namespace MovilAppProject.Models
{
    // Grupo de la regla: Necesidades, Ahorro e inversión o Lujos (Id fijos)
    [Table("grupo_presupuesto")]
    public class GrupoPresupuesto
    {
        public const int NecesidadesId = 1;
        public const int AhorroInversionId = 2;
        public const int LujosId = 3;

        [PrimaryKey]
        public int Id { get; set; }

        [NotNull, MaxLength(30)]
        public string Nombre { get; set; } = string.Empty;

        public int Orden { get; set; }

        public override string ToString() => Nombre;
    }
}
