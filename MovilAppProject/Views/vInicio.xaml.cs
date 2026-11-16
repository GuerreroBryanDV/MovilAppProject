using MovilAppProject.Models;
using MovilAppProject.Services;
using MovilAppProject.Utils;

namespace MovilAppProject.Views;

// Pantalla principal de GastoFácil (según el mockup).
// PB07: lee los movimientos del mes desde SQLite (App.MovimientoRepo).
// PB14: filtro por mes con los botones ‹ › (no se permiten meses futuros, V10).
public partial class vInicio : ContentPage
{
    // Cuántos movimientos se muestran en "Últimos movimientos"
    const int maximoMovimientos = 5;

    // PB14: el mes seleccionado vive en FiltroMes para compartirlo con vRegla.
    // Las metas de la regla ya no están aquí: salen del perfil de PB15 vía ReglaPresupuestoService.

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

    // ===== PB14: mes anterior =====
    private void btnMesAnterior_Clicked(object sender, EventArgs e)
    {
        FiltroMes.Anterior();
        CargarDatos();
    }

    // ===== PB14: mes siguiente (nunca pasa del mes actual, V10) =====
    private void btnMesSiguiente_Clicked(object sender, EventArgs e)
    {
        FiltroMes.Siguiente();
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
            // PB14: mes seleccionado: "Noviembre 2026"
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

            // Movimientos del mes seleccionado, del más reciente al más antiguo.
            // Se consulta una sola vez y de aquí salen todos los totales.
            List<Movimiento> movimientos = App.MovimientoRepo.ListarPorMes(FiltroMes.Mes.Year, FiltroMes.Mes.Month);

            // Totales
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

            MostrarRegla();
            MostrarRecomendacion(ingresos, egresos);
            MostrarMovimientos(movimientos);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // ===== Resumen de la regla =====
    // Todo sale de ReglaPresupuestoService (PB11) con el perfil elegido en PB15,
    // así vInicio y vRegla muestran siempre los mismos números y el mismo semáforo.
    private void MostrarRegla()
    {
        ResultadoRegla resultado = App.ReglaService.CalcularRegla(FiltroMes.Mes.Year, FiltroMes.Mes.Month);

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

    // Crea una fila como en el mockup (al tocarla se edita, PB09):
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

        // PB09: al tocar la fila se sombrea y se abre el movimiento para editarlo
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
