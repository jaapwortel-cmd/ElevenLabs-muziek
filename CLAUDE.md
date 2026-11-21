# ElevenLabs Muziek (Nederlandse versie)

Nederlandse versie van "ElevenLabs Music and Sound FX Generator" van Andre Louis (MIT, naam en licentie laten staan).
Broncode van https://github.com/OnjLouis/ElevenLabsMusicGenerator met zijn hele geschiedenis (opgehaald 19 nov 2026,
commit fab10f3). Zie LEES-MIJ.txt voor het plan.

## Met Jaap praten

- Nederlands, eenvoudige taal, met "je" en "jij" (geen "u").
- Jaap is blind en werkt met een schermlezer (NVDA, JAWS): korte uitleg, in duidelijke stappen.
- Jaap schrijft zelf geen code.

## Stand van zaken

- 19 nov 2026: alleen Andres code neergezet, nog niets vertaald. Jaap heeft Andre op 19 nov via een GitHub-issue
  ("Request: Dutch language support" in OnjLouis/ElevenLabsMusicGenerator) gevraagd of hij zelf Nederlands wil toevoegen;
  Jaap biedt de vertaling en het testen aan;
  Andre antwoordde (19 nov): hij maakt GEEN Nederlandse versie en geen menu Taal (te veel onderhoud), maar Nederlandse
  liedteksten werken al (alleen de stijlbeschrijving moet in het Engels). Dus: wij maken de Nederlandse versie zelf.
- Plan: teksten naar een tekstbestand (nl en en, menu Taal), handleiding vertalen, de updatefunctie
  (src/UpdateService.cs, ProjectUrl) naar deze repo laten wijzen, ondertekenen met Jaaps certificaat
  (zelfde afspraak als in Jaaps andere programma's: sleutel uit Dropbox /Certificaat Jaap Wortel, nooit op GitHub).
- Bouwminuten sparen: wijzigingen verzamelen en samen pushen.

## Samenvoegen met de Media Suite (Jaap, 19 nov 2026)

- Jaap wil de functies van "ElevenLabs Media Suite" (https://github.com/audioses/elevenlabs-media-suite, MIT,
  "ElevenLabs Media Suite contributors") in dit programma samenvoegen. NIET in Python: alles in C# (.NET 4.8, WinForms)
  zoals de rest van dit programma. Alleen de ideeën en functies overnemen, geen Python-code.
- Functies uit de Media Suite die hier nog missen: tekst naar spraak, gesprek met meerdere stemmen, stemmen zoeken,
  stem ontwerpen, stem klonen, stem omzetten, achtergrondgeluid weghalen, spraak naar tekst, nasynchroniseren,
  tekst uitlijnen op een opname, uitspraaklijsten, geschiedenis.
- Handige ideeën uit de Media Suite: één keuzelijst (Alt+F) die het paneel wisselt in plaats van tabbladen, focus springt
  nooit vanzelf, lange klussen op de achtergrond, bestandsnamen als "regen_op_dak-001-a1b2c3.mp3" met een .json ernaast
  (prompt, model, instellingen), API-sleutel veilig bewaard.
- Tip: Andres "ElevenLabs Speech Generator" (https://github.com/OnjLouis/ElevenLabsSpeechGenerator, MIT, ook C# .NET 4.8)
  heeft de meeste van deze functies al in C#. Die code is waarschijnlijk makkelijker over te nemen dan de Media Suite
  na te bouwen. Naam en licentie van alle makers laten staan.

- 19 nov 2026: Jaap koos C# (niet Rust, niet Python). Het API-deel van de Media Suite is omgezet naar C# in
  src/MediaSuite (namespace ElevenLabsMusicGenerator.MediaSuite), zie src/MediaSuite/LEES-MIJ.txt. Bouwt zonder fouten
  met Andres code (net48, C# 5); niet getest met een echte sleutel. Wordt nog niet meegebouwd (Build.ps1 leest alleen src\*.cs).
  Volgende stap: de vensters in WinForms. Lokaal bouwen: dotnet-sdk-8.0 + NuGet Microsoft.NETFramework.ReferenceAssemblies.net48
  1.0.3, GenerateAssemblyInfo=false, alle src/*.cs + NAudio.dll + System.Web.Extensions e.d. (zie Build.ps1).

## Samengevoegd en twee talen (21 nov 2026, versie 1.5.1.1, PROEF 1)

- Jaap: "voeg de twee programma's samen en maak het volledig Nederlands", daarna: "de Engelse versie moet blijven, Nederlands
  is de eerste taal", en "met toegankelijkheid en schermlezers".
- Talen: `src/Taal.cs` (klasse `L`): elke tekst staat als paar in de code, `L.T("Nederlands", "English")`. Geen tekstbestand:
  zo kan een vertaling niet vergeten worden. Keuze in `User\Taal.txt` (nl/en, standaard nl), menu Opties, Taal (Language);
  het programma start opnieuw (`--herstart` wacht op de mutex). Nieuwe teksten ALTIJD als L.T-paar.
  Let op: L.T mag niet in const, standaardwaarde van een parameter, case of attribuut.
- Keuzes die bewaard worden (update-frequentie, aansluiting high/medium/low) gaan op volgorde, niet op de tekst.
- Mapnamen voor spraak blijven Nederlands in beide talen (Spraak, Gesprekken, Stem omzetten, Schoon geluid,
  Stemvoorbeelden, Nagesynchroniseerd, Geschiedenis, Teksten; `AssetNaming.Categories`).
- Sneltoetsen hoofdvenster: Nederlands Alt+T tegoed, Alt+C statusberichten, Alt+S menu Spraak en stemmen;
  Engels houdt Andres Alt+B en Alt+S, menu "Spee&ch and voices".
- `src/SpraakVenster.cs`: venster Spraak en stemmen (Ctrl+T, menu Spraak en stemmen) met de 12 functies van de Media Suite,
  keuzelijst Functie (Alt+F), Meldingen (Alt+L) met UIA-melding (RaiseAutomationNotification), klussen via `JobQueue`.
  `MediaSuiteClient.AddSharedVoice` (POST v1/voices/add/{public_owner_id}/{voice_id}) is erbij gekomen.
- `src/Controle.cs`: `--controle <rapport>` bouwt alle vensters in nl en en en meldt onderdelen zonder naam en dubbele
  Alt-letters (ook in menu's). Draait in de bouwronde en lokaal met Mono + Xvfb (Voorkeuren alleen op Windows, winmm).
- Handleidingen: `Handleiding.html` (nl, F1 in het Nederlands) en `Manual.html` (en). Beide in de zip en in ProgramFiles.
- `tests/TestHarness.cs` (Andre) draait in het Engels (`L.ZetVoorControle(true)`); in de bouwronde ter informatie.
- Bouwronde `.github/workflows/build.yml` (Windows): Build.ps1, controle, Andres proeven, proefstart,
  tools/Toegankelijkheid.ps1, zip naar `exe-builds` ("ElevenLabs Muziek en Spraak <versie> PROEF n.zip").
- Lokaal bouwen: dotnet build van een eigen csproj in de scratchpad (net48, LangVersion 5, src/**/*.cs, NAudio als resource).
- Updates: `UpdateService.ProjectUrl` wijst naar deze repo; er zijn nog geen releases met handtekening, dus "geen updates".
  Updates via Dropbox komen later.
- Mutex nu "JaapWortel.ElevenLabsMuziekEnSpraak" (mag naast Andres Engelse programma draaien).
