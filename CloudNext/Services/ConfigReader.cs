using Microsoft.Extensions.Configuration;

namespace CloudNext.Services;

class ConfigReader
{
    public string GetConfig(string key, string value)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        string? configValue = config.GetValue<string>($"{key}:{value}");

        return configValue!;
    }
}