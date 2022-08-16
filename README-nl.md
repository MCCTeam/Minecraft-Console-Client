Minecraft Console Client
========================

[![GitHub Actions build status](https://github.com/MCCTeam/Minecraft-Console-Client/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest)

Minecraft Console Client (MCC) is een simpele app waarmee je Minecraft servers kan joinen, command sturen en berichten ontvangen op een snelle en makkelijke manier zonder Minecraft echt hoeven te openen. Het biedt ook verschillende geautomatiseerde functies die je kan aanzetten voor administratie en andere dingen.

## Download 🔽

Verkrijg hier de laatste ontwikkelde binaire versie. [development build](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest).
De exe file is een .NET binary die ook werkt op Mac en Linux.

## Hoe Te Gebruiken  📚

Bekijk de [sample configuration files](MinecraftClient/config/) die de how-to-use bevat [README](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#minecraft-console-client-user-manual).

## Hulp Krijgen 🙋

Bekijk de [README](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#minecraft-console-client-user-manual) en de bestaande [Discussions](https://github.com/MCCTeam/Minecraft-Console-Client/discussions): Misschien is je vraag daar al beantwoord. Als dat niet zo is open dan een [New Discussion](https://github.com/MCCTeam/Minecraft-Console-Client/discussions/new) en stel je vraag. Als je een bug vind, raporteer dat in de [Issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues) sectie.

## Steentje bijdragen ❤️
 
We zijn een kleine community, dus we hebben hulp nodig bij het implementeren van upgrades voor nieuwe Minecraft-versies, het oplossen van bugs en het uitbreiden van het project. We zijn altijd op zoek naar gemotiveerde mensen om bij te dragen. Als je het gevoel hebt dat jij het zou kunnen zijn, kijk dan eens naar de [issues](https://github.com/MCCTeam/Minecraft-Console-Client/issues?q=is%3Aissue+is%3Aopen+label%3Awaiting-for%3Acontributor) sectie :)

## Hoe draag je bij 📝

Als je graag mee wilt helpen aan Minecraft Console Client, geweldig! Fork de repository en dien een pull request in op de *Master* branch. MCC is op dit moment alleen uitgegeven in developemnt builds (die normaal gesproken stabiel zijn) dus we gebruiken de *Indev* branch niet meer.

## Minecraft Console Client Vertalen 🌍

Als je de Minecraft Console Client wilt vertalen naar een andere taal, download dan het vertalings bestand via [the lang folder](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/Resources/lang) of fork de repository. Nadat je klaar bent met de vertaling, dien dan de vertaling in of stuur ons het bestand door [Issue](https://github.com/MCCTeam/Minecraft-Console-Client/issues) in het geval dat je niet bekend bent met Git.

Als u het vertaalde taalbestand wilt gebruiken, plaatst u het onder `lang/mcc/` map en stel uw taal in `.ini` config. U kunt de map maken als deze niet bestaat.

Voor de namen van het vertaalbestand, zie [this comment](https://github.com/MCCTeam/Minecraft-Console-Client/pull/1282#issuecomment-711150715).

## Building from source 🏗️

_De aanbevolen ontwikkelomgeving is [Visual Studio](https://visualstudio.microsoft.com/). Als u het project wilt bouwen zonder een ontwikkelomgeving te installeren, kunt u ook deze instructies volgen:_

Download allereerst de .NET 6.0 SDK [here](https://dotnet.microsoft.com/en-us/download) en volg de installatie-instructies.

Bemachtig een [zip of source code](https://github.com/MCCTeam/Minecraft-Console-Client/archive/master.zip), pak het uit en navigeer naar de `MinecraftClient` map.

### Met Windows 🪟

1. Open een Terminal / Opdrachtprompt.
2. Typ in `dotnet publish --no-self-contained -r win-x64 -c Release`.
3. Als de build slaagt, kunt u vinden `MinecraftClient.exe` onder `MinecraftClient\bin\Release\net6.0\win-x64\publish\`

### Met Linux 🐧

1. Open een Terminal / Opdrachtprompt.
2. Typ in `dotnet publish --no-self-contained -r linux-x64 -c Release`.
3. Als de build slaagt, kunt u vinden `MinecraftClient` onder `MinecraftClient\bin\Release\net6.0\linux-x64\publish\`

### Met Mac 🍎

1. Open een Terminal / Opdrachtprompt.
2. Typ in `dotnet publish --no-self-contained -r osx-x64 -c Release`.
3. Als de build slaagt, kunt u vinden `MinecraftClient` onder `MinecraftClient\bin\Release\net6.0\osx-x64\publish\`

## License ⚖️

Unless specifically stated, the code is from the MCC Team or Contributors, and available under CDDL-1.0. Else, the license and original author are mentioned in source file headers.
The main terms of the CDDL-1.0 license are basically the following:

- U mag de gelicentieerde code geheel of gedeeltelijk gebruiken in elk programma dat u wenst, ongeacht de licentie van het programma als geheel (of beter gezegd, als exclusief de code die u leent). Het programma zelf kan open of closed source, gratis of commercieel zijn.
- In alle gevallen echter, eventuele wijzigingen, verbeteringen of toevoegingen aan de CDDL-code (elke code waarnaar wordt verwezen in directe wijzigingen aan de CDDL-code wordt beschouwd als een toevoeging aan de CDDL-code en is dus gebonden aan deze vereiste; bijvoorbeeld een wijziging van een wiskundige functie om een snelle opzoektabel te gebruiken, maakt die tabel zelf een toevoeging aan de CDDL-code,  ongeacht of het in een eigen broncodebestand staat) moet openbaar en vrij beschikbaar worden gesteld in de bron, onder de CDDL-licentie zelf.
- In elk programma (bron of binair) dat CDDL-code gebruikt, moet herkenning worden gegeven aan de bron (project of auteur) van de CDDL-code. Ook mogen wijzigingen in de CDDL-code (die als bron moet worden verspreid) geen kennisgevingen verwijderen die de afkomst van de code aangeven.

Meer info op http://qstuff.blogspot.fr/2007/04/why-cddl.html
Volledige licentie op http://opensource.org/licenses/CDDL-1.0
