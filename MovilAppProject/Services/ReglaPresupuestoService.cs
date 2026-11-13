using Microsoft.Maui.Graphics;
using MovilAppProject.Models;
using MovilAppProject.Repositories;

namespace MovilAppProject.Services
{
    // Estado del semáforo de un grupo (PB13 · V12)
    public enum EstadoSemaforo
    {
        Verde = 0,     // gasto <= 100 % de la meta
        Amarillo = 1,  // gasto > 100 % y <= 150 % de la meta
        Rojo = 2       // gasto > 150 % de la meta
    }

    // Resultado del cálculo de UN grupo de la regla (Necesidades, Ahorro e inversión o Lujos)
    public class ResumenGrupo
    {
        public int GrupoId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        // Meta del perfil en % (ej. 50) y en dólares (ej. 500,00 con ingresos de 1000)
        public int PorcentajeMeta { get; set; }
        public decimal Meta { get; set; }

        // Total gastado en el grupo y su % real sobre los ingresos del mes
        public decimal Gastado { get; set; }
        public decimal PorcentajeReal { get; set; }

        // Dinero restante (si no pasó la meta) o excedido (si la pasó)
        public decimal Restante { get; set; }
        public decimal Excedido { get; set; }
        public bool EstaExcedido => Excedido > 0;

        // Gasto frente a la meta en %: 120 significa que gastó 120 % de su meta
        public decimal PorcentajeDeLaMeta { get; set; }

        // Valor de 0 a 1 para un ProgressBar (no pasa de 1)
        public double Progreso { get; set; }

        // PB13 · V12: color del semáforo
        public EstadoSemaforo Semaforo { get; set; }

        // Texto de la etiqueta del semáforo (igual que el mockup de vRegla)
        public string TextoSemaforo
        {
            get
            {
                if (Semaforo == EstadoSemaforo.Verde)
                {
                    return "En meta";
                }

                if (Semaforo == EstadoSemaforo.Amarillo)
                {
                    return "Cerca";
                }

                return "Fuera de meta";
            }
        }

        // Ahorro e inversión es un mínimo a alcanzar; Necesidades y Lujos son un tope máximo
        public bool EsAhorro => GrupoId == GrupoPresupuesto.AhorroInversionId;

        // Línea del mockup: "de 50 % · quedan $100,00 · tope máximo"
        public string TextoDetalle
        {
            get
            {
                string texto = "de " + PorcentajeMeta + " % · ";

                if (EsAhorro)
                {
                    if (Restante > 0)
                    {
                        texto = texto + "faltan $" + Restante.ToString("N2");
                    }
                    else
                    {
                        texto = texto + "meta cumplida";
                    }

                    return texto + " · mínimo a alcanzar";
                }

                if (EstaExcedido)
                {
                    texto = texto + "pasado $" + Excedido.ToString("N2");
                }
                else
                {
                    texto = texto + "quedan $" + Restante.ToString("N2");
                }

                return texto + " · tope máximo";
            }
        }

        // --- Solo para dibujar la barra de vRegla (valores de 0 a 1) ---
        // Ancho de la parte llena (% real) y posición de la marca de la meta.
        // La barra completa equivale a la meta más alta del perfil + 20 %, como en el mockup.
        public double AnchoBarra { get; set; }
        public double PosicionMeta { get; set; }

        // Rectángulos para AbsoluteLayout (x, y, ancho, alto)
        public Rect RectBarra => new Rect(0, 4, AnchoBarra, 6);
        public Rect RectMeta => new Rect(PosicionMeta, 0, 2, 14);
    }

    // Resultado completo de la regla de un mes
    public class ResultadoRegla
    {
        public decimal Ingresos { get; set; }

        // V11: false cuando los ingresos del mes son 0 (no se calculan porcentajes)
        public bool HayIngresos { get; set; }

        // Aviso para la vista (vacío si se pudo calcular)
        public string Mensaje { get; set; } = string.Empty;

        public PerfilPresupuesto Perfil { get; set; } = PerfilPresupuesto.Regla50_40_10;

        public List<ResumenGrupo> Grupos { get; set; } = new();

        // "Cumplimiento del mes: 1 de 3 grupos" (grupos en verde)
        public int GruposEnMeta
        {
            get
            {
                int total = 0;
                foreach (ResumenGrupo grupo in Grupos)
                {
                    if (grupo.Semaforo == EstadoSemaforo.Verde)
                    {
                        total = total + 1;
                    }
                }
                return total;
            }
        }
    }

    // PB11 · Lógica de la regla en un servicio independiente de la interfaz (RF05, RNF07).
    // No usa ninguna vista: solo lee los movimientos desde MovimientoRepository.
    // Lo pueden reutilizar vRegla (PB12), vInicio y vRecomendaciones (PB22).
    public class ReglaPresupuestoService
    {
        // Mensaje exacto de la hoja Validaciones (V11)
        public const string MensajeSinIngresos = "Registra un ingreso para ver tu regla del mes.";

        // Límites del semáforo (V12)
        const decimal limiteVerde = 100;
        const decimal limiteAmarillo = 150;

        private readonly MovimientoRepository _repo;

        // Mensaje del último resultado (igual que en el repositorio)
        public string StatusMessage { get; set; } = string.Empty;

        // Recibe el repositorio por inyección de dependencias (MauiProgram.cs)
        public ReglaPresupuestoService(MovimientoRepository repo)
        {
            _repo = repo;
        }

        // Calcula la regla del mes con el perfil por defecto 50/40/10.
        // En PB15 se pasará el perfil guardado en Preferences.
        public ResultadoRegla CalcularRegla(int anio, int mes)
        {
            return CalcularRegla(anio, mes, PerfilPresupuesto.Regla50_40_10);
        }

        // Calcula por grupo: total gastado, % real sobre ingresos, meta y dinero restante o excedido
        public ResultadoRegla CalcularRegla(int anio, int mes, PerfilPresupuesto perfil)
        {
            ResultadoRegla resultado = new() { Perfil = perfil };

            try
            {
                List<Movimiento> movimientos = _repo.ListarPorMes(anio, mes);

                // Ingresos del mes
                decimal ingresos = 0;
                foreach (Movimiento m in movimientos)
                {
                    if (m.Tipo == TipoMovimiento.Ingreso)
                    {
                        ingresos = ingresos + m.Monto;
                    }
                }
                resultado.Ingresos = ingresos;

                // V11: con ingresos 0 no se calculan porcentajes (evita dividir para cero)
                if (ingresos <= 0)
                {
                    resultado.HayIngresos = false;
                    resultado.Mensaje = MensajeSinIngresos;
                    StatusMessage = MensajeSinIngresos;
                    return resultado;
                }
                resultado.HayIngresos = true;

                // Grupo de cada categoría de gasto (Comida -> Necesidades, etc.)
                Dictionary<int, int> grupoDeCategoria = new();
                foreach (Categoria c in _repo.ObtenerCategoriasGasto())
                {
                    if (c.GrupoId is int grupoId)
                    {
                        grupoDeCategoria[c.Id] = grupoId;
                    }
                }

                // Un resumen por grupo, en el orden de la regla
                foreach (GrupoPresupuesto grupo in _repo.ObtenerGrupos())
                {
                    // Total gastado en el grupo
                    decimal gastado = 0;
                    foreach (Movimiento m in movimientos)
                    {
                        if (m.Tipo == TipoMovimiento.Gasto
                            && grupoDeCategoria.TryGetValue(m.CategoriaId, out int grupoId)
                            && grupoId == grupo.Id)
                        {
                            gastado = gastado + m.Monto;
                        }
                    }

                    int porcentajeMeta = PorcentajeDelGrupo(perfil, grupo.Id);
                    decimal meta = Math.Round(ingresos * porcentajeMeta / 100m, 2, MidpointRounding.AwayFromZero);
                    decimal porcentajeDeLaMeta = PorcentajeDeLaMeta(gastado, meta);

                    resultado.Grupos.Add(new ResumenGrupo
                    {
                        GrupoId = grupo.Id,
                        Nombre = grupo.Nombre,
                        PorcentajeMeta = porcentajeMeta,
                        Meta = meta,
                        Gastado = gastado,
                        PorcentajeReal = Math.Round(gastado * 100m / ingresos, 2, MidpointRounding.AwayFromZero),
                        Restante = gastado <= meta ? meta - gastado : 0,
                        Excedido = gastado > meta ? gastado - meta : 0,
                        PorcentajeDeLaMeta = porcentajeDeLaMeta,
                        Progreso = (double)Math.Min(porcentajeDeLaMeta / 100m, 1m),
                        Semaforo = CalcularSemaforo(gastado, meta)
                    });
                }

                CalcularBarras(resultado.Grupos);

                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                resultado.Mensaje = StatusMessage;
            }

            return resultado;
        }

        // PB13 · V12: verde <= 100 %, amarillo > 100 % y <= 150 %, rojo > 150 % de la meta en dólares.
        // Ejemplo con meta de lujos de 100 USD: 90 -> Verde, 120 -> Amarillo, 160 -> Rojo.
        public static EstadoSemaforo CalcularSemaforo(decimal gastado, decimal meta)
        {
            decimal porcentaje = PorcentajeDeLaMeta(gastado, meta);

            if (porcentaje <= limiteVerde)
            {
                return EstadoSemaforo.Verde;
            }

            if (porcentaje <= limiteAmarillo)
            {
                return EstadoSemaforo.Amarillo;
            }

            return EstadoSemaforo.Rojo;
        }

        // Gasto frente a la meta en %. Si la meta es 0 (perfil personalizado con 0 %),
        // cualquier gasto ya pasa la meta y se marca en rojo.
        private static decimal PorcentajeDeLaMeta(decimal gastado, decimal meta)
        {
            if (meta <= 0)
            {
                return gastado > 0 ? limiteAmarillo + 1 : 0;
            }

            return Math.Round(gastado * 100m / meta, 2, MidpointRounding.AwayFromZero);
        }

        // Escala de las barras de vRegla: la barra completa es la meta más alta + 20 %
        // (con 50/40/10 la barra completa es 60 %, igual que el mockup).
        private static void CalcularBarras(List<ResumenGrupo> grupos)
        {
            decimal metaMasAlta = 0;
            foreach (ResumenGrupo grupo in grupos)
            {
                if (grupo.PorcentajeMeta > metaMasAlta)
                {
                    metaMasAlta = grupo.PorcentajeMeta;
                }
            }

            decimal escala = metaMasAlta > 0 ? metaMasAlta * 1.2m : 100m;

            foreach (ResumenGrupo grupo in grupos)
            {
                grupo.AnchoBarra = (double)Math.Min(grupo.PorcentajeReal / escala, 1m);
                grupo.PosicionMeta = (double)Math.Min(grupo.PorcentajeMeta / escala, 1m);
            }
        }

        // % de la meta que corresponde a cada grupo según el perfil
        private static int PorcentajeDelGrupo(PerfilPresupuesto perfil, int grupoId)
        {
            if (grupoId == GrupoPresupuesto.NecesidadesId)
            {
                return perfil.PorcentajeNecesidades;
            }

            if (grupoId == GrupoPresupuesto.AhorroInversionId)
            {
                return perfil.PorcentajeAhorroInversion;
            }

            if (grupoId == GrupoPresupuesto.LujosId)
            {
                return perfil.PorcentajeLujos;
            }

            return 0;
        }
    }
}
