using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class OllamaOptionsDto
{
    [JsonPropertyName("num_keep")]
    public int? NumKeep { get; set; }

    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    [JsonPropertyName("num_predict")]
    public int? NumPredict { get; set; }

    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }

    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    [JsonPropertyName("min_p")]
    public double? MinP { get; set; }

    [JsonPropertyName("typical_p")]
    public double? TypicalP { get; set; }

    [JsonPropertyName("repeat_last_n")]
    public int? RepeatLastN { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("repeat_penalty")]
    public double? RepeatPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    public double? PresencePenalty { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public double? FrequencyPenalty { get; set; }

    [JsonPropertyName("mirostat")]
    public int? Mirostat { get; set; }

    [JsonPropertyName("mirostat_tau")]
    public double? MirostatTau { get; set; }

    [JsonPropertyName("mirostat_eta")]
    public double? MirostatEta { get; set; }

    [JsonPropertyName("penalize_newline")]
    public bool? PenalizeNewline { get; set; }

    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }

    [JsonPropertyName("numa")]
    public bool? Numa { get; set; }

    [JsonPropertyName("num_ctx")]
    public int? NumCtx { get; set; }

    [JsonPropertyName("num_batch")]
    public int? NumBatch { get; set; }

    [JsonPropertyName("num_gpu")]
    public int? NumGpu { get; set; }

    [JsonPropertyName("main_gpu")]
    public int? MainGpu { get; set; }

    [JsonPropertyName("low_vram")]
    public bool? LowVram { get; set; }

    [JsonPropertyName("vocab_only")]
    public bool? VocabOnly { get; set; }

    [JsonPropertyName("use_mmap")]
    public bool? UseMmap { get; set; }

    [JsonPropertyName("use_mlock")]
    public bool? UseMlock { get; set; }

    [JsonPropertyName("num_thread")]
    public int? NumThread { get; set; }
}
