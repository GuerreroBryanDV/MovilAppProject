using System.Globalization;
using MovilAppProject.Models;

namespace MovilAppProject.Views;

// PB05 · MAP-5 · HU1 Registrar gasto  /  PB07 · guardado en SQLite
// Validaciones V01 a V08 de la hoja "Validaciones" del Product Backlog.
public partial class vMovimiento : ContentPage
{
    // Tipo de movimiento seleccionado (V01). Por defecto: Gasto.
    string tipo = "Gasto";

    // Límites del monto (V04)
    const decimal montoMaximo = 99999.99m;

    // Categorías de gasto guardadas en SQLite, en el mismo orden que el Picker
    private readonly List<Categoria> _categorias;

    // Nombre del grupo de cada categoría, para mostrar "Comida - Necesidades"
    private readonly Dictionary<int, string> _grupos;

    public vMovimiento()
    {
        InitializeComponent();

        // Las categorías y sus grupos vienen de la base de datos (PB03)
        _categorias = App.MovimientoRepo.ObtenerCategoriasGasto();
        _grupos = App.MovimientoRepo.ObtenerGrupos().ToDictionary(g => g.Id, g => g.Nombre);

        // Picker: "Categoría - Grupo" (igual que el mockup)
        foreach (Categoria categoria in _categorias)
        {
            pkCategoria.Items.Add(categoria.Nombre + " - " + NombreGrupo(categoria));
        }

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

    // ===== Tipo: Ingreso =====
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

        string grupo = NombreGrupo(_categorias[pkCategoria.SelectedIndex]);
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

            // Acepta coma o punto como separador decimal (25,50 o 25.50)
            decimal monto = decimal.Parse(montoTexto.Replace(',', '.'), CultureInfo.InvariantCulture);

            // V04: mayor a 0 y máximo 99.999,99
            if (monto <= 0 || monto > montoMaximo)
            {
                await DisplayAlertAsync("Error", "El monto debe ser mayor a 0 y no superar $99.999,99.", "Cerrar");
                txtMonto.Text = "";
                return;
            }

            // V05: categoría obligatoria (solo para Gasto).
            // En un ingreso se guarda la categoría "Ingreso".
            int categoriaId;
            if (tipo == "Gasto")
            {
                if (pkCategoria.SelectedIndex == -1)
                {
                    await DisplayAlertAsync("Error", "Selecciona una categoría.", "Cerrar");
                    return;
                }

                categoriaId = _categorias[pkCategoria.SelectedIndex].Id;
            }
            else
            {
                Categoria? categoriaIngreso = App.MovimientoRepo.ObtenerCategoriaIngreso();
                if (categoriaIngreso is null)
                {
                    await DisplayAlertAsync("Error", App.MovimientoRepo.StatusMessage, "Cerrar");
                    return;
                }

                categoriaId = categoriaIngreso.Id;
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
            Movimiento movimiento = new()
            {
                Tipo = tipo == "Gasto" ? TipoMovimiento.Gasto : TipoMovimiento.Ingreso,
                Monto = monto,
                CategoriaId = categoriaId,
                Fecha = fecha,
                Nota = nota
            };

            if (!App.MovimientoRepo.AgregarMovimiento(movimiento))
            {
                await DisplayAlertAsync("Error", App.MovimientoRepo.StatusMessage, "Cerrar");
                return;
            }

            await DisplayAlertAsync("Éxito", "Movimiento guardado", "Cerrar");

            // Regresa a vInicio, que recarga el saldo en OnAppearing
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Cerrar");
        }
    }

    // Nombre del grupo de una categoría ("Necesidades", "Lujos"...)
    private string NombreGrupo(Categoria categoria)
    {
        if (categoria.GrupoId is int grupoId && _grupos.TryGetValue(grupoId, out string? nombre))
        {
            return nombre;
        }

        return "";
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
}
