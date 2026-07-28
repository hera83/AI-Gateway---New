# LogService

## Formål
Giver mulighed for at søge/filtrere i de logs, som Serilog skriver til `App_dbs/AiGatewayLogs.db` (tabellen `Logs`, se `CLAUDE.md`s Logging-konvention). `LogService` ejer ikke tabellen — skemaet (`id`, `Timestamp`, `Level`, `Exception`, `RenderedMessage`, `Properties`) er defineret af `Serilog.Sinks.SQLite` selv, så der findes bevidst ingen EF Core-entitet eller migration for den. `LogService` læser med rå `Microsoft.Data.Sqlite`-kald i stedet for `AppDbContext`.

## Forbindelse
Forbindelsesstrengen (peger på samme fil som Serilog-sinket skriver til) bygges ét sted i `Program.cs` (`logDbPath`) og gives videre til `LogService` via en factory i DI-registreringen — der er bevidst ingen selvstændig sti-udregning i `LogService`, så filstien aldrig kan drifte i forhold til den sti, Serilog rent faktisk skriver til.

## Søgning
`SearchAsync` understøtter filtrering på `Level` (præcist match, ikke-versalfølsomt), fritekst-`Search` (case-insensitive `LIKE` mod både besked og exception), samt et `From`/`To`-tidsinterval. Tidsstempler i tabellen er gemt som tekst i formatet `yyyy-MM-ddTHH:mm:ss.fff` (UTC, jf. `storeTimestampInUtc: true` i `Program.cs`) — filtrering sker derfor som streng-sammenligning i samme format frem for datofunktioner, som SQLite ikke altid kan oversætte effektivt. Resultatet er sideopdelt (`Page`/`PageSize`), nyeste post først (`ORDER BY id DESC`).

## Forbrugere
`Controllers/LogsController.cs` eksponerer `ILogService` som `GET /Logs/Search`, kun tilgængeligt for `Administrator`-rollen (`[Authorize(Roles = "Administrator")]`) — logindhold (stacktraces, request-detaljer) kan være følsomt, så samme adgangsniveau som `KeysController`.

## Udvidelse
Nye filtre (f.eks. `SourceContext`, `RequestId` fra `Properties`-JSON'en) tilføjes som nye felter på `Service/Logs/Dtos/LogSearchDto.cs` + tilsvarende `WHERE`-betingelse i `LogService.SearchAsync`, samt et matchende felt på `Dto/Logs/LogSearchRequestDto.cs`. Undgå at parse hele `Properties`-JSON'en for hver række med mindre det bliver nødvendigt — det er i dag eksponeret rå, så forbrugeren selv kan parse det, den har brug for.
