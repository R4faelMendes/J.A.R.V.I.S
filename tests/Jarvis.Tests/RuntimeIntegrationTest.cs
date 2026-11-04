using Jarvis.Core;
using Jarvis.Sistema;
using Jarvis.Voice;
using NAudio.Wave;
using System.Diagnostics;
using System.Text.Json;

namespace Jarvis.Tests;

public sealed class RuntimeIntegrationTest
{
    [Fact]
    public async Task PiperRealESessaoSpotify()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "models"))) dir = dir.Parent;
        var pasta = Path.Combine(dir!.FullName, "artifacts", "voice"); Directory.CreateDirectory(pasta);
        var anterior = ConfiguracaoApp.ModeloVoz;
        var medidas = new List<object>();
        try
        {
            using var piper = new PiperTts(Path.Combine(dir.FullName, "models"));
            foreach (var modelo in new[] { "pt_BR-faber-medium.onnx", "pt_BR-cadu-medium.onnx" })
            {
                ConfiguracaoApp.ModeloVoz = modelo;
                var destino = Path.Combine(pasta, modelo + ".wav");
                var tempo = Stopwatch.StartNew();
                await piper.SintetizarAsync("Olá, Rafael. Sou o Jarvis. Posso ler seus e-mails, controlar sua música e ajudar com o computador.", destino);
                using var wav = new WaveFileReader(destino);
                Assert.True(wav.TotalTime.TotalSeconds > 2);
                medidas.Add(new { modelo, segundosGeracao = tempo.Elapsed.TotalSeconds, segundosAudio = wav.TotalTime.TotalSeconds, bytesModelo = new FileInfo(piper.ModeloAtual).Length });
            }
            var status = await new MediaController().ObterEstadoAsync();
            await File.WriteAllTextAsync(Path.Combine(pasta, "verificacao.json"), JsonSerializer.Serialize(new { vozes = medidas, spotifyDisponivel = status.Disponivel, spotifyTocando = status.Tocando }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { ConfiguracaoApp.ModeloVoz = anterior; }
    }
}
