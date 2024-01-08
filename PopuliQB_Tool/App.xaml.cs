using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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
    private static IServiceProvider _services => ConfigureServices();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var vm = _services.GetRequiredService<MainWindowViewModel>();

        MainWindow mainWin = new()
        {
            DataContext = vm,
        };

        mainWin.Show();
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
        
        services.AddSingleton<QBInvoiceService>();
        services.AddSingleton<PopInvoiceToQbInvoiceBuilder>();        
        
        services.AddSingleton<QbAccountsService>();
        services.AddSingleton<PopAccountsToQbAccountsBuilder>();

        services.AddSingleton<QbItemService>();
        services.AddSingleton<PopItemToQbItemBuilder>();
        services.AddSingleton<PopCreditMemoToQbCreditMemoBuilder>();

        services.AddSingleton<QbPaymentsService>();
        services.AddSingleton<PopPaymentToQbPaymentBuilder>();
        services.AddSingleton<PopRefundToQbChequeBuilder>();
        services.AddSingleton<PopDepositToQbDepositBuilder>();


        return services.BuildServiceProvider();
    }
}