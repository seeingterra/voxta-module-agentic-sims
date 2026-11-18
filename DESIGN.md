# Design and upstream evidence

Reviewed 2026-11-17:

- [Elite Dangerous module](https://github.com/voxta-ai/voxta-module-elite-dangerous):
  a native registration, lifetime service, per-chat augmentation, configuration
  form, journal-derived state/context, semantic action inference and event feedback.
- [Module SDK examples](https://github.com/voxta-ai/voxta-sdk-examples): current
  two-phase `PlanAsync`/`ActivateAsync`, opt-in tools, permission classes and UI cards.
- [Module template](https://github.com/voxta-ai/voxta-module-template): manifest,
  locked restore, SDK packaging and public registry/release workflow.
- [Module docs](https://doc.voxta.ai/docs/developers/modules): native versus external
  integrations. Some prose still describes the older instance API and manual DLL
  distribution; this module compiles against the current template's SDK.

| Elite Dangerous pattern | Agentic Sims implementation |
|---|---|
| Native module and typed configuration | Native Games module with account-bound runtime folder/port |
| Long-lived file watcher | Existing companion remains authoritative; no second game watcher |
| Per-chat augmentation | Explicit opt-in instance, account check, disposal-owned HTTP client |
| Inject journal state | Replace current shared-Sim status context at activation and user turns |
| Semantic ship actions and keypresses | Structured tool calls through the existing player-granted bridge |
| Events can trigger speech | No unsolicited replies or polling; avoid duplicated conversation loops |

The Sim scripts must still execute in The Sims 4's Python runtime, while Voxta owns
chat orchestration. Moving those into an in-process Voxta DLL would lose the game
thread, mailbox isolation and player settings. The native module is therefore an
optional Voxta-side adapter to the existing integration, not another companion.

## Boundaries

- Fixed `127.0.0.1` endpoint, no proxies/redirects, bounded request fields and
  responses, cancellation and whole-call timeout. Only an admin sets a runtime path.
- Read the ignored local key on every call. Never include it in configuration
  forms, tool schemas/results, context or logs. Companion revalidates each call.
- Fail closed for a mismatched/missing Voxta account UUID. Opt-in only, no catch-all.
  Only Voxta admins may install/configure this machine-access module.
- Existing grant enforces world, Sim scope, proposal review, preview/autonomy gates
  and durable request UUIDs. The module does not authorize game actions itself.
- Action tools use `Act` permission and are off initially. No persistent
  “always allow” grants are written by this module. Configure local grants visibly.
- Treat game labels/tool JSON as observation data. Unavailable context replaces
  previous live status. Historical chat/tool messages remain subject to Voxta's
  retention and model-provider settings.
- No webhook, chat identity impersonation, save manipulation, Patreon secret,
  signing key, game files or AI provider keys are packaged.

## Compatibility and scope

Module version and app version are independent. First module target:
`0.1.0-preview.1`, SDK `1.13.0-beta.1`, Agentic Sims `0.3.8-preview+`, Windows x64.
The registry manifest currently allows compatible newer Voxta versions; cap/yank
a released range if live compatibility tests fail. It is an unstable test module,
not a claim of official catalog acceptance or completed in-game validation.

This implementation is original bridge code. Upstream example/template projects
are MIT licensed; no Elite Dangerous code, logo, binaries or gameplay content is
redistributed. Voxta SDK licensing remains with its publisher.
