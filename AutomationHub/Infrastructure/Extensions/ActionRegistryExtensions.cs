using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Adapters.Outbound;

namespace AutomationHub.Infrastructure.Extensions;

public static class ActionRegistryExtensions
{
    public static IServiceCollection AddActionHandlers(this IServiceCollection services)
    {
        services.AddSingleton<ConsoleActionHandler>();
        services.AddSingleton<MqttPublisher>();
        services.AddSingleton<SmtpSender>();

        services.AddSingleton<IActionRegistry>(sp =>
        {
            var registry = new ActionRegistry();
            registry.RegisterAction(ActionType.LogEvent, sp.GetRequiredService<ConsoleActionHandler>());
            registry.RegisterAction(ActionType.PublishMqtt, sp.GetRequiredService<MqttPublisher>());
            registry.RegisterAction(ActionType.SendEmail, sp.GetRequiredService<SmtpSender>());
            return registry;
        });

        return services;
    }
}