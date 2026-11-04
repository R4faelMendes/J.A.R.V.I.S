using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Jarvis.Core;
using NAudio.Wave;

namespace Jarvis.Voice;

/// <summary>Piper sob demanda; uma fala por vez, cache limitado e cancelamento real.</summary>
public sealed class PiperTts : IDisposable
{
    private readonly string _piperExe, _pastaVozes, _pastaCache;
    private readonly SemaphoreSlim _fila = new(1, 1);
    private readonly CancellationTokenSource _vida = new();
    public double Velocidade { get; set; } = 1;
    public bool Falando { get; private set; }
    private NivelFalaSampleProvider? _medidor;
    public double NivelAudio => Volatile.Read(ref _medidor)?.Nivel ?? 0;
    public event EventHandler? FalandoMudou;
    public PiperTts(string pastaModelos)
    {
        _piperExe = Path.Combine(pastaModelos, "piper", "piper.exe");
        _pastaVozes = Path.Combine(pastaModelos, "piper-voices");
        _pastaCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "cache-tts");
        Directory.CreateDirectory(_pastaCache);
    }
    public string ModeloAtual
    {
        get
        {
            var nome = Path.GetFileName(ConfiguracaoApp.ModeloVoz);
            var p = Path.Combine(_pastaVozes, nome);
            if (!File.Exists(p)) p = Path.Combine(_pastaVozes, "pt_BR-faber-medium.onnx");
            return p;
        }
    }
    public void Aquecer() { /* Sem processo/modelo residente: preserva memória no boot. */ }
    public static string Escala(double velocidade) => (1 / Math.Clamp(velocidade, .7, 1.3)).ToString("0.###", CultureInfo.InvariantCulture);
    public async Task SintetizarAsync(string texto, string destino, CancellationToken ct = default)
    {
        if (!File.Exists(_piperExe) || !File.Exists(ModeloAtual)) throw new FileNotFoundException("Instale os modelos Piper na pasta models antes de usar a voz.");
        var psi = new ProcessStartInfo(_piperExe)
        {
            WorkingDirectory = Path.GetDirectoryName(_piperExe)!, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardError = true, RedirectStandardOutput = true,
            StandardInputEncoding = new UTF8Encoding(false)
        };
        foreach (var arg in new[] { "--model", ModeloAtual, "--output_file", destino, "--length_scale", Escala(Velocidade), "--sentence_silence", "0.18", "-q" }) psi.ArgumentList.Add(arg);
        using var processo = Process.Start(psi) ?? throw new InvalidOperationException("Não foi possível iniciar a voz.");
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(ct, _vida.Token);
        prazo.CancelAfter(TimeSpan.FromSeconds(60));
        var erro = processo.StandardError.ReadToEndAsync();
        var saida = processo.StandardOutput.ReadToEndAsync();
        try
        {
            await processo.StandardInput.WriteLineAsync(TtsTextSanitizer.Sanitize(texto).AsMemory(), prazo.Token);
            processo.StandardInput.Close();
            await processo.WaitForExitAsync(prazo.Token);
            await saida; await erro;
            if (processo.ExitCode != 0 || !File.Exists(destino)) throw new InvalidOperationException("O sintetizador não conseguiu gerar o áudio.");
        }
        catch
        {
            try { if (!processo.HasExited) processo.Kill(true); } catch { }
            throw;
        }
    }
    public void Falar(string texto) => FalarAsync(texto).GetAwaiter().GetResult();
    public async Task FalarAsync(string texto, CancellationToken ct = default, bool guardarCache = true)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(ct, _vida.Token);
        await _fila.WaitAsync(prazo.Token);
        string? temporario = null;
        try
        {
            Falando = true; FalandoMudou?.Invoke(this, EventArgs.Empty);
            var limpo = TtsTextSanitizer.Sanitize(texto);
            var chave = ModeloAtual + "|" + Escala(Velocidade) + "|" + limpo;
            var nome = guardarCache ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave))) : Guid.NewGuid().ToString("N");
            var wav = Path.Combine(_pastaCache, nome + ".wav");
            if (!guardarCache) temporario = wav;
            if (!File.Exists(wav))
            {
                var parcial = wav + ".part";
                try { await SintetizarAsync(limpo, parcial, prazo.Token); File.Move(parcial, wav, true); }
                finally { if (File.Exists(parcial)) File.Delete(parcial); }
            }
            using var leitor = new AudioFileReader(wav);
            using var player = new WaveOutEvent { DesiredLatency = 100, NumberOfBuffers = 2 };
            var medidor = new NivelFalaSampleProvider(leitor);
            Volatile.Write(ref _medidor, medidor);
            player.Init(new NAudio.Wave.SampleProviders.SampleToWaveProvider(medidor)); player.Play();
            try { while (player.PlaybackState == PlaybackState.Playing) await Task.Delay(40, prazo.Token); }
            finally { player.Stop(); }
        }
        finally
        {
            Volatile.Write(ref _medidor, null);
            if (temporario is not null) try { File.Delete(temporario); } catch { }
            try
            {
                foreach (var f in new DirectoryInfo(_pastaCache).GetFiles("*.wav").OrderByDescending(f => f.LastWriteTimeUtc).Skip(60)) f.Delete();
            }
            catch { }
            Falando = false; FalandoMudou?.Invoke(this, EventArgs.Empty); _fila.Release();
        }
    }
    public void Dispose() => _vida.Cancel();
}
