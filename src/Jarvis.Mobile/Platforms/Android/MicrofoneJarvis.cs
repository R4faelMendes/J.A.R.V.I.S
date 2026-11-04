using Android.Media;
namespace Jarvis.Mobile.Services;

public static class MicrofoneJarvis
{
    public static async Task<byte[]> GravarAsync(Action<double> nivel, CancellationToken ct)
    {
        if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted) throw new InvalidOperationException("Permita o microfone nas configurações do Jarvis no Android para falar.");
        return await Task.Run(() =>
        {
            var size = AudioRecord.GetMinBufferSize(16000, ChannelIn.Mono, Android.Media.Encoding.Pcm16bit);
            if (size <= 0) throw new InvalidOperationException("O Android não disponibilizou o microfone em 16 kHz.");
            using var mic = new AudioRecord(AudioSource.VoiceRecognition, 16000, ChannelIn.Mono, Android.Media.Encoding.Pcm16bit, Math.Max(size * 2, 6400));
            if (mic.State != State.Initialized) throw new InvalidOperationException("Microfone indisponível ou ocupado por outro aplicativo.");
            using var output = new MemoryStream(); var samples = new short[1600]; var bytes = new byte[3200]; var clock = System.Diagnostics.Stopwatch.StartNew(); double ultimaFala = 0; var falou = false;
            try
            {
                mic.StartRecording();
                while (!ct.IsCancellationRequested && clock.Elapsed.TotalSeconds < 12)
                {
                    var n = mic.Read(samples, 0, samples.Length); if (n <= 0) throw new InvalidOperationException("O Android interrompeu a gravação do microfone.");
                    double energia = 0; for (var i = 0; i < n; i++) energia += Math.Pow(samples[i] / 32768d, 2);
                    var rms = Math.Sqrt(energia / n); nivel(Math.Clamp(rms * 8, 0, 1));
                    Buffer.BlockCopy(samples, 0, bytes, 0, n * 2); output.Write(bytes, 0, n * 2);
                    if (rms > .008) { falou = true; ultimaFala = clock.Elapsed.TotalSeconds; }
                    if (falou && clock.Elapsed.TotalSeconds > 1.3 && clock.Elapsed.TotalSeconds - ultimaFala > 1.1) break;
                }
                if (!falou) throw new InvalidOperationException("Não ouvi fala. Aproxime o celular e confira se outro app está usando o microfone.");
                return output.ToArray();
            }
            finally { try { mic.Stop(); } catch (Java.Lang.Exception) { } mic.Release(); nivel(0); }
        });
    }
}
