using System.Security.Cryptography;
using MovilAppProject.Models;
using MovilAppProject.Utils;
using SQLite;

namespace MovilAppProject.Repositories
{
    // Acceso a la base de datos SQLite local de GastoFácil (PB03 / PB06)
    public class MovimientoRepository
    {
        string _dbPath;
        private SQLiteConnection? _conn;

        // PB19: las pantallas leen en segundo plano mientras vMovimiento guarda en el hilo principal.
        // El candado evita que dos operaciones usen la misma conexión al mismo tiempo.
        private readonly object _bloqueo = new();

        // Mensaje del último resultado (igual que en el ejemplo de la Semana 5)
        public string StatusMessage { get; set; } = string.Empty;

        // Constructor: recibe la ruta de la base desde MauiProgram.cs.
        // La base y las tablas se crean al iniciar la app (PB03).
        public MovimientoRepository(string path)
        {
            _dbPath = path;
            Init();
        }

        // Conexión a la base de datos, creación de tablas y datos iniciales
        private void Init()
        {
            if (_conn is not null)
                return;

            _conn = new SQLiteConnection(_dbPath);
            _conn.CreateTable<GrupoPresupuesto>();
            _conn.CreateTable<Categoria>();
            _conn.CreateTable<Movimiento>();
            _conn.CreateTable<Usuario>();

            CargarDatosPredefinidos(_conn);

            // PB19: categorías y grupos son fijos en v1.0 (no hay categorías personalizadas),
            // así que se leen una sola vez y no en cada consulta del mes.
            _categorias = _conn.Table<Categoria>().OrderBy(c => c.Id).ToList();
            _grupos = _conn.Table<GrupoPresupuesto>().OrderBy(g => g.Orden).ToList();
        }

        // PB19: copia en memoria de las tablas fijas (se llenan en Init)
        private List<Categoria> _categorias = new();
        private List<GrupoPresupuesto> _grupos = new();

        // Carga los grupos y las categorías una sola vez (si las tablas están vacías)
        private static void CargarDatosPredefinidos(SQLiteConnection conn)
        {
            if (conn.Table<GrupoPresupuesto>().Count() == 0)
            {
                conn.InsertAll(new List<GrupoPresupuesto>
                {
                    new() { Id = GrupoPresupuesto.NecesidadesId, Nombre = "Necesidades", Orden = 1 },
                    new() { Id = GrupoPresupuesto.AhorroInversionId, Nombre = "Ahorro e inversión", Orden = 2 },
                    new() { Id = GrupoPresupuesto.LujosId, Nombre = "Lujos", Orden = 3 }
                });
            }

            if (conn.Table<Categoria>().Count() == 0)
            {
                conn.InsertAll(new List<Categoria>
                {
                    // Categorías predefinidas según el documento de requisitos (sección 2.3)
                    // Necesidades
                    new() { Nombre = "Comida", GrupoId = GrupoPresupuesto.NecesidadesId },
                    new() { Nombre = "Vivienda", GrupoId = GrupoPresupuesto.NecesidadesId },
                    new() { Nombre = "Salud", GrupoId = GrupoPresupuesto.NecesidadesId },
                    new() { Nombre = "Transporte", GrupoId = GrupoPresupuesto.NecesidadesId },
                    new() { Nombre = "Servicios", GrupoId = GrupoPresupuesto.NecesidadesId },
                    // Ahorro e inversión
                    new() { Nombre = "Fondo de emergencia", GrupoId = GrupoPresupuesto.AhorroInversionId },
                    new() { Nombre = "Educación", GrupoId = GrupoPresupuesto.AhorroInversionId },
                    new() { Nombre = "Invertir", GrupoId = GrupoPresupuesto.AhorroInversionId },
                    // Lujos
                    new() { Nombre = "Restaurantes", GrupoId = GrupoPresupuesto.LujosId },
                    new() { Nombre = "Entretenimiento", GrupoId = GrupoPresupuesto.LujosId },
                    new() { Nombre = "Vacaciones", GrupoId = GrupoPresupuesto.LujosId },
                    new() { Nombre = "Electrónicos", GrupoId = GrupoPresupuesto.LujosId },
                    // Sin grupo: se usa en los ingresos (V05)
                    new() { Nombre = Categoria.NombreIngreso, GrupoId = null }
                });
            }
        }

        // ---------------- Movimientos ----------------

        // Insert: guarda un gasto o un ingreso. Devuelve true si se guardó.
        public bool AgregarMovimiento(Movimiento movimiento)
        {
            try
            {
                Init();
                int result;
                lock (_bloqueo)
                {
                    movimiento.UsuarioId = Sesion.UsuarioId;
                    result = _conn!.Insert(movimiento);
                }
                if (result == 0)
                    throw new Exception("No se pudo guardar el movimiento");
                StatusMessage = "Movimiento guardado";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return false;
            }
        }

        // Update: para editar un movimiento (PB09)
        public bool EditarMovimiento(Movimiento movimiento)
        {
            try
            {
                Init();
                if (movimiento.Id <= 0)
                    throw new Exception("Seleccione un registro válido");
                int result;
                lock (_bloqueo)
                {
                    movimiento.UsuarioId = Sesion.UsuarioId;
                    result = _conn!.Update(movimiento);
                }
                if (result == 0)
                    throw new Exception("No se encontró el registro");
                StatusMessage = "Movimiento guardado";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return false;
            }
        }

        // Delete: para eliminar un movimiento (PB10)
        public bool EliminarMovimiento(int id)
        {
            try
            {
                Init();
                if (id <= 0)
                    throw new Exception("Seleccione un registro válido");
                int result;
                lock (_bloqueo)
                {
                    result = _conn!.Delete<Movimiento>(id);
                }
                if (result == 0)
                    throw new Exception("No se encontró el registro");
                StatusMessage = "Movimiento eliminado";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return false;
            }
        }

        // Select: movimientos de un mes, del más reciente al más antiguo, con el nombre de su categoría
        public List<Movimiento> ListarPorMes(int anio, int mes)
        {
            try
            {
                Init();
                DateTime inicio = new DateTime(anio, mes, 1);
                DateTime fin = inicio.AddMonths(1);

                // PB19 (RNF04): el filtro y el orden los hace SQLite usando el índice de Fecha,
                // así solo se leen los movimientos del mes y ya vienen ordenados.
                int usuarioId = Sesion.UsuarioId;
                List<Movimiento> movimientos;
                lock (_bloqueo)
                {
                    movimientos = _conn!.Table<Movimiento>()
                        .Where(m => m.Fecha >= inicio && m.Fecha < fin && m.UsuarioId == usuarioId)
                        .OrderByDescending(m => m.Fecha)
                        .ThenByDescending(m => m.Id)
                        .ToList();
                }

                Dictionary<int, Categoria> categorias = _categorias.ToDictionary(c => c.Id);
                Dictionary<int, string> grupos = _grupos.ToDictionary(g => g.Id, g => g.Nombre);

                foreach (Movimiento m in movimientos)
                {
                    if (!categorias.TryGetValue(m.CategoriaId, out Categoria? categoria))
                        continue;

                    m.NombreCategoria = categoria.Nombre;
                    // La categoría "Ingreso" no tiene grupo: NombreGrupo queda vacío
                    if (categoria.GrupoId is int grupoId && grupos.TryGetValue(grupoId, out string? nombreGrupo))
                        m.NombreGrupo = nombreGrupo;
                }

                return movimientos;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            return new List<Movimiento>();
        }

        // PB25: todos los movimientos, para respaldarlos en MySQL (servicio web)
        public List<Movimiento> ListarTodos()
        {
            try
            {
                Init();
                lock (_bloqueo)
                {
                    return _conn!.Table<Movimiento>().OrderBy(m => m.Id).ToList();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            return new List<Movimiento>();
        }

        // ---------------- Totales del mes (HU2 criterio 2 / PB07) ----------------

        public decimal ObtenerIngresosDelMes(int anio, int mes)
        {
            return ListarPorMes(anio, mes)
                .Where(m => m.Tipo == TipoMovimiento.Ingreso)
                .Sum(m => m.Monto);
        }

        public decimal ObtenerGastosDelMes(int anio, int mes)
        {
            return ListarPorMes(anio, mes)
                .Where(m => m.Tipo == TipoMovimiento.Gasto)
                .Sum(m => m.Monto);
        }

        // Saldo = ingresos - gastos del mes
        public decimal ObtenerSaldoDelMes(int anio, int mes)
        {
            return ObtenerIngresosDelMes(anio, mes) - ObtenerGastosDelMes(anio, mes);
        }

        // ---------------- Saldo acumulado y coherencia (gasto <= ingresos) ----------------

        // Saldo acumulado: ingresos - gastos de TODOS los movimientos hasta el final del mes indicado.
        // Así lo que sobra de un mes pasa al siguiente.
        public decimal ObtenerSaldoAcumuladoHasta(int anio, int mes)
        {
            try
            {
                Init();
                DateTime fin = new DateTime(anio, mes, 1).AddMonths(1);
                int usuarioId = Sesion.UsuarioId;
                List<Movimiento> lista;
                lock (_bloqueo)
                {
                    lista = _conn!.Table<Movimiento>().Where(m => m.Fecha < fin && m.UsuarioId == usuarioId).ToList();
                }
                return lista.Sum(m => m.Tipo == TipoMovimiento.Ingreso ? m.Monto : -m.Monto);
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return 0;
            }
        }

        // Valida que, al guardar o borrar un movimiento, el saldo acumulado no quede negativo
        // en ningún mes (el saldo de cada mes incluye lo que sobró de los anteriores).
        // nuevo: el movimiento que se guardará (null si se está eliminando).
        // idExcluir: Id del movimiento que se edita o elimina (0 si es uno nuevo).
        // Devuelve null si todo está bien o el mensaje de error.
        // Si los datos ya estaban negativos antes del cambio, solo se bloquea lo que lo empeore.
        public string? ValidarSaldo(Movimiento? nuevo, int idExcluir)
        {
            try
            {
                Init();
                int usuarioId = Sesion.UsuarioId;
                List<Movimiento> todos;
                lock (_bloqueo)
                {
                    todos = _conn!.Table<Movimiento>().Where(m => m.UsuarioId == usuarioId).ToList();
                }
                // Situación actual, con el movimiento original
                List<Movimiento> antes = todos;
                List<Movimiento> despues = todos.Where(m => m.Id != idExcluir).ToList();
                if (nuevo != null)
                {
                    despues.Add(nuevo);
                }
                decimal minimoAntes = SaldoMinimoMensual(antes);
                decimal minimoDespues = SaldoMinimoMensual(despues);
                if (minimoDespues >= 0 || minimoDespues >= minimoAntes)
                {
                    return null;
                }
                if (nuevo != null && nuevo.Tipo == TipoMovimiento.Gasto)
                {
                    DateTime fin = new DateTime(nuevo.Fecha.Year, nuevo.Fecha.Month, 1).AddMonths(1);
                    decimal disponible = despues.Where(m => m != nuevo && m.Fecha < fin)
                        .Sum(m => m.Tipo == TipoMovimiento.Ingreso ? m.Monto : -m.Monto);
                    if (disponible < 0)
                    {
                        disponible = 0;
                    }
                    return "El gasto supera tu saldo disponible de " + disponible.ToString("C2") + ".";
                }
                return "No se puede: tus gastos registrados superarían tus ingresos.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return "Error: " + ex.Message;
            }
        }

        // Saldo acumulado más bajo al cierre de cada mes, desde el primer movimiento hasta el último
        private static decimal SaldoMinimoMensual(List<Movimiento> movimientos)
        {
            if (movimientos.Count == 0)
            {
                return 0;
            }
            decimal acumulado = 0;
            decimal minimo = decimal.MaxValue;
            foreach (var mes in movimientos
                .GroupBy(m => new DateTime(m.Fecha.Year, m.Fecha.Month, 1))
                .OrderBy(g => g.Key))
            {
                acumulado += mes.Sum(m => m.Tipo == TipoMovimiento.Ingreso ? m.Monto : -m.Monto);
                if (acumulado < minimo)
                {
                    minimo = acumulado;
                }
            }
            return minimo;
        }
        // ---------------- Usuario (login tipo banco) ----------------

        private const int IteracionesHash = 100_000;

        // El usuario activo de este dispositivo (null si no hay ninguno registrado)
        public Usuario? ObtenerUsuarioActivo()
        {
            try
            {
                Init();
                lock (_bloqueo)
                {
                    return _conn!.Table<Usuario>().Where(u => u.Activo).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return null;
            }
        }

        // Registra al único usuario del dispositivo. Los movimientos anteriores al login pasan a ser suyos.
        public Usuario? RegistrarUsuario(string nombre, string contrasena)
        {
            try
            {
                Init();
                if (ObtenerUsuarioActivo() != null)
                {
                    StatusMessage = "Error: ya hay un usuario activo en este dispositivo";
                    return null;
                }
                byte[] sal = RandomNumberGenerator.GetBytes(16);
                Usuario usuario = new()
                {
                    Nombre = nombre.Trim(),
                    Sal = Convert.ToBase64String(sal),
                    HashContrasena = CalcularHash(contrasena, sal)
                };
                lock (_bloqueo)
                {
                    _conn!.Insert(usuario);
                    // Solo los movimientos sin dueño (de antes del login) se asignan al nuevo usuario
                    _conn.Execute("UPDATE movimiento SET UsuarioId = ? WHERE UsuarioId = 0", usuario.Id);
                }
                StatusMessage = "Usuario registrado";
                return usuario;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return null;
            }
        }

        // true si la contraseña coincide con la del usuario activo
        public bool VerificarContrasena(string contrasena)
        {
            Usuario? usuario = ObtenerUsuarioActivo();
            if (usuario == null)
            {
                return false;
            }
            byte[] sal = Convert.FromBase64String(usuario.Sal);
            byte[] esperado = Convert.FromBase64String(usuario.HashContrasena);
            byte[] actual = Convert.FromBase64String(CalcularHash(contrasena, sal));
            return CryptographicOperations.FixedTimeEquals(esperado, actual);
        }

        // Borrado lógico: la cuenta queda inactiva y con fecha de baja; sus movimientos se conservan
        public bool InactivarUsuarioActivo()
        {
            try
            {
                Init();
                Usuario? usuario = ObtenerUsuarioActivo();
                if (usuario == null)
                {
                    return false;
                }
                usuario.Activo = false;
                usuario.FechaBaja = DateTime.Now;
                lock (_bloqueo)
                {
                    _conn!.Update(usuario);
                }
                StatusMessage = "Cuenta inactivada";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                return false;
            }
        }

        private static string CalcularHash(string contrasena, byte[] sal)
        {
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, IteracionesHash, HashAlgorithmName.SHA256, 32);
            return Convert.ToBase64String(hash);
        }
        // ---------------- Categorías ----------------

        // Categorías para el Picker cuando el tipo es Gasto (sin "Ingreso")
        public List<Categoria> ObtenerCategoriasGasto()
        {
            try
            {
                Init();
                // PB19: sale de la copia en memoria (una lista nueva, para que nadie cambie la copia)
                return _categorias
                    .Where(c => c.GrupoId != null)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            return new List<Categoria>();
        }

        // Grupos de la regla (Necesidades, Ahorro e inversión, Lujos) en su orden
        public List<GrupoPresupuesto> ObtenerGrupos()
        {
            try
            {
                Init();
                // PB19: sale de la copia en memoria
                return _grupos.ToList();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            return new List<GrupoPresupuesto>();
        }

        // Categoría "Ingreso" que se guarda en todos los ingresos (V05)
        public Categoria? ObtenerCategoriaIngreso()
        {
            try
            {
                Init();
                // PB19: sale de la copia en memoria
                return _categorias.FirstOrDefault(c => c.Nombre == Categoria.NombreIngreso);
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            return null;
        }
    }
}
