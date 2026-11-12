using System.IO;
using Jarvis.Ai;
using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Voice;

namespace Jarvis.Desktop.Services;

public sealed class MotorVoz : IDisposable
{
    private readonly VoiceService _voz = new();
    private readonly WakeWordListener _wake;
    private readonly PiperTts _tts;
    private readonly DictationService _ditado;
    private readonly string _pastaModelos;
    private readonly IEmailService _email;
    private readonly IMediaController _midia;
    private IAppController? _apps;
    private readonly CancellationTokenSource _vida = new();
    private readonly object _falaGate = new();
    private CancellationTokenSource? _fala;
    private int _ocupado;
    private bool _microfonePronto;
    private (AppInfo App, DateTime Ate)? _confirmacao;
    private (AppInfo App, DateTime Ate)? _contextoApp;

    private readonly SemaphoreSlim _micGate = new(1, 1);
    private readonly SemaphoreSlim _execucaoComando = new(1, 1);
    public event EventHandler<ComandoProcessado>? ComandoConcluido;
    private bool _aguardandoComplemento;
    public event EventHandler<string>? Transcricao;
    public event EventHandler<ReactorState>? EstadoMudou;
    public event EventHandler<string>? RespostaPronta;
    public event EventHandler<PageType>? SolicitouNavegacao;
    public double NivelAudio => _tts.Falando ? _tts.NivelAudio : _voz.NivelAudio;
    public VoiceService Voz => _voz;

    public MotorVoz(string pastaModelos, IEmailService? email = null, IMediaController? midia = null, IAppController? apps = null)
    {
        _pastaModelos = pastaModelos;
        _apps = apps;
        _email = email ?? new GoogleMailService();
        _midia = midia ?? new Jarvis.Sistema.MediaController();
        _wake = new WakeWordListener(_voz);
        _tts = new PiperTts(pastaModelos);
        _ditado = new DictationService(Path.Combine(pastaModelos, File.Exists(Path.Combine(pastaModelos, "ggml-base.bin")) ? "ggml-base.bin" : "ggml-tiny.bin"));
        _voz.FiltroMicrofone = ConfiguracaoApp.MicrofonePreferido;
        ConfiguracaoApp.ConfiguracoesAlteradas += AtualizarMicrofone;
        _tts.FalandoMudou += (_, _) =>
        {
            _wake.Suspender(_tts.Falando);
            EstadoMudou?.Invoke(this, _tts.Falando ? ReactorState.Falando : ReactorState.Repouso);
        };
        _wake.Parcial += (_, texto) => Transcricao?.Invoke(this, texto);
        _wake.AoOuvirJarvis += (_, _) => { EstadoMudou?.Invoke(this, ReactorState.Ouvindo); Transcricao?.Invoke(this, "Estou ouvindo. Pode falar o comando."); };
        _wake.SegmentoPronto += (_, segmento) => _ = Task.Run(() => ProcessarAudioAsync(segmento.Audio, segmento.VerificarAtivacao, segmento.TextoVosk));
        _wake.ComandoDetectado += (_, texto) =>
        {
            if (_tts.Falando && RoteadorIntencoes.Rotear(texto)?.Acao is "stop" or "cancel") { PararFala();  return; }
        };
    }
    internal static void RegistrarVoz(string tipo, string texto)
    {
        try
        {
            var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
            Directory.CreateDirectory(pasta);
            var log = Path.Combine(pasta, "voz.log");
            if (File.Exists(log) && new FileInfo(log).Length > 1_000_000) File.Move(log, log + ".old", true);
            File.AppendAllText(log, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{tipo}] {texto}{Environment.NewLine}");
        }
        catch { }
    }
    public void Iniciar()
    {
        RegistrarVoz("boot", "carregando Vosk");
        try
        {
            _wake.Carregar(Path.Combine(_pastaModelos, "vosk-model-small-pt-0.3"));
            _wake.Iniciar(); _microfonePronto = true;
            RegistrarVoz("boot", $"microfone aberto: {_voz.NomeMicrofone}; {_voz.FormatoMicrofone}");
        }
        catch (Exception ex)
        {
            RegistrarVoz("boot-erro", ex.GetType().Name);
            Transcricao?.Invoke(this, "Microfone indisponível. Verifique o dispositivo padrão do Windows.");
            throw;
        }
        EstadoMudou?.Invoke(this, ReactorState.Repouso);
    }
    public double Calibrar() => _voz.Calibrar(2);
    private async void AtualizarMicrofone(object? sender, EventArgs e)
    {
        if (_vida.IsCancellationRequested || _voz.FiltroMicrofone == ConfiguracaoApp.MicrofonePreferido) return;
        await _micGate.WaitAsync();
        try
        {
            if (_vida.IsCancellationRequested) return;
            await Task.Run(() => { _wake.Parar(); _voz.FiltroMicrofone = ConfiguracaoApp.MicrofonePreferido; _wake.LiberarComando(); _wake.Iniciar(); });
            _microfonePronto = true; Transcricao?.Invoke(this, "Escuta ativa em " + _voz.NomeMicrofone);
        }
        catch (Exception ex) { _microfonePronto = false; RegistrarVoz("microfone", ex.GetType().Name); Transcricao?.Invoke(this, "Não consegui abrir esse microfone. Selecione outro em Configurações."); }
        finally { _micGate.Release(); }
    }
    public void Falar(string texto) => _ = FalarAsync(texto, guardarCache: false);
    public async Task FalarAsync(string texto, bool guardarCache = true)
    {
        CancellationTokenSource fala;
        lock (_falaGate)
        {
            _fala?.Cancel();
            fala = CancellationTokenSource.CreateLinkedTokenSource(_vida.Token);
            _fala = fala;
        }
        RespostaPronta?.Invoke(this, texto);
        try
        {
            _tts.Velocidade = ConfiguracaoApp.VelocidadeVoz;
            await _tts.FalarAsync(texto, fala.Token, guardarCache);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            RegistrarVoz("tts-erro", ex.GetType().Name);
            Transcricao?.Invoke(this, "Não foi possível reproduzir a voz. Verifique os modelos e a saída de áudio.");
        }
        finally
        {
            lock (_falaGate) { if (ReferenceEquals(_fala, fala)) _fala = null; }
            fala.Dispose();
        }
    }
    public void PararFala() { lock (_falaGate) _fala?.Cancel(); }
    public void Interromper() { PararFala();  }
    public void DefinirEscuta(bool ligada) { _voz.DefinirMicrofoneLigado(ligada); _wake.LiberarComando(); EstadoMudou?.Invoke(this, ligada ? ReactorState.Repouso : ReactorState.Mudo); }
    public void AtivarEscutaPorTecla()
    {
        PararFala();
        if (!_microfonePronto) { Transcricao?.Invoke(this, "Microfone indisponível. Confira o dispositivo em Configurações."); return; }
        _wake.AtivarComando();
    }
    private async Task ProcessarAudioAsync(byte[] audio, bool verificarAtivacao = false, string textoVosk = "")
    {
        if (Interlocked.CompareExchange(ref _ocupado, 1, 0) != 0) return;
        var rearmar = false;
        try
        {
            if (!verificarAtivacao) EstadoMudou?.Invoke(this, ReactorState.Pensando);
            string texto;
            try { texto = await _ditado.TranscreverAsync(audio, _vida.Token); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { RegistrarVoz("whisper", ex.GetType().Name + ": " + ex.Message); texto = ""; }
            texto = EscolhaTranscricao.Selecionar(texto, textoVosk, ConfiguracaoApp.PalavraAtivacao, verificarAtivacao);
            if (verificarAtivacao && !WakeWordMatcher.AtivacaoNoInicio(texto, ConfiguracaoApp.PalavraAtivacao)) return;
            var comando = WakeWordMatcher.StripWakeWord(texto, ConfiguracaoApp.PalavraAtivacao).Trim(' ', '.', ',', '!', '?');
            if (string.IsNullOrWhiteSpace(comando))
            {
                rearmar = !_aguardandoComplemento && WakeWordMatcher.AtivacaoNoInicio(texto, ConfiguracaoApp.PalavraAtivacao);
                _aguardandoComplemento = rearmar;
                Transcricao?.Invoke(this, rearmar ? "Pode falar o comando. Estou ouvindo." : "Não captei o comando. Diga Jarvis e fale novamente."); return;
            }
            _aguardandoComplemento = false;
            RegistrarVoz("comando-local", comando);
            Transcricao?.Invoke(this, comando);
            await ExecutarComandoAsync(comando);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { RegistrarVoz("comando-erro", ex.GetType().Name + ": " + ex.Message); Transcricao?.Invoke(this, "Não consegui concluir o comando: " + ex.Message); }
        finally { Interlocked.Exchange(ref _ocupado, 0); _wake.LiberarComando(rearmar); if (rearmar && !_vida.IsCancellationRequested) _wake.AtivarComando(); else if (!_tts.Falando) EstadoMudou?.Invoke(this, ReactorState.Repouso); }
    }
    public async Task<string> ExecutarComandoAsync(string comando, bool falar = true, CancellationToken ct = default, string origem = "voz")
    {
        if (string.IsNullOrWhiteSpace(comando) || comando.Length > 4000) return "Use um comando de até 4.000 caracteres.";
        using var vida = CancellationTokenSource.CreateLinkedTokenSource(ct, _vida.Token);
        await _execucaoComando.WaitAsync(vida.Token);
        try {
            var resposta = await ExecutarInternoAsync(comando.Trim(), falar, vida.Token);
            ComandoConcluido?.Invoke(this, new(comando.Trim(), resposta, origem));
            RespostaPronta?.Invoke(this, resposta);
            return resposta;
        } finally { _execucaoComando.Release(); }
    }
    private async Task<string> ExecutarInternoAsync(string comando, bool falar, CancellationToken ct)
    {
        EstadoMudou?.Invoke(this, ReactorState.Pensando);
        string resposta;
        try
        {
            var normal = RoteadorIntencoes.Normalizar(comando);
            normal = System.Text.RegularExpressions.Regex.Replace(normal, @"^jarvis\s+", "");
            if (normal is "modo jogo" or "ativar modo jogo" or "ativar modo de jogo")
                return await FinalizarRespostaAsync((await ServicosAplicacao.Jogo.AtivarAsync(Array.Empty<int>())).Mensagem, falar);
            if (normal is "sair do modo jogo" or "desativar modo jogo" or "sair do modo de jogo" or "desativar modo de jogo")
                return await FinalizarRespostaAsync((await ServicosAplicacao.Jogo.DesativarAsync()).Mensagem, falar);
            if (normal is "agora minimize" or "minimize" or "minimize ele" or "agora maximize" or "maximize" or "maximize ele")
                return await FinalizarRespostaAsync(await AplicarContextoAsync(normal.Contains("maximize")), falar);
            if (normal is "cancelar" or "pare" or "parar") {  PararFala(); }
            var intencao = RoteadorIntencoes.Rotear(comando);
            if (intencao?.Acao is { } acao) resposta = await ExecutarAcaoAsync(acao, intencao.Alvo);
            else if (intencao?.Resposta is { } pronta) resposta = pronta;
            else resposta = AssistenteLocal.Responder(comando);
            if (intencao?.Acao != "read_inbox") resposta = PersonalizacaoVoz.Resposta(resposta, ConfiguracaoApp.NomeUsuario);
            if (falar) await FalarAsync(resposta, intencao?.Acao != "read_inbox");
            return resposta;
        }
        catch (OperationCanceledException) { return "Comando cancelado."; }
        catch (Exception ex)
        {
            resposta = "Não consegui concluir: " + ex.Message;
            if (falar) await FalarAsync(resposta);
            return resposta;
        }
        finally { if (!_tts.Falando) EstadoMudou?.Invoke(this, ReactorState.Repouso); }
    }
    private async Task<string> ExecutarAcaoAsync(string acao, string? alvo)
    {
        if (acao == "stop") { PararFala(); _confirmacao = null; return "Certo, parei."; }
        if (acao == "cancel") { _confirmacao = null; return "Cancelado."; }
        if (acao == "confirm")
        {
            var pedido = _confirmacao; _confirmacao = null;
            if (pedido is null || pedido.Value.Ate < DateTime.UtcNow) return "Não há confirmação pendente. Peça a ação novamente.";
            if (new Permissoes().Obter(TipoAcao.FecharApp) == Permissao.Negado) return "Fechar aplicativos está bloqueado em Segurança.";
            _apps!.Fechar(pedido.Value.App.Id); return $"Solicitei o fechamento de {pedido.Value.App.Nome}.";
        }
        if (acao == "read_inbox") return ResumoCaixaEntrada.ParaFala(await _email.SincronizarAsync(_vida.Token));
        if (acao == "media_status")
        {
            var status = await _midia.ObterEstadoAsync();
            return status.Disponivel ? $"{status.Titulo}, de {status.Artista}." : "Abra o Spotify e inicie uma música para conectar os controles.";
        }
        if (acao.StartsWith("media_"))
        {
            var c = acao switch { "media_pause" => ComandoMidia.Pausar, "media_play" => ComandoMidia.Continuar, "media_next" => ComandoMidia.Proxima, _ => ComandoMidia.Anterior };
            return await _midia.ExecutarAsync(c) ? c switch
            { ComandoMidia.Pausar => "Música pausada.", ComandoMidia.Continuar => "Continuando a música.", ComandoMidia.Proxima => "Próxima faixa.", _ => "Faixa anterior." }
                : "O Spotify não disponibilizou esse controle. Abra o player e inicie uma música.";
        }
        if (acao == "set_volume")
        {
            var valor = int.Parse(alvo!);
            if (valor > 100) return "Escolha um volume entre zero e cem.";
            _midia.DefinirVolume(valor); return $"Volume em {valor} por cento.";
        }
        if (acao == "perf_status") { SolicitouNavegacao?.Invoke(this, PageType.Desempenho); return "Abrindo o desempenho do computador."; }
        if (acao is "open_app" or "close_app")
        {
            var tipo = acao == "open_app" ? TipoAcao.AbrirApp : TipoAcao.FecharApp;
            var permissao = new Permissoes().Obter(tipo);
            if (permissao == Permissao.Negado) return "Essa ação está bloqueada em Segurança.";
            _apps ??= new Jarvis.Sistema.AppController();
            var lista = _apps.ListarApps();
            var app = ResolverAplicativo.Encontrar(lista, alvo ?? "", aproximar: acao == "open_app");
            if (app is null) return $"Não encontrei o aplicativo {alvo}.";
            if (acao == "open_app")
            {
                if (app.EhJogo || permissao == Permissao.Confirmar) return "Esse aplicativo exige confirmação. Abra-o pela página Apps e confirme a ação.";
                _apps.Abrir(app.Id); _contextoApp = (app, DateTime.UtcNow.AddSeconds(60)); return $"Abrindo {app.Nome}.";
            }
            if (app.EhJogo || permissao == Permissao.Confirmar)
            {
                _confirmacao = (app, DateTime.UtcNow.AddSeconds(30));
                return $"Fechar {app.Nome} pode perder trabalho não salvo. Diga Jarvis, confirmar em até trinta segundos, ou cancelar.";
            }
            _apps.Fechar(app.Id); return $"Solicitei o fechamento de {app.Nome}.";
        }
        return "Esse comando ainda não está disponível.";
    }
    public void ResponderHora() => Falar($"São {DateTime.Now.Hour} horas e {DateTime.Now.Minute} minutos.");
    private async Task<string> FinalizarRespostaAsync(string resposta, bool falar) { resposta = PersonalizacaoVoz.Resposta(resposta, ConfiguracaoApp.NomeUsuario); if (falar) await FalarAsync(resposta, false); return resposta; }
    private async Task<string> AplicarContextoAsync(bool maximizar)
    {
        var context = _contextoApp;
        if (context is null || context.Value.Ate < DateTime.UtcNow) return "Abra um aplicativo primeiro. O contexto de janela dura um minuto.";
        var permission = new Permissoes().Obter(TipoAcao.ControlarJanela);
        if (permission != Permissao.Sozinho) return "Use a página Apps para controlar a janela com a confirmação exigida em Segurança.";
        return await Task.Run(() =>
        {
            var controller = new Jarvis.Sistema.WindowController();
            var windows = controller.ListarJanelas().Where(w => w.Titulo.Contains(context.Value.App.Nome, StringComparison.OrdinalIgnoreCase) || w.App.Equals(context.Value.App.Id, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (windows.Length != 1) return "Não identifiquei uma única janela desse aplicativo. Escolha a janela na página Apps.";
            if (maximizar) controller.Maximizar(windows[0].Handle); else controller.Minimizar(windows[0].Handle);
            return maximizar ? "Janela maximizada." : "Janela minimizada.";
        });
    }
    public void Dispose()
    {
        ConfiguracaoApp.ConfiguracoesAlteradas -= AtualizarMicrofone;
        _vida.Cancel(); PararFala(); _wake.Dispose(); _tts.Dispose(); _voz.Dispose(); _ditado.Dispose();
    }
}


