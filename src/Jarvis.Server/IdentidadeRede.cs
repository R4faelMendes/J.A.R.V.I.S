using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Jarvis.Core;

namespace Jarvis.Server;
public static class IdentidadeRede
{
    public static IdentidadePc Obter(string? ip = null)
    {
        ip ??= AparelhosIp.Local();
        var endpoints = RedeDireta.Filtrar(AparelhosIp.Candidatos().Select(a => $"https://{a.Ip}:5189").Concat(RedeDireta.EnderecosExternos()));
        var remote = RedeDireta.EnderecosExternos().FirstOrDefault();
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if ((adapter.Name + adapter.Description).Contains("tailscale", StringComparison.OrdinalIgnoreCase)) continue;
            var address = adapter.GetIPProperties().UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork && a.Address.ToString() == ip);
            if (address is null || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            var mac = adapter.GetPhysicalAddress().ToString();
            if (mac.Length != 12) continue;
            var bytes = address.Address.GetAddressBytes(); var mask = address.IPv4Mask.GetAddressBytes();
            var broadcast = new IPAddress(bytes.Select((b, i) => (byte)(b | ~mask[i])).ToArray()).ToString();
            return new(Environment.MachineName, string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2))), broadcast, endpoints, remote);
        }
        // A interface VPN não fornece MAC físico para Wake-on-LAN.
        if (!IPAddress.IsLoopback(IPAddress.Parse(ip)))
        {
            var local = AparelhosIp.Candidatos().FirstOrDefault(a => !a.Ip.StartsWith("100.") && a.Cabeado).Ip;
            if (local is not null && local != ip) return Obter(local) with { Enderecos = endpoints, EnderecoRemoto = remote };
        }
        return new(Environment.MachineName, "", "255.255.255.255", endpoints, remote);
    }
}
