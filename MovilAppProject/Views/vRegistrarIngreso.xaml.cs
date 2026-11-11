using System.Globalization;
using System.Text.RegularExpressions;
using MovilAppProject.Models;

namespace MovilAppProject.Views;

public partial class vRegistrarIngreso : ContentPage
{
    // Reglas de validación (las mismas que en el gasto - HU2 criterio 3)
    const decimal MONTO_MAXIMO = 99999.99m;   // V04

    // Mensajes EXACTOS de la hoja Validaciones del backlog
    const string MSG_V02 = "El monto es obligatorio.";
    const string MSG_V03 = "El monto solo admite números con hasta 2 decimales.";
    const string MSG_V04 = "El monto debe ser mayor a 0 y no superar $99.999,99.";
    const string MSG_V05 = "Selecciona una categoría.";
    const string MSG_V06 = "La fecha no puede ser posterior a hoy.";

    // V03: solo números, con punto o coma decimal y máximo 2 decimales (ej. 25,50 o 25.5)
    static readonly Regex FormatoMonto = new(@"^\d+([.,]\d{1,2})?$");

    public vRegistrarIngreso()
    {
        InitializeComponent();

        // No permitir fechas futuras (V06)
        dpFecha.MaximumDate = DateTime.Today;

        LimpiarFormulario();
    }

    // Cada vez que se abre la pantalla se cargan las categorías y el saldo desde SQLite
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarCategorias();
        CargarResumen();
    }

    // V01 / V05: al cambiar a Ingreso se oculta la categoría
    private void pkTipo_SelectedIndexChanged(object sender, EventArgs e)
    {
        bool esIngreso = EsIngreso();

        lblCategoria.IsVisible = !esIngreso;
        pkCategoria.IsVisible = !esIngreso;
        pkCategoria.SelectedIndex = -1;

        lblTitulo.Text = esIngreso ? "REGISTRAR INGRESO" : "REGISTRAR GASTO";
    }

    private bool EsIngreso()
    {
        return pkTipo.SelectedItem?.ToString() == "Ingreso";
    }

    // Llena el Picker con las categorías de gasto guardadas en SQLite
    private void CargarCategorias()
    {
        pkCategoria.ItemsSource = App.MovimientoRepo.ObtenerCategoriasGasto();
        pkCategoria.SelectedIndex = -1;
    }

    // Valida V02–V06 en orden; si una falla muestra su mensaje y no se guarda nada (V08)
    private async Task<bool> ValidarDatos()
    {
        string montoTexto = txtMonto.Text?.Trim() ?? "";

        // V02: monto obligatorio
        if (string.IsNullOrEmpty(montoTexto))
        {
            await DisplayAlertAsync("Error", MSG_V02, "Aceptar");
            return false;
        }

        // V03: solo números con hasta 2 decimales ("abc" o "12,345" no pasan)
        if (!FormatoMonto.IsMatch(montoTexto))
        {
            await DisplayAlertAsync("Error", MSG_V03, "Aceptar");
            return false;
        }

        // V04: mayor a 0 y no mayor a 99.999,99
        decimal monto = LeerMonto();
        if (monto <= 0 || monto > MONTO_MAXIMO)
        {
            await DisplayAlertAsync("Error", MSG_V04, "Aceptar");
            return false;
        }

        // V05: la categoría solo se pide cuando es Gasto
        if (!EsIngreso() && pkCategoria.SelectedItem is not Categoria)
        {
            await DisplayAlertAsync("Error", MSG_V05, "Aceptar");
            return false;
        }

        // V06: no se permiten fechas futuras
        DateTime fecha = dpFecha.Date ?? DateTime.Today;
        if (fecha.Date > DateTime.Today)
        {
            await DisplayAlertAsync("Error", MSG_V06, "Aceptar");
            return false;
        }

        // V07: la nota es opcional; el Entry ya limita a 100 caracteres (MaxLength)
        return true;
    }

    // Muestra el saldo y los ingresos del mes actual desde SQLite (HU2 criterio 2)
    private void CargarResumen()
    {
        DateTime hoy = DateTime.Today;
        decimal saldo = App.MovimientoRepo.ObtenerSaldoDelMes(hoy.Year, hoy.Month);
        decimal ingresosMes = App.MovimientoRepo.ObtenerIngresosDelMes(hoy.Year, hoy.Month);

        lblSaldo.Text = "$ " + saldo.ToString("N2");
        lblSaldo.TextColor = saldo < 0 ? Color.FromArgb("#FF8A80") : Colors.White; // negativo en rojo
        lblIngresosMes.Text = "Ingresos del mes: $ " + ingresosMes.ToString("N2");
    }

    // Convierte el monto aceptando punto o coma decimal
    private decimal LeerMonto()
    {
        string texto = (txtMonto.Text ?? "0").Trim().Replace(',', '.');
        return decimal.Parse(texto, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    // V08: valida, guarda en SQLite, muestra "Movimiento guardado" y actualiza el saldo
    private async void btnGuardar_Clicked(object sender, EventArgs e)
    {
        if (!await ValidarDatos())
            return;

        bool esIngreso = EsIngreso();

        // En un ingreso se guarda la categoría "Ingreso" (V05)
        int categoriaId;
        if (esIngreso)
        {
            Categoria? categoriaIngreso = App.MovimientoRepo.ObtenerCategoriaIngreso();
            if (categoriaIngreso is null)
            {
                await DisplayAlertAsync("Error", App.MovimientoRepo.StatusMessage, "Aceptar");
                return;
            }
            categoriaId = categoriaIngreso.Id;
        }
        else
        {
            categoriaId = ((Categoria)pkCategoria.SelectedItem).Id;
        }

        Movimiento movimiento = new()
        {
            Tipo = esIngreso ? TipoMovimiento.Ingreso : TipoMovimiento.Gasto,
            Monto = LeerMonto(),
            CategoriaId = categoriaId,
            Fecha = dpFecha.Date ?? DateTime.Today,
            Nota = txtNota.Text   // el modelo quita espacios y guarda null si queda vacía (V07)
        };

        bool guardado = App.MovimientoRepo.AgregarMovimiento(movimiento);
        if (!guardado)
        {
            await DisplayAlertAsync("Error", App.MovimientoRepo.StatusMessage, "Aceptar");
            return;
        }

        await DisplayAlertAsync("GastoFácil", "Movimiento guardado", "Aceptar");

        CargarResumen();      // el saldo y los ingresos del mes suben (criterio 2)
        LimpiarFormulario();

        // Si la lista está abierta, se actualiza con el nuevo movimiento
        if (listaMovimientos.IsVisible || lblSinMovimientos.IsVisible)
            CargarMovimientos();

        // Si se abrió desde vInicio (botón "+ Nuevo"), regresa a esa pantalla (V08)
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
    }

    // Muestra u oculta la lista de movimientos del mes
    private void btnListar_Clicked(object sender, EventArgs e)
    {
        bool estaAbierta = listaMovimientos.IsVisible || lblSinMovimientos.IsVisible;

        if (estaAbierta)
        {
            listaMovimientos.IsVisible = false;
            lblSinMovimientos.IsVisible = false;
            btnListar.Text = "Ver movimientos del mes";
            return;
        }

        CargarMovimientos();
        btnListar.Text = "Ocultar movimientos";
    }

    // Select: carga los movimientos del mes actual desde SQLite (más reciente primero)
    private void CargarMovimientos()
    {
        DateTime hoy = DateTime.Today;
        List<Movimiento> movimientos = App.MovimientoRepo.ListarPorMes(hoy.Year, hoy.Month);

        listaMovimientos.ItemsSource = movimientos;

        // V10: si no hay datos se muestra el mensaje en lugar de la lista
        bool hayDatos = movimientos.Count > 0;
        listaMovimientos.IsVisible = hayDatos;
        lblSinMovimientos.IsVisible = !hayDatos;
    }

    private void btnLimpiar_Clicked(object sender, EventArgs e)
    {
        LimpiarFormulario();
    }

    // Deja el formulario listo para un nuevo ingreso
    private void LimpiarFormulario()
    {
        pkTipo.SelectedIndex = 1; // Ingreso por defecto en esta pantalla (HU2)
        pkCategoria.SelectedIndex = -1;
        txtMonto.Text = "";
        txtNota.Text = "";
        dpFecha.Date = DateTime.Today;
    }
}
