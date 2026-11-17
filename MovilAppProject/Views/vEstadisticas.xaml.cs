using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MovilAppProject.Models;
using MovilAppProject.Utils;
using SkiaSharp;

namespace MovilAppProject.Views;

// PB16 · MAP-16 · Estadísticas con gráfico por categoría (RF11, HU7, HU8).
// Muestra el gasto total del mes y un gráfico de dona por categoría (LiveCharts2, D21).
// Usa el mes seleccionado en vInicio y vRegla (PB14, Utils/FiltroMes).
public partial class vEstadisticas : ContentPage
{
    // Colores por grupo, en tonos del color de cada grupo (Colors.xaml), para que
    // el gráfico se lea igual que vRegla: azules = Necesidades, verdes = Ahorro, naranjas = Lujos.
    static readonly Dictionary<string, string[]> paletaPorGrupo = new()
    {
        ["Necesidades"] = new[] { "#1F5A86", "#2F73A6", "#4A8EC0", "#73AAD3", "#A0C6E4" },
        ["Ahorro e inversión"] = new[] { "#1B7A52", "#2E9968", "#5DB58B" },
        ["Lujos"] = new[] { "#A6501A", "#C56A31", "#DB8A57", "#EAB088" }
    };

    // Si una categoría no tiene grupo conocido
    const string colorPorDefecto = "#6B706C";

    public vEstadisticas()
    {
        InitializeComponent();
    }

    // Se ejecuta cada vez que se entra a la pestaña, así refleja los gastos recién registrados
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarEstadisticas();
    }

    // ===== PB14: mes anterior / siguiente (nunca pasa del mes actual, V10) =====
    private void btnMesAnterior_Clicked(object sender, EventArgs e)
    {
        FiltroMes.Anterior();
        CargarEstadisticas();
    }

    private void btnMesSiguiente_Clicked(object sender, EventArgs e)
    {
        FiltroMes.Siguiente();
        CargarEstadisticas();
    }

    // ===== Carga los gastos del mes seleccionado =====
    private async void CargarEstadisticas()
    {
        try
        {
            lblMes.Text = FiltroMes.Texto();

            // V10: el botón › se deshabilita en el mes actual (no hay meses futuros)
            btnMesSiguiente.IsEnabled = !FiltroMes.EsMesActual;
            btnMesSiguiente.Opacity = FiltroMes.EsMesActual ? 0.3 : 1;

            List<Movimiento> movimientos = App.MovimientoRepo.ListarPorMes(FiltroMes.Mes.Year, FiltroMes.Mes.Month);

            // Total por categoría, de la que más gasta a la que menos (solo gastos)
            List<GastoCategoria> categorias = movimientos
                .Where(m => m.Tipo == TipoMovimiento.Gasto)
                .GroupBy(m => m.CategoriaId)
                .Select(g => new GastoCategoria
                {
                    Categoria = g.First().NombreCategoria,
                    Grupo = g.First().NombreGrupo,
                    Total = g.Sum(m => m.Monto)
                })
                .OrderByDescending(c => c.Total)
                .ToList();

            // HU7: el total del gráfico es la suma de los gastos del mes (= egresos de vInicio)
            decimal total = categorias.Sum(c => c.Total);
            lblTotal.Text = "$" + total.ToString("N2");

            // V10: sin gastos no se dibuja el gráfico
            if (categorias.Count == 0)
            {
                brdSinMovimientos.IsVisible = true;
                brdGrafico.IsVisible = false;
                chartGastos.Series = Array.Empty<ISeries>();
                slCategorias.Children.Clear();
                return;
            }

            brdSinMovimientos.IsVisible = false;
            brdGrafico.IsVisible = true;

            AsignarColores(categorias);
            MostrarGrafico(categorias);
            MostrarLeyenda(categorias, total);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // Un tono distinto del color de su grupo para cada categoría
    private static void AsignarColores(List<GastoCategoria> categorias)
    {
        Dictionary<string, int> usados = new();

        foreach (GastoCategoria c in categorias)
        {
            if (!paletaPorGrupo.TryGetValue(c.Grupo, out string[]? tonos))
            {
                c.ColorHex = colorPorDefecto;
                continue;
            }

            usados.TryGetValue(c.Grupo, out int indice);
            c.ColorHex = tonos[indice % tonos.Length];
            usados[c.Grupo] = indice + 1;
        }
    }

    // Gráfico de dona: una porción por categoría
    private void MostrarGrafico(List<GastoCategoria> categorias)
    {
        List<ISeries> series = new();

        foreach (GastoCategoria c in categorias)
        {
            series.Add(new PieSeries<double>
            {
                Name = c.Categoria,
                Values = new[] { (double)c.Total },
                InnerRadius = 60,
                Fill = new SolidColorPaint(SKColor.Parse(c.ColorHex)),
                Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
            });
        }

        chartGastos.Series = series;
    }

    // Leyenda como en el mockup:  ● Comida · Necesidades          $120,00 · 40 %
    private void MostrarLeyenda(List<GastoCategoria> categorias, decimal total)
    {
        slCategorias.Children.Clear();

        foreach (GastoCategoria c in categorias)
        {
            decimal porcentaje = total > 0 ? Math.Round(c.Total * 100m / total, 0, MidpointRounding.AwayFromZero) : 0;

            Grid fila = new Grid { ColumnSpacing = 10 };
            fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            BoxView punto = new BoxView
            {
                Color = Color.FromArgb(c.ColorHex),
                WidthRequest = 12,
                HeightRequest = 12,
                CornerRadius = 6,
                VerticalOptions = LayoutOptions.Center
            };

            Label lblNombre = new Label
            {
                Text = c.Categoria + " · " + c.Grupo,
                FontSize = 14,
                TextColor = (Color)Application.Current!.Resources["Primary"],
                VerticalOptions = LayoutOptions.Center
            };

            Label lblMonto = new Label
            {
                Text = "$" + c.Total.ToString("N2") + " · " + porcentaje + " %",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)Application.Current.Resources["Primary"],
                VerticalOptions = LayoutOptions.Center
            };

            fila.Add(punto, 0, 0);
            fila.Add(lblNombre, 1, 0);
            fila.Add(lblMonto, 2, 0);

            slCategorias.Children.Add(fila);
        }
    }

    // Total gastado en una categoría del mes (solo para esta pantalla)
    private class GastoCategoria
    {
        public string Categoria { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string ColorHex { get; set; } = colorPorDefecto;
    }
}
