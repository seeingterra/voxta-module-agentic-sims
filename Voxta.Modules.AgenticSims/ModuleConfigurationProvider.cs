using Voxta.Abstractions.Registration;
using Voxta.Abstractions.Security;
using Voxta.Model.Shared.Forms;

namespace Voxta.Modules.AgenticSims;

public class ModuleConfigurationProvider : ModuleConfigurationProviderBase, IModuleConfigurationProvider
{
    public static string[] FieldsRequiringReload => [Runtime.Name, Port.Name, Owner.Name, Actions.Name];
    /// <summary>Where Agentic Sims installs by default; its runtime folder holds remote-bridge.json.</summary>
    public static readonly string DefaultRuntime = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Agentic Sims", ".runtime");
    public static readonly FormTextField Runtime = new() { Name = "RuntimeFolder", Label = "Agentic Sims runtime folder", Required = true, DefaultValue = DefaultRuntime, Text = "The .runtime folder inside your Agentic Sims installation. The default install uses this path; if you installed elsewhere, copy it from Agentic Sims → Remote Sims → Native Voxta module. Don't have Agentic Sims yet? Download it free from https://seeingterra.github.io/agentic-sims/#download, then run AgenticSims.Setup.exe with The Sims 4 closed. Keep the key private: never paste the contents of remote-bridge.json here." };
    public static readonly FormIntField Port = new() { Name = "CompanionPort", Label = "Companion port", DefaultValue = 5390 };
    public static readonly FormTextField Owner = new() { Name = "OwnerUserId", Label = "Voxta account allowed to use this world", Required = true, DefaultValue = "", Text = "Your Voxta user UUID. Only chats belonging to this account can access the world. Filled from the account configuring this module when empty." };
    public static readonly FormTextField GamePath = new() { Name = "GamePath", Label = "The Sims 4 game path (optional)", Required = false, DefaultValue = "", Text = "Only needed if Voxta can't find the game in Steam or the EA app. TS4_x64.exe, or The Sims 4 folder (for example C:/Program Files/EA Games/The Sims 4)." };
    public static readonly FormBooleanField Actions = new() { Name = "EnableRequests", Label = "Offer action requests and text shortcuts", DefaultValue = false, Text = "Optional. Voxta asks for tool permission; Agentic Sims still enforces the selected Sims, world, local approval and preview availability. Monitoring works without this." };

    public Task<FormField[]> GetModuleConfigurationFieldsAsync(IAuthenticationContext auth, ISettingsSource settings, CancellationToken cancellationToken) =>
        Task.FromResult(FormBuilder.Build(
            FormTitleField.Create("Agentic Sims · experimental", "Enable Remote Sims in the local app, select Sims and start with Monitor only. Enable this augmentation explicitly on a private assistant chat. Do not also attach the MCP bridge to that chat."),
            Runtime, Port,
            new FormTextField { Name = Owner.Name, Label = Owner.Label, Text = Owner.Text, Required = true, DefaultValue = auth.UserId.ToString() }, Actions, GamePath));
}
