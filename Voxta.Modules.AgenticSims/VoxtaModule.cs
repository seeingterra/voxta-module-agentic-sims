using Voxta.Abstractions.Modules;
using Voxta.Abstractions.Registration;

namespace Voxta.Modules.AgenticSims;

public class VoxtaModule : IVoxtaModule
{
    public const string ServiceName = "AgenticSims";
    public const string ContextKey = "agentic_sims.native";
    public const string AugmentationKey = "agentic_sims.native";
    public static readonly AugmentationKeyDescriptor[] Augmentations =
        [new(AugmentationKey, "Agentic Sims", "Your Sims from The Sims 4 in this chat: their mood, needs, relationships and what they're doing, plus suggestions you approve in Agentic Sims.")];

    public void Configure(IVoxtaModuleBuilder builder)
    {
        builder.Register(new()
        {
            ServiceName = ServiceName, Label = "Agentic Sims", Version = ModuleInfo.Version,
            Flags = [ModuleFlag.Experimental],
            InstallableBy = ModuleInstallableBy.AdminsOnly,
            Supports = new() { ChatAugmentations = new(ServiceDefinitionCategoryScore.High), Launcher = new(ServiceDefinitionCategoryScore.High) },
            Pricing = ServiceDefinitionPricing.Free, Hosting = ServiceDefinitionHosting.Builtin,
            Notes = "How it works: Agentic Sims (a free app and Sims 4 mod) runs next to the game. In its Remote Sims page you pick which Sims Voxta may see. Add \"Agentic Sims\" to a private chat in Voxta to ask about those Sims and suggest actions; nothing happens in your game unless you allow it in Agentic Sims. Only your Voxta account can use your game. Experimental test build: play on a spare save.",
            HelpLink = "https://seeingterra.github.io/agentic-sims/",
            Augmentations = Augmentations,
            Logo = ModuleLogo.EmbeddedResource(typeof(VoxtaModule), "Assets.logo.svg"),
            ModuleConfigurationProviderType = typeof(ModuleConfigurationProvider),
            ModuleConfigurationFieldsRequiringReload = ModuleConfigurationProvider.FieldsRequiringReload,
        });
        builder.AddChatAugmentationsService<AgenticSimsService>(ServiceName);
        // The Sims 4 card on Voxta's Play page.
        builder.Contribute<Voxta.Abstractions.Services.Launchers.ILauncher, SimsLauncher>();
    }
}
