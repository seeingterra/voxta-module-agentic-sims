# Registry submission and release checklist

## Repository

Public repository: https://github.com/seeingterra/voxta-module-agentic-sims (MIT licence). It contains only this
module; the Agentic Sims app is a separate download and is not part of this repository. Releases use Voxta's
official reusable workflows (`voxta-ai/voxta-module-actions@v1`), as in Voxta's module template.
`VOXTA_PUBLISH` is not set: nothing is published to Voxta's module source until Voxta trusts the repository.

At the exported root the manifest is
`Voxta.Modules.AgenticSims/module.json`, the module solution is
`Voxta.Modules.AgenticSims.slnx`, and `.github/workflows` provides tested builds,
releases and range-capping/yanking via official Voxta reusable workflows.

1. Run locked restores, module checks and the live checklist below.
2. Protect `main` and release tags `v*` with GitHub rulesets. Review changes to
   workflows and package dependencies before merging. Keep `VOXTA_PUBLISH` unset
   or false until Voxta explicitly trusts the public repository and module ID.
3. Set the module version in `module.json` and tag **that exact version** prefixed
   by `v`, e.g. `v0.1.0-preview.1`. This is separate from app release tags.
4. Release workflow runs checks, calls Voxta's official packaging/release action,
   and uploads the module ZIP and `voxta-module.json`. GitHub attestations require
   the public repository. Verify with `gh attestation verify <zip> --repo owner/repo`.
5. Add a README install link using the **actual** published repo:
   `https://portal.voxta.ai/modules/install?repo=OWNER/REPO&module=Voxta.Modules.AgenticSims`.
   Do not advertise an install link before a release exists. Users can add the
   public `owner/repo` in **Add Modules → Module Registries** independently of
   default-catalog approval.
6. Ask Voxta to review/trust the repository and module ID under their **Admin →
   Module repos** process. Supply the public repo, tagged test release, module ID,
   dependency/permission explanation, test evidence, app dependency and policy links.
   Catalog acceptance is their decision; there is no assumed public JSON PR target.
7. Only after acceptance set repository variable `VOXTA_PUBLISH=true`. Official
   workflows publish with GitHub OIDC; do not create/store a Voxta publishing key.
8. For a broken release use **edit release** to cap its Voxta range or yank it.
   Deleting a GitHub release does not withdraw its Voxta catalog entry.

## Live verification required before submission

- Compatible Voxta loads the native DLL and displays the logo, Games listing,
  experimental description and configuration form.
- Correct user can opt in on a private assistant; a second Voxta account cannot
  activate it. Wildcard selection does not expose the game tools.
- Monitoring lists only shared Sims and shows correct live/offline/paused status.
  Inspect returns mood, needs, relationships and activity without private transcripts.
- Turn the bridge off, rotate its key, switch world and stop the companion:
  verify the next turn replaces live context and tools fail clearly.
- Discover a harmless activity, propose it with a UUID, approve in Agentic Sims
  and inspect its receipt. Repeat the UUID and verify no second game dispatch.
  Decline another proposal. Do not infer completion from acceptance.
- Verify direct action restrictions and unavailable preview gating; ensure the
  module cannot alter local permissions or enable Sim autonomy.
- Exercise travel, unloaded Sims, game pause, save rewind and stale scene rejects.
  Leave existing companion-managed in-game conversations on their existing path.
- Tool timeout/disconnect does not retry a mutation, trigger spontaneous speech
  or start another companion. Test on a disposable save.

## Suggested catalog copy

**Agentic Sims** — Bring your selected Sims into Voxta. Inspect their needs,
moods and activities, discover interactions and propose what to do next, with
local approval and action receipts. Requires the separate Agentic Sims companion,
The Sims 4 and a compatible Voxta server on Windows x64. Tool-capable text generation
is required; voice services are optional. Experimental pre-release: separate test
save only. Some app features are time-limited previews; no extra payment or API key
is required by this adapter. Not affiliated with EA or endorsed by Voxta.
