Minecraft Console Client
========================

[![GitHub Actions build status](https://github.com/MCCTeam/Minecraft-Console-Client/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest)

Minecraft Console Client (MCC) is een simpele app waarmee je Minecraft servers kan joinen, command sturen en berichten ontvangen op een snelle en makkelijke manier zonder Minecraft echt hoeven te openen. Het biedt ook verschillende geautomatiseerde functies die je kan aanzetten voor administratie en andere dingen.

## Download 🔽

Verkrijg [hier](https://github.com/MCCTeam/Minecraft-Console-Client/releases/latest) de laatste ontwikkelde binaire versie.
De exe file is een .NET binary die ook werkt op Mac en Linux.

## Hoe Te Gebruiken  📚

Bekijk [hier](MinecraftClient/config/) een voorbeeld van de configuratie bestand, deze bevat uitleg om het te gebruiken. Daarnaats kan je de [README](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#minecraft-console-client-user-manual) lezen.

## Hulp Krijgen 🙋

Bekijk de [README](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/config#minecraft-console-client-user-manual) en de bestaande [discussies](https://github.com/MCCTeam/Minecraft-Console-Client/discussions): Misschien is je vraag daar al beantwoord. Als dat niet zo is open dan een [nieuwe discussie](https://github.com/MCCTeam/Minecraft-Console-Client/discussions/new) en stel je vraag. Als je een bug vind, rapporteer dat bij de [problemen](https://github.com/MCCTeam/Minecraft-Console-Client/issues) sectie.

## Steentje bijdragen ❤️
 
We zijn een kleine community, dus we hebben hulp nodig bij het implementeren van verbeteringen voor de nieuwe Minecraft-versies, het oplossen van bugs en het uitbreiden van het project. We zijn altijd op zoek naar gemotiveerde mensen om een steentje bij te dragen. Als je het gevoel hebt dat jij de persoon bent die wij zoeken, kijk dan eens naar de [problemen](https://github.com/MCCTeam/Minecraft-Console-Client/issues?q=is%3Aissue+is%3Aopen+label%3Awaiting-for%3Acontributor) sectie :)

## Hoe draag je bij 📝

Als je graag mee wilt helpen aan Minecraft Console Client, geweldig! Fork de repository en dien een pull request in op de *Master* branch. MCC gebruikt de *master* branch voor de stabiel versies (dus we gebruiken de *Indev* branch niet meer).

## Minecraft Console Client Vertalen 🌍

Als je de Minecraft Console Client wilt vertalen naar een andere taal, download dan het vertalings bestand via [de talen folder](https://github.com/MCCTeam/Minecraft-Console-Client/tree/master/MinecraftClient/Resources/lang) of fork de repository. Nadat je klaar bent met de vertaling dien je de vertaling in of stuur je ons het bestand door d.m.v. een [probleem](https://github.com/MCCTeam/Minecraft-Console-Client/issues) (voorals je nieuw bent met Git(Hub)).

Als je het vertaalde taalbestand wilt gebruiken, plaats je het onder `lang/mcc/` map en stel je de taal in `.ini` config. Je kunt de map maken als deze niet bestaat.

Voor de namen van het vertaalbestand, zie [dit bericht](https://github.com/MCCTeam/Minecraft-Console-Client/pull/1282#issuecomment-711150715).

## Eigen versie bouwen 🏗️

_De aanbevolen ontwikkelomgeving is [Visual Studio](https://visualstudio.microsoft.com/). Mocht je het project willen bouwen zonder een ontwikkelomgeving te installeren, dan kun je de volgende instructies volgen:_

Download allereerst de .NET 6.0 SDK [here](https://dotnet.microsoft.com/en-us/download) en volg de installatie-instructies.

Download de [zip met alle code](https://github.com/MCCTeam/Minecraft-Console-Client/archive/master.zip), pak deze uit en navigeer naar de `MinecraftClient` map.

### Op Windows 🪟

1. Open een Commandprompt / Opdrachtprompt.
2. Typ in `dotnet publish --no-self-contained -r win-x64 -c Release`.
3. Als de build slaagt, kunt je deze als `MinecraftClient.exe` in de folder pad `MinecraftClient\bin\Release\net6.0\win-x64\publish\`.

### Op Linux 🐧

1. Open een Commandprompt / Opdrachtprompt.
2. Typ in `dotnet publish --no-self-contained -r linux-x64 -c Release`.
3. Als de build slaagt, kunt je deze als `MinecraftClient` in de folder pad `MinecraftClient\bin\Release\net6.0\linux-x64\publish\`.

### Op Mac 🍎

1. Open een Commandprompt / Opdrachtprompt.
2. Typ in `dotnet publish --no-self-contained -r osx-x64 -c Release`.
3. Als de build slaagt, kunt je deze als `MinecraftClient` in de folder pad `MinecraftClient\bin\Release\net6.0\osx-x64\publish\`.

## Licentie ⚖️

Tenzij anders aangegeven, is de code van hete MCC Team of vrijwilligers, en beschikbaar onder CDDL-1.0. De licentie en orginele autheur zijn te vinden in de kopteksten van de bronnen te vinden.
De belangrijkste punten van de CDDL-1.0 licentie zijn als volgt:

- Je mag de gelicentieerde code in zijn geheel of gedeeltelijk gebruiken in je eigen programma's, ongeacht de licentie van het programma als geheel (of beter gezegd, als exclusief de code die u leent). Het programma zelf kan open of closed source, gratis of commercieel zijn.
- In alle gevallen echter, eventuele wijzigingen, verbeteringen of toevoegingen aan de CDDL-code (elke code waarnaar wordt verwezen in directe wijzigingen aan de CDDL-code wordt beschouwd als een toevoeging aan de CDDL-code en is dus gebonden aan deze vereiste; bijvoorbeeld een wijziging van een wiskundige functie om een snelle opzoektabel te gebruiken, maakt die tabel zelf een toevoeging aan de CDDL-code,  ongeacht of het in een eigen broncodebestand staat) moet openbaar en vrij beschikbaar worden gesteld in de bron, onder de CDDL-licentie zelf.
- In elk programma (bron of binair) dat CDDL-code gebruikt, moet herkenning worden gegeven aan de bron (project of auteur) van de CDDL-code. Ook mogen wijzigingen in de CDDL-code (die als bron moet worden verspreid) geen kennisgevingen verwijderen die de afkomst van de code aangeven.

Meer info op http://qstuff.blogspot.fr/2007/04/why-cddl.html
Volledige licentie op http://opensource.org/licenses/CDDL-1.0
