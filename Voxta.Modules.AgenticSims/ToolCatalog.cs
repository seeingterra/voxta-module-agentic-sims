using System.Text.Json;
using Voxta.Abstractions.Services.Agents;
using Voxta.Model.Shared;

namespace Voxta.Modules.AgenticSims;

public sealed record ToolSpec(string Name, string Description, string[] Required, string[] Optional, bool Writes = false);

public static class ToolCatalog
{
    public static readonly ToolSpec[] Specs =
    [
        new("agentic_sims_status", "Check running/paused/offline state, the current scene, and only player-shared Sims. Read before claiming anything happened.", [], []),
        new("agentic_sims_inspect", "Read needs, mood, wants, relationships and activities of one shared Sim. Honor state_is_current and observed_at; offline information is last observed.", ["sim_id"], []),
        new("agentic_sims_interactions", "Find actual interaction IDs for a shared Sim, optionally by search. Candidates do not prove eligibility; use the returned scene for a proposal.", ["sim_id"], ["search"]),
        new("agentic_sims_request_action", "Request ONE exact interaction only on the player's request. Local monitoring/proposal/direct-action permissions still apply. New work needs a new UUID request_id; reuse that UUID on retries. Accepted is not completed; unknown outcomes must not be blindly retried.", ["sim_id", "scene", "option_id", "request_id"], [], true),
        new("agentic_sims_receipt", "Read an existing action receipt without executing again. Pending awaits local approval; unknown/dispatching must not be blindly retried.", ["request_id"], []),
        new("agentic_sims_command", "Run a shortcut: /help, /household, /checkin, /needs, /stats, /relationships, /activity, /where, /wants, /skills, /career, /plans, /cancel. Optional exact shared Sim ID follows a Sim command. /cancel changes a pending request.", ["text"], [], true),
    ];

    public static IReadOnlyList<ChatTool> Create(bool actions) => Specs.Select(spec => new ChatTool(
        spec.Name, spec.Description,
        ChatToolSchema.FromArguments(spec.Required.Concat(spec.Optional).Select(field => new FunctionArgumentDefinition
        {
            Name = field, Type = FunctionArgumentType.String, Required = spec.Required.Contains(field),
            Description = field switch { "request_id" => "UUID: stable across retries of the same request.", "text" => "Shortcut, at most 300 characters.", "search" => "Specific interaction search, at most 150 characters.", _ => "Exact value returned by Agentic Sims, at most 150 characters." }
        }).ToArray()), VoxtaModule.ContextKey)
    {
        AugmentationKey = VoxtaModule.AugmentationKey,
        Enabled = !spec.Writes || actions,
        Permission = spec.Writes ? ToolPermissionClass.Act : ToolPermissionClass.Read,
        AlwaysAllowed = false,
        Display = ToolDisplayMode.Card,
    }).ToArray();

    public static void Validate(string name, JsonElement args)
    {
        var spec = Specs.SingleOrDefault(s => s.Name == name) ?? throw new BridgeException("Unknown Agentic Sims tool.");
        if (args.ValueKind != JsonValueKind.Object) throw new BridgeException("Tool arguments must be an object.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in args.EnumerateObject())
        {
            if (!seen.Add(field.Name) || !spec.Required.Concat(spec.Optional).Contains(field.Name) ||
                field.Value.ValueKind != JsonValueKind.String || field.Value.GetString() is not {} value ||
                value.Length > (field.Name == "text" ? 300 : 150) || value.Any(char.IsControl))
                throw new BridgeException("Unknown, duplicate or invalid tool argument.");
            if (spec.Required.Contains(field.Name) && string.IsNullOrWhiteSpace(value))
                throw new BridgeException("Required tool arguments cannot be empty.");
            if (field.Name == "request_id" && !Guid.TryParse(value, out _)) throw new BridgeException("request_id must be a UUID.");
        }
        if (spec.Required.Any(f => !seen.Contains(f))) throw new BridgeException("A required tool argument is missing.");
    }
}
