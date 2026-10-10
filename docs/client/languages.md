# Languages

MCC's interface language, Minecraft locale, plugin translations, and documentation translations are separate parts of the system.

## Select the MCC language

In `client.toml`:

```toml
[Localization]
Language = "auto"
```

`auto` follows the operating system. Use a language tag such as `de` or `pt-BR` to request a language explicitly.

Inside MCC:

```text
/lang
/lang de
/lang auto
```

`/lang` shows the configured interface language, resolved culture, server locale, and loaded plugin coverage. Changing the language saves `Localization.Language` and updates supported live translation consumers.

A valid culture tag does not mean every text resource has a translation. Missing resource translations fall back through available parent languages and English.

## Keep the server locale separate

```toml
[ClientSettings]
Locale = "en_US"
```

This is the Minecraft locale announced to the server. A server plugin can use it to choose messages. It does not select MCC's own interface translation.

Setting this locale to `auto` lets the client derive a Minecraft locale from the resolved culture.

## Plugin language coverage

A plugin can contain its own language tables. MCC selects a compatible table and uses fallback for missing keys. `/lang` reports what the loaded plugin supplies.

To rewrite plugin settings comments in the selected language while preserving values:

```text
/plugins settings all regen
```

`reset` has a different purpose. It replaces settings with defaults. Do not use it to translate comments.

Already generated configuration comments do not automatically change when you change a language. The comment language and live UI language can differ.

## Translate MCC and the documentation

Read [the translation guide](../contributing/translations.md) for Crowdin, resource files, and translated documentation folders.

Do not translate executable identifiers in examples. Keep command names, TOML keys, plugin IDs, file extensions, and C# symbols unchanged. Translate explanations and human-facing text around them.

A translated guide can lag behind the English source. Use the current command help when a translated example disagrees with the running build.
