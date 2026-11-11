using MovilAppProject.Models;
using SQLite;

namespace MovilAppProject.Repositories
{
    // Acceso a la base de datos SQLite local de GastoFácil (PB03 / PB06)
    public class MovimientoRepository
    {
        string _dbPath;
        private SQLiteConnection? _conn;

        // Mensaje del último resultado (igual que en el ejemplo de la Semana 5)
        public string StatusMessage { get; set; } = string.Empty;

        // Constructor: recibe la ruta de la base desde MauiProgram.cs
        public MovimientoRepository(string path)
        {
            _dbPath = path;
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
        }

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
                    // Necesidades
                    new() { Nombre = "Comida", GrupoId = GrupoPresupuesto.NecesidadesId },
                    new() { Nombre = "Vivienda", GrupoId = GrupoPresupuesto.NecesidadesId },
                    new() { Nombre = "Servicios básicos", GrupoId = GrupoPresupuesto.NecesidadesId },
                    new() { Nombre = "Transporte", GrupoId = GrupoPresupuesto.NecesidadesId },
                    new() { Nombre = "Salud", GrupoId = GrupoPresupuesto.NecesidadesId },
                    new() { Nombre = "Educación", GrupoId = GrupoPresupuesto.NecesidadesId },
                    // Ahorro e inversión
                    new() { Nombre = "Ahorro", GrupoId = GrupoPresupuesto.AhorroInversionId },
                    new() { Nombre = "Inversión", GrupoId = GrupoPresupuesto.AhorroInversionId },
                    new() { Nombre = "Fondo de emergencia", GrupoId = GrupoPresupuesto.AhorroInversionId },
                    new() { Nombre = "Pago de deudas", GrupoId = GrupoPresupuesto.AhorroInversionId },
                    // Lujos
                    new() { Nombre = "Restaurantes", GrupoId = GrupoPresupuesto.LujosId },
                    new() { Nombre = "Entretenimiento", GrupoId = GrupoPresupuesto.LujosId },
                    new() { Nombre = "Ropa", GrupoId = GrupoPresupuesto.LujosId },
                    new() { Nombre = "Suscripciones", GrupoId = GrupoPresupuesto.LujosId },
                    new() { Nombre = "Viajes", GrupoId = GrupoPresupuesto.LujosId },
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
                int result = _conn!.Insert(movimiento);
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
                int result = _conn!.Update(movimiento);
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
                int result = _conn!.Delete<Movimiento>(id);
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

                List<Movimiento> movimientos = _conn!.Table<Movimiento>()
                    .Where(m => m.Fecha >= inicio && m.Fecha < fin)
                    .ToList()
                    .OrderByDescending(m => m.Fecha)
                    .ThenByDescending(m => m.Id)
                    .ToList();

                Dictionary<int, string> categorias = _conn.Table<Categoria>()
                    .ToList()
                    .ToDictionary(c => c.Id, c => c.Nombre);

                foreach (Movimiento m in movimientos)
                    m.NombreCategoria = categorias.TryGetValue(m.CategoriaId, out string? nombre) ? nombre : "";

                return movimientos;
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
                return _conn!.Table<Categoria>()
                    .Where(c => c.GrupoId != null)
                    .OrderBy(c => c.Id)
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            return new List<Categoria>();
        }

        // Categoría "Ingreso" que se guarda en todos los ingresos (V05)
        public Categoria? ObtenerCategoriaIngreso()
        {
            try
            {
                Init();
                return _conn!.Table<Categoria>()
                    .Where(c => c.Nombre == Categoria.NombreIngreso)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            return null;
        }
    }
}
