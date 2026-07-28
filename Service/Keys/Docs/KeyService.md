# KeyService

## Formål
Livscyklus-styring af de API-nøgler (`X-Api-Key`), der bruges til at autentificere kald mod gatewayen. Nøgler oprettes, redigeres, roteres ("rollover"), aktiveres/deaktiveres, og enhver ændring logges i en audit-log. Data ligger i SQLite (`AiGateway.Data.AppDbContext`, `Data/Entities/ApiKey.cs` og `ApiKeyAuditLog.cs`), i databasefilen `App_dbs/AiGateway.db`.

## Master-nøglen
Der findes præcis én administrator-nøgle: `Auth:MasterApiKey` i `appsettings.json`. Den er den eneste nøgle med rollen `Administrator` og lever udelukkende i konfiguration — den har ingen række i `ApiKeys`-tabellen, kan ikke oprettes, redigeres, roteres eller ses via `KeyService`/`KeysController`, og skal roteres manuelt ved at ændre `appsettings.json`. Alle nøgler oprettet via `KeyService` får rollen `Standard` (se `AiGateway.Authentication.ApiKeyAuthenticationHandler`).

## Nøgle-hemmelighed
Selve nøgleværdien er en tilfældig 256-bit værdi med præfikset `agw_` (jf. GitHub/Stripe-stil, gør lækkede nøgler genkendelige). Den vises i klartekst udelukkende i svaret fra `POST /keys` (oprettelse) og `POST /keys/{id}/rollover` (rotation) — herefter gemmes kun en SHA-256-hash (`ApiKey.KeyHash`, unik indekseret). Nøglen kan altså ikke hentes frem igen; går den tabt, er eneste mulighed at rotere den. Hashing sker via `AiGateway.Data.ApiKeyHasher`, som bruges både her og i `ApiKeyAuthenticationHandler` ved login — hold dem synkroniseret, ellers holder ingen nøgler op med at virke.

## Forbrugere
`Controllers/KeysController.cs` eksponerer `IKeyService` som `/keys/*`, kun tilgængeligt for `Administrator`-rollen (`[Authorize(Roles = "Administrator")]`) — dvs. kun master-nøglen. `ApiKeyAuthenticationHandler` slår selv op direkte i `AppDbContext.ApiKeys` (ikke via `IKeyService`) for at validere indkommende kald og opdatere `LastUsedAt`.

## Metadata pr. nøgle
Hver nøgle bærer hvem den er udstedt til: `Name` (kaldenavn), `ResponsibleName` (ansvarlig person), `ContactInfo` (kontaktoplysninger, f.eks. e-mail), samt valgfri `ExpiresAt`. En udløbet eller deaktiveret (`IsActive = false`) nøgle afvises ved autentificering, men slettes aldrig — historik og audit-log bevares.

## Udvidelse
Nye handlinger (f.eks. mere granulære roller end Standard/Administrator, eller kvote/rate-limit pr. nøgle) tilføjes som nye felter på `ApiKey`-entiteten plus en ny EF Core-migration (`dotnet ef migrations add <navn>`), en metode på `IKeyService`/`KeyService`, og en tilsvarende action + DTO i `Dto/Keys/`/`KeysController`.
