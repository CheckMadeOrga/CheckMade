using Microsoft.Extensions.DependencyInjection;

namespace CheckMade.ChatBot.Logic;

public static class ServiceRegistration
{
    public static void Register_TelegramLogic_Services(this IServiceCollection services)
    {
        services.AddScoped<IInputProcessorFactory, InputProcessorFactory>();
        services.AddScoped<IWorkflowIdentifier, WorkflowIdentifier>();
    }
}