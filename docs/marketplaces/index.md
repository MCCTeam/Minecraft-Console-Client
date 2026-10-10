# Marketplaces

MCC includes the [official Marketplace](https://github.com/MCCTeam/Marketplace) as its default publisher. Search it with `/plugins search <text> in official` and install with `/plugins install <id>@official`. Automatic updates start off.

A marketplace is a publisher catalogue. It lists plugin identities, release versions, dependencies, and download assets. It does not need to store every binary inside Git.

[Use a marketplace in MCC](using.md) explains installation, version selection, updates, and recovery. [Create a marketplace](creating.md) explains repository layout and publication. The [format reference](format.md) defines schema 2 and the shared DMCBK APIs.

The index points to one release catalogue per plugin. Each catalogue lists historical versions. Each version offers one or more source or compiled assets. MCC resolves the dependency graph and downloads one selected asset per changed plugin.

Old MCC marketplace formats do not work. New manifests, catalogues, and installation locks use schema 2.

## Agent skills

Install the [marketplaces authoring skill](https://github.com/MCCTeam/MCC-Skills/tree/master/skills/dmcbk-marketplace-authoring) for your coding agent:

```bash
npx skills add MCCTeam/MCC-Skills --skill dmcbk-marketplace-authoring
```

The skill includes standalone references and examples. Read [agent skill installation](../contributing/agent-skills.md) for agent selection and installation scope.
