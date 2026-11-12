using System.IO;
using System.Net.Http;
using Jarvis.Core;
using NAudio.CoreAudioApi;
namespace Jarvis.Desktop.Services;
public sealed class DiagnosticoService : IDiagnosticoService
{
    public async Task<IReadOnlyList<ResultadoDiagnostico>> VerificarAsync(CancellationToken ct = default)
    {
        var itens = new List<ResultadoDiagnostico>();
        await Task.Run(() =>
        {
            try
            {
                using var devices = new MMDeviceEnumerator(); using var mic = devices.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                itens.Add(new("Microfone", mic.State == DeviceState.Active, mic.FriendlyName));
            }
            catch (Exception ex) { itens.Add(new("Microfone", false, ex.Message)); }
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "models"))) dir = dir.Parent;
            var models = dir is null ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "models") : Path.Combine(dir.FullName, "models");
            foreach (var (nome, caminho, pasta) in new[] { ("Wake word Vosk", "vosk-model-small-pt-0.3", true), ("Síntese Piper", "piper/piper.exe", false), ("Voz selecionada", "piper-voices/" + ConfiguracaoApp.ModeloVoz, false), ("Transcrição Whisper", "ggml-tiny.bin", false) })
            {
                ct.ThrowIfCancellationRequested(); var path = Path.Combine(models, caminho);
                itens.Add(new(nome, pasta ? Directory.Exists(path) : File.Exists(path), path));
            }
            itens.Add(new("Google", ServicosAplicacao.Email.Conectado, ServicosAplicacao.Email.Conectado ? "Conta conectada." : "Conecte sua conta na página E-mails."));
            foreach (var item in RedeService.VerificarWol()) itens.Add(new(item.Nome, item.Ok, item.Detalhe));
        }, ct);
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var resposta = await http.GetAsync("http://127.0.0.1:5188/ping", ct);
            itens.Add(new("Servidor local", resposta.IsSuccessStatusCode, $"HTTP {(int)resposta.StatusCode} · diagnóstico na porta 5188"));
        }
        catch (Exception ex) { itens.Add(new("Servidor local", false, ex.Message)); }
        itens.Add(new("Dados protegidos", RedeService.PortaOuvindo(5189), "HTTPS 5189 · token e impressão do certificado no app."));
        itens.Add(new("Renderização", true, "SkiaSharp com fallback WPF; " + (ConfiguracaoApp.ReduzirAnimacoes || ConfiguracaoApp.ModoEconomico ? "animações reduzidas." : "animações habilitadas.") + " FPS não é exibido no dock."));
        return itens;
    }
}
