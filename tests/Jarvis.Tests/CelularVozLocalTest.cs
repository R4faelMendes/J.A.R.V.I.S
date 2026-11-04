using Jarvis.Ai;
using Jarvis.Core;
using Jarvis.Server;
using Jarvis.Voice;
using NAudio.Wave;
namespace Jarvis.Tests;
public sealed class CelularVozLocalTest
{
    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void AudioPcmInteiroPreservaSinal(int bits)
    {
        var formato = new WaveFormat(48000, bits, 1);
        var conversor = new ConversorMicrofone(formato);
        var energia = 0.0; var total = 0;
        for (var bloco = 0; bloco < 30; bloco++)
        {
            var entrada = new byte[4800 * bits / 8];
            for (var i = 0; i < 4800; i++)
            {
                var v = .25 * Math.Sin(2 * Math.PI * 440 * (bloco * 4800 + i) / 48000);
                var valor = (long)(v * (Math.Pow(2, bits - 1) - 1));
                for (var b = 0; b < bits / 8; b++) entrada[i * bits / 8 + b] = (byte)(valor >> (8 * b));
            }
            var saida = conversor.Converter(entrada, entrada.Length);
            Assert.InRange(saida.Length, 3000, 3300);
            for (var i = 0; i < saida.Length; i += 2) { var v = BitConverter.ToInt16(saida, i) / 32768.0; energia += v * v; total++; }
        }
        Assert.InRange(Math.Sqrt(energia / total), .16, .19);
    }
    [Fact]
    public void AudioFloatStereoTambemPreservaSinal()
    {
        var c = new ConversorMicrofone(WaveFormat.CreateIeeeFloatWaveFormat(16000, 2));
        var bytes = new byte[1600 * 8];
        for (var i = 0; i < 3200; i++) BitConverter.GetBytes(.25f).CopyTo(bytes, i * 4);
        var saida = c.Converter(bytes, bytes.Length);
        Assert.Equal(3200, saida.Length);
        Assert.InRange(BitConverter.ToInt16(saida, 100), 8100, 8300);
    }
    [Fact]
    public void ComandoNaturalResolveNomeSemProvedor()
    {
        var rota = RoteadorIntencoes.Rotear("Jarvis, pode abrir o crome por favor?");
        Assert.Equal("open_app", rota?.Acao);
        var apps = new[] { new AppInfo("chrome", "Google Chrome", "chrome", false, false) };
        Assert.Equal("chrome", ResolverAplicativo.Encontrar(apps, rota!.Alvo!)?.Id);
        Assert.Null(ResolverAplicativo.Encontrar(apps, ""));
        Assert.Contains("12", AssistenteLocal.Responder("quanto é 3 vezes 4"));
    }
    [Fact]
    public async Task MicrofoneRealEntregaBlocosConvertidos()
    {
        using var voz = new VoiceService();
        var blocos = 0;
        voz.Iniciar(b => { if (b.Length > 0) Interlocked.Increment(ref blocos); });
        await Task.Delay(2500);
        voz.Parar();
        Assert.True(blocos > 0, "O dispositivo real não entregou áudio.");
    }
}
