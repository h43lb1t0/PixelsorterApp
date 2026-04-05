using Microsoft.Extensions.DependencyInjection;

namespace PixelsorterApp
{
    public partial class App : Application
    {
        private readonly IServiceProvider services;

        public App(IServiceProvider services)
        {
            this.services = services;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var appShell = services.GetRequiredService<AppShell>();

            if (appShell.Items.Count == 0)
            {
                appShell.Items.Add(new ShellContent
                {
                    Title = string.Empty,
                    Route = nameof(MainPage),
                    Content = services.GetRequiredService<MainPage>()
                });
            }

            return new Window(appShell);
        }
    }
}