# Contributing

At this moment this page needs to be created.

For now you can use our article from the [Git Hub repository Wiki](https://github.com/MCCTeam/Minecraft-Console-Client/wiki/Update-console-client-to-new-version) written by [ReinforceZwei](https://github.com/ReinforceZwei).

## Translations

MCC now supports the following languages (Alphabetical order) :
  * `de.ini` : Deutsch - German
  * **`en.ini` : English - English**
  * `fr.ini` : Français (France) - French
  * `ru.ini` : Русский (Russkiy) - Russian
  * `vi.ini` : Tiếng Việt (Việt Nam) - Vietnamese
  * `zh-Hans.ini` : 简体中文 - Chinese Simplified
  * `zh-Hant.ini` : 繁體中文 - Chinese Traditional

### Add new translation

1. First you need to get the name of the translated file.
    * Visit [this link](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c) and find the first occurrence of the language you need to translate in the table below.
    * Use the language code of the row in the table as the name of the translation file.
    * For example:
        * `English` -> row `English 0x0009` -> `en` -> `en.ini`
        * `Chinese (Traditional)` -> row `Chinese (Traditional) 0x7C04` -> `zh-Hant` -> `zh-Hant.ini`

2. Which system languages are recommended to use this translation?
    * Still check the table in [this link](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c), one language may have multiple rows.
    * You will need to indicate which language codes this translation applies to.
    * For example:
        * Translation `de.ini` applies to `de`, `de-AT`, `de-BE`, `de-DE`, ...
        * Translation `zh-Hans.ini` applies to `zh-Hans`, `zh`, `zh-CN`, `zh-SG`.

3. Which game languages are recommended to use this translation?
    * Check out the table in [this link](https://github.com/MCCTeam/Minecraft-Console-Client/discussions/2239), where the `Locale Code` column indicates the language code in minecraft.
    * You will need to indicate which locale codes this translation applies to.
    * For example:
        * Translation `fr.ini` applies to `fr_ca`, `fr_fr`.
        * Translation `zh-Hans.ini` applies to `zh_cn`.

4. Add the new translation to the code. (Optional)
    * **If you are not familiar with programming, you can skip this step and just write the above information in your PR or issue.**
    * Add the newly created translation file `xx.ini` to the project `/Resources/lang/xx.ini`.
    * Open `/DefaultConfigResource.resx`.
    * Click `Add Resources`.
    * Choose `/Resources/lang/xx.ini`.
    * Rename the added resource file in `/DefaultConfigResource.resx` to `Translation_xx`.
    * Open `/Translations.cs`.
    * Find `public static Tuple<string, string[]> GetTranslationPriority();`
    * Update the mapping of system language codes to translation files.
    * Find `public static string[] GetTranslationPriority(string gameLanguage);`
    * Update the mapping of game locale code to translation files.

5. Follow the section "Update existing translation".

### Update existing translation

1. Visit [the lang folder](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/Resources/lang), download `en.ini` and the language you want to translate(`xx.ini`).

2. Compare `en.ini` and `xx.ini` and update outdated or non-existent entries in `xx.ini`.

3. Once you finished the translation work, submit a pull request or send us the file through an [Issue](https://github.com/MCCTeam/Minecraft-Console-Client/issues) in case you are not familiar with Git.

### Translate README.md

1. Get the English version of the README.md from [here](https://raw.githubusercontent.com/MCCTeam/Minecraft-Console-Client/master/README.md).

2. See `Add new translation -> 1.` for the target language code. Assume it is `xx`.

3. Complete the translation according to the English README.md and name the translated version as `README-xx.md`.

4. In the English README, above the "About" section, add the name of the language and a hyperlink to `README-xx.md`.

## Contributors

[Check out our contributors on Github](https://github.com/MCCTeam/Minecraft-Console-Client/graphs/contributors).
