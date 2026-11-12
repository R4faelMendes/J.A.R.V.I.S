// JARVIS.Voice — baixa modelos gratuitos (Vosk pt-BR, Whisper tiny) com progresso.
namespace Jarvis.Voice;

/// <summary>URLs oficiais verificadas (alphacephei.com e huggingface.co).</summary>
public static class ModelDownloader
{
    public const string UrlVoskPt =
        "https://alphacephei.com/vosk/models/vosk-model-small-pt-0.3.zip";
    public const string UrlWhisperTiny =
        "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin";

    /// <summary>Baixa o arquivo se ainda não existir. Reporta bytes recebidos.</summary>
    public static async Task BaixarSeAusenteAsync(string url, string destino,
        IProgress<long>? progresso = null, CancellationToken ct = default)
    {
        if (File.Exists(destino)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        await using var rede = await resp.Content.ReadAsStreamAsync(ct);
        long total = 0;
        await using (var arq = File.Create(destino + ".part")) {
            var buf = new byte[81920]; int lidos;
            while ((lidos = await rede.ReadAsync(buf, ct)) > 0) {
                await arq.WriteAsync(buf.AsMemory(0, lidos), ct);
                total += lidos; progresso?.Report(total);
            }
        }
        if (resp.Content.Headers.ContentLength is long esperado && total != esperado)
            throw new IOException("Download incompleto. Tente novamente.");
        ct.ThrowIfCancellationRequested();
        File.Move(destino + ".part", destino, overwrite: true);
    }
}
