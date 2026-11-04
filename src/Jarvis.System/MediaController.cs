using System.Diagnostics;
using System.Runtime.InteropServices;
using Jarvis.Core;
using NAudio.CoreAudioApi;
using Windows.Media.Control;

namespace Jarvis.Sistema;

/// <summary>Controla a sessão real do Spotify (incluindo Lite) publicada no Windows.</summary>
public sealed class MediaController : IMediaController
{
    [DllImport("user32.dll")] private static extern bool LockWorkStation();

    private static async Task<GlobalSystemMediaTransportControlsSession?> SessaoAsync()
    {
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        // Não controlar outro player por engano quando o Spotify não publicou sessão.
        return manager.GetSessions().FirstOrDefault(s =>
            s.SourceAppUserModelId.Contains("spotify", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<EstadoMidia> ObterEstadoAsync()
    {
        var s = await SessaoAsync();
        if (s is null) return new(false, "Abra o Spotify e inicie uma música", "", "Spotify / Lite", false);
        var p = await s.TryGetMediaPropertiesAsync();
        byte[]? capa = null;
        try
        {
            if (p.Thumbnail is not null)
            {
                using var stream = await p.Thumbnail.OpenReadAsync();
                if (stream.Size is > 0 and <= 1_000_000)
                {
                    using var input = stream.GetInputStreamAt(0);
                    using var reader = new Windows.Storage.Streams.DataReader(input);
                    await reader.LoadAsync((uint)stream.Size); capa = new byte[(int)stream.Size]; reader.ReadBytes(capa);
                }
            }
        }
        catch { }
        var tempo = s.GetTimelineProperties();
        return new(true, p.Title, p.Artist, "Spotify", s.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing, capa, tempo.Position.TotalSeconds, (tempo.EndTime - tempo.StartTime).TotalSeconds);
    }

    public async Task<bool> ExecutarAsync(ComandoMidia comando)
    {
        var s = await SessaoAsync();
        if (s is null) return false;
        return comando switch
        {
            ComandoMidia.Pausar => await s.TryPauseAsync(),
            ComandoMidia.Continuar => await s.TryPlayAsync(),
            ComandoMidia.Alternar => await s.TryTogglePlayPauseAsync(),
            ComandoMidia.Proxima => await s.TrySkipNextAsync(),
            ComandoMidia.Anterior => await s.TrySkipPreviousAsync(),
            _ => false
        };
    }

    public void DefinirVolume(int porcentagem) => Volume = porcentagem;
    public void TocarPausar() => _ = ExecutarAsync(ComandoMidia.Alternar);
    public void Proxima() => _ = ExecutarAsync(ComandoMidia.Proxima);
    public void Anterior() => _ = ExecutarAsync(ComandoMidia.Anterior);
    public static float Volume
    {
        get
        {
            using var disp = new MMDeviceEnumerator();
            using var dev = disp.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return dev.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
        }
        set
        {
            using var disp = new MMDeviceEnumerator();
            using var dev = disp.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            dev.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(value / 100f, 0f, 1f);
        }
    }
    public static void BloquearPC() => LockWorkStation();
}
