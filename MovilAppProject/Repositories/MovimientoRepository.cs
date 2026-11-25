using MovilAppProject.Models;
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
                List<Movimiento> movimientos;
                lock (_bloqueo)
                {
                    movimientos = _conn!.Table<Movimiento>()
                        .Where(m => m.Fecha >= inicio && m.Fecha < fin)
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
