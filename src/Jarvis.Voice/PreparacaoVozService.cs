using System.IO.Compression;
using System.Text.Json;
using Jarvis.Core;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Jarvis.Voice;

public sealed class PreparacaoVozService : IPreparacaoVoz
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public string Pasta { get; }
    public const string PiperUrl = "https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip";
    public const string VozUrl = "https://huggingface.co/rhasspy/piper-voices/resolve/main/pt/pt_BR/faber/medium/pt_BR-faber-medium.onnx";
    public PreparacaoVozService(string? pasta = null) => Pasta = pasta ?? Localizar();
    private static string Localizar()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent) {
            var path = Path.Combine(dir.FullName, "models");
            if (File.Exists(Path.Combine(path, "piper", "piper.exe"))) return path;
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "models");
    }
    private bool Arquivo(string relativo, long minimo) => File.Exists(Path.Combine(Pasta, relativo)) && new FileInfo(Path.Combine(Pasta, relativo)).Length >= minimo;
    public IReadOnlyList<RecursoVoz> Verificar()
    {
        var nomeVoz = Path.GetFileName(ConfiguracaoApp.ModeloVoz);
        bool json = false;
        try { using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Pasta, "piper-voices", nomeVoz + ".json"))); json = doc.RootElement.TryGetProperty("audio", out _); } catch (IOException) { } catch (JsonException) { }
        return new[] {
            new RecursoVoz("Vosk português", ModeloVosk.Completo(Path.Combine(Pasta, "vosk-model-small-pt-0.3")), "Ativação por voz · modelo oficial Alpha Cephei"),
            new RecursoVoz("Whisper tiny", Arquivo("ggml-tiny.bin", 50_000_000), "Transcrição local · modelo oficial whisper.cpp"),
            new RecursoVoz("Piper Windows", Arquivo("piper/piper.exe", 10_000) && Directory.GetFiles(Path.Combine(Pasta, "piper"), "*.dll").Length > 0, "Síntese local · distribuição oficial Windows x64"),
            new RecursoVoz("Voz em português", Arquivo("piper-voices/" + nomeVoz, 1_000_000) && json, nomeVoz)
        };
    }
    public async Task InstalarAsync(IProgress<ProgressoDownload> progresso, CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct)) throw new InvalidOperationException("Já existe uma instalação em andamento.");
        var temporario = Path.Combine(Path.GetTempPath(), "jarvis-voz-" + Guid.NewGuid().ToString("N"));
        try {
            Directory.CreateDirectory(temporario); Directory.CreateDirectory(Pasta);
            var estado = Verificar();
            async Task Download(string titulo, string url, string arquivo) {
                progresso.Report(new(titulo, 0));
                await ModelDownloader.BaixarSeAusenteAsync(url, arquivo, new Progress<long>(n => progresso.Report(new(titulo, n))), ct);
            }
            if (!estado[0].Pronto) {
                var zip = Path.Combine(temporario, "vosk.zip"); await Download("Baixando Vosk", ModelDownloader.UrlVoskPt, zip);
                await Task.Run(() => ZipFile.ExtractToDirectory(zip, Path.Combine(temporario, "vosk")), ct);
                CopiarArvore(Path.Combine(temporario, "vosk", "vosk-model-small-pt-0.3"), Path.Combine(Pasta, "vosk-model-small-pt-0.3"), ct);
            }
            if (!estado[1].Pronto) {
                var bin = Path.Combine(temporario, "ggml-tiny.bin"); await Download("Baixando Whisper tiny", ModelDownloader.UrlWhisperTiny, bin);
                if (new FileInfo(bin).Length < 50_000_000) throw new InvalidDataException("Whisper incompleto.");
                File.Move(bin, Path.Combine(Pasta, "ggml-tiny.bin"), true);
            }
            if (!estado[2].Pronto) {
                var zip = Path.Combine(temporario, "piper.zip"); await Download("Baixando Piper", PiperUrl, zip);
                await Task.Run(() => ZipFile.ExtractToDirectory(zip, Path.Combine(temporario, "piper")), ct);
                var exe = Directory.EnumerateFiles(Path.Combine(temporario, "piper"), "piper.exe", SearchOption.AllDirectories).Single();
                CopiarArvore(Path.GetDirectoryName(exe)!, Path.Combine(Pasta, "piper"), ct);
            }
            if (!estado[3].Pronto) {
                // O assistente instala a voz padrão; vozes personalizadas permanecem intactas.
                var voz = Path.Combine(temporario, "pt_BR-faber-medium.onnx");
                await Download("Baixando voz Faber", VozUrl, voz);
                await Download("Baixando configuração da voz", VozUrl + ".json", voz + ".json");
                if (new FileInfo(voz).Length < 1_000_000) throw new InvalidDataException("Voz incompleta.");
                using (var doc = JsonDocument.Parse(await File.ReadAllTextAsync(voz + ".json", ct))) _ = doc.RootElement.GetProperty("audio");
                Directory.CreateDirectory(Path.Combine(Pasta, "piper-voices"));
                File.Move(voz, Path.Combine(Pasta, "piper-voices", "pt_BR-faber-medium.onnx"), true);
                File.Move(voz + ".json", Path.Combine(Pasta, "piper-voices", "pt_BR-faber-medium.onnx.json"), true);
                ConfiguracaoApp.ModeloVoz = "pt_BR-faber-medium.onnx"; ConfiguracaoApp.NotificarAlteracao();
            }
            if (Verificar().Any(r => !r.Pronto)) throw new InvalidDataException("A validação encontrou arquivos incompletos. Execute Reparar novamente.");
            progresso.Report(new("Instalação validada. Faça o teste de voz.", 0));
        } finally {
            try { if (Directory.Exists(temporario)) Directory.Delete(temporario, true); } catch (IOException) { }
            _gate.Release();
        }
    }
    private static void CopiarArvore(string origem, string destino, CancellationToken ct)
    {
        foreach (var arquivo in Directory.EnumerateFiles(origem, "*", SearchOption.AllDirectories)) {
            ct.ThrowIfCancellationRequested(); var alvo = Path.Combine(destino, Path.GetRelativePath(origem, arquivo));
            Directory.CreateDirectory(Path.GetDirectoryName(alvo)!); File.Copy(arquivo, alvo, true);
        }
    }
    public async Task<string> TestarAsync(CancellationToken ct)
    {
        if (Verificar().Any(r => !r.Pronto)) throw new InvalidOperationException("Instale os recursos antes de testar.");
        var wav = Path.Combine(Path.GetTempPath(), "jarvis-teste-" + Guid.NewGuid().ToString("N") + ".wav");
        try {
            using var tts = new PiperTts(Pasta);
            await tts.SintetizarAsync("Jarvis, diga que o teste de voz está funcionando.", wav, ct);
            using var audio = new AudioFileReader(wav);
            var samples = new WdlResamplingSampleProvider(audio, 16000);
            using var pcm = new MemoryStream(); var buffer = new float[1600]; int lidos;
            while ((lidos = samples.Read(buffer, 0, buffer.Length)) > 0) {
                ct.ThrowIfCancellationRequested();
                for (var i = 0; i < lidos; i++) pcm.Write(BitConverter.GetBytes((short)(Math.Clamp(buffer[i], -1, 1) * 32767)));
            }
            using var ditado = new DictationService(Path.Combine(Pasta, "ggml-tiny.bin"));
            var texto = await ditado.TranscreverAsync(pcm.ToArray(), ct);
            if (string.IsNullOrWhiteSpace(texto)) throw new InvalidOperationException("A síntese funcionou, mas Whisper não reconheceu o áudio de teste.");
            await tts.FalarAsync("Teste concluído. Sua voz local está pronta.", ct, false);
            return "Síntese e transcrição locais funcionando. Reconhecido: " + texto;
        } finally { if (File.Exists(wav)) File.Delete(wav); }
    }
    public async Task<string> TestarMicrofoneAsync(CancellationToken ct)
    {
        using var voz = new VoiceService { FiltroMicrofone = ConfiguracaoApp.MicrofonePreferido };
        using var pcm = new MemoryStream(); var gate = new object();
        try {
            voz.Iniciar(bloco => { lock (gate) if (pcm.Length < 32000 * 6) pcm.Write(bloco); });
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        } finally { voz.Parar(); }
        byte[] dados; lock (gate) dados = pcm.ToArray();
        using var ditado = new DictationService(Path.Combine(Pasta, "ggml-tiny.bin"));
        var texto = await ditado.TranscreverAsync(dados, ct);
        return string.IsNullOrWhiteSpace(texto) ? "Nenhuma fala reconhecida. Confira microfone e volume de entrada." : "Reconhecido (sem executar): " + texto;
    }
}
