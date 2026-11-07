namespace MovilAppProject.Views;

// PB05 · MAP-5 · HU1 Registrar gasto
// Validaciones V01 a V08 de la hoja "Validaciones" del Product Backlog.
public partial class vMovimiento : ContentPage
{
    // Tipo de movimiento seleccionado (V01). Por defecto: Gasto.
    string tipo = "Gasto";

    // Límites del monto (V04)
    const decimal montoMaximo = 99999.99m;

    public vMovimiento()
    {
        InitializeComponent();

        // Categorías predefinidas: "Categoría - Grupo" (igual que el mockup)
        // Necesidades
        pkCategoria.Items.Add("Comida - Necesidades");
        pkCategoria.Items.Add("Vivienda - Necesidades");
        pkCategoria.Items.Add("Salud - Necesidades");
        pkCategoria.Items.Add("Transporte - Necesidades");
        // Ahorro e inversión
        pkCategoria.Items.Add("Fondo de emergencia - Ahorro e inversión");
        pkCategoria.Items.Add("Educación - Ahorro e inversión");
        pkCategoria.Items.Add("Invertir - Ahorro e inversión");
        // Lujos
        pkCategoria.Items.Add("Restaurantes - Lujos");
        pkCategoria.Items.Add("Entretenimiento - Lujos");
        pkCategoria.Items.Add("Vacaciones - Lujos");

        // V06: no se puede elegir una fecha posterior a hoy
        dpFecha.MaximumDate = DateTime.Today;
        dpFecha.Date = DateTime.Today;
    }

    // ===== Tipo: Gasto =====
    private void btnGasto_Clicked(object sender, EventArgs e)
    {
        tipo = "Gasto";
        MarcarTipo(btnGasto, btnIngreso);

        slCategoria.IsVisible = true;
        btnGuardar.Text = "Guardar gasto";
    }

    // ===== Tipo: Ingreso (la regla de guardado del ingreso es de PB06) =====
    private void btnIngreso_Clicked(object sender, EventArgs e)
    {
        tipo = "Ingreso";
        MarcarTipo(btnIngreso, btnGasto);

        // Al ser Ingreso se oculta la categoría
        slCategoria.IsVisible = false;
        pkCategoria.SelectedIndex = -1;
        btnGuardar.Text = "Guardar ingreso";
    }

    // Pinta el botón seleccionado en verde oscuro y el otro sin fondo
    private void MarcarTipo(Button seleccionado, Button otro)
    {
        Color primario = (Color)Application.Current!.Resources["Primary"];

        seleccionado.BackgroundColor = primario;
        seleccionado.TextColor = Colors.White;

        otro.BackgroundColor = Colors.Transparent;
        otro.TextColor = primario;
    }

    // Muestra en qué grupo de la regla se contará el gasto
    private void pkCategoria_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (pkCategoria.SelectedIndex == -1)
        {
            brdAviso.IsVisible = false;
            return;
        }

        string grupo = ObtenerGrupo(pkCategoria.SelectedItem.ToString()!);
        lblAviso.Text = "Se contará en " + grupo + ".";
        brdAviso.IsVisible = true;
    }

    // ===== Guardar =====
    private async void btnGuardar_Clicked(object sender, EventArgs e)
    {
        try
        {
            string montoTexto = (txtMonto.Text ?? "").Trim();
            string nota = (txtNota.Text ?? "").Trim();
            DateTime fecha = Convert.ToDateTime(dpFecha.Date);

            // V02: monto obligatorio
            if (string.IsNullOrEmpty(montoTexto))
            {
                await DisplayAlertAsync("Error", "El monto es obligatorio.", "Cerrar");
                return;
            }

            // V03: solo números con hasta 2 decimales
            if (!MontoValido(montoTexto))
            {
                await DisplayAlertAsync("Error", "El monto solo admite números con hasta 2 decimales.", "Cerrar");
                txtMonto.Text = "";
                return;
            }

            // Se usa la coma como separador decimal (cultura es-EC)
            decimal monto = decimal.Parse(montoTexto.Replace(".", ","));

            // V04: mayor a 0 y máximo 99.999,99
            if (monto <= 0 || monto > montoMaximo)
            {
                await DisplayAlertAsync("Error", "El monto debe ser mayor a 0 y no superar $99.999,99.", "Cerrar");
                txtMonto.Text = "";
                return;
            }

            // V05: categoría obligatoria (solo para Gasto)
            string categoria = "Ingreso";
            string grupo = "Ingreso";
            if (tipo == "Gasto")
            {
                if (pkCategoria.SelectedIndex == -1)
                {
                    await DisplayAlertAsync("Error", "Selecciona una categoría.", "Cerrar");
                    return;
                }

                string seleccion = pkCategoria.SelectedItem.ToString()!;
                categoria = ObtenerCategoria(seleccion);
                grupo = ObtenerGrupo(seleccion);
            }

            // V06: la fecha no puede ser posterior a hoy
            if (fecha.Date > DateTime.Today)
            {
                await DisplayAlertAsync("Error", "La fecha no puede ser posterior a hoy.", "Cerrar");
                dpFecha.Date = DateTime.Today;
                return;
            }

            // V07: nota opcional de máximo 100 caracteres
            if (nota.Length > 100)
            {
                await DisplayAlertAsync("Error", "La nota admite máximo 100 caracteres.", "Cerrar");
                return;
            }

            // V08: solo se llega aquí si todo es válido
            GuardarMovimiento(tipo, monto, categoria, grupo, fecha, nota);

            await DisplayAlertAsync("Éxito", "Movimiento guardado", "Cerrar");

            // Regresa a vInicio
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Cerrar");
        }
    }

    // Guarda el movimiento en el teléfono (archivo de texto).
    // PENDIENTE PB03: al integrar con la rama de María, reemplazar este
    // contenido por la llamada al repositorio SQLite (Repositories).
    private void GuardarMovimiento(string tipo, decimal monto, string categoria,
                                   string grupo, DateTime fecha, string nota)
    {
        string ruta = Path.Combine(FileSystem.AppDataDirectory, "movimientos.txt");

        // Una línea por movimiento: Tipo;Monto;Categoria;Grupo;Fecha;Nota
        string linea = tipo + ";" +
                       monto.ToString("0.00") + ";" +
                       categoria + ";" +
                       grupo + ";" +
                       fecha.ToString("dd/MM/yyyy") + ";" +
                       nota.Replace(";", ",") + "\n";

        File.AppendAllText(ruta, linea);
    }

    // V03: true si el texto tiene solo números y como máximo 2 decimales.
    // Acepta coma o punto como separador decimal (25,50 o 25.50).
    private bool MontoValido(string texto)
    {
        string[] partes = texto.Replace(".", ",").Split(',');

        // Más de un separador decimal: "1,2,3"
        if (partes.Length > 2)
        {
            return false;
        }

        // Parte entera: obligatoria y solo números
        if (partes[0].Length == 0 || !SoloNumeros(partes[0]))
        {
            return false;
        }

        // Parte decimal: de 1 a 2 números
        if (partes.Length == 2)
        {
            if (partes[1].Length == 0 || partes[1].Length > 2 || !SoloNumeros(partes[1]))
            {
                return false;
            }
        }

        return true;
    }

    // Devuelve true si el texto tiene solo números
    private bool SoloNumeros(string texto)
    {
        foreach (char c in texto)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }
        return true;
    }

    // "Comida - Necesidades"  →  "Comida"
    private string ObtenerCategoria(string seleccion)
    {
        int posicion = seleccion.IndexOf(" - ");
        return seleccion.Substring(0, posicion);
    }

    // "Comida - Necesidades"  →  "Necesidades"
    private string ObtenerGrupo(string seleccion)
    {
        int posicion = seleccion.IndexOf(" - ");
        return seleccion.Substring(posicion + 3);
    }
}
