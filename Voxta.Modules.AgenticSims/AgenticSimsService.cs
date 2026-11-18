using Microsoft.Extensions.Logging;
using Voxta.Abstractions.Services;
using Voxta.Abstractions.Services.ChatAugmentations;

namespace Voxta.Modules.AgenticSims;

public class AgenticSimsService(ILogger<AgenticSimsService> logger) : ServiceBase(logger), IToolsChatAugmentationsService
{
    public Task<AugmentationPlanItem[]> PlanAsync(IChatAugmentationPlanContext context, CancellationToken cancellationToken) =>
        Task.FromResult<AugmentationPlanItem[]>([new()
        {
            Key = VoxtaModule.AugmentationKey, Label = "Agentic Sims",
            Description = "Brings the Sims you shared in Agentic Sims (Remote Sims) into this chat. Ask how they're doing or what they could do next; game actions only happen when you allow them in Agentic Sims.",
            Activation = AugmentationActivation.OptIn, IncludeInCatchAll = false, Suggested = false,
            Tools = ToolCatalog.Specs.Select(s => new AugmentationToolDeclaration(s.Name, VoxtaModule.ContextKey)).ToArray(),
            ActivateAsync = async (session, ct) =>
            {
                if (!Guid.TryParse(ModuleConfiguration.GetRequired(ModuleConfigurationProvider.Owner), out var owner) || owner != session.Auth.UserId)
                    return null;
                var client = new BridgeClient(ModuleConfiguration.GetRequired(ModuleConfigurationProvider.Runtime), ModuleConfiguration.GetRequired(ModuleConfigurationProvider.Port));
                var instance = new AgenticSimsInstance(session, client, ModuleConfiguration.GetRequired(ModuleConfigurationProvider.Actions));
                try { await instance.RefreshAsync(ct); return instance; }
                catch { await instance.DisposeAsync(); throw; }
            }
        }]);
}
