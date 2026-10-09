# Agent skills

An agent skill gives a coding assistant instructions, reference material, and examples for a specific task. [MCC Skills](https://github.com/MCCTeam/MCC-Skills) provides skills for Beacon, plugins, marketplaces, and C# development.

You can read the guides without an agent. Skills help an agent use the same syntax, package contracts, and checks.

## Use skills in this checkout

MCC includes MCC Skills as a pinned Git submodule at `MCC-Skills/`. Seven entries in `.skills/` are relative symbolic links to that submodule. ASD-STE100 and Humanizer remain local. The Claude, Codex and Agents discovery directories all point to `.skills/`.

Initialize the submodule from the MCC repository directory:

```bash
git submodule update --init MCC-Skills
```

For a new checkout, include the submodule when cloning:

```bash
git clone --branch feat/mcc-2.0 --recurse-submodules https://github.com/MCCTeam/Minecraft-Console-Client.git
```

Git checks out the skill revision recorded by MCC. A normal submodule update restores that revision. It does not select the latest upstream commit.

The linked skills use their own bundled references and examples. Read the relevant `SKILL.md` through `.skills/`. Git must support symbolic links for these discovery paths.

On Windows, use WSL for symbolic links. For native Git, enable [Developer Mode](https://learn.microsoft.com/en-us/windows/advanced-settings/developer-mode) before cloning. Enable symbolic links during the clone:

```powershell
git -c core.symlinks=true clone --branch feat/mcc-2.0 --recurse-submodules https://github.com/MCCTeam/Minecraft-Console-Client.git
```

Read [Git for Windows' symbolic-link guide](https://gitforwindows.org/symbolic-links) for platform requirements.

## Update the shared revision

1. Check that the submodule has no local edits.
2. Fetch its upstream `master` branch.
3. Review the skill changes.
4. Check out the selected commit.
5. Check the `.skills` links.
6. Commit the updated submodule pointer in MCC.

```bash
git -C MCC-Skills status --short
git -C MCC-Skills fetch origin master
git -C MCC-Skills log --oneline HEAD..origin/master
git -C MCC-Skills checkout <reviewed-commit>
git diff --submodule=log
git add MCC-Skills
```

Replace `<reviewed-commit>` with the commit you selected. Shared skill edits belong in the MCC Skills repository. MCC records the selected revision and its discovery links.

## Install a skill

Use `npx skills` when you want the shared skills in another project or a global agent directory.

Install [Node.js](https://nodejs.org/en/download) to use `npx`. Open a terminal in your project directory.

List the available skills:

```bash
npx skills add MCCTeam/MCC-Skills --list
```

Install the Beacon skill for Codex:

```bash
npx skills add MCCTeam/MCC-Skills --skill beacon-scripting --agent codex
```

For Claude Code, replace `codex` with `claude-code`. Omit `--agent` to choose an agent in the installer.

Add `--global` when you want the skill across projects. The default installs it for the current project.

## Choose the skill

| Task | Skill and documentation |
| --- | --- |
| Write or check `.bcn` scripts | [Beacon scripting](https://github.com/MCCTeam/MCC-Skills/tree/master/skills/beacon-scripting) |
| Build a source or compiled plugin | [Plugin authoring](https://github.com/MCCTeam/MCC-Skills/tree/master/skills/dmcbk-plugin-authoring) |
| Publish versioned plugin assets | [Marketplace authoring](https://github.com/MCCTeam/MCC-Skills/tree/master/skills/dmcbk-marketplace-authoring) |
| Write or review C# | [C# best practices](https://github.com/MCCTeam/MCC-Skills/tree/master/skills/csharp-best-practices) |
| Review interfaces and responsibilities | [C# SOLID principles](https://github.com/MCCTeam/MCC-Skills/tree/master/skills/csharp-solid-principles) |
| Diagnose .NET performance | [Performance](https://github.com/MCCTeam/MCC-Skills/tree/master/skills/dotnet-performance-profiling-and-optimization) |
| Review .NET security | [Security](https://github.com/MCCTeam/MCC-Skills/tree/master/skills/dotnet-security-review) |

Install the two publishing skills together:

```bash
npx skills add MCCTeam/MCC-Skills --skill dmcbk-plugin-authoring dmcbk-marketplace-authoring --agent codex
```

Install all skills for Codex without prompts:

```bash
npx skills add MCCTeam/MCC-Skills --skill '*' --agent codex --yes
```

Read the [skills CLI documentation](https://github.com/vercel-labs/skills) for supported agents, installation scope, and update commands.

## Give the agent a task

Include the behavior, versions, file paths, and checks that matter. For example:

> Create `welcome.bcn`. Reply only to an exact `!hello` chat message. Save the greeting count. Include lint and live-test steps.

> Create a source plugin with a translated command and a session-start counter. Use schema 2 and the pinned DMCBK packages. Include cleanup tests.

> Create a plugin catalogue with two historical versions. Provide Windows x64, Linux x64, and portable source assets. Require explicit source fallback.

The authoring skills bundle their documentation and examples. They do not require an MCC or DMCBK source checkout.

Check the generated files and test results. Offline lint cannot prove a game action. A simulated session cannot prove server acceptance.
