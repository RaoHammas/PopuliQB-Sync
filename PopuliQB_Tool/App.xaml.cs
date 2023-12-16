using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.BusinessServices;
using PopuliQB_Tool.Services;
using PopuliQB_Tool.ViewModels;
using QBFC16Lib;

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

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MessageBoxService>();

        services.AddTransient<QBConnectionCheckService>();
        services.AddTransient<PopuliAccessService>();
        services.AddTransient<QbCustomerService>();
        services.AddTransient<PopPersonToQbCustomerBuilder>();


        return services.BuildServiceProvider();
    }
}