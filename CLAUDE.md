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
  hoor van Jaap of hij antwoord kreeg voordat je begint.
- Plan: teksten naar een tekstbestand (nl en en, menu Taal), handleiding vertalen, de updatefunctie
  (src/UpdateService.cs, ProjectUrl) naar deze repo laten wijzen, ondertekenen met Jaaps certificaat
  (zelfde afspraak als in Jaaps andere programma's: sleutel uit Dropbox /Certificaat Jaap Wortel, nooit op GitHub).
- Bouwminuten sparen: wijzigingen verzamelen en samen pushen.

## Idee: samenvoegen (Jaap, 19 nov 2026, nog NIET bouwen)

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
