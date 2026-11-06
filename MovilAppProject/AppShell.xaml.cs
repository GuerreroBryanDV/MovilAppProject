namespace MovilAppProject
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Las rutas de páginas que NO son pestañas (ej. vMovimiento)
            // se registran aquí con Routing.RegisterRoute en sus propias HU.
        }
    }
}
