# TextExtractionService

## Formål
Konverterer en uploadet fil (byte-stream) til ren tekst, så indholdet kan chunkes og embeddes af `KnowledgeBaseService`. Vælger extraction-strategi ud fra filendelsen: `.txt`/`.md` læses direkte, `.pdf` via `UglyToad.PdfPig`, `.docx` via `DocumentFormat.OpenXml`. Kaster `UnsupportedFileTypeException` (mappet til 400 i `GlobalExceptionHandler`) for alle andre filtyper.

## Forbrugere
`Service/KnowledgeBase/KnowledgeBaseService.cs` kalder `ITextExtractionService.ExtractTextAsync` som første trin i dokument-upload-flowet, før chunking og embedding.

## Udvidelse
Nye filtyper tilføjes som en ny `case`/handler i `TextExtractionService.ExtractTextAsync` — ingen ændringer nødvendige i `KnowledgeBaseService`, som er ligeglad med hvordan teksten blev udtrukket. Husk at opdatere fejlbeskeden i `UnsupportedFileTypeException` og valideringen af tilladte filtyper i `KnowledgeBaseController.UploadDocument`, hvis den understøttede liste udvides.
