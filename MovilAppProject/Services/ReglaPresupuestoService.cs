using Microsoft.Maui.Graphics;
using MovilAppProject.Models;
using MovilAppProject.Repositories;

namespace MovilAppProject.Services
{
    // Estado del semáforo de un grupo (V12)
    public enum EstadoSemaforo
    {
        // Necesidades y Lujos (tope): verde <= 100 %, amarillo <= 130 %, rojo > 130 % de la meta
        // Ahorro e inversión (mínimo): verde >= 100 %, amarillo 75 a 99 %, rojo < 75 % de la meta
        Verde = 0,
        Amarillo = 1,
        Rojo = 2
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

        // Color del semáforo (V12)
        public EstadoSemaforo Semaforo { get; set; }

        // Texto de la etiqueta del semáforo
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

        // Formato: "de 50 % · quedan $100,00 · tope máximo"
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

        // Textos de las tarjetas de vRecomendaciones

        // "LUJOS · 12 % DE 10 %"
        public string EncabezadoIA => Nombre.ToUpper() + " · " + PorcentajeReal.ToString("N0")
                                      + " % DE " + PorcentajeMeta + " %";

        // "Te pasaste $20,00 este mes." / "Te faltan $150,00 para tu meta." / "Vas bien: ..."
        public string TextoSituacion
        {
            get
            {
                if (EsAhorro)
                {
                    if (Restante > 0)
                    {
                        return "Te faltan $" + Restante.ToString("N2") + " para tu meta de ahorro.";
                    }

                    return "Cumpliste tu meta de ahorro este mes.";
                }

                if (EstaExcedido)
                {
                    return "Te pasaste $" + Excedido.ToString("N2") + " este mes.";
                }

                return "Vas bien: te quedan $" + Restante.ToString("N2") + " de margen.";
            }
        }

        // Acción de la tarjeta
        public string TextoAccion
        {
            get
            {
                if (EsAhorro)
                {
                    return "Programar recordatorio";
                }

                if (GrupoId == GrupoPresupuesto.LujosId)
                {
                    return "Ver movimientos";
                }

                return "Registrar movimiento";
            }
        }

        // Solo para dibujar la barra de vRegla (valores de 0 a 1)
        // Ancho de la parte llena (% real) y posición de la marca de la meta.
        // La barra completa equivale a la meta más alta del perfil + 20 %.
        public double AnchoBarra { get; set; }
        public double PosicionMeta { get; set; }

        // Rectángulos para AbsoluteLayout (x, y, ancho, alto)
        public Rect RectBarra => new Rect(0, 4, AnchoBarra, 6);
        public Rect RectMeta => new Rect(PosicionMeta, 0, 2, 14);
    }

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

    // Cálculo de la regla 50/40/10 (PB11). No depende de ninguna vista: lo usan vRegla, vInicio y vRecomendaciones.
    public class ReglaPresupuestoService
    {
        // Mensaje exacto (V11)
        public const string MensajeSinIngresos = "Registra un ingreso para ver tu regla del mes.";

        // Claves de Preferences del perfil de porcentajes
        public const string ClavePerfil = "PerfilRegla";
        public const string ClaveNecesidades = "PorcentajeNecesidades";
        public const string ClaveAhorro = "PorcentajeAhorroInversion";
        public const string ClaveLujos = "PorcentajeLujos";

        // Límites del semáforo.
        // Tope (Necesidades y Lujos): verde hasta 100 %, amarillo hasta 130 %.
        // Mínimo (Ahorro e inversión): verde desde 100 %, amarillo desde 75 %.
        const decimal limiteVerde = 100;
        const decimal limiteAmarillo = 130;
        const decimal limiteAhorroAmarillo = 75;

        private readonly MovimientoRepository _repo;

        public string StatusMessage { get; set; } = string.Empty;

        public ReglaPresupuestoService(MovimientoRepository repo)
        {
            _repo = repo;
        }

        // Calcula la regla del mes con el perfil que eligió el usuario.
        // Si no ha elegido ninguno se usa 50/40/10 (por defecto).
        public ResultadoRegla CalcularRegla(int anio, int mes)
        {
            return CalcularRegla(anio, mes, ObtenerPerfilActual());
        }

        // Lee el perfil guardado. Si no hay nada guardado o los datos no son válidos,
        // devuelve el perfil por defecto 50/40/10.
        public PerfilPresupuesto ObtenerPerfilActual()
        {
            string nombre = Preferences.Get(ClavePerfil, PerfilPresupuesto.Regla50_40_10.Nombre);

            if (nombre == PerfilPresupuesto.Clasica50_20_30.Nombre)
            {
                return PerfilPresupuesto.Clasica50_20_30;
            }

            if (nombre == PerfilPresupuesto.NombrePersonalizado)
            {
                PerfilPresupuesto personalizado = PerfilPresupuesto.Personalizado(
                    Preferences.Get(ClaveNecesidades, 50),
                    Preferences.Get(ClaveAhorro, 40),
                    Preferences.Get(ClaveLujos, 10));

                if (personalizado.EsValido)
                {
                    return personalizado;
                }
            }

            return PerfilPresupuesto.Regla50_40_10;
        }

        // Guarda el perfil elegido. Solo se guarda si los porcentajes suman 100 (V15).
        public bool GuardarPerfil(PerfilPresupuesto perfil)
        {
            try
            {
                if (!perfil.EsValido)
                {
                    throw new Exception("Los porcentajes deben sumar 100 % (actual: " + perfil.Suma + " %).");
                }

                Preferences.Set(ClavePerfil, perfil.Nombre);
                Preferences.Set(ClaveNecesidades, perfil.PorcentajeNecesidades);
                Preferences.Set(ClaveAhorro, perfil.PorcentajeAhorroInversion);
                Preferences.Set(ClaveLujos, perfil.PorcentajeLujos);

                StatusMessage = "Porcentajes guardados";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                return false;
            }
        }

        // Calcula por grupo: total gastado, % real sobre ingresos, meta y dinero restante o excedido
        public ResultadoRegla CalcularRegla(int anio, int mes, PerfilPresupuesto perfil)
        {
            return CalcularRegla(_repo.ListarPorMes(anio, mes), perfil);
        }

        // Calcula con los movimientos del mes que la pantalla ya leyó,
        // así vInicio consulta la base una sola vez para saldo, regla y movimientos.
        public ResultadoRegla CalcularRegla(List<Movimiento> movimientos)
        {
            return CalcularRegla(movimientos, ObtenerPerfilActual());
        }

        public ResultadoRegla CalcularRegla(List<Movimiento> movimientos, PerfilPresupuesto perfil)
        {
            ResultadoRegla resultado = new() { Perfil = perfil };

            try
            {
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

                foreach (GrupoPresupuesto grupo in _repo.ObtenerGrupos())
                {
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
                        Semaforo = CalcularSemaforo(grupo.Id, gastado, meta)
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

        // Semáforo de un grupo.
        // Ahorro e inversión es un mínimo a alcanzar; Necesidades y Lujos son un tope.
        // Ejemplo con meta de lujos de 100 USD: 90 -> Verde, 120 -> Amarillo, 160 -> Rojo.
        public static EstadoSemaforo CalcularSemaforo(int grupoId, decimal gastado, decimal meta)
        {
            if (grupoId == GrupoPresupuesto.AhorroInversionId)
            {
                return CalcularSemaforoAhorro(gastado, meta);
            }

            return CalcularSemaforoTope(gastado, meta);
        }

        // Mínimo: verde con 100 % o más de la meta, amarillo de 75 % a 99 %, rojo con menos de 75 %.
        // Si la meta es 0 % (perfil personalizado) no hay nada que alcanzar: verde.
        private static EstadoSemaforo CalcularSemaforoAhorro(decimal gastado, decimal meta)
        {
            if (meta <= 0)
            {
                return EstadoSemaforo.Verde;
            }

            decimal porcentaje = PorcentajeDeLaMeta(gastado, meta);

            if (porcentaje >= limiteVerde)
            {
                return EstadoSemaforo.Verde;
            }

            if (porcentaje >= limiteAhorroAmarillo)
            {
                return EstadoSemaforo.Amarillo;
            }

            return EstadoSemaforo.Rojo;
        }

        // Tope: verde hasta 100 % de la meta, amarillo hasta 130 %, rojo por encima.
        private static EstadoSemaforo CalcularSemaforoTope(decimal gastado, decimal meta)
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
        // (con 50/40/10 la barra completa es 60 %).
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
