using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.BusinessServices;
using PopuliQB_Tool.Services;
using PopuliQB_Tool.ViewModels;

namespace PopuliQB_Tool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private static IServiceProvider Services => ConfigureServices();

    protected override void OnStartup(StartupEventArgs e)
    {
        _logger.Info("-------------------------------------------------");
        _logger.Info("STARTING THE SYNC TOOL");
        _logger.Info("-------------------------------------------------");
        base.OnStartup(e);
        SetupExceptionHandling();

        var vm = Services.GetRequiredService<MainWindowViewModel>();

        MainWindow mainWin = new()
        {
            DataContext = vm,
        };

        mainWin.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _logger.Info("-------------------------------------------------");
        _logger.Info("EXITING THE SYNC TOOL");
        _logger.Info("-------------------------------------------------");
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MessageBoxService>();

        services.AddSingleton<QBCompanyService>();
        services.AddSingleton<PopuliAccessService>();

        services.AddSingleton<QbCustomerService>();
        services.AddSingleton<PopPersonToQbCustomerBuilder>();
        services.AddSingleton<PopInvoiceToQbInvoiceBuilder>();
        services.AddSingleton<QbAccountsService>();
        services.AddSingleton<PopAccountsToQbAccountsBuilder>();
        services.AddSingleton<QbItemService>();
        services.AddSingleton<PopItemToQbItemBuilder>();
        services.AddSingleton<PopCreditMemoToQbCreditMemoBuilder>();
        services.AddSingleton<PopPaymentToQbPaymentBuilder>();
        services.AddSingleton<PopRefundToQbChequeBuilder>();
        services.AddSingleton<PopDepositToQbDepositBuilder>();
        services.AddSingleton<PopReversalToJournalBuilder>();


        services.AddSingleton<QbInvoiceServiceQuick>();
        services.AddSingleton<QbPaymentServiceQuick>();
        services.AddSingleton<QbDepositServiceQuick>();
        services.AddSingleton<QbCreditMemoServiceQuick>();
        services.AddSingleton<QbRefundServiceQuick>();
        services.AddSingleton<CustomFieldBuilderQuick>();
        services.AddSingleton<QbJournalServiceQuick>();

        services.AddSingleton<QbService>();

        services.AddSingleton<AppConfiguration>();
        services.AddSingleton<IOService>();

        return services.BuildServiceProvider();
    }
    
    private void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

        DispatcherUnhandledException += (s, e) =>
        {
            LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }

    private void LogUnhandledException(Exception exception, string source)
    {
        var message = $"Unhandled exception ({source})";
        try
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            message = $"Unhandled exception in {assemblyName.Name} v{assemblyName.Version}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception in LogUnhandledException");
        }
        finally
        {
            _logger.Error(exception, message);
        }
    }
}