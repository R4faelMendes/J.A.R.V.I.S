using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace Jarvis.Core;

public sealed record PcNaRede(string Ip, string Nome);

/// <summary>Descoberta é apenas uma sugestão de endereço; autorização continua exigindo PIN/token e pinning.</summary>
public static class DescobertaRede
{
    public static bool Local(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = ip.GetAddressBytes();
        return b[0] == 10 || b[0] == 192 && b[1] == 168 || b[0] == 172 && b[1] is >= 16 and <= 31;
    }
    public static IPAddress Broadcast(IPAddress ip, IPAddress mask)
    {
        var a = ip.GetAddressBytes(); var m = mask.GetAddressBytes();
        if (a.Length != 4 || m.Length != 4) throw new ArgumentException("Rede IPv4 esperada.");
        return new IPAddress(a.Select((v, i) => (byte)(v | ~m[i])).ToArray());
    }
    public static IReadOnlyList<IPAddress> Destinos()
    {
        var result = new List<IPAddress> { IPAddress.Broadcast };
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                foreach (var a in nic.GetIPProperties().UnicastAddresses.Where(a => Local(a.Address)))
                    if (a.IPv4Mask is { } mask && !mask.Equals(IPAddress.Any)) result.Add(Broadcast(a.Address, mask));
        }
        catch { }
        return result.Distinct().Take(8).ToArray();
    }
    public static PcNaRede? Interpretar(ReadOnlyMemory<byte> payload, IPAddress origem)
    {
        if (payload.Length > 8192 || !Local(origem)) return null;
        try
        {
            using var document = JsonDocument.Parse(payload); var json = document.RootElement;
            if (json.GetProperty("app").GetString() != "Jarvis") return null;
            if (json.TryGetProperty("https", out var port) && port.GetInt32() != 5189) return null;
            var nome = json.GetProperty("nome").GetString(); if (string.IsNullOrWhiteSpace(nome) || nome.Length > 80) return null;
            // O endereço que respondeu é alcançável nesta rede; não preferir IP de VPN/Hyper-V anunciado.
            return new(origem.ToString(), nome);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException) { return null; }
    }
    public static async Task<IReadOnlyList<PcNaRede>> ProcurarAsync(int timeoutMs = 2500, CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(Math.Clamp(timeoutMs, 500, 5000));
        using var udp = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        var found = new Dictionary<string, PcNaRede>(); var destinos = Destinos();
        async Task AnunciarAsync()
        {
            for (var rodada = 0; rodada < 3; rodada++)
            {
                foreach (var ip in destinos)
                    try { await udp.SendAsync("JARVIS_DISCOVER"u8.ToArray(), new IPEndPoint(ip, 5187), timeout.Token); } catch (SocketException) { }
                await Task.Delay(350, timeout.Token);
            }
        }
        var announce = AnunciarAsync();
        try
        {
            for (var i = 0; i < 64; i++)
            {
                var response = await udp.ReceiveAsync(timeout.Token);
                if (Interpretar(response.Buffer, response.RemoteEndPoint.Address) is { } pc) found[pc.Ip] = pc;
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
        catch (SocketException) { }
        finally
        {
            timeout.Cancel();
            try { await announce; } catch (OperationCanceledException) { } catch (SocketException) { }
        }
        ct.ThrowIfCancellationRequested(); return found.Values.ToArray();
    }
}
