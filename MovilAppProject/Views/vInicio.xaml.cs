namespace MovilAppProject.Views;

public partial class vInicio : ContentPage
{
    public vInicio()
    {
        InitializeComponent();
    }

    // PB05: abre la pantalla "Nuevo movimiento"
    private async void btnNuevo_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new vMovimiento());
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "Cerrar");
        }
    }
}
