# Agentic Sims for Voxta

**Experimental test module · 0.1.0-preview.5. Use a separate test save.**

[![Install in Voxta](.github/install-in-voxta.svg)](https://portal.voxta.ai/modules/install?repo=seeingterra/voxta-module-agentic-sims)

Or add `seeingterra/voxta-module-agentic-sims` in Voxta under **Add Modules → Module Registries**.

Bring your selected Sims into a private Voxta assistant chat: check who is around,
inspect needs, moods, wants, relationships and activities, discover real game
interactions and propose an activity. Requests use Agentic Sims' existing local
permissions, approval screen and receipts. An accepted request does not prove that
the Sim completed it.

This is a native .NET **Games** module, built against **Voxta 1.13.0-beta.1**.
It requires that version or a compatible newer server, Windows x64,
[Agentic Sims 0.3.10-preview or newer](https://seeingterra.github.io/agentic-sims/)
and your own copy of The Sims 4. The separate companion and game script are still
required. The module does not bundle the game, Voxta, AI models or the app installer.
It does not replace the companion's existing in-game voice/text conversations.

## Install and connect

1. Start Agentic Sims. In **Remote Sims**, select the Sims to share, choose
   **Monitor only**, enable the bridge and save. Keep one companion per installation.
2. Build this module (below), then in compatible Voxta versions use **Add Modules →
   Add Custom Module**, choose `Voxta.Modules.AgenticSims`, and Apply. For a manual
   DLL installation, extract the module ZIP into Voxta's `Modules` folder with Voxta
   closed; the `Voxta.Modules.AgenticSims.dll` must be at its top level. Restart Voxta.
   Managed registry installation will be available after a public module release.
3. Configure **Agentic Sims** in Voxta. Paste the runtime folder and port from the
   app's **Remote Sims → Native Voxta module** card. Existing 0.3.8 installs can use
   the folder following `--root` in their MCP connection arguments and port 5390.
   **Never copy the contents of `remote-bridge.json` into a chat or settings form.**
4. The **Voxta account allowed to use this world** field defaults to the configuring
   user's UUID. Keep that account, or enter the intended account's exact Voxta UUID.
   Other accounts cannot activate this module. Install/configure it as an admin.
5. Explicitly enable **`agentic_sims.native`** on a private assistant chat. A wildcard
   does not attach this module. Do not attach the MCP bridge to the same chat, or
   enable this augmentation on characters used for companion-managed in-game chats.
6. Ask: “Use Agentic Sims to list my shared Sims, then inspect their needs.” Verify
   the tool results in Voxta's Chat Inspector. Tools require a tool-capable text
   generation model; STT and TTS are optional for typed use. There are no additional
   cloud credentials or model calls made directly by this module.

To propose an activity, enable **Offer action requests and text shortcuts** in the
module and choose **Propose · approve here** in Agentic Sims. Start with a harmless
activity on a test save. Voxta's own tool permission prompts still apply. Direct
actions additionally require the locally enabled preview and Sim autonomy settings.
Revoking access in Agentic Sims affects the next call. Already accepted actions
cannot be recalled by revocation.

The account restriction identifies the Voxta session owner, **not** individual
Discord/channel participants. Use owner-only private chats and trusted characters.
Do not expose this world through a public bot. State placed in a chat may remain
in Voxta history or reach your selected AI provider. Historical replies cannot be
erased by revoking future access. See the [privacy policy](https://seeingterra.github.io/agentic-sims/privacy.html)
and [terms](https://seeingterra.github.io/agentic-sims/terms.html), including Voxta's
own linked policies.

## Tools

| Tool | Purpose |
|---|---|
| `agentic_sims_status` | Running/paused/offline state, current scene, shared Sims |
| `agentic_sims_inspect` | One shared Sim's observed needs, mood, wants and activity |
| `agentic_sims_interactions` | Discover actual interaction IDs; eligibility is tested by the game |
| `agentic_sims_request_action` | Propose or locally authorized dispatch; stable UUID prevents duplicate retries |
| `agentic_sims_receipt` | Check an existing request without executing again |
| `agentic_sims_command` | Existing text shortcuts, including cancellation of pending requests |

Requests and shortcuts are off by default. Reads do not need the action-request
switch. Offline/unloaded observations are labelled as last observed. No background
LLM chatter, automatic replies, keyboard emulation, new public listener or direct
game-file edits are introduced. The module refreshes only at activation and user
turns; individual tools read through the existing authenticated bridge.

## Build, test and pack

From the repository root, with the .NET 11 SDK (Voxta 1.13 runs on .NET 11):

```powershell
dotnet restore tests/ModuleChecks.csproj --locked-mode
dotnet run --project tests -c Release --no-restore
dotnet publish Voxta.Modules.AgenticSims -c Release -p:VoxtaModulePack=dist
```

The SDK writes `Voxta.Modules.AgenticSims-<version>-any.zip` and
`voxta-module.json` to `dist` relative to the shell's working directory. The
descriptor contains the SHA-256, SDK and compatibility metadata. Voxta supplies
its own SDK assemblies at runtime. Lockfiles are included for repeatable restores.

Releases are built by Voxta's official workflows (`voxta-ai/voxta-module-actions`): push a tag named after
`module.json`'s version (for example `v0.1.0-preview.5`). See [review and release notes](SUBMISSION.md).
The Agentic Sims app itself is a separate download; this repository contains only the Voxta module.

## License

MIT. See [LICENSE](LICENSE).

## Troubleshooting

- **No tools:** explicitly select the native augmentation, use a tool-capable
  model, check the owning Voxta account UUID and compatible server version.
- **Unavailable:** start the companion; check the absolute runtime folder, port,
  bridge enablement, current world and selected Sims. Never paste a token to fix it.
- **Requests disabled:** turn on the module's optional switch and configure the
  local proposal grant. A model cannot change either setting.
- **Stale scene:** refresh status and interactions. A different action needs a new
  UUID; a timeout needs a receipt check with the original UUID first.

Not affiliated with EA or endorsed by Voxta. References and upstream patterns are
recorded in [DESIGN.md](DESIGN.md). Live Voxta/game validation remains required.
