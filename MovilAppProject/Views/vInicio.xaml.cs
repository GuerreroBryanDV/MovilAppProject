using MovilAppProject.Models;
using MovilAppProject.Services;
using MovilAppProject.Utils;

namespace MovilAppProject.Views;

// Pantalla principal (PB07, PB08, PB14).
public partial class vInicio : ContentPage
{
    // Cuántos movimientos se muestran en "Últimos movimientos"
    const int maximoMovimientos = 5;

    // Número de la última carga pedida. Si el usuario cambia de mes rápido,
    // solo se muestra la última y no una anterior que terminó tarde.
    int cargaActual = 0;

    // El mes seleccionado vive en FiltroMes para compartirlo con vRegla.

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

        // Respalda en MySQL lo que quedó pendiente (por ejemplo, si XAMPP
        // estaba apagado). Va en segundo plano: no bloquea la pantalla.
        _ = ServicioWeb.SincronizarAsync();
    }

    private void btnMesAnterior_Clicked(object sender, EventArgs e)
    {
        FiltroMes.Anterior();
        CargarDatos();
    }

    // Mes siguiente (nunca pasa del mes actual, V10)
    private void btnMesSiguiente_Clicked(object sender, EventArgs e)
    {
        FiltroMes.Siguiente();
        CargarDatos();
    }

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

    // Carga todos los datos de la pantalla
    private async void CargarDatos()
    {
        try
        {
            // Mes seleccionado: "Noviembre 2026"
            lblMes.Text = FiltroMes.Texto();

            // V10: el botón › se deshabilita en el mes actual (no hay meses futuros)
            btnMesSiguiente.IsEnabled = !FiltroMes.EsMesActual;
            if (btnMesSiguiente.IsEnabled)
            {
                btnMesSiguiente.Opacity = 1;
            }
            else
            {
                btnMesSiguiente.Opacity = 0.3;
            }

            // La base se consulta una sola vez y fuera del hilo de la pantalla,
            // así la vista no se congela con muchos movimientos. De esa consulta salen
            // saldo, regla y últimos movimientos.
            int carga = ++cargaActual;
            DateTime mes = FiltroMes.Mes;
            PerfilPresupuesto perfil = App.ReglaService.ObtenerPerfilActual();

            (List<Movimiento> movimientos, ResultadoRegla regla) = await Task.Run(() =>
            {
                List<Movimiento> lista = App.MovimientoRepo.ListarPorMes(mes.Year, mes.Month);
                return (lista, App.ReglaService.CalcularRegla(lista, perfil));
            });

            // Si mientras tanto se cambió de mes, este resultado ya no sirve
            if (carga != cargaActual)
            {
                return;
            }

            decimal ingresos = 0;
            decimal egresos = 0;

            foreach (Movimiento m in movimientos)
            {
                if (m.Tipo == TipoMovimiento.Ingreso)
                {
                    ingresos = ingresos + m.Monto;
                }
                else
                {
                    egresos = egresos + m.Monto;
                }
            }

            // Saldo = ingresos − gastos del mes (en rojo si es negativo)
            // El saldo es acumulado: incluye lo que sobró de los meses anteriores
            decimal saldo = await Task.Run(() => App.MovimientoRepo.ObtenerSaldoAcumuladoHasta(mes.Year, mes.Month));
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

            MostrarRegla(regla);
            MostrarMovimientos(movimientos);
            await MostrarRecomendacionAsync(regla, ingresos, egresos, carga);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // Todo sale de ReglaPresupuestoService con el perfil elegido,
    // así vInicio y vRegla muestran siempre los mismos números y el mismo semáforo.
    private void MostrarRegla(ResultadoRegla resultado)
    {
        // Título con los porcentajes del perfil: "Regla 60/30/10"
        lblTituloRegla.Text = "Regla " + resultado.Perfil.PorcentajeNecesidades + "/"
                            + resultado.Perfil.PorcentajeAhorroInversion + "/"
                            + resultado.Perfil.PorcentajeLujos;

        // V11: si no hay ingresos no se calculan porcentajes (evita dividir para cero)
        if (!resultado.HayIngresos)
        {
            pbNecesidades.Progress = 0;
            pbAhorro.Progress = 0;
            pbLujos.Progress = 0;
            lblEnMeta.Text = "";
            lblAvisoRegla.Text = resultado.Mensaje;
            lblAvisoRegla.IsVisible = true;
            return;
        }

        lblAvisoRegla.IsVisible = false;

        // La barra muestra cuánto se ha usado de la meta (máximo llena)
        foreach (ResumenGrupo grupo in resultado.Grupos)
        {
            if (grupo.GrupoId == GrupoPresupuesto.NecesidadesId)
            {
                pbNecesidades.Progress = grupo.Progreso;
            }
            else if (grupo.GrupoId == GrupoPresupuesto.AhorroInversionId)
            {
                pbAhorro.Progress = grupo.Progreso;
            }
            else if (grupo.GrupoId == GrupoPresupuesto.LujosId)
            {
                pbLujos.Progress = grupo.Progreso;
            }
        }

        lblEnMeta.Text = resultado.GruposEnMeta + " de " + resultado.Grupos.Count + " en meta";
    }

    // Se guarda la última respuesta: si los datos del mes no cambiaron, se reutiliza
    // y no se vuelve a consultar a la IA cada vez que se abre la pantalla.
    static string ultimaClaveIA = "";
    static string ultimoTextoIA = "";

    private async Task MostrarRecomendacionAsync(ResultadoRegla regla, decimal ingresos, decimal egresos, int carga)
    {
        lblRecomendacion.MaxLines = 4;
        lblRecomendacion.LineBreakMode = LineBreakMode.TailTruncation;

        // V24: se necesita al menos un ingreso y un gasto en el mes
        if (ingresos == 0 || egresos == 0)
        {
            lblRecomendacion.Text = "Registra al menos un ingreso y un gasto este mes para recibir recomendaciones.";
            return;
        }

        string clave = FiltroMes.Mes.Year + "-" + FiltroMes.Mes.Month + "|" + ingresos + "|" + egresos + "|"
                     + string.Join(",", regla.Grupos.Select(g => g.GrupoId + ":" + g.Progreso));
        if (clave == ultimaClaveIA && ultimoTextoIA != "")
        {
            lblRecomendacion.Text = ultimoTextoIA;
            return;
        }

        lblRecomendacion.Text = ultimoTextoIA != "" ? ultimoTextoIA : "Consultando a la IA...";
        ResultadoIA respuesta = await App.IAService.ObtenerRecomendacionesAsync(regla);

        // Si mientras tanto se cambió de mes, este resultado ya no sirve
        if (carga != cargaActual)
        {
            return;
        }
        if (respuesta.Exito)
        {
            ultimaClaveIA = clave;
            ultimoTextoIA = respuesta.Texto;
            lblRecomendacion.Text = respuesta.Texto;
        }
        else
        {
            lblRecomendacion.Text = respuesta.Mensaje;
        }
    }
    private void MostrarMovimientos(List<Movimiento> movimientos)
    {
        slMovimientos.Children.Clear();

        // V10: mes sin movimientos
        if (movimientos.Count == 0)
        {
            lblSinMovimientos.IsVisible = true;
            return;
        }

        lblSinMovimientos.IsVisible = false;

        // El repositorio ya los entrega del más reciente al más antiguo
        int cantidad = 0;
        foreach (Movimiento m in movimientos)
        {
            if (cantidad == maximoMovimientos)
            {
                break;
            }

            slMovimientos.Children.Add(CrearFila(m));
            cantidad = cantidad + 1;
        }
    }

    // Crea una fila (al tocarla se edita):
    //   Almuerzo                    -$4,50
    //   Comida · Necesidades · 07/11/2026
    private Border CrearFila(Movimiento m)
    {
        bool esIngreso = m.Tipo == TipoMovimiento.Ingreso;
        string fecha = m.Fecha.ToString("dd/MM/yyyy");

        // Si no hay nota se muestra la categoría como título
        string titulo = m.Nota ?? m.NombreCategoria;

        string detalle = m.NombreCategoria + " · " + m.NombreGrupo + " · " + fecha;
        if (esIngreso)
        {
            detalle = "Ingreso · " + fecha;
        }

        // Gastos en negativo, ingresos en positivo
        string textoMonto = "-" + FormatoMonto(m.Monto);
        Color colorMonto = (Color)Application.Current!.Resources["Primary"];
        if (esIngreso)
        {
            textoMonto = "+" + FormatoMonto(m.Monto);
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

        // Contenedor redondeado que se sombrea al seleccionar la fila
        Color colorSombreado = (Color)Application.Current.Resources["ColorCampo"];
        Border contenedor = new Border
        {
            Content = fila,
            BackgroundColor = Colors.Transparent,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(8, 6),
            // Margen negativo para que el texto siga alineado con el título
            Margin = new Thickness(-8, 0)
        };

        // Al tocar la fila se sombrea y se abre el movimiento para editarlo
        TapGestureRecognizer toque = new TapGestureRecognizer();
        toque.Tapped += async (sender, e) =>
        {
            contenedor.BackgroundColor = colorSombreado;
            await Task.Delay(150);
            await Navigation.PushAsync(new vMovimiento(m));
        };
        contenedor.GestureRecognizers.Add(toque);

        // En Windows: sombreado al pasar el mouse por encima
        PointerGestureRecognizer puntero = new PointerGestureRecognizer();
        puntero.PointerEntered += (sender, e) =>
        {
            contenedor.BackgroundColor = colorSombreado;
        };
        puntero.PointerExited += (sender, e) =>
        {
            contenedor.BackgroundColor = Colors.Transparent;
        };
        contenedor.GestureRecognizers.Add(puntero);

        return contenedor;
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
