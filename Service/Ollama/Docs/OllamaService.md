# OllamaService

## Formål
Gateway/pass-through til en lokal eller ekstern Ollama-instans' REST API (se `Docs/Ollama REST API.postman_collection.json` for den oprindelige collection). Dækker hele Ollama API-fladen: `generate`, `chat`, `embed`, list af lokale modeller (`tags`), list af kørende modeller (`ps`), `show`, `copy`, `delete`, `pull`, `push`, `create` og `version`.

`OllamaService` er en typed `HttpClient`-service (`builder.Services.AddHttpClient<IOllamaService, OllamaService>()` i `Program.cs`) hvis `BaseAddress` sættes fra konfigurationsnøglen `Ollama:BaseUrl` (default `http://localhost:11434` i `appsettings.json`).

## Forbrugere
`Controllers/OllamaController.cs` eksponerer alle metoder på `IOllamaService` som `/ollama/*`-endpoints og mapper mellem controllerens egne DTO'er (`Dto/Ollama/`) og servicens wire-format DTO'er (`Service/Ollama/Dtos/`).

## Streaming
Ollama's `generate`, `chat`, `pull`, `push` og `create` endpoints kan stream'e som newline-delimited JSON. Da controller-DTO-konventionen kræver et fast, typed response-shape, sætter `OllamaService` altid `"stream": false` på disse kald, uanset hvad kalderen sender — der returneres ét samlet JSON-svar. Understøttelse af rigtig streaming (f.eks. via `IAsyncEnumerable`/SSE ud til klienten) er ikke implementeret og kan tilføjes som en separat metode/endpoint hvis det bliver nødvendigt.

## DTO-lag
Der er bevidst to sæt DTO'er for samme data, jf. den generelle Service-konvention:
- `Service/Ollama/Dtos/` — spejler Ollama's egne JSON-felter 1:1 via `[JsonPropertyName]` (snake_case), og bruges direkte til (de)serialisering mod Ollama's HTTP-API.
- `Dto/Ollama/` — controllerens egne PascalCase-DTO'er (serialiseres til camelCase JSON ud til klienter af gatewayen, som resten af API'et).

`OllamaController` indeholder de statiske `ToService(...)`/`ToApi(...)`-mapperne mellem de to lag.

## Forenklinger
- `embed`: `input` accepteres kun som en liste af strenge i vores API (ikke Ollama's alternative enkelt-streng-form) for at holde kontrakten simpel.
- `format` (structured output) og tool-`parameters` (JSON schema) er typet som rå `JsonElement`, da deres skema er fritstående JSON og varierer fra kald til kald.
- `show`-svarets `model_info` er typet som `Dictionary<string, JsonElement>`, da Ollama returnerer et varierende sæt nøgler pr. modelfamilie.
- Fejl fra Ollama (ikke-2xx) kastes videre som `HttpRequestException` med status og response-body i beskeden; der er ikke tilføjet en global exception-mapper endnu.

## Udvidelse
Nye Ollama-endpoints tilføjes ved at: 1) lægge wire-format-DTO'er i `Service/Ollama/Dtos/`, 2) tilføje metoden på `IOllamaService`/`OllamaService`, 3) lægge tilsvarende controller-DTO'er i `Dto/Ollama/`, og 4) tilføje en action + mapping i `OllamaController`.
