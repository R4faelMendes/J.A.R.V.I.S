using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Jarvis.Core;
namespace Jarvis.Desktop.Services;
public sealed class AcessoExternoService : IAcessoExterno
{
    public void AbrirPagina(string url) {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https") throw new ArgumentException("Endereço inválido.");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
    public string? Endereco => RedeDireta.EnderecosExternos().FirstOrDefault();
    public void Salvar(string endereco) => RedeDireta.Salvar(endereco);
}
