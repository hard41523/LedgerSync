using CommunityToolkit.Mvvm.DependencyInjection;
using LedgerSyncViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Windows;

namespace LedgerSync
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Services = ConfigureServices();
            Ioc.Default.ConfigureServices(Services);
        }

        // FIX: store handle so we can dispose it in OnExit
        private EventWaitHandle _singleInstanceHandle;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createNew;
            _singleInstanceHandle = new EventWaitHandle(
                false, EventResetMode.AutoReset, "LedgerSync", out createNew);

            if (!createNew)
            {
                MessageBox.Show("Application is already running.");
                App.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        // FIX: release EventWaitHandle on exit to prevent OS handle leak
        protected override void OnExit(ExitEventArgs e)
        {
            _singleInstanceHandle?.Close();
            _singleInstanceHandle?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        public IServiceProvider Services { get; }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<MenuViewModel>();
            services.AddSingleton<SecretKeyViewModel>();
            services.AddSingleton<AnalyzeViewModel>();
            services.AddSingleton<TradeDataViewModel>();
            return services.BuildServiceProvider();
        }
    }
}
