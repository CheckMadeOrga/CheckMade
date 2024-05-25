using System.Configuration;
using CheckMade.Common.Utils.UiTranslation;
using CheckMade.Telegram.Function.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheckMade.Tests.Startup;

public abstract class TestStartupBase
{
    protected IConfigurationRoot Config { get; private init; }
    protected string HostingEnvironment { get; private init; }
    internal ServiceCollection Services { get; } = [];
    
    protected TestStartupBase()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../"));
        
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(projectRoot)
            // If this file can't be found we assume the test runs on GitHub Actions Runner with corresp. env. variables! 
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            // This config (the secrets.json of the main Telegram project) gets ignored on the GitHub Actions Runner
            .AddUserSecrets("dd4f1069-ae94-4987-9751-690e8da6f3c0") 
            // This also includes Env Vars set in GitHub Actions Workflow
            .AddEnvironmentVariables();
        Config = configBuilder.Build();

        // This is taken either from local.settings.json or from env variable set in GitHub Actions workflow!
        const string keyToHostEnv = "HOSTING_ENVIRONMENT";
        HostingEnvironment = Config.GetValue<string>(keyToHostEnv)
            ?? throw new ConfigurationErrorsException($"Can't find {keyToHostEnv}");
    }

    protected void ConfigureServices()
    {
        RegisterBaseServices();
        RegisterTestTypeSpecificServices();
    }

    private void RegisterBaseServices()
    {
        Services.AddLogging(loggingConfig =>
        {
            loggingConfig.ClearProviders();
            loggingConfig.AddConsole(); 
            loggingConfig.AddDebug(); 
        });
        
        Services.AddSingleton<ITestUtils, TestUtils>();

        Services.AddScoped<DefaultUiLanguageCodeProvider>(_ => new DefaultUiLanguageCodeProvider(LanguageCode.En));
        
        Services.ConfigureBotUpdateHandlingServices();
        Services.ConfigureUtilityServices();
        Services.ConfigureBotBusinessServices();
    }

    protected abstract void RegisterTestTypeSpecificServices();
}