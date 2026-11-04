using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Server;
namespace Jarvis.Desktop.Services;

public sealed class ControleRemotoPc : IControleRemoto
{
    private static readonly Lazy<Jarvis.Voice.DictationService> DitadoMobile = new(() =>
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null) { var modelo = System.IO.Path.Combine(dir.FullName, "models", "ggml-base.bin"); if (System.IO.File.Exists(modelo)) return new(modelo); dir = dir.Parent; }
        throw new InvalidOperationException("Modelo Whisper local não encontrado no PC.");
    });
    public Task<string> TranscreverAsync(byte[] pcm, CancellationToken ct) => Task.Run(() => DitadoMobile.Value.TranscreverAsync(pcm, ct), ct);
    public EstadoEntrada Entrada() => new(ServicosAplicacao.Entrada.Permitida && new Permissoes().Obter(TipoAcao.ControlarJanela) != Permissao.Negado, "Ative Compartilhar tela e Permitir controle por toque em Dispositivos no PC.");
    public Task ToqueAsync(PedidoToque pedido, CancellationToken ct)
    {
        if (!Entrada().Permitida) throw new InvalidOperationException(Entrada().Mensagem);
        return ServicosAplicacao.Entrada.ExecutarAsync(pedido, ct);
    }
    public IReadOnlyList<Compromisso> Agenda() => ServicosAplicacao.Agenda.Listar();
    public Compromisso SalvarCompromisso(Compromisso item) => ServicosAplicacao.Agenda.Salvar(item);
    public Task<EstadoMidia> MidiaAsync() => _midia.ObterEstadoAsync();
    private readonly Lazy<IAppController> _apps = new(() => ServicosAplicacao.Apps);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IMediaController _midia = ServicosAplicacao.Midia;
    private readonly IWindowController _janelas = ServicosAplicacao.Janelas;
    public IReadOnlyList<MonitorInfo> Monitores() => _janelas.ListarMonitores();
    public QuadroTela Tela() => ServicosAplicacao.Tela.Capturar();
    public QuadroTela Tela(string? monitorId) => ServicosAplicacao.Tela.Capturar(monitorId);
    public ISessaoTela AbrirTransmissao(string? monitorId, OpcoesTransmissao opcoes) => ServicosAplicacao.Tela.AbrirSessao(monitorId, opcoes);
    public async Task<ResultadoLayout> OrganizarAsync(PedidoLayout pedido, bool confirmado)
    {
        var p = new Permissoes().Obter(TipoAcao.ControlarJanela);
        if (p == Permissao.Negado) throw new ArgumentException("Controle de janelas bloqueado em Segurança.");
        if (p == Permissao.Confirmar && !confirmado) throw new ArgumentException("Confirme a organização das janelas.");
        await _gate.WaitAsync();
        try { return await Task.Run(() => _janelas.Organizar(pedido)); }
        finally { _gate.Release(); }
    }
    public IReadOnlyList<JanelaRemota> Janelas() => _janelas.ListarJanelas().Select(j => new JanelaRemota((long)j.Handle, j.Titulo, j.App)).ToArray();
    public Task<TelemetriaPc> TelemetriaAsync() => ServicosAplicacao.Telemetria.ObterAsync();
    public IReadOnlyList<AppInfo> Apps() => _apps.Value.ListarApps();
    public IReadOnlyList<AppInfo> AppsDisponiveis() => _apps.Value.ListarDisponiveis();
    public void SelecionarApps(string[] ids) => _apps.Value.SelecionarApps(ids);
    public async Task<string> ExecutarAsync(string comando, string? id, bool confirmado)
    {
        var result = await ExecutarInternoAsync(comando, id, confirmado);
        return PersonalizacaoVoz.Resposta(result, ConfiguracaoApp.NomeUsuario);
    }
    private async Task<string> ExecutarInternoAsync(string comando, string? id, bool confirmado)
    {
        await _gate.WaitAsync();
        try
        {
            if (comando == "ler_desempenho")
            {
                var t = await TelemetriaAsync(); return $"CPU {t.Cpu:0}% · RAM {t.Memoria:0}% · GPU {t.Gpu:0}% · Disco {t.Disco:0}%";
            }
            if (comando == "falar") return "Use os controles do aplicativo para enviar ações ao PC. Texto livre remoto não é executado.";
            if (comando.StartsWith("midia_"))
            {
                var acao = comando switch { "midia_pausar" => ComandoMidia.Pausar, "midia_continuar" => ComandoMidia.Continuar, "midia_proxima" => ComandoMidia.Proxima, "midia_anterior" => ComandoMidia.Anterior, "midia_alternar" => ComandoMidia.Alternar, _ => throw new ArgumentException("Controle de mídia inválido.") };
                return await _midia.ExecutarAsync(acao) ? "Controle enviado ao Spotify." : "Abra o Spotify e inicie uma música no PC.";
            }
            if (comando == "volume")
            {
                if (!int.TryParse(id, out var volume) || volume is < 0 or > 100) throw new ArgumentException("Volume deve estar entre 0 e 100.");
                _midia.DefinirVolume(volume); return $"Volume em {volume}%.";
            }
            if (comando is "bloquear" or "suspender" or "reiniciar")
            {
                var p = new Permissoes().Obter(TipoAcao.ReiniciarPc);
                if (p == Permissao.Negado) return "Energia bloqueada em Segurança.";
                if (!confirmado) return "Confirme a ação no celular antes de continuar.";
                Jarvis.Sistema.EnergiaController.Executar(comando);
                return "Ação de energia solicitada.";
            }
            if (comando is "layout" or "minimizar_janela" or "maximizar_janela" or "focar_janela" or "fechar_janela")
            {
                var p = new Permissoes().Obter(TipoAcao.ControlarJanela);
                if (p == Permissao.Negado) return "Controle de janelas bloqueado em Segurança.";
                if (comando == "fechar_janela" && new Permissoes().Obter(TipoAcao.FecharApp) == Permissao.Negado) return "Fechamento bloqueado em Segurança.";
                if (p == Permissao.Confirmar && !confirmado) return "Confirme a ação no celular antes de continuar.";
                if (comando == "fechar_janela" && !confirmado) return "Confirme o fechamento desta janela no celular.";
                if (comando == "layout")
                {
                    if (id is not ("Metade" or "TresColunas" or "FocoLateral" or "Grade2x2")) throw new ArgumentException("Layout inválido.");
                    _janelas.AplicarLayout(id);
                }
                else
                {
                    if (!long.TryParse(id, out var h) || !Janelas().Any(j => j.Id == h)) throw new ArgumentException("Janela indisponível. Atualize a lista.");
                    if (comando == "minimizar_janela") _janelas.Minimizar((nint)h);
                    else if (comando == "maximizar_janela") _janelas.Maximizar((nint)h);
                    else if (comando == "fechar_janela") _janelas.Fechar((nint)h);
                    else _janelas.TrazerParaFrente((nint)h);
                }
                return "Janelas atualizadas.";
            }
            if (comando is not ("abrir_app" or "fechar_app")) throw new ArgumentException("Comando não permitido.");
            var app = Apps().FirstOrDefault(a => a.Id == id);
            if (app is null) return "Aplicativo não encontrado no catálogo do PC.";
            var tipo = comando == "abrir_app" ? TipoAcao.AbrirApp : TipoAcao.FecharApp;
            var permissao = new Permissoes().Obter(tipo);
            if (permissao == Permissao.Negado) return "Ação bloqueada nas configurações de Segurança do PC.";
            if ((permissao == Permissao.Confirmar || comando == "fechar_app" || app.EhJogo) && !confirmado)
                return "Confirme a ação no celular antes de continuar.";
            await Task.Run(() => { if (comando == "abrir_app") _apps.Value.Abrir(app.Id); else _apps.Value.Fechar(app.Id); });
            return comando == "abrir_app" ? $"Abrindo {app.Nome}." : $"Solicitei o fechamento de {app.Nome}.";
        }
        finally { _gate.Release(); }
    }
}
