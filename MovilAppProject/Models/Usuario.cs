using SQLite;

namespace MovilAppProject.Models
{
    // Usuario único de la app en este dispositivo (tabla "usuario").
    // Nunca se borra: al inactivar la cuenta se marca Activo = false y se guarda la fecha de baja
    // (borrado lógico), así su historial queda disponible para la sincronización.
    [Table("usuario")]
    public class Usuario
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Identificador único global (para sincronizar con el servidor, p. ej. Supabase o MySQL)
        [NotNull]
        public string Uid { get; set; } = Guid.NewGuid().ToString();

        [NotNull]
        public string Nombre { get; set; } = string.Empty;

        // La contraseña nunca se guarda: solo su hash PBKDF2 y la sal
        [NotNull]
        public string HashContrasena { get; set; } = string.Empty;

        [NotNull]
        public string Sal { get; set; } = string.Empty;

        [Indexed, NotNull]
        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Fecha en que se inactivó la cuenta (null mientras esté activa)
        public DateTime? FechaBaja { get; set; }
    }
}