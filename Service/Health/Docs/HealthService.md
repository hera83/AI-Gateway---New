# HealthService

## Formål
Selv-kontrol af applikationen: verificerer at processen kan eksekvere kode og svare, og at allokeret GC-hukommelse er under en tærskel (1 GB, defineret i `HealthService.MaxAllocatedBytes`).

## Forbrugere
`HealthChecks/SelfHealthCheck.cs` kalder `IHealthService.CheckSelf()` og oversætter resultatet til et `Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult`, som eksponeres via `GET /health` (`Controllers/HealthController.cs`).

## Udvidelse
Nye selv-tjek (disk space, thread pool starvation, osv.) tilføjes som yderligere metoder på `IHealthService`. Tjek af eksterne afhængigheder (database, cache, tredjeparts-API'er) bør have deres egen service under `Service/<Navn>/` og eget `IHealthCheck` i `HealthChecks/`, registreret i `Program.cs` via `AddHealthChecks().AddCheck<T>("navn")`.
