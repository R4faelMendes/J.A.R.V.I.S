namespace Jarvis.Core;

public enum ComandoMidia { Pausar, Continuar, Alternar, Proxima, Anterior }
public sealed record EstadoMidia(bool Disponivel, string Titulo, string Artista, string Aplicativo, bool Tocando, byte[]? Capa = null, double PosicaoSegundos = 0, double DuracaoSegundos = 0);
public interface IMediaController
{
    Task<EstadoMidia> ObterEstadoAsync();
    Task<bool> ExecutarAsync(ComandoMidia comando);
    void DefinirVolume(int porcentagem);
}

public sealed record EmailResumo(string Id, string Remetente, string Assunto, string Previa, DateTimeOffset Data, bool NaoLido);
public sealed record CaixaEntrada(IReadOnlyList<EmailResumo> Mensagens, int Total, int NaoLidos);
public interface IEmailService
{
    bool Configurado { get; }
    bool Conectado { get; }
    Task ConfigurarClienteAsync(string arquivoJson);
    Task ConectarAsync(CancellationToken ct = default);
    Task<CaixaEntrada> SincronizarAsync(CancellationToken ct = default);
    Task DesconectarAsync(CancellationToken ct = default);
    Task ConectarSenhaAppAsync(string email, string senhaApp, CancellationToken ct = default) => throw new NotSupportedException("Este provedor não oferece conexão por senha de app.");
}

public static class ResumoCaixaEntrada
{
    public static string ParaFala(CaixaEntrada caixa, int limite = 5)
    {
        if (caixa.Total == 0) return "Sua caixa de entrada está vazia.";
        var mensagens = caixa.Mensagens.OrderByDescending(m => m.Data).Take(Math.Clamp(limite, 1, 10)).ToArray();
        var inicio = $"Você tem {caixa.Total} mensagens na caixa de entrada, sendo {caixa.NaoLidos} não lidas. ";
        if (mensagens.Length == 0) return inicio;
        return inicio + $"Vou ler os {mensagens.Length} assuntos mais recentes. " +
            string.Join(" ", mensagens.Select((m, i) => $"{i + 1}. De {m.Remetente}. Assunto: {m.Assunto}."));
    }
}
