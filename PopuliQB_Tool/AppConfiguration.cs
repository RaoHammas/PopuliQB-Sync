using System.IO;
using Microsoft.Extensions.Configuration;

namespace PopuliQB_Tool;

public class AppConfiguration
{
    private readonly IConfigurationRoot _configuration;

    public AppConfiguration()
    {
        var confBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSetting.json", optional: false, reloadOnChange: true);

        _configuration = confBuilder.Build();
    }

    public string? GetValue(string key)
    {
        return _configuration.GetSection(key).Value;
    }

    public string? this[string key]
    {
        get => _configuration[key];
        set => _configuration[key] = value;
    }
}