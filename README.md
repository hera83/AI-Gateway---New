# AiGateway

AiGateway er en .NET 10 controller-baseret Web API, der fungerer som en samlet, nøgle-styret gateway foran interne AI-tjenester — i dag [Ollama](https://ollama.com/) (LLM-inferens) og [Speaches](https://speaches.ai/) (tale-til-tekst/tekst-til-tale) — plus en indbygget knowledge base til Retrieval-Augmented Generation (RAG). Formålet er at give ét sikkert, ensartet indgangspunkt (med API-nøgler, audit-log og søgbare logs) i stedet for at eksponere de underliggende tjenester direkte.

## Funktioner

| Område | Beskrivelse |
|---|---|
| **Health** | `GET /health` — selv-tjek af processen (kører/svarer, hukommelsesforbrug under grænse). Kræver ingen API-nøgle, beregnet til load balancers/uptime-monitorering. |
| **API-nøgler** (`/keys`) | Fuld livscyklus for API-nøgler: oprettelse, redigering, rotation ("rollover"), aktivering/deaktivering samt en audit-log pr. nøgle. Kun tilgængeligt for administrator-nøglen. |
| **Ollama-gateway** (`/ollama`) | Videresender hele Ollama-API-fladen: generate, chat, embeddings, model-administration (list, show, copy, delete, pull, push, create), version m.m. |
| **Speaches-gateway** (`/speaches`) | Videresender Speaches' OpenAI-kompatible API: chat completions, transskribering, oversættelse, tale-syntese, speaker-embedding, VAD-timestamps, diarization samt model-administration. |
| **Knowledge Base / RAG** (`/knowledge-base`) | Hver API-nøgle har sin egen private vidensbank: navngivne grupper, upload af dokumenter (txt/md/pdf/docx) der tekst-udtrækkes, chunkes og embeddes, samt søgning og et samlet RAG-chat-endpoint. Data fra forskellige nøgler er fuldstændig isoleret fra hinanden. |
| **Logs** (`/Logs/Search`) | Søgning/filtrering (niveau, fritekst, tidsinterval, paginering) i applikationens egne logs. Administrator-only, da logindhold kan indeholde følsomme detaljer. |
| **Swagger UI** | Fuld, interaktiv API-dokumentation på `/swagger`, inkl. "Authorize"-knap til `X-Api-Key`. Tilgængelig i alle miljøer (også produktion) — et diskret farvet badge viser altid hvilket miljø (`ASPNETCORE_ENVIRONMENT`) du kigger på. |

## Arkitektur i korte træk

- **.NET 10**, ASP.NET Core MVC-controllere (ikke Minimal API) — én controller pr. ressource under `Controllers/`.
- **Autentificering**: en enkelt custom `X-Api-Key`-header. Der findes præcis én administrator-nøgle (`Auth:MasterApiKey` i konfiguration); alle øvrige nøgler oprettes via `/keys` og har rollen `Standard`.
- **Data**: SQLite via EF Core. Tre separate databasefiler under `App_dbs/`:
  - `AiGateway.db` — API-nøgler og knowledge base-metadata.
  - `AiGatewayVectors.db` — embeddings til RAG-søgning (via `sqlite-vec`), holdt adskilt så tung vektor-trafik ikke går ud over resten af systemet.
  - `AiGatewayLogs.db` — applikationens logs (skrevet af Serilog).
- **Filer**: originale knowledge base-uploads gemmes som almindelige filer under `App_files/<GruppeId>/<DokumentId>` — ikke i databasen.
- **Logging**: Serilog til både konsol og SQLite, søgbart via `/Logs/Search`.

Se `CLAUDE.md` og de enkelte `Service/<Navn>/Docs/*.md`-filer for en langt mere detaljeret gennemgang af arkitekturen.

## Kom i gang med Docker (anbefalet)

Dette er den hurtigste vej til en kørende instans. Systemet består af containeren selv plus to volumener (`App_dbs/`, `App_files/`) der ligger som mapper i projektroden, så data overlever genstart/genbygning af containeren.

### 1. Forudsætninger

Installer Docker Desktop (Windows/Mac) eller Docker Engine + Compose-plugin (Linux) — begge inkluderer `docker compose`-kommandoen som bruges herunder.

- **Windows/Mac**: hent og installer [Docker Desktop](https://www.docker.com/products/docker-desktop/), start det, og vent til det viser "Docker Desktop is running".
- **Linux**: følg Dockers officielle vejledning til [Install Docker Engine](https://docs.docker.com/engine/install/) for din distribution — den installerer også `docker compose`.

Verificér installationen:

```bash
docker --version
docker compose version
```

### 2. Hent projektet

```bash
git clone <repo-url>
cd "AI Gateway - New"
```

### 3. Opret `.env`-filen

Projektet styres via miljøvariabler i en `.env`-fil, som **ikke** er en del af Git (den indeholder hemmeligheder). Kopiér skabelonen og udfyld den:

```bash
cp .env.example .env
```

Åbn `.env` og udfyld:

| Variabel | Standardværdi | Skal ændres? | Beskrivelse |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Nej, medmindre du kører mod et testmiljø | Styrer bl.a. farven på Swagger UI's miljø-badge. |
| `HOST_PORT` | `8080` | Nej, medmindre porten er optaget på din maskine | Den port containeren eksponeres på (containeren lytter altid internt på 8080). |
| `AUTH_MASTER_API_KEY` | *(placeholder)* | **Ja, altid** | Den ene administrator-nøgle til hele API'et. Appen fejler bevidst ved opstart uden en værdi her. Generér en stærk, tilfældig værdi, fx: `openssl rand -hex 32`. |
| `OLLAMA_BASE_URL` | `http://host.docker.internal:11434` | Ja, hvis Ollama ikke kører på samme maskine som containeren | Adressen på din Ollama-instans. `host.docker.internal` peger på Docker-værtsmaskinen selv. |
| `SPEACHES_BASE_URL` | `http://host.docker.internal:8000` | Ja, hvis Speaches ikke kører på samme maskine som containeren | Adressen på din Speaches-instans. |

### 4. Byg og start

```bash
docker compose up --build -d
```

Dette bygger imaget (multi-stage build, `.NET 10 SDK` → `.NET 10 ASP.NET runtime`), opretter `App_dbs/` og `App_files/` i projektroden hvis de ikke findes, kører EF Core-migrationerne automatisk ved opstart, og starter containeren i baggrunden.

### 5. Tjek at det virker

- Swagger UI: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- Health-endpoint (kræver ingen nøgle): `GET http://localhost:8080/health`
- Klik "Authorize" i Swagger og indsæt din `AUTH_MASTER_API_KEY` for at prøve de beskyttede endpoints (fx opret en almindelig API-nøgle via `POST /keys`).

### 6. Almindelige kommandoer

```bash
docker compose logs -f          # følg logs live
docker compose down             # stop og fjern containeren (data i App_dbs/ og App_files/ bevares)
docker compose up --build -d    # genbyg og genstart efter kodeændringer
```

### Data og persistens

`App_dbs/` (SQLite-databaser) og `App_files/` (originale dokument-uploads) er bind-mountet ind i containeren fra projektroden. De overlever altså `docker compose down`/`up`/genbygning — kun `docker compose down -v` eller manuel sletning af mapperne fjerner data. Begge mapper er tilføjet til `.gitignore` og bliver derfor aldrig committet.

## Lokal udvikling uden Docker

Kræver [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet restore
dotnet run --launch-profile http    # http://localhost:5207/swagger
```

Lokalt bruges `appsettings.json`/`appsettings.Development.json` i stedet for `.env` — se `CLAUDE.md` for detaljer om konfiguration, migrationer og de øvrige `dotnet`-kommandoer.

## Yderligere dokumentation

- `CLAUDE.md` — fuld arkitektur-, konventions- og kommandooversigt.
- `Service/<Navn>/Docs/*.md` — én dybdegående beskrivelse pr. service (Health, Keys, Ollama, Speaches, KnowledgeBase, Logs, TextExtraction).
- `AiGateway.http` — manuel scratch-fil med eksempel-requests til alle endpoints.
