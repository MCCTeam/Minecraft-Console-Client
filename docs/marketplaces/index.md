# Marketplaces

A marketplace is a publisher catalogue. It lists plugin identities, release versions, dependencies, and download assets. It does not need to store every binary inside Git.

[Use a marketplace in MCC](using.md) explains installation, version selection, updates, and recovery. [Create a marketplace](creating.md) explains repository layout and publication. The [format reference](format.md) defines schema 2 and the shared DMCBK APIs.

The index points to one release catalogue per plugin. Each catalogue lists historical versions. Each version offers one or more source or compiled assets. MCC resolves the dependency graph and downloads one selected asset per changed plugin.

Old MCC marketplace formats do not work. New manifests, catalogues, and installation locks use schema 2.
