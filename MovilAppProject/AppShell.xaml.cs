namespace MovilAppProject
{
    public partial class AppShell : Shell
    {
        // PB18: el desbloqueo se pide una sola vez, al abrir la app
        bool desbloqueoMostrado = false;

        public AppShell()
        {
            InitializeComponent();

            // Las rutas de páginas que NO son pestañas (ej. vMovimiento)
            // se registran aquí con Routing.RegisterRoute en sus propias HU.

            // PB18: cuando el Shell ya está cargado se revisa si hay bloqueo
            Loaded += AppShell_Loaded;
        }

        // PB18 · MAP-18: si el bloqueo está activo (vAjustes, PB17) se muestra
        // vDesbloqueo encima de todo; sin autenticarse no se accede a vInicio.
        private async void AppShell_Loaded(object? sender, EventArgs e)
        {
            if (desbloqueoMostrado)
            {
                return;
            }
            desbloqueoMostrado = true;

            bool bloqueoActivo = Preferences.Get(Views.vAjustes.ClaveBloqueo, false);
            bool hayPin = Preferences.ContainsKey(Views.vAjustes.ClavePin);

            if (bloqueoActivo && hayPin)
            {
                await Navigation.PushModalAsync(new Views.vDesbloqueo());
            }
        }
    }
}
