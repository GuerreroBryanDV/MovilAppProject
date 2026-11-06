using System.Globalization;

namespace MovilAppProject.Views;

public partial class vRegistrarIngreso : ContentPage
{

    //Reglas de validacion (mismas que en el gasto - HU2 criterio 3)
    const decimal MONTO_MAXIMO = 99999.99m;   // V04
    const int NOTA_MAXIMO = 100;              // V07
    const string CATEGORIA_INGRESO = "Ingreso"; // V05

    //Acceso a la base de datos SQLite (PB03)
   // readonly MovimientoRepository repositorio = new MovimientoRepository();

    public vRegistrarIngreso()
	{
		InitializeComponent();

        //No permitir fechas futuras (V06)
        dpFecha.MaximumDate = DateTime.Today;

        LimpiarFormulario();
    }

    //Cada vez que se abre la pantalla se actualiza el saldo
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        //await CargarResumen();
    }

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

    //Validar todos los campos del formulario
    private async Task<bool> ValidarDatos()
    {
        string montoTexto = txtMonto.Text?.Trim() ?? "";
        string nota = txtNota.Text?.Trim() ?? "";

        //Tipo
        if (pkTipo.SelectedIndex == -1)
        {
            await DisplayAlertAsync("Error", "Seleccione el tipo de movimiento", "OK");
            return false;
        }

        //Categoria: solo se pide cuando es Gasto
        if (!EsIngreso() && pkCategoria.SelectedIndex == -1)
        {
            await DisplayAlertAsync("Error", "Seleccione una categoría", "OK");
            return false;
        }

        //Monto: obligatorio (V02)
        if (string.IsNullOrWhiteSpace(montoTexto))
        {
            await DisplayAlertAsync("Error", "El campo monto no debe ser vacío", "OK");
            return false;
        }

        //Monto: numerico y mayor a 0 (V03)
        if (!decimal.TryParse(montoTexto.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal monto)
            || monto <= 0)
        {
            await DisplayAlertAsync("Error", "El monto debe ser un número mayor a 0", "OK");
            return false;
        }

        //Monto: maximo 2 decimales y valor maximo (V04)
        if (decimal.Round(monto, 2) != monto || monto > MONTO_MAXIMO)
        {
            await DisplayAlertAsync("Error", "El monto admite máximo 2 decimales y hasta 99999.99", "OK");
            return false;
        }

        //Fecha: no se permiten fechas futuras (V06)
        DateTime fecha = dpFecha.Date ?? DateTime.Today;
        if (fecha.Date > DateTime.Today)
        {
            await DisplayAlertAsync("Error", "La fecha no puede ser posterior a hoy", "OK");
            return false;
        }

        //Nota: opcional, maximo 100 caracteres (V07)
        if (nota.Length > NOTA_MAXIMO)
        {
            await DisplayAlertAsync("Error", "La nota no puede superar los 100 caracteres", "OK");
            return false;
        }

        return true;
    }



    //Muestra el saldo y los ingresos del mes desde SQLite
    private async Task CargarResumen()
    {
        try
        {
            DateTime hoy = DateTime.Today;
           // decimal saldo = await repositorio.ObtenerSaldoAsync();
            ///decimal ingresosMes = await repositorio.ObtenerIngresosDelMesAsync(hoy.Year, hoy.Month);

           // lblSaldo.Text = "$ " + saldo.ToString("0.00");
           // lblIngresosMes.Text = "Ingresos del mes: $ " + ingresosMes.ToString("0.00");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    //Muestra el saldo y los ingresos del mes desde SQLite
    /* private async Task CargarResumen()
     {
         try
         {
             DateTime hoy = DateTime.Today;
             decimal saldo = await repositorio.ObtenerSaldoAsync();
             decimal ingresosMes = await repositorio.ObtenerIngresosDelMesAsync(hoy.Year, hoy.Month);

             lblSaldo.Text = "$ " + saldo.ToString("0.00");
             lblIngresosMes.Text = "Ingresos del mes: $ " + ingresosMes.ToString("0.00");
         }
         catch (Exception ex)
         {
             await DisplayAlertAsync("Error", ex.Message, "OK");
         }
     }*/

    //Convierte el monto aceptando punto o coma decimal
    private decimal LeerMonto()
    {
        string texto = (txtMonto.Text ?? "0").Trim().Replace(',', '.');
        return decimal.Parse(texto, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private void btnGuardar_Clicked(object sender, EventArgs e)
    {
       
    }

    private void btnLimpiar_Clicked(object sender, EventArgs e)
    {
        LimpiarFormulario();
    }

    //Deja el formulario listo para un nuevo ingreso
    private void LimpiarFormulario()
    {
        pkTipo.SelectedIndex = 1; // Ingreso por defecto (HU2)
        pkCategoria.SelectedIndex = -1;
        txtMonto.Text = "";
        txtNota.Text = "";
        dpFecha.Date = DateTime.Today;
    }
}