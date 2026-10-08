# Dependencies and assembly loading

A plugin dependency and a private DLL dependency solve different problems. The first selects another plugin. The second supplies implementation code inside one package.

## Declare plugin dependencies

```toml
[requires]
shared-tools = "^2.1.0"

[optional]
alerts = ">=3.0.0 <4.0.0"
```

Required dependencies must be installed at compatible versions. Resolution includes their transitive requirements before installation changes begin.

Optional dependencies are not installed automatically. An incompatible optional provider is unavailable to the consumer. The consumer must handle its absence.

One active version exists per plugin ID. Conflicting required ranges, missing providers and required dependency cycles reject the plan.

Pins restrict updates. Yanked releases remain in history. New selection excludes them, but resolution can retain an installed yanked version that remains compatible.

1. Use required dependencies for functionality that must exist.
2. Use optional dependencies for enhancements that can be absent.
3. Specify the versions that you tested with your contract.

## Publish shared contracts

A provider can export an assembly containing public interfaces and message types:

```toml
[exports]
assemblies = ["SharedTools.Contracts.dll"]
```

The consumer declares the provider under `[requires]`. The runtime resolves the exported assembly through the provider's load context.

That shared type identity matters. Two private copies of the same interface name are different CLR types.

1. Put public contracts in a small separate assembly.
2. Keep implementation dependencies out of that contract assembly.
3. Build consumers against the matching contract package.
4. Declare the provider and its version range in each consumer manifest.
5. Include the exported contract in the provider package.

## Services and messages

| API | Purpose |
| --- | --- |
| `Services.Register<T>(instance)` | Publish a class or interface service |
| `Services.TryGet<T>(out service)` | Request a service without assuming availability |
| `Messenger.Subscribe<T>(callback)` | Receive typed notifications |
| `Messenger.Publish<T>(message)` | Publish a notification |
| `Messenger.RegisterResponder<TRequest,TResponse>(callback)` | Register a typed request handler |
| `Messenger.TryRequest<TRequest,TResponse>(request, out response)` | Request a response when a handler exists |

Registration handles can be disposed to withdraw a service or subscription early. The runtime also cleans up plugin-owned registrations on unload.

Custom message and service types must have compatible assembly identity. Shared framework types and explicitly exported provider contracts satisfy that boundary.

Avoid retaining another plugin's service after its provider unloads. Re-resolve optional services at the point of use.

## Private managed dependencies

The manifest lists private helper DLLs in `deps`:

```toml
deps = ["lib/ExampleHelpers.dll"]
```

Private dependencies resolve in the plugin's collectible load context. Different plugins can use different private library versions.

The runtime shares DMCBK, UMPK and designated framework contracts with the host. It rejects packages containing duplicate host contracts.

## Native dependencies

1. Publish a compiled asset for each supported runtime target.
2. Include the entry assembly's `.deps.json` file when using native dependency resolution.
3. Include the corresponding native libraries and private managed helpers.
4. Test loading on the actual operating system and process architecture.

The runtime uses `AssemblyDependencyResolver` for managed and native lookup. File names, dependency metadata and packaged paths must agree.

An `any` asset is appropriate for portable managed code. It cannot hide an architecture-specific native dependency.

Collectible contexts enable unloading, but references, running tasks and native handles can delay release. Never depend on overwriting a loaded DLL.

Next: [Testing and release](testing-and-release.md).

## Choose the correct dependency mechanism

| Requirement | Mechanism |
| --- | --- |
| Another plugin must run | Manifest `[requires]` |
| Another plugin adds optional behavior | Manifest `[optional]` and absence handling |
| A DLL implements private code | Packaged helper under `deps` |
| Both plugins exchange a typed object | Exported contract assembly |
| The host provides a module | Manifest `needs` |
| The entry project needs a build reference | NuGet `PackageReference` in the author project |

A NuGet reference in an author project does not install another plugin. A manifest dependency does not restore a NuGet package during source compilation.

## Version ranges in practice

`^2.1.0` accepts compatible releases from 2.1.0 up to, but excluding, 3.0.0. `>=2.1.0 <2.4.0` applies an explicit lower and upper bound. An exact version selects that release.

Required ranges must intersect across every installed dependent. If one consumer requires version 2 and another requires version 3, the resolver rejects the graph. It does not load both provider versions side by side.

Private helper versions can coexist because they belong to separate load contexts. This does not apply to one active plugin ID or to shared host contracts.

## Contract design

A shared interface should describe data and operations without exposing implementation types. Keep the contract assembly small. Avoid references to UI frameworks or private helper libraries.

A provider owns the exported contract at runtime. Consumers compile against the same contract definition and declare the provider's compatible range. Copying the DLL privately into a consumer creates a different type identity.

Treat a cached optional service as invalid after provider unload. Request it again when the operation begins. Do not keep references that prevent a provider's load context from becoming collectible.

## Diagnose dependency failures

1. Check the dependency's plugin ID.
2. Check the provider's installed version.
3. Check the required range.
4. Check whether the provider loaded successfully.
5. Check exported contract paths.
6. Check whether the consumer packaged a duplicate shared contract.

For native files, test the actual process target. An x64 machine can run an x86 process. The x86 process requires an x86 native library.


This chapter follows the [DMCBK plugin guide](https://github.com/MCCTeam/DMCBK/tree/master/docs/plugins). MCC uses the same SDK and package formats.
