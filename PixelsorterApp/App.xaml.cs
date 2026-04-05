using Microsoft.Extensions.DependencyInjection;

namespace PixelsorterApp
{
    /// <summary>
    /// Application root that composes shell and initial navigation window.
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider services;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <param name="services">Application service provider.</param>
        public App(IServiceProvider services)
        {
            this.services = services;
            InitializeComponent();
        }

        /// <summary>
        /// Creates the main application window.
        /// </summary>
        /// <param name="activationState">Activation context provided by the platform.</param>
        /// <returns>The initialized application window.</returns>
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