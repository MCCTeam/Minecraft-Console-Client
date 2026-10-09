# Contribute to MCC

MCC owns terminal behavior and presentation. DMCBK owns the client services. UMPK owns the protocol and game engine.

Start with the repository [README](https://github.com/MCCTeam/Minecraft-Console-Client) and `AGENTS.md`. Build the active `Mcc.slnx` solution before you change code.

- [Windows development](windows.md): recommended WSL workflow and native PowerShell helpers.
- [Translations](translations.md): translate the site, CLI resources and plugin text.
- [Documentation](documentation.md): write and check guides, examples and references.
- [Release packages](../deployment/releases.md): prepare complete distribution archives.

Keep changes in the repository that owns their behavior. Do not copy protocol implementations or library source into MCC.

Read the [review and checks](review.md) for the validation scope and changes from the reader council.

## Agent skills

Use [MCC Skills](agent-skills.md) for agent-assisted Beacon, plugin, marketplace, and C# tasks. The guide includes `npx skills` installation commands.

## Source architecture

Open `src/README.md` in your checkout for the application and library boundaries, startup sequence, command flow, and feature ownership.
