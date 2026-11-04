using Jarvis.Core;
namespace Jarvis.Server;

public interface IControleRemoto
{
    Task<string> TranscreverAsync(byte[] pcm, CancellationToken ct) => throw new InvalidOperationException("Atualize o Jarvis no PC para usar a voz local.");
    EstadoEntrada Entrada() => new(false, "Controle por toque indisponível neste PC.");
    Task ToqueAsync(PedidoToque pedido, CancellationToken ct) => throw new InvalidOperationException("Controle por toque indisponível.");
    IReadOnlyList<Compromisso> Agenda() => [];
    Compromisso SalvarCompromisso(Compromisso item) => throw new InvalidOperationException("Agenda indisponível.");
    Task<EstadoMidia> MidiaAsync() => Task.FromResult(new EstadoMidia(false, "Spotify indisponível", "", "Spotify", false));
    IReadOnlyList<AppInfo> Apps();
    IReadOnlyList<AppInfo> AppsDisponiveis() => Apps();
    void SelecionarApps(string[] ids) => throw new ArgumentException("Seleção indisponível.");
    Task<string> ExecutarAsync(string comando, string? id, bool confirmado);
    Task<TelemetriaPc> TelemetriaAsync() => Task.FromResult(new TelemetriaPc(null, null, null, null, null, DateTimeOffset.UtcNow));
    IReadOnlyList<JanelaRemota> Janelas() => Array.Empty<JanelaRemota>();
    IReadOnlyList<MonitorInfo> Monitores() => Array.Empty<MonitorInfo>();
    Task<ResultadoLayout> OrganizarAsync(PedidoLayout pedido, bool confirmado) => throw new ArgumentException("Organização indisponível.");
    QuadroTela Tela() => throw new InvalidOperationException("Transmissão indisponível neste PC.");
    QuadroTela Tela(string? monitorId) => Tela();
    ISessaoTela AbrirTransmissao(string? monitorId, OpcoesTransmissao opcoes) => throw new InvalidOperationException("Transmissão indisponível.");
}
public sealed class ControleRemotoIndisponivel : IControleRemoto
{
    public IReadOnlyList<AppInfo> Apps() => Array.Empty<AppInfo>();
    public Task<string> ExecutarAsync(string comando, string? id, bool confirmado) =>
        Task.FromResult("Controle do PC indisponível. Abra o Jarvis no computador.");
}
public sealed record PedidoComando(string Nome, string? Argumento, bool Confirmado = false);
