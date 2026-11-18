using Voxta.Abstractions.Modules;
using Voxta.Abstractions.Registration;

namespace Voxta.Modules.AgenticSims;

public class VoxtaModule : IVoxtaModule
{
    public const string ServiceName = "AgenticSims";
    public const string ContextKey = "agentic_sims.native";
    public const string AugmentationKey = "agentic_sims.native";
    public static readonly AugmentationKeyDescriptor[] Augmentations =
        [new(AugmentationKey, "Agentic Sims", "Player-shared Sims, needs and activities with locally controlled action requests.")];

    public void Configure(IVoxtaModuleBuilder builder)
    {
        builder.Register(new()
        {
            ServiceName = ServiceName, Label = "Agentic Sims", Version = ModuleInfo.Version,
            Flags = [ModuleFlag.Experimental],
            InstallableBy = ModuleInstallableBy.AdminsOnly,
            Supports = new() { ChatAugmentations = new(ServiceDefinitionCategoryScore.High), Launcher = new(ServiceDefinitionCategoryScore.High) },
            Pricing = ServiceDefinitionPricing.Free, Hosting = ServiceDefinitionHosting.Builtin,
            Notes = "Requires Agentic Sims on this Windows computer. Opt-in, account-bound access; local grants control all game actions. Unstable test build: use a separate test save.",
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
