using Microsoft.AspNetCore.SignalR;
namespace Jarvis.Server;
// A autenticação de todas as requisições do hub é feita pelo middleware X-Jarvis-Token.
public sealed class JarvisHub(IControleRemoto controle) : Hub
{
    public Task<string> Comando(string nome, string? argumento)
    {
        if (!ComandosFixos.Permitidos.Contains(nome)) throw new HubException("Comando não permitido.");
        return controle.ExecutarAsync(nome, argumento, false);
    }
}
