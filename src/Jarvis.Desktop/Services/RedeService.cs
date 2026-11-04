// JARVIS Desktop — rede local: firewall, perfil de rede e Wake-on-LAN (só leitura sem UAC).
using System.Diagnostics;
using System.Management;

namespace Jarvis.Desktop.Services;

/// <summary>Diagnóstico e correção da rede local. Mudanças pedem UAC uma vez.</summary>
public static class RedeService
{
    public sealed record ItemDiag(string Nome, bool Ok, string Detalhe, string? Acao = null);

    public static bool PortaOuvindo(int porta)
    {
        try
        {
            foreach (var c in System.Net.NetworkInformation.IPGlobalProperties
                         .GetIPGlobalProperties().GetActiveTcpListeners())
                if (c.Port == porta) return true;
        }
        catch { }
        return false;
    }

    public static (bool Existe, string Detalhe) RegraFirewallExiste()
    {
        try
        {
            var psi = new ProcessStartInfo("netsh",
                "advfirewall firewall show rule name=\"Jarvis\"")
            { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            var saida = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10000);
            return (saida.Contains("5188") || saida.Contains("5189"), saida.Trim());
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>Cria regra de entrada (perfis Privado, só sub-rede local). Pede UAC.</summary>
    public static (bool Ok, string Msg) CriarRegraFirewall()
    {
        try
        {
            foreach (var (porta, protocolo) in new[] { ("5188", "TCP"), ("5189", "TCP"), ("5187", "UDP") })
            {
                var psi = new ProcessStartInfo("netsh",
                    $"advfirewall firewall add rule name=\"Jarvis\" dir=in action=allow " +
                    $"protocol={protocolo} localport={porta} profile=private remoteip=localsubnet")
                { UseShellExecute = true, Verb = "runas", CreateNoWindow = true };
                using var p = Process.Start(psi);
                p?.WaitForExit(30000);
                if (p?.ExitCode != 0) return (false, $"netsh devolveu {p?.ExitCode} na porta {porta}.");
            }
            return (true, "Regra criada (privado, só rede local).");
        }
        catch (Exception ex) { return (false, "UAC negado ou erro: " + ex.Message); }
    }

    /// <summary>Perfil da rede ativa (Privado/Publico). Leitura via PowerShell.</summary>
    public static (string Perfil, string Detalhe) PerfilRede()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell",
                "-NoProfile -Command \"Get-NetConnectionProfile | Select-Object -First 1 Name,NetworkCategory | Format-List | Out-String\"")
            { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            var saida = p.StandardOutput.ReadToEnd();
            p.WaitForExit(15000);
            var privado = saida.Contains("Private");
            var publico = saida.Contains("Public");
            return (privado ? "Privado" : publico ? "Público" : "Desconhecido", saida.Trim());
        }
        catch (Exception ex) { return ("Desconhecido", ex.Message); }
    }

    /// <summary>Troca a rede ativa para Privada. Pede UAC.</summary>
    public static (bool Ok, string Msg) TornarRedePrivada()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell",
                "-NoProfile -Command \"Get-NetConnectionProfile | Set-NetConnectionProfile -NetworkCategory Private\"")
            { UseShellExecute = true, Verb = "runas", CreateNoWindow = true };
            using var p = Process.Start(psi);
            p?.WaitForExit(30000);
            return (p?.ExitCode == 0, p?.ExitCode == 0 ? "Rede agora Privada." : "Falhou. Rode como administrador.");
        }
        catch (Exception ex) { return (false, "UAC negado ou erro: " + ex.Message); }
    }

    /// <summary>Checklist Wake-on-LAN do adaptador cabeado (só leitura; BIOS o app não vê).</summary>
    public static List<ItemDiag> VerificarWol()
    {
        var itens = new List<ItemDiag>();
        try
        {
            using var busca = new ManagementObjectSearcher("SELECT Name,MACAddress,PNPDeviceID FROM Win32_NetworkAdapter WHERE NetConnectionStatus = 2 AND PhysicalAdapter = TRUE");
            string? nome = null, mac = null, pnp = null;
            foreach (ManagementObject o in busca.Get())
            {
                var id = o["PNPDeviceID"] as string ?? "";
                if (!id.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) && !id.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase)) continue;
                nome = o["Name"] as string; mac = o["MACAddress"] as string; pnp = id; break;
            }
            itens.Add(new("Wake-on-LAN · placa de rede", nome is not null, nome is null ? "Nenhuma placa física ativa encontrada." : nome + " · " + mac));
            var permitido = false;
            if (pnp is not null)
            {
                using var wake = new ManagementObjectSearcher(@"root\wmi", "SELECT InstanceName,Enable FROM MSPower_DeviceWakeEnable");
                foreach (ManagementObject entry in wake.Get())
                    if ((entry["InstanceName"] as string ?? "").StartsWith(pnp + "_", StringComparison.OrdinalIgnoreCase)) permitido |= entry["Enable"] is true;
            }
            itens.Add(new("Wake-on-LAN · Windows", permitido, permitido ? "A placa de rede está autorizada a acordar o PC." : "A placa ainda não está armada para despertar. Ative Magic Packet e a permissão de acordar nas propriedades da placa; após mudanças no driver pode ser necessário reiniciar o Windows."));
            itens.Add(new("Wake-on-LAN · BIOS", false, "Confirme no setup: Wake on LAN = LAN Only; Deep Sleep Control = Disabled (Dell OptiPlex). A configuração da BIOS não pôde ser lida pelo aplicativo."));
            itens.Add(new("Wake-on-LAN · fora de casa", false, "O servidor Jarvis não funciona com o PC desligado. Para acordá-lo de outra rede é necessário um roteador com Wake-on-LAN ou outro aparelho sempre ligado na rede de casa, atuando como retransmissor."));
        }
        catch (Exception ex) { itens.Add(new("Wake-on-LAN · diagnóstico", false, "Não foi possível consultar o driver: " + ex.Message)); }
        return itens;
    }
}