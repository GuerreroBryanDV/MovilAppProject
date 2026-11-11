namespace MovilAppProject.Views;

// Pantalla principal de GastoFácil (según el mockup).
// Lee los movimientos del archivo que guarda vMovimiento.
// PENDIENTE BDD (Bryan): al integrar, reemplazar LeerMovimientos()
// por la consulta a la base de datos.
public partial class vInicio : ContentPage
{
    // Metas de la regla 50/40/10 (en %)
    const decimal metaNecesidades = 50;
    const decimal metaAhorro = 40;
    const decimal metaLujos = 10;

    // Cuántos movimientos se muestran en "Últimos movimientos"
    const int maximoMovimientos = 5;

    public vInicio()
    {
        InitializeComponent();
    }

    // Se ejecuta cada vez que la pantalla aparece
    // (al abrir la app y al regresar de vMovimiento), así el saldo se actualiza.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarDatos();
    }

    // Abre la pantalla "Nuevo movimiento"
    private async void btnNuevo_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new vMovimiento());
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Carga todos los datos de la pantalla =====
    private async void CargarDatos()
    {
        try
        {
            DateTime hoy = DateTime.Today;

            // Mes actual: "Noviembre 2026"
            string mes = hoy.ToString("MMMM yyyy");
            lblMes.Text = mes.Substring(0, 1).ToUpper() + mes.Substring(1);

            // Movimientos del mes actual
            List<string[]> movimientos = LeerMovimientos(hoy.Month, hoy.Year);

            // Totales
            decimal ingresos = 0;
            decimal egresos = 0;
            decimal necesidades = 0;
            decimal ahorro = 0;
            decimal lujos = 0;

            foreach (string[] m in movimientos)
            {
                string tipo = m[0];
                decimal monto = decimal.Parse(m[1]);
                string grupo = m[3];

                if (tipo == "Ingreso")
                {
                    ingresos = ingresos + monto;
                }
                else
                {
                    egresos = egresos + monto;

                    if (grupo == "Necesidades")
                    {
                        necesidades = necesidades + monto;
                    }
                    else if (grupo == "Ahorro e inversión")
                    {
                        ahorro = ahorro + monto;
                    }
                    else if (grupo == "Lujos")
                    {
                        lujos = lujos + monto;
                    }
                }
            }

            // PB07: saldo = ingresos − gastos del mes (en rojo si es negativo)
            decimal saldo = ingresos - egresos;
            lblSaldo.Text = FormatoMonto(saldo);
            if (saldo < 0)
            {
                lblSaldo.TextColor = (Color)Application.Current!.Resources["ColorGasto"];
            }
            else
            {
                lblSaldo.TextColor = Colors.White;
            }

            lblIngresos.Text = FormatoMonto(ingresos);
            lblEgresos.Text = FormatoMonto(egresos);

            MostrarRegla(ingresos, necesidades, ahorro, lujos);
            MostrarRecomendacion(ingresos, egresos);
            MostrarMovimientos(movimientos);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Regla 50/40/10 =====
    private void MostrarRegla(decimal ingresos, decimal necesidades, decimal ahorro, decimal lujos)
    {
        // V11: si no hay ingresos no se calculan porcentajes (evita dividir para cero)
        if (ingresos == 0)
        {
            pbNecesidades.Progress = 0;
            pbAhorro.Progress = 0;
            pbLujos.Progress = 0;
            lblEnMeta.Text = "";
            lblAvisoRegla.IsVisible = true;
            return;
        }

        lblAvisoRegla.IsVisible = false;

        // Porcentaje real de cada grupo sobre los ingresos
        decimal porcNecesidades = necesidades * 100 / ingresos;
        decimal porcAhorro = ahorro * 100 / ingresos;
        decimal porcLujos = lujos * 100 / ingresos;

        // La barra muestra cuánto se ha usado de la meta (máximo llena)
        pbNecesidades.Progress = CalcularBarra(porcNecesidades, metaNecesidades);
        pbAhorro.Progress = CalcularBarra(porcAhorro, metaAhorro);
        pbLujos.Progress = CalcularBarra(porcLujos, metaLujos);

        // Necesidades y Lujos son topes máximos; Ahorro es un mínimo a alcanzar
        int enMeta = 0;
        if (porcNecesidades <= metaNecesidades)
        {
            enMeta = enMeta + 1;
        }
        if (porcAhorro >= metaAhorro)
        {
            enMeta = enMeta + 1;
        }
        if (porcLujos <= metaLujos)
        {
            enMeta = enMeta + 1;
        }

        lblEnMeta.Text = enMeta + " de 3 en meta";
    }

    // Devuelve un valor de 0 a 1 para la ProgressBar
    private double CalcularBarra(decimal porcentaje, decimal meta)
    {
        decimal valor = porcentaje / meta;
        if (valor > 1)
        {
            valor = 1;
        }
        return (double)valor;
    }

    // ===== Recomendación IA (texto provisional hasta el Sprint 4) =====
    private void MostrarRecomendacion(decimal ingresos, decimal egresos)
    {
        // V24: se necesita al menos un ingreso y un gasto en el mes
        if (ingresos == 0 || egresos == 0)
        {
            lblRecomendacion.Text = "Registra al menos un ingreso y un gasto este mes para recibir recomendaciones.";
        }
        else
        {
            lblRecomendacion.Text = "Las recomendaciones con IA estarán disponibles próximamente.";
        }
    }

    // ===== Últimos movimientos (PB08) =====
    private void MostrarMovimientos(List<string[]> movimientos)
    {
        slMovimientos.Children.Clear();

        // V10: mes sin movimientos
        if (movimientos.Count == 0)
        {
            lblSinMovimientos.IsVisible = true;
            return;
        }

        lblSinMovimientos.IsVisible = false;

        // Del más reciente al más antiguo:
        // el archivo guarda en orden de registro, se recorre desde el final
        // y se ordena por fecha.
        movimientos.Reverse();
        movimientos.Sort((a, b) => Convert.ToDateTime(b[4]).CompareTo(Convert.ToDateTime(a[4])));

        int cantidad = 0;
        foreach (string[] m in movimientos)
        {
            if (cantidad == maximoMovimientos)
            {
                break;
            }

            slMovimientos.Children.Add(CrearFila(m));
            cantidad = cantidad + 1;
        }
    }

    // Crea una fila como en el mockup:
    //   Almuerzo                    -$4,50
    //   Comida · Necesidades · 07/11/2026
    private Grid CrearFila(string[] m)
    {
        string tipo = m[0];
        decimal monto = decimal.Parse(m[1]);
        string categoria = m[2];
        string grupo = m[3];
        string fecha = m[4];
        string nota = m[5];

        // Si no hay nota se muestra la categoría como título
        string titulo = nota;
        if (string.IsNullOrEmpty(titulo))
        {
            titulo = categoria;
        }

        string detalle = categoria + " · " + grupo + " · " + fecha;
        if (tipo == "Ingreso")
        {
            detalle = "Ingreso · " + fecha;
        }

        // Gastos en negativo, ingresos en positivo
        string textoMonto = "-" + FormatoMonto(monto);
        Color colorMonto = (Color)Application.Current!.Resources["Primary"];
        if (tipo == "Ingreso")
        {
            textoMonto = "+" + FormatoMonto(monto);
            colorMonto = (Color)Application.Current.Resources["ColorIngreso"];
        }

        Grid fila = new Grid();
        fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        fila.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        fila.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        Label lblTitulo = new Label
        {
            Text = titulo,
            FontSize = 15,
            TextColor = (Color)Application.Current.Resources["Primary"]
        };

        Label lblDetalle = new Label
        {
            Text = detalle,
            FontSize = 12,
            TextColor = (Color)Application.Current.Resources["ColorTextoSecundario"]
        };

        Label lblMonto = new Label
        {
            Text = textoMonto,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = colorMonto,
            VerticalOptions = LayoutOptions.Center
        };

        fila.Add(lblTitulo, 0, 0);
        fila.Add(lblDetalle, 0, 1);
        fila.Add(lblMonto, 1, 0);
        Grid.SetRowSpan(lblMonto, 2);

        return fila;
    }

    // ===== Lectura del archivo =====
    // Cada línea: Tipo;Monto;Categoria;Grupo;Fecha;Nota  (la escribe vMovimiento)
    // Devuelve solo los movimientos del mes y año indicados.
    private List<string[]> LeerMovimientos(int mes, int anio)
    {
        List<string[]> lista = new List<string[]>();

        string ruta = Path.Combine(FileSystem.AppDataDirectory, "movimientos.txt");

        // Si todavía no se ha registrado nada, el archivo no existe
        if (!File.Exists(ruta))
        {
            return lista;
        }

        string[] lineas = File.ReadAllLines(ruta);

        foreach (string linea in lineas)
        {
            if (string.IsNullOrWhiteSpace(linea))
            {
                continue;
            }

            string[] datos = linea.Split(';');
            if (datos.Length < 6)
            {
                continue;
            }

            DateTime fecha = Convert.ToDateTime(datos[4]);
            if (fecha.Month == mes && fecha.Year == anio)
            {
                lista.Add(datos);
            }
        }

        return lista;
    }

    // Formato de dinero: $1.000,00 (USD, 2 decimales, cultura es-EC)
    private string FormatoMonto(decimal monto)
    {
        if (monto < 0)
        {
            monto = -monto;
            return "-$" + monto.ToString("N2");
        }
        return "$" + monto.ToString("N2");
    }
}
