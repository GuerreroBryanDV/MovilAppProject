using MovilAppProject.Models;

namespace MovilAppProject.Views;

// PB15 · Perfiles de porcentajes de la regla (RF07)
// Validaciones V13, V14 y V15 de la hoja "Validaciones" del Product Backlog.
// El perfil se guarda en Preferences mediante ReglaPresupuestoService (PB11),
// así vRegla calcula las metas con los nuevos porcentajes.
public partial class vPorcentajes : ContentPage
{
    // Mensajes exactos de la hoja Validaciones
    const string MSG_V14 = "Cada porcentaje debe ser un número entero entre 0 y 100.";

    // Perfil seleccionado (V13). Por defecto: 50/40/10.
    string perfilSeleccionado = PerfilPresupuesto.Regla50_40_10.Nombre;

    // Evita mostrar mensajes mientras el código llena los campos
    bool cargando = false;

    public vPorcentajes()
    {
        InitializeComponent();
    }

    // Cada vez que se abre la pantalla se carga el perfil guardado
    protected override void OnAppearing()
    {
        base.OnAppearing();

        PerfilPresupuesto perfil = App.ReglaService.ObtenerPerfilActual();

        // Primero se escriben los porcentajes guardados (en Personalizado son los del usuario)
        // y luego se marca la tarjeta, que también recalcula la suma.
        MostrarPorcentajes(perfil);
        SeleccionarPerfil(perfil.Nombre);
    }

    // ===== Perfiles (V13) =====
    private void brdDefecto_Tapped(object sender, TappedEventArgs e)
    {
        SeleccionarPerfil(PerfilPresupuesto.Regla50_40_10.Nombre);
    }

    private void brdClasica_Tapped(object sender, TappedEventArgs e)
    {
        SeleccionarPerfil(PerfilPresupuesto.Clasica50_20_30.Nombre);
    }

    private void brdPersonalizado_Tapped(object sender, TappedEventArgs e)
    {
        SeleccionarPerfil(PerfilPresupuesto.NombrePersonalizado);
    }

    // Marca la tarjeta elegida y llena los campos.
    // En los perfiles fijos los porcentajes son de solo lectura (V13).
    private void SeleccionarPerfil(string nombre)
    {
        perfilSeleccionado = nombre;
        bool esPersonalizado = nombre == PerfilPresupuesto.NombrePersonalizado;

        MarcarTarjeta(brdDefecto, nombre == PerfilPresupuesto.Regla50_40_10.Nombre);
        MarcarTarjeta(brdClasica, nombre == PerfilPresupuesto.Clasica50_20_30.Nombre);
        MarcarTarjeta(brdPersonalizado, esPersonalizado);

        if (nombre == PerfilPresupuesto.Clasica50_20_30.Nombre)
        {
            MostrarPorcentajes(PerfilPresupuesto.Clasica50_20_30);
        }
        else if (!esPersonalizado)
        {
            MostrarPorcentajes(PerfilPresupuesto.Regla50_40_10);
        }
        // En Personalizado se dejan los valores que ya están escritos para editarlos

        txtNecesidades.IsReadOnly = !esPersonalizado;
        txtAhorro.IsReadOnly = !esPersonalizado;
        txtLujos.IsReadOnly = !esPersonalizado;

        ActualizarSuma();
    }

    // Tarjeta elegida: borde verde oscuro más grueso (igual que el mockup)
    private void MarcarTarjeta(Border tarjeta, bool seleccionada)
    {
        Color primario = (Color)Application.Current!.Resources["Primary"];
        Color borde = (Color)Application.Current.Resources["ColorBorde"];

        tarjeta.Stroke = seleccionada ? primario : borde;
        tarjeta.StrokeThickness = seleccionada ? 2 : 1;
    }

    // Escribe los porcentajes de un perfil en los campos
    private void MostrarPorcentajes(PerfilPresupuesto perfil)
    {
        cargando = true;
        txtNecesidades.Text = perfil.PorcentajeNecesidades.ToString();
        txtAhorro.Text = perfil.PorcentajeAhorroInversion.ToString();
        txtLujos.Text = perfil.PorcentajeLujos.ToString();
        cargando = false;
    }

    // ===== Suma en vivo (V14, V15) =====
    private void txtPorcentaje_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (cargando)
        {
            return;
        }

        ActualizarSuma();
    }

    // Recalcula la suma y habilita "Guardar cambios" solo si todo es válido
    private void ActualizarSuma()
    {
        Color verde = (Color)Application.Current!.Resources["ColorIngreso"];
        Color rojo = (Color)Application.Current.Resources["ColorGasto"];

        // V14: cada campo debe ser un entero de 0 a 100
        if (!PorcentajeValido(txtNecesidades.Text, out int necesidades)
            || !PorcentajeValido(txtAhorro.Text, out int ahorro)
            || !PorcentajeValido(txtLujos.Text, out int lujos))
        {
            lblSuma.Text = "— %";
            lblSuma.TextColor = rojo;
            lblMensaje.Text = MSG_V14;
            lblMensaje.IsVisible = true;
            btnGuardar.IsEnabled = false;
            return;
        }

        int suma = necesidades + ahorro + lujos;
        lblSuma.Text = suma + " %";

        // V15: la suma debe ser exactamente 100
        if (suma != 100)
        {
            lblSuma.TextColor = rojo;
            lblMensaje.Text = MensajeSuma(suma);
            lblMensaje.IsVisible = true;
            btnGuardar.IsEnabled = false;
            return;
        }

        lblSuma.TextColor = verde;
        lblMensaje.IsVisible = false;
        btnGuardar.IsEnabled = true;
    }

    // V14: al salir de un campo con un valor no válido se muestra el error
    private async void txtPorcentaje_Unfocused(object sender, FocusEventArgs e)
    {
        if (perfilSeleccionado != PerfilPresupuesto.NombrePersonalizado)
        {
            return;
        }

        Entry campo = (Entry)sender;
        if (!PorcentajeValido(campo.Text, out _))
        {
            await DisplayAlertAsync("Error", MSG_V14, "Aceptar");
        }
    }

    // ===== Guardar cambios =====
    private async void btnGuardar_Clicked(object sender, EventArgs e)
    {
        try
        {
            PerfilPresupuesto perfil;

            if (perfilSeleccionado == PerfilPresupuesto.Clasica50_20_30.Nombre)
            {
                perfil = PerfilPresupuesto.Clasica50_20_30;
            }
            else if (perfilSeleccionado == PerfilPresupuesto.NombrePersonalizado)
            {
                // V14: enteros de 0 a 100 ("abc", "12,5" o vacío no son válidos)
                if (!PorcentajeValido(txtNecesidades.Text, out int necesidades)
                    || !PorcentajeValido(txtAhorro.Text, out int ahorro)
                    || !PorcentajeValido(txtLujos.Text, out int lujos))
                {
                    await DisplayAlertAsync("Error", MSG_V14, "Aceptar");
                    return;
                }

                perfil = PerfilPresupuesto.Personalizado(necesidades, ahorro, lujos);

                // V15: la suma debe ser 100
                if (!perfil.EsValido)
                {
                    await DisplayAlertAsync("Error", MensajeSuma(perfil.Suma), "Aceptar");
                    return;
                }
            }
            else
            {
                perfil = PerfilPresupuesto.Regla50_40_10;
            }

            // Se guarda en Preferences; vRegla usará las nuevas metas
            if (!App.ReglaService.GuardarPerfil(perfil))
            {
                await DisplayAlertAsync("Error", App.ReglaService.StatusMessage, "Aceptar");
                return;
            }

            await DisplayAlertAsync("Éxito", App.ReglaService.StatusMessage, "Aceptar");

            // Regresa a vAjustes
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Aceptar");
        }
    }

    // V15: "Los porcentajes deben sumar 100 % (actual: 90 %)."
    private string MensajeSuma(int suma)
    {
        return "Los porcentajes deben sumar 100 % (actual: " + suma + " %).";
    }

    // V14: true si el texto es un número entero entre 0 y 100 (sin decimales ni vacío)
    private bool PorcentajeValido(string? texto, out int valor)
    {
        valor = 0;
        string limpio = (texto ?? "").Trim();

        if (limpio.Length == 0 || limpio.Length > 3)
        {
            return false;
        }

        foreach (char c in limpio)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        valor = int.Parse(limpio);
        return valor >= 0 && valor <= 100;
    }
}
