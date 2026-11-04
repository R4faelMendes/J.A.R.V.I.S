using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
namespace Jarvis.Core;

public static class RedeDireta
{
    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "endereco-direto.txt");
    public static bool Publico(IPAddress ip)
    {
        if (ip.AddressFamily == AddressFamily.InterNetworkV6) return (ip.GetAddressBytes()[0] & 0xe0) == 0x20 && !ip.ToString().StartsWith("2001:db8:", StringComparison.OrdinalIgnoreCase);
        var b = ip.GetAddressBytes(); return b.Length == 4 && !DescobertaRede.Local(ip) && b[0] is > 0 and < 224 && b[0] != 127 && !(b[0] == 100 && b[1] is >= 64 and <= 127) && !(b[0] == 169 && b[1] == 254);
    }
    public static bool Publico(string? endereco) => Uri.TryCreate(endereco, UriKind.Absolute, out var uri) && IPAddress.TryParse(uri.Host.Trim('[', ']'), out var ip) && Publico(ip);
    public static string[] Filtrar(IEnumerable<string?> enderecos) => EnderecosJarvis.Reunir(enderecos).Where(e => !EnderecosJarvis.Tailscale(e)).ToArray();
    public static string? Configurado { get { try { return EnderecosJarvis.Normalizar(File.ReadAllText(Arquivo)); } catch { return null; } } }
    public static void Salvar(string endereco)
    {
        var value = EnderecosJarvis.Normalizar(endereco);
        if (value is null || EnderecosJarvis.Tailscale(value)) throw new ArgumentException("Informe o endereço HTTPS direto do PC, com a porta. Ex.: https://[IPv6]:5189 ou seu domínio.");
        Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!); File.WriteAllText(Arquivo, value);
    }
    public static string[] PublicosDoPc()
    {
        var list = new List<(string Url, int Prioridade)>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211))
                foreach (var addr in nic.GetIPProperties().UnicastAddresses.Where(a => Publico(a.Address)))
                {
                    var rank = 2; if (OperatingSystem.IsWindows()) { try { if (addr.PrefixOrigin == PrefixOrigin.Dhcp) rank = 0; else if (addr.SuffixOrigin != SuffixOrigin.Random) rank = 1; } catch { } }
                    list.Add((new UriBuilder("https", addr.Address.ToString(), 5189).Uri.GetLeftPart(UriPartial.Authority), rank));
                }
        }
        catch { }
        return list.OrderBy(p => p.Prioridade).Select(p => p.Url).Distinct().Take(4).ToArray();
    }
    public static string[] EnderecosExternos() => Filtrar(new[] { Configurado }.Concat(PublicosDoPc()));
    public static bool MesmaRede(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || DescobertaRede.Local(ip)) return true;
        if (ip.AddressFamily != AddressFamily.InterNetworkV6) return false;
        var bytes = ip.GetAddressBytes();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up))
                foreach (var addr in nic.GetIPProperties().UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6))
                {
                    var local = addr.Address.GetAddressBytes(); var bits = addr.PrefixLength;
                    if (bits is < 48 or > 128) continue;
                    if (Enumerable.Range(0, bits).All(i => (bytes[i / 8] & (1 << (7 - i % 8))) == (local[i / 8] & (1 << (7 - i % 8))))) return true;
                }
        }
        catch { }
        return false;
    }
}
