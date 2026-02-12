using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Models.Constants;
using AutomationHub.Infrastructure.Adapters.Outbound;

namespace AutomationHub.Infrastructure.Extensions;

public static class ActionRegistryExtensions
{
    public static IServiceCollection AddActionHandlers(this IServiceCollection services)
    {
        services.AddSingleton<ConsoleLogger>();
        services.AddSingleton<MqttPublisher>();

        services.AddSingleton<IActionRegistry>(sp =>
        {
            var registry = new ActionRegistry();
            registry.RegisterAction(ActionType.LogEvent, sp.GetRequiredService<ConsoleLogger>());
            registry.RegisterAction(ActionType.PublishMqtt, sp.GetRequiredService<MqttPublisher>());
            return registry;
        });

        return services;
    }
}