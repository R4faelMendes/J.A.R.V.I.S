namespace Jarvis.Core;

public sealed record TelemetriaPc(double? Cpu, double? Memoria, double? Gpu, double? Disco, double? Temperatura, DateTimeOffset Instante, ulong? MemoriaUsadaBytes = null, ulong? MemoriaTotalBytes = null);
public sealed record IdentidadePc(string Nome, string Mac, string Broadcast, string[]? Enderecos = null, string? EnderecoRemoto = null);
public sealed record EstadoCelular(string Nome, int? Bateria, string Conexao);
public sealed record JanelaRemota(long Id, string Titulo, string App);
public sealed record ResultadoDiagnostico(string Nome, bool Ok, string Detalhe);
public interface IDiagnosticoService { Task<IReadOnlyList<ResultadoDiagnostico>> VerificarAsync(CancellationToken ct = default); }
public interface IAudioDispositivos { IReadOnlyList<string> ListarMicrofones(); }
public sealed record PareamentoQr(string Codigo, byte[] Png, string Ip, int Porta, DateTimeOffset Expira);
public sealed record InstalacaoMobile(string Url, byte[] Png, string? Alternativa = null);
public interface IConexaoCelular
{
    string ArquivoApk { get; }
    string? CelularOnline { get; }
    Task<PareamentoQr> CriarPareamentoAsync(CancellationToken ct = default);
    InstalacaoMobile ObterInstalacao() => throw new InvalidOperationException("Instalador indisponível.");
    byte[] ObterQrInstalacao(string url) => throw new InvalidOperationException("QR indisponível.");
}
public sealed record PassoRotina(string Comando, string? Argumento = null);
public sealed record ProgressoRotina(int Atual, int Total, string Mensagem, bool Concluido);
public interface IRotinaService
{
    Task ExecutarAsync(string passosJson, IProgress<ProgressoRotina> progresso, CancellationToken ct);
}
public sealed record RotinaDefinida(long Id, string Nome, string Gatilho, string Acoes, bool Ativa);
public interface ICatalogoRotinas
{
    IReadOnlyList<RotinaDefinida> Listar();
    void Adicionar(string nome, string gatilho, string acoes);
    void Alternar(long id, bool ativa);
}
public sealed record PreferenciaPessoal(string Chave, string Valor);
public interface IMemoriaPreferencias
{
    bool Ativa { get; set; }
    IReadOnlyList<PreferenciaPessoal> Listar();
    void Salvar(string chave, string valor);
    void Apagar(string chave);
    void Limpar();
}
