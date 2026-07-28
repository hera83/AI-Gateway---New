# SpeachesService

## Formål
Gateway/pass-through til en lokal eller ekstern [Speaches](https://speaches.ai/)-instans (tidligere `faster-whisper-server`) — en OpenAI-kompatibel speech-to-text/text-to-speech-server. Den oprindelige OpenAPI-definition ligger i `Docs/openapi.json`. Dækker: chat completions, transskribering, oversættelse, modeller/registry/ps (indlæste modeller), tale-syntese, speaker-embedding, VAD-timestamps og diarization.

`SpeachesService` er en typed `HttpClient`-service (`builder.Services.AddHttpClient<ISpeachesService, SpeachesService>()` i `Program.cs`) hvis `BaseAddress` sættes fra konfigurationsnøglen `Speaches:BaseUrl` (default `http://localhost:8000` i `appsettings.json` — opdatér til din faktiske Speaches-instans, ligesom `Ollama:BaseUrl`).

## Forbrugere
`Controllers/SpeachesController.cs` eksponerer alle metoder på `ISpeachesService` som `/speaches/*`-endpoints og mapper mellem controllerens egne DTO'er (`Dto/Speaches/`) og servicens wire-format DTO'er (`Service/Speaches/Dtos/`), efter samme mønster som `OllamaController`/`Dto/Ollama`.

## DTO-lag
To sæt DTO'er for samme data, jf. den generelle Service-konvention:
- `Service/Speaches/Dtos/` — spejler Speaches' egne JSON-felter 1:1 via `[JsonPropertyName]` (snake_case).
- `Dto/Speaches/` — controllerens egne PascalCase-DTO'er (camelCase JSON ud til klienter af gatewayen).

`SpeachesController` indeholder de statiske `ToService(...)`/`ToApi(...)`-mapperne mellem de to lag.

## Filupload
`transcriptions`, `translations`, `speech/embedding`, `speech/timestamps` og `diarization` uploader en lydfil. Controlleren binder disse via `[FromForm]` til en request-DTO med en `IFormFile File`-property (Swagger UI viser dem som fil-upload-felter). De service-lags request-DTO'er for disse endpoints (`TranscriptionRequestDto`, `TranslationRequestDto`, `SpeechEmbeddingRequestDto`, `SpeechTimestampsRequestDto`, `DiarizationRequestDto`) er bevidst *ikke* `[JsonPropertyName]`-annoterede JSON-wire-DTO'er som resten af laget — de holder blot `Stream File`/`FileName`/`ContentType` plus de øvrige felter, og `SpeachesService` bygger selv `MultipartFormDataContent` ud fra dem (feltnavnene sættes direkte i service-koden, jf. Speaches' `application/x-www-form-urlencoded`-dokumenterede men reelt multipart-baserede endpoints).

## Binært lyd-svar
`POST /speaches/audio/speech` (tale-syntese) returnerer rå lydbytes (mp3/wav/flac/opus/aac/pcm afhængig af `responseFormat`), ikke JSON. Dette er den ene bevidste undtagelse fra DTO-konventionens "alt output skal have en `*ResponseDto`" — `SpeachesController.SynthesizeSpeech` returnerer et `FileResult` (`File(bytes, contentType)`) med content-type videresendt fra Speaches, som er standard ASP.NET Core-praksis for binære svar.

## Forenklinger
- `chat/completions`: Speaches' OpenAPI-skema for denne endpoint er hele OpenAI's `chat.completions`-skema (30+ besked-/tool-/content-part-typer). For at holde kontrakten overskuelig er `content`, `tool_calls`, `tools`, `tool_choice`, `response_format` og `logprobs`/`stop` typet som rå `JsonElement`, ligesom `format`/tool-`parameters` i `OllamaService` — samme forenklingsprincip.
- `transcriptions`/`translations`: `response_format` eksponeres ikke i vores API — servicen sætter altid `verbose_json`, så svaret altid har det faste `TranscriptionResultDto`/`TranslationResultDto`-shape (i stedet for Speaches' `anyOf`-svar mellem ren tekst/`json`/`verbose_json`/`srt`/`vtt`). `stream` sættes altid til `false`.
- `diarization`: `response_format` sættes altid til `json` (ikke `rttm`-tekstformatet), af samme grund.
- `audio/speech`: `stream_format` sættes altid til `audio` (ikke `sse`), da gatewayen ikke understøtter server-sent events ud til klienten.
- `usage`-felter på transskriberings-/oversættelsessvar varierer i shape (token-baseret vs. varighed-baseret) og er ikke medtaget i vores forenklede `TranscriptionResultDto`/`TranslationResultDto` — brug `segments`/`words` i stedet, som har et fast shape.
- Modeller identificeres med Hugging Face-repo-id'er der kan indeholde `/` (f.eks. `Systran/faster-distil-whisper-large-v3`). `{modelId}`-routes videresender denne værdi uændret til Speaches uden URL-encoding af `/`.
- Fejl fra Speaches (ikke-2xx) kastes videre som `HttpRequestException` med status og response-body i beskeden, som i `OllamaService`.

## Ikke understøttet
- `GET /health` (Speaches' eget health-ping) er ikke eksponeret, i tråd med at `OllamaService` heller ikke eksponerer en tilsvarende ping/health-endpoint for Ollama.
- `POST /v1/realtime` (WebRTC-baseret realtids-taleforhandling via SDP-offer/-answer) er ikke implementeret — det er ikke en almindelig JSON-request/response-udveksling, og OpenAPI-dokumentationen for endpointet beskriver ikke selve SDP-content-typen. Kan tilføjes separat hvis realtids-voice-chat bliver nødvendigt.

## Udvidelse
Nye Speaches-endpoints tilføjes ved at: 1) lægge wire-format-DTO'er i `Service/Speaches/Dtos/`, 2) tilføje metoden på `ISpeachesService`/`SpeachesService`, 3) lægge tilsvarende controller-DTO'er i `Dto/Speaches/`, og 4) tilføje en action + mapping i `SpeachesController`.
