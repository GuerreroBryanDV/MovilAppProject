using System.Globalization;
using MovilAppProject.Models;
using MovilAppProject.Services;

namespace MovilAppProject.Views;

// Registro, edición y eliminación de movimientos (PB05, PB09, PB10).
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

    // Movimiento que se está editando (null = nuevo movimiento)
    private Movimiento? _movimientoEditar;

    public vMovimiento()
    {
        InitializeComponent();

        // Las categorías y sus grupos vienen de la base de datos
        _categorias = App.MovimientoRepo.ObtenerCategoriasGasto();
        _grupos = App.MovimientoRepo.ObtenerGrupos().ToDictionary(g => g.Id, g => g.Nombre);

        // Picker: "Categoría - Grupo"
        foreach (Categoria categoria in _categorias)
        {
            pkCategoria.Items.Add(categoria.Nombre + " - " + NombreGrupo(categoria));
        }

        // V06: no se puede elegir una fecha posterior a hoy
        dpFecha.MaximumDate = DateTime.Today;
        dpFecha.Date = DateTime.Today;
    }

    // Se llama desde vInicio al tocar un movimiento: recibe el movimiento
    // y carga sus datos en el formulario.
    public vMovimiento(Movimiento movimiento) : this()
    {
        _movimientoEditar = movimiento;

        lblTitulo.Text = "Editar movimiento";
        Title = "Editar movimiento";

        if (movimiento.Tipo == TipoMovimiento.Ingreso)
        {
            btnIngreso_Clicked(btnIngreso, EventArgs.Empty);
        }
        else
        {
            btnGasto_Clicked(btnGasto, EventArgs.Empty);

            for (int i = 0; i < _categorias.Count; i++)
            {
                if (_categorias[i].Id == movimiento.CategoriaId)
                {
                    pkCategoria.SelectedIndex = i;
                    break;
                }
            }
        }

        txtMonto.Text = movimiento.Monto.ToString("0.00");
        dpFecha.Date = movimiento.Fecha;
        txtNota.Text = movimiento.Nota;

        btnGuardar.Text = "Guardar cambios";

        // En modo edición se puede eliminar
        btnEliminar.IsVisible = true;
    }

    private void btnGasto_Clicked(object sender, EventArgs e)
    {
        tipo = "Gasto";
        MarcarTipo(btnGasto, btnIngreso);

        slCategoria.IsVisible = true;
        if (_movimientoEditar == null)
        {
            btnGuardar.Text = "Guardar gasto";
        }
    }

    private void btnIngreso_Clicked(object sender, EventArgs e)
    {
        tipo = "Ingreso";
        MarcarTipo(btnIngreso, btnGasto);

        // Al ser Ingreso se oculta la categoría
        slCategoria.IsVisible = false;
        pkCategoria.SelectedIndex = -1;
        if (_movimientoEditar == null)
        {
            btnGuardar.Text = "Guardar ingreso";
        }
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
                await DisplayAlertAsync("Error", "El monto es obligatorio.", "Aceptar");
                return;
            }

            // V03: solo números con hasta 2 decimales
            if (!MontoValido(montoTexto))
            {
                await DisplayAlertAsync("Error", "El monto solo admite números con hasta 2 decimales.", "Aceptar");
                txtMonto.Text = "";
                return;
            }

            // Acepta coma o punto como separador decimal (25,50 o 25.50)
            decimal monto = decimal.Parse(montoTexto.Replace(',', '.'), CultureInfo.InvariantCulture);

            // V04: mayor a 0 y máximo 99.999,99
            if (monto <= 0 || monto > montoMaximo)
            {
                await DisplayAlertAsync("Error", "El monto debe ser mayor a 0 y no superar $99.999,99.", "Aceptar");
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
                    await DisplayAlertAsync("Error", "Selecciona una categoría.", "Aceptar");
                    return;
                }

                categoriaId = _categorias[pkCategoria.SelectedIndex].Id;
            }
            else
            {
                Categoria? categoriaIngreso = App.MovimientoRepo.ObtenerCategoriaIngreso();
                if (categoriaIngreso is null)
                {
                    await DisplayAlertAsync("Error", MensajeRepositorio(), "Aceptar");
                    return;
                }

                categoriaId = categoriaIngreso.Id;
            }

            // V06: la fecha no puede ser posterior a hoy
            if (fecha.Date > DateTime.Today)
            {
                await DisplayAlertAsync("Error", "La fecha no puede ser posterior a hoy.", "Aceptar");
                dpFecha.Date = DateTime.Today;
                return;
            }

            // V07: nota opcional; el Entry ya limita a 100 caracteres (MaxLength)
            // y el modelo la guarda sin espacios o vacía (no lleva mensaje propio).

            // V08: solo se llega aquí si todo es válido
            Movimiento movimiento = new()
            {
                Tipo = tipo == "Gasto" ? TipoMovimiento.Gasto : TipoMovimiento.Ingreso,
                Monto = monto,
                CategoriaId = categoriaId,
                Fecha = fecha,
                Nota = nota
            };

            // Coherencia: un gasto no puede superar el saldo acumulado (ingresos - gastos de meses anteriores y del actual)
            string? errorSaldo = App.MovimientoRepo.ValidarSaldo(movimiento, _movimientoEditar?.Id ?? 0);
            if (errorSaldo != null)
            {
                await DisplayAlertAsync("Error", errorSaldo, "Aceptar");
                return;
            }
            bool guardado;
            if (_movimientoEditar == null)
            {
                guardado = App.MovimientoRepo.AgregarMovimiento(movimiento);

                // POST al servicio web (Insert ya le puso el Id de SQLite)
                if (guardado)
                    _ = ServicioWeb.AgregarAsync(movimiento);
            }
            else
            {
                // Se actualiza el mismo registro (por su Id)
                movimiento.Id = _movimientoEditar.Id;
                guardado = App.MovimientoRepo.EditarMovimiento(movimiento);

                // PUT al servicio web
                if (guardado)
                    _ = ServicioWeb.ActualizarAsync(movimiento);
            }

            if (!guardado)
            {
                await DisplayAlertAsync("Error", MensajeRepositorio(), "Aceptar");
                return;
            }

            await DisplayAlertAsync("Éxito", "Movimiento guardado", "Aceptar");

            // Regresa a vInicio, que recarga el saldo en OnAppearing
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    private async void btnEliminar_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_movimientoEditar == null)
            {
                return;
            }

            // V09: confirmar antes de borrar; con Cancelar no cambia nada
            bool eliminar = await DisplayAlertAsync("¿Eliminar este movimiento?",
                                                    "Esta acción no se puede deshacer.",
                                                    "Eliminar", "Cancelar");
            if (!eliminar)
            {
                return;
            }

            // Coherencia: no se puede borrar un ingreso si los gastos quedarían sin respaldo
            string? errorSaldo = App.MovimientoRepo.ValidarSaldo(null, _movimientoEditar.Id);
            if (errorSaldo != null)
            {
                await DisplayAlertAsync("Error", errorSaldo, "Aceptar");
                return;
            }
            if (!App.MovimientoRepo.EliminarMovimiento(_movimientoEditar.Id))
            {
                await DisplayAlertAsync("Error", MensajeRepositorio(), "Aceptar");
                return;
            }

            // DELETE en el servicio web
            _ = ServicioWeb.EliminarAsync(_movimientoEditar.Id);

            await DisplayAlertAsync("Éxito", MensajeRepositorio(), "Aceptar");

            // Regresa a vInicio, que recarga el saldo en OnAppearing
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // El repositorio devuelve "Error: ..."; como la alerta ya se titula
    // "Error", se quita el prefijo para no repetir la palabra.
    private string MensajeRepositorio()
    {
        string mensaje = App.MovimientoRepo.StatusMessage;
        if (mensaje.StartsWith("Error: "))
        {
            mensaje = mensaje.Substring(7);
        }
        return mensaje;
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
