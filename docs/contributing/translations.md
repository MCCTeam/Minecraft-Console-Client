# Translate the documentation and CLI

MCC uses separate translation systems for its own interface, Minecraft messages and plugins. A translated game item name does not translate MCC's menus.

## What belongs where

| Text | Source | Distribution |
| --- | --- | --- |
| MCC CLI and TUI | `src/Mcc.Cli/Resources/TextResources.resx` and `Localization/Resources/MccStrings.resx` | .NET resource assemblies in culture folders. |
| Container diagrams | `Localization/Resources/ContainerArt.resx` | MCC resource assemblies. |
| Site pages | English Markdown under `docs/` | Translations under `docs/l10n/<locale>/`. |
| Site navigation | `docs/.vuepress/ui/en.json` | A UI JSON file for each locale. |
| DMCBK commands, errors and configuration comments | DMCBK resources | DMCBK NuGet packages. |
| Minecraft item/chat translation keys | UMPK language data and server resource packs | Loaded by the client. |
| Plugin messages and settings comments | Each plugin's `lang/*.toml` | The plugin package. |

MCC's `crowdin.yml` maps its resources and site pages. DMCBK and plugin translations need changes in their own repositories.

## Translate through Crowdin

1. Join the MCC Crowdin project through the team's project invitation.
2. Select your language.
3. Select a source file.
4. Translate one string or paragraph.
5. Keep command names, TOML keys, filenames and code blocks unchanged.
6. Submit the translation for review.

Do not translate `--configurations`, `/scripts run`, `.bcn`, `schema-version`, or plugin identifiers. Explain those terms in prose instead.

Formatted strings use numbered placeholders such as `{0}` and `{1}`. Keep every placeholder and its format suffix. You can change their order if your language needs it.

For example, a template might contain:

```text
To sign in, open {0} in a browser and enter the code: {1}
```

`{0}` is the URL. `{1}` is the authentication code. Never replace either with a literal example value.

Keep escaped braces `{{` and `}}` in formatted resources. They represent visible braces. Preserve Markdown code fences in help pages.

## Maintainer workflow

The translation workflows use `CROWDIN_PROJECT_ID` and `CROWDIN_TOKEN` as GitHub Actions secrets. The token needs access to the MCC project.

`Translations` uploads the current sources and downloads reviewed translations. It builds and tests the CLI with the downloaded resource files. It also builds the translated site.

The workflow uploads artifacts. It does not create commits, push a branch or deploy the site. A maintainer reviews the generated output before adding translations to a release.

Crowdin exports culture tags through `%locale%`, such as `fr-FR` or `pt-BR`. The .NET SDK compiles `TextResources.fr-FR.resx` into a resource assembly under `fr-FR/`.

The configuration follows [Crowdin's hierarchy rules](https://support.crowdin.com/developer/configuration-file/).

## Check a CLI translation locally

Install the [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) before you build MCC.

1. Place the translated `.resx` beside its English source.
2. Keep the source filename and add the culture tag before `.resx`.
3. Build MCC.
4. Run MCC with that language.

```bash
source tools/mcc-env.sh
mcc-build
mcc-run --localization.language=fr-FR --connection.auto-connect=false
```

You can also set this value in `client.toml`:

```toml
[Localization]
Language = "fr-FR"
```

`auto` uses the operating system's UI culture. A missing translation uses the parent language, then English.

This repository includes a small French prompt sample to test resource loading. It is not a complete French interface. Downloaded Crowdin files provide the reviewed translations.

A language switch changes UI text. It does not rename commands, configuration keys or plugin IDs. Early messages before configuration loads can use the operating system's language.

## Check a site translation locally

Install [Node.js with npm](https://nodejs.org/en/download) and [Python](https://www.python.org/downloads/) 3.10 or later before you build the site.

1. Place translated pages under `docs/l10n/<locale>/`.
2. Preserve each page's path below `docs/`.
3. Add a translated `index.md` at the locale root.
4. Add navigation labels in `docs/.vuepress/ui/<locale>.json`.
5. Build the site.

```text
docs/getting-started/first-session.md
docs/l10n/fr-FR/getting-started/first-session.md
docs/l10n/fr-FR/index.md
docs/.vuepress/ui/fr-FR.json
```

```bash
npm --prefix docs ci
npm --prefix docs run docs:build
```

The language selector lists downloaded locales with a root index. Missing pages use an English fallback with a visible notice. The build creates those fallback pages locally.

Relative links stay within the same translated tree. When a page uses an absolute site link, check that it points at the intended language.

## Common errors

| Error | Fix |
| --- | --- |
| A resource format fails | Restore every numbered placeholder and its braces. |
| A page fails to build | Check code fences, Markdown links and literal HTML characters. |
| A locale does not appear | Add its translated root `index.md`. |
| Some command messages remain English | Check DMCBK's translations and the package version. |
| A plugin prints a key | Add that key to its language file or use its English fallback. |

Never include account passwords, browser codes or token caches in translation files.
