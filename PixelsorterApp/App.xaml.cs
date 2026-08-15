using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;

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
            RequestedThemeChanged += (s, e) => UpdateStatusBar(e.RequestedTheme);

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

            var window = new Window(appShell);

            window.Activated += (s, e) => UpdateStatusBar(RequestedTheme);

            return window;
        }

        /// <summary>
        /// Updates the status bar on Android based on the app theme with the page background color for that theme
        /// </summary>
        /// <param name="theme"></param>
        static void UpdateStatusBar(AppTheme theme)
        {
            # if ANDROID
            Color dark = Colors.Black;
            Color light = Colors.White;

            if (Application.Current?.Resources.TryGetValue("PageBackgroundDark", out var darkColor) == true)
                dark = (Color)darkColor;

            if (Application.Current?.Resources.TryGetValue("PageBackgroundLight", out var lightColor) == true)
                light = (Color)lightColor;

            var color = theme == AppTheme.Dark ? dark : light;
            var style = theme == AppTheme.Dark ? StatusBarStyle.LightContent : StatusBarStyle.DarkContent;

            StatusBar.SetColor(color);
            StatusBar.SetStyle(style);
            #endif
        }
    }
}