// JARVIS.Mobile — conexão com o PC: pareamento, pinning, SignalR, Wake-on-LAN.
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using Jarvis.Core;
using System.Text.Json;

namespace Jarvis.Mobile.Services;

/// <summary>Fala com o servidor do PC. Token no SecureStorage, pinning da impressão.</summary>
public sealed class ConexaoPc
{
    public async Task EscolherTemaAsync(string id)
    {
        using var response = await (_http ?? throw new InvalidOperationException("Conecte ao PC para mudar o tema dos dois apps.")).PostAsJsonAsync("/api/tema", new EstadoTema(id, 0));
        response.EnsureSuccessStatusCode(); var estado = await response.Content.ReadFromJsonAsync<EstadoTema>(); if (estado is not null) await MainThread.InvokeOnMainThreadAsync(() => TemasJarvis.Sincronizar(estado));
    }
    public async Task<string> TranscreverAudioAsync(byte[] pcm, CancellationToken ct)
    {
        var client = _http ?? throw new InvalidOperationException("Conecte ao PC antes de falar.");
        using var content = new ByteArrayContent(pcm); content.Headers.ContentType = new("audio/pcm");
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(client.BaseAddress!, "/api/voz/transcrever")) { Content = content };
        // A primeira carga do modelo é mais demorada que uma requisição de telemetria.
        using var handler = _fabricaHandler!(); using var voz = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(90) };
        request.Headers.Add("X-Jarvis-Token", _token);
        using var response = await voz.SendAsync(request, ct); var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(body?.GetValueOrDefault("mensagem") ?? "O PC não conseguiu transcrever. Confira a conexão e tente novamente.");
        return body?.GetValueOrDefault("texto") ?? "";
    }
    public ConexaoPc() { Connectivity.Current.ConnectivityChanged += (_, _) => { if (_pin is not null) _ = VerificarAutomaticamenteAsync(); }; }
    private readonly SemaphoreSlim _reconexao = new(1, 1);
    private readonly SemaphoreSlim _carregamento = new(1, 1);
    private string[] _enderecos = [];
    public string UltimoDiagnostico { get; private set; } = "Aguardando verificação.";
    public bool Verificando { get; private set; }
    public async Task CarregarSeNecessarioAsync() { if (_http is null) await CarregarAsync(); }
    public string IdentificadorPareamento => _pin is null ? "local" : _pin[..Math.Min(16, _pin.Length)];
    public async Task<List<Compromisso>> ListarAgendaAsync() => await (_http ?? throw new InvalidOperationException("Pareie primeiro.")).GetFromJsonAsync<List<Compromisso>>("/api/agenda") ?? [];
    public async Task<Compromisso> SalvarCompromissoAsync(Compromisso item)
    {
        using var response = await (_http ?? throw new InvalidOperationException("Pareie primeiro.")).PostAsJsonAsync("/api/agenda", item);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict) throw new ConflitoAgendaException();
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) { var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(); throw new ArgumentException(error?.GetValueOrDefault("mensagem") ?? "Revise as datas do compromisso."); }
        response.EnsureSuccessStatusCode(); return await response.Content.ReadFromJsonAsync<Compromisso>() ?? throw new InvalidOperationException("Compromisso inválido.");
    }
    public async Task<EstadoMidia> MidiaAsync(CancellationToken ct = default) => await (_http ?? throw new InvalidOperationException("Pareie primeiro.")).GetFromJsonAsync<EstadoMidia>("/api/midia", ct) ?? new(false, "Spotify indisponível", "", "Spotify", false);
    public async Task<EstadoEntrada> EntradaAsync() => await (_http ?? throw new InvalidOperationException("Pareie primeiro.")).GetFromJsonAsync<EstadoEntrada>("/api/tela/entrada") ?? new(false, "Atualize o Jarvis no PC.");
    public async Task<OrdemAparelho[]> PulsoAparelhoAsync(DadosAparelho dados, CancellationToken ct)
    {
        using var response = await (_http ?? throw new InvalidOperationException("Pareie primeiro.")).PostAsJsonAsync("/api/celular/pulso", dados, ct);
        response.EnsureSuccessStatusCode(); return await response.Content.ReadFromJsonAsync<OrdemAparelho[]>(cancellationToken: ct) ?? [];
    }
    public async Task ConfirmarAparelhoAsync(ConfirmacaoAparelho ack, CancellationToken ct)
    { using var response = await (_http ?? throw new InvalidOperationException("Pareie primeiro.")).PostAsJsonAsync("/api/celular/confirmar", ack, ct); response.EnsureSuccessStatusCode(); }
    public async Task<Guid> IniciarEspelhoCelularAsync(CancellationToken ct)
    {
        using var response = await (_http ?? throw new InvalidOperationException("Pareie primeiro.")).PostAsync("/api/celular/tela/iniciar", null, ct); response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessaoEspelho>(cancellationToken: ct) ?? throw new InvalidOperationException("Sessão inválida.")).Id;
    }
    public async Task EnviarQuadroCelularAsync(Guid sessao, byte[] jpeg, string pareamento, CancellationToken ct, int rotacao = 0)
    {
        var client = _http ?? throw new InvalidOperationException("Pareie primeiro.");
        if (IdentificadorPareamento != pareamento) throw new InvalidOperationException("Pareamento alterado. Autorize uma nova transmissão.");
        using var content = new ByteArrayContent(jpeg); content.Headers.ContentType = new("image/jpeg");
        using var response = await client.PostAsync("/api/celular/tela/quadro?sessao=" + sessao + "&rotacao=" + rotacao, content, ct);
        response.EnsureSuccessStatusCode();
    }
    public async Task PararEspelhoCelularAsync(Guid sessao)
    { using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(3)); using var response = await (_http ?? throw new InvalidOperationException()).PostAsJsonAsync("/api/celular/tela/parar", new SessaoEspelho(sessao), ct.Token); }
    public async Task<EntradaCelularPendente[]> PuxarEntradasCelularAsync(Guid sessao, string pareamento, CancellationToken ct)
    {
        if (IdentificadorPareamento != pareamento) throw new InvalidOperationException("Pareamento alterado.");
        return await (_http ?? throw new InvalidOperationException()).GetFromJsonAsync<EntradaCelularPendente[]>("/api/celular/tela/entradas?sessao=" + sessao, ct) ?? [];
    }
    public async Task ConfirmarEntradaCelularAsync(ConfirmacaoEntradaCelular ack, string pareamento, CancellationToken ct)
    {
        if (IdentificadorPareamento != pareamento) throw new InvalidOperationException("Pareamento alterado.");
        using var r = await (_http ?? throw new InvalidOperationException()).PostAsJsonAsync("/api/celular/tela/entrada/confirmar", ack, ct); r.EnsureSuccessStatusCode();
    }
    public async Task ToqueAsync(PedidoToque pedido, CancellationToken ct)
    {
        pedido.Validar();
        using var response = await (_http ?? throw new InvalidOperationException("Pareie primeiro.")).PostAsJsonAsync("/api/tela/entrada", pedido, ct);
        if ((int)response.StatusCode == 429) throw new InvalidOperationException("Muitos gestos ao mesmo tempo. Aguarde um instante antes do próximo gesto.");
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);
            throw new InvalidOperationException(error?.GetValueOrDefault("mensagem") ?? "O PC recusou o gesto.");
        }
    }
    public async Task TransmitirTelaAsync(string monitor, OpcoesTransmissao opcoes, Func<byte[], Task> apresentar, CancellationToken ct)
    {
        opcoes.Validar();
        if (BaseUrl is null || _token is null || _pin is null) throw new InvalidOperationException("Pareie com o PC primeiro.");
        var pin = _pin;
        using var socket = new System.Net.WebSockets.ClientWebSocket();
        socket.Options.SetRequestHeader("X-Jarvis-Token", _token);
        socket.Options.RemoteCertificateValidationCallback = (_, cert, _, _) => cert is not null && Convert.ToHexString(SHA256.HashData(cert.GetRawCertData())).Equals(pin, StringComparison.OrdinalIgnoreCase);
        var uri = new UriBuilder(BaseUrl) { Scheme = "wss", Path = "/api/tela/stream", Query = $"monitor={Uri.EscapeDataString(monitor)}&fps={opcoes.Fps}&largura={opcoes.Largura}&qualidade={opcoes.Qualidade}" };
        try { await socket.ConnectAsync(uri.Uri, ct); }
        catch (System.Net.WebSockets.WebSocketException ex) { throw new InvalidOperationException("Não foi possível transmitir. Confira se o Jarvis está aberto no PC, se a conexão está ativa e se ambos os apps estão atualizados.", ex); }
        var buffer = new byte[64 * 1024];
        using var frame = new MemoryStream();
        while (!ct.IsCancellationRequested)
        {
            frame.SetLength(0);
            System.Net.WebSockets.WebSocketReceiveResult part;
            do
            {
                part = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (part.MessageType == System.Net.WebSockets.WebSocketMessageType.Close) throw new InvalidOperationException(part.CloseStatusDescription ?? "Transmissão encerrada no PC.");
                if (part.MessageType != System.Net.WebSockets.WebSocketMessageType.Binary || frame.Length + part.Count > 8 * 1024 * 1024) throw new InvalidOperationException("Quadro inválido.");
                frame.Write(buffer, 0, part.Count);
            } while (!part.EndOfMessage);
            await apresentar(frame.ToArray());
            await socket.SendAsync(new ArraySegment<byte>(new byte[] { 1 }), System.Net.WebSockets.WebSocketMessageType.Binary, true, ct);
        }
    }
    private const string ChaveToken = "jarvis_token";
    private const string ChaveBase = "jarvis_base";
    private const string ChavePin = "jarvis_pin";
    private const string ChaveRemota = "jarvis_endereco_remoto";
    private const string ChaveEnderecos = "jarvis_enderecos";
    public string? EnderecoRemoto { get; private set; }
    private HttpClient? _http;
    private string? _pin, _token;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _monitor;
    private int _versao;
    public IdentidadePc? Identidade { get; private set; }
    public TelemetriaPc? Telemetria { get; private set; }
    public event EventHandler<TelemetriaPc>? TelemetriaMudou;


    public string? BaseUrl { get; private set; }
    public bool Conectado { get; private set; }
    public event EventHandler<string>? EstadoMudou;

    public async Task CarregarAsync()
    {
        await _carregamento.WaitAsync();
        try
        {
        if (_http is not null) return;
        BaseUrl = await SecureStorage.GetAsync(ChaveBase);
        EnderecoRemoto = await SecureStorage.GetAsync(ChaveRemota);
        try { _enderecos = JsonSerializer.Deserialize<string[]>(await SecureStorage.GetAsync(ChaveEnderecos) ?? "[]") ?? []; } catch { _enderecos = []; }
        var pin = await SecureStorage.GetAsync(ChavePin);
        var token = await SecureStorage.GetAsync(ChaveToken);
        var identity = await SecureStorage.GetAsync("jarvis_identidade");
        try { if (identity is not null) Identidade = JsonSerializer.Deserialize<IdentidadePc>(identity); } catch { }
        if (BaseUrl is null || token is null || pin?.Length != 64) { UltimoDiagnostico = "Pareamento ausente ou incompleto. Leia o QR de pareamento do PC."; return; }
        if (EnderecosJarvis.Tailscale(BaseUrl)) BaseUrl = RedeDireta.Filtrar(_enderecos).FirstOrDefault() ?? Identidade?.Enderecos?.FirstOrDefault(e => !EnderecosJarvis.Tailscale(e));
        if (BaseUrl is null) return;
        MontarHttp(pin ?? "", token);
        await TestarAsync();
        if (!Conectado) await VerificarAutomaticamenteAsync();
        }
        finally { _carregamento.Release(); }
    }

    private Func<HttpMessageHandler>? _fabricaHandler;

    private void MontarHttp(string pinEsperado, string token)
    {
        _versao++;
        if (_pin is not null && !_pin.Equals(pinEsperado, StringComparison.OrdinalIgnoreCase)) { Identidade = null; SecureStorage.Remove("jarvis_identidade"); }
        _pin = pinEsperado; _token = token;
        _fabricaHandler = () => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, cadeia, erros) =>
            {
                if (cert is null) return false;
                var imp = Convert.ToHexString(SHA256.HashData(cert.RawData));
                return imp.Equals(pinEsperado, StringComparison.OrdinalIgnoreCase);
            }
        };
        var handler = (HttpClientHandler)_fabricaHandler();
        var antigo = _http; _http = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl!), Timeout = TimeSpan.FromSeconds(5) };
        if (antigo is not null) _ = LiberarClienteAsync(antigo);
        _http.DefaultRequestHeaders.Add("X-Jarvis-Token", token);
    }
    private static async Task LiberarClienteAsync(HttpClient client) { await Task.Delay(TimeSpan.FromSeconds(15)); client.Dispose(); }
    private async Task AprenderEnderecosAsync(IdentidadePc identity)
    {
        var learned = RedeDireta.Filtrar(EnderecosJarvis.Reunir([BaseUrl, EnderecoRemoto], _enderecos, identity.Enderecos ?? [], [identity.EnderecoRemoto]));
        var remote = identity.EnderecoRemoto is { } externo && EnderecosJarvis.Normalizar(externo) is { } atual && !EnderecosJarvis.Tailscale(atual) ? atual : learned.FirstOrDefault(RedeDireta.Publico);
        if (remote is not null && remote != EnderecoRemoto) { EnderecoRemoto = remote; await SecureStorage.SetAsync(ChaveRemota, remote); }
        if (!_enderecos.SequenceEqual(learned)) { _enderecos = learned; await SecureStorage.SetAsync(ChaveEnderecos, JsonSerializer.Serialize(learned)); }
    }
    private Task AprenderPareamentoAsync(JsonElement root)
    {
        var urls = root.TryGetProperty("enderecos", out var addresses) && addresses.ValueKind == JsonValueKind.String ? addresses.GetString()?.Split('|', StringSplitOptions.RemoveEmptyEntries) : [];
        var remote = root.TryGetProperty("remoto", out var vpn) && vpn.ValueKind == JsonValueKind.String ? vpn.GetString() : null;
        return AprenderEnderecosAsync(new IdentidadePc("", "", "", urls, remote));
    }

    /// <summary>Pareia via QR (url|token|pin vindos do código escaneado).</summary>
    public async Task<bool> ParearAsync(string ip, int porta, string tokenUnico, string pin, string nome, IEnumerable<string>? candidatos = null)
    {
        EspelhoCelular.Parar(); DispositivoCompanheiro.Parar(true);
        Exception? erro = null;

        var destinos = new[] { ip }.Concat(candidatos ?? []).Where(c => System.Net.IPAddress.TryParse(c, out _)).Distinct().Where(c => !EnderecosJarvis.Tailscale(new UriBuilder("https", c, porta).Uri.ToString())).Take(8);
        foreach (var candidato in destinos)
        {
            try { if (await ParearEnderecoAsync(candidato, porta, tokenUnico, pin, nome)) { IniciarMonitor(); return true; } }
            catch (HttpRequestException ex) { erro = ex; }
            catch (TaskCanceledException ex) { erro = ex; }
        }
        if (erro is not null) throw new InvalidOperationException("Não alcancei o PC pelos endereços do QR. Confira se o Jarvis está aberto e se há Wi-Fi local ou endereço público acessível. Use Conexão automática para ver o diagnóstico.", erro);
        return false;
    }
    private async Task<bool> ParearEnderecoAsync(string ip, int porta, string tokenUnico, string pin, string nome)
    {
        if (!System.Net.IPAddress.TryParse(ip, out _) || porta is < 1 or > 65535 || pin.Length != 64 || !pin.All(Uri.IsHexDigit)) throw new ArgumentException("QR de pareamento inválido.");
        var endereco = new UriBuilder("https", ip, porta).Uri.GetLeftPart(UriPartial.Authority);
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, cadeia, erros) =>
                cert is not null && Convert.ToHexString(SHA256.HashData(cert.RawData))
                    .Equals(pin, StringComparison.OrdinalIgnoreCase)
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(endereco), Timeout = TimeSpan.FromSeconds(4) };
        using var r = await http.PostAsJsonAsync("/api/parear/confirmar", new { token = tokenUnico, nome });
        if (!r.IsSuccessStatusCode) return false;
        using var corpo = await JsonDocument.ParseAsync(await r.Content.ReadAsStreamAsync());
        if (!corpo.RootElement.TryGetProperty("token", out var tok) || tok.GetString() is not { } duradouro) return false;
        if (_pin is not null && _pin != pin) { _enderecos = []; EnderecoRemoto = null; SecureStorage.Remove(ChaveRemota); }
        BaseUrl = endereco;
        await SecureStorage.SetAsync(ChaveBase, BaseUrl);
        await SecureStorage.SetAsync(ChavePin, pin);
        await SecureStorage.SetAsync(ChaveToken, duradouro);
        MontarHttp(pin, duradouro);
        await AprenderPareamentoAsync(corpo.RootElement);
        await TestarAsync(true);
        return Conectado;
    }

    /// <summary>Pareia via código PIN de 6 dígitos + IP.</summary>
    public async Task<bool> ParearPorCodigoAsync(string ip, int porta, string codigo, string nome)
    {
        EspelhoCelular.Parar(); DispositivoCompanheiro.Parar(true);
        if (!System.Net.IPAddress.TryParse(ip, out _) || porta is < 1 or > 65535 || codigo.Length != 6 || !codigo.All(char.IsAsciiDigit)) throw new ArgumentException("Digite um IP válido e o código de seis dígitos.");
        var endereco = new UriBuilder("https", ip, porta).Uri.GetLeftPart(UriPartial.Authority);
        string? observedPin = null;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
            {
                if (cert is null) return false;
                var fingerprint = Convert.ToHexString(SHA256.HashData(cert.RawData));
                observedPin ??= fingerprint; return observedPin == fingerprint;
            }
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(endereco), Timeout = TimeSpan.FromSeconds(5) };
        using var r = await http.PostAsJsonAsync("/api/parear/confirmar-codigo", new { codigo, nome });
        if (!r.IsSuccessStatusCode) return false;
        using var corpo = await JsonDocument.ParseAsync(await r.Content.ReadAsStreamAsync());
        if (!corpo.RootElement.TryGetProperty("token", out var tok) || tok.GetString() is not { } duradouro) return false;
        var pin = corpo.RootElement.GetProperty("impressao").GetString() ?? "";
        if (observedPin is null || !observedPin.Equals(pin, StringComparison.OrdinalIgnoreCase)) return false;
        if (_pin is not null && _pin != pin) { _enderecos = []; EnderecoRemoto = null; SecureStorage.Remove(ChaveRemota); }
        BaseUrl = endereco;
        await SecureStorage.SetAsync(ChaveBase, BaseUrl);
        await SecureStorage.SetAsync(ChavePin, pin);
        await SecureStorage.SetAsync(ChaveToken, duradouro);
        MontarHttp(pin, duradouro);
        await AprenderPareamentoAsync(corpo.RootElement);
        IniciarMonitor();
        await TestarAsync(true);
        return Conectado;
    }

    public Task TestarAsync() => TestarAsync(false);
    private async Task TestarAsync(bool aguardar)
    {
        if (aguardar) await _gate.WaitAsync();
        else if (!await _gate.WaitAsync(0)) return;
        var version = _versao;
        try
        {
            var client = _http;
            if (client is null) { Conectado = false; return; }
            using var identityRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identidade");
            try
            {
                if (DispositivoCompanheiro.LerBateria().Percentual is { } battery)
                    identityRequest.Headers.Add("X-Jarvis-Bateria", battery.ToString(System.Globalization.CultureInfo.InvariantCulture));
                identityRequest.Headers.Add("X-Jarvis-Conexao", Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.WiFi) ? "Wi-Fi" : Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.Cellular) ? "Dados moveis" : "Rede privada");
            }
            catch { }
            using var identityResponse = await client.SendAsync(identityRequest);
            if (identityResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized) { if (version == _versao) { Conectado = false; UltimoDiagnostico = "Token recusado pelo PC. Leia um novo QR de pareamento."; } return; }
            identityResponse.EnsureSuccessStatusCode();
            var identity = await identityResponse.Content.ReadFromJsonAsync<IdentidadePc>();
            if (version != _versao) return;
            Conectado = identity is not null;
            if (Conectado)
            {
                if (version != _versao) return;
                if (JsonSerializer.Serialize(identity) != JsonSerializer.Serialize(Identidade))
                {
                    Identidade = identity;
                    if (identity is not null) await SecureStorage.SetAsync("jarvis_identidade", JsonSerializer.Serialize(identity));
                }
                if (identity is not null) await AprenderEnderecosAsync(identity);
                try { var tema = await client.GetFromJsonAsync<EstadoTema>("/api/tema"); if (version == _versao && tema is not null) await MainThread.InvokeOnMainThreadAsync(() => TemasJarvis.Sincronizar(tema)); } catch { }
                UltimoDiagnostico = RedeDireta.Publico(BaseUrl) ? "Online · internet direta" : "Online · conexão direta";
                try { Telemetria = await client.GetFromJsonAsync<TelemetriaPc>("/api/telemetria"); if (version == _versao && Telemetria is not null) TelemetriaMudou?.Invoke(this, Telemetria); }
                catch { Telemetria = null; }
            }
        }
        catch { if (version == _versao) { Conectado = false; UltimoDiagnostico = "Endereço atual sem resposta. Verificando as alternativas…"; } }
        finally { _gate.Release(); }
        if (version == _versao) EstadoMudou?.Invoke(this, UltimoDiagnostico);
    }

    public void IniciarMonitor()
    {
        if (_monitor is not null) return;
        var cts = new CancellationTokenSource(); _monitor = cts; _ = RodarMonitorAsync(cts);
    }
    private async Task RodarMonitorAsync(CancellationTokenSource cts)
    {
        try { await MonitorarAsync(cts.Token); }
        finally { if (ReferenceEquals(_monitor, cts)) _monitor = null; cts.Dispose(); }
    }
    public void PararMonitor() { _monitor?.Cancel(); _monitor = null; }
    private async Task MonitorarAsync(CancellationToken ct)
    {
        var ultimaAgenda = DateTimeOffset.MinValue;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await TestarAsync();
                if (Conectado && DateTimeOffset.UtcNow - ultimaAgenda > TimeSpan.FromMinutes(1)) { ultimaAgenda = DateTimeOffset.UtcNow; await App.Agenda.SincronizarAsync(); }
                if (!Conectado) await VerificarAutomaticamenteAsync(ct);
                await Task.Delay(TimeSpan.FromSeconds(Conectado ? 3 : 10), ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { EstadoMudou?.Invoke(this, "Conexão interrompida: " + ex.Message); }
    }

    public async Task<SondagemPc[]> VerificarAutomaticamenteAsync(CancellationToken ct = default)
    {
        if (!await _reconexao.WaitAsync(0, ct)) return [];
        Verificando = true;
        try
        {

            var pin = _pin; var token = _token;
            if (pin is null || token is null) { UltimoDiagnostico = "Leia o QR de pareamento do PC uma vez."; return []; }
            UltimoDiagnostico = "Procurando o PC pareado no Wi-Fi e na internet direta…"; EstadoMudou?.Invoke(this, UltimoDiagnostico);
            var urls = RedeDireta.Filtrar(EnderecosJarvis.Reunir([BaseUrl, EnderecoRemoto], _enderecos, Identidade?.Enderecos ?? [], [Identidade?.EnderecoRemoto]));
            var results = (await ConexaoPareada.ProcurarAsync(urls, pin, token, ct)).ToList();
            if (!results.Any(r => r.Conectado))
            {
                var discovered = (await DescobrirAsync(1800)).Select(c => $"https://{c.Ip}:5189").Except(urls, StringComparer.OrdinalIgnoreCase);
                results.AddRange(await ConexaoPareada.ProcurarAsync(discovered, pin, token, ct));
            }
            var success = results.FirstOrDefault(r => r.Conectado);
            if (_pin != pin || _token != token) return [];
            if (success?.Identidade is { } identity)
            {
                await _gate.WaitAsync(ct);
                try
                {
                    if (_pin != pin || _token != token) return [];
                    BaseUrl = success.Endereco; await SecureStorage.SetAsync(ChaveBase, BaseUrl); MontarHttp(pin, token);
                    Identidade = identity; Conectado = true; await SecureStorage.SetAsync("jarvis_identidade", JsonSerializer.Serialize(identity)); await AprenderEnderecosAsync(identity);
                    UltimoDiagnostico = success.Mensagem;
                }
                finally { _gate.Release(); }
            }
            else
            {
                Conectado = false;
                UltimoDiagnostico = results.FirstOrDefault(r => r.Mensagem.Contains("Token recusado") || r.Mensagem.Contains("certificado"))?.Mensagem
                    ?? (EnderecoRemoto is null ? "PC sem resposta. Conecte no Wi-Fi uma vez para aprender o endereço direto do PC. " : "PC sem resposta nos endereços salvos. Verifique se a porta HTTPS está alcançável pela internet.");
            }
            EstadoMudou?.Invoke(this, UltimoDiagnostico); return results.ToArray();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { UltimoDiagnostico = "A verificação falhou temporariamente. O pareamento foi mantido; tentarei novamente."; return []; }
        finally { Verificando = false; _reconexao.Release(); }
    }

    public async Task<List<JanelaRemota>> ListarJanelasAsync() => _http is null ? throw new InvalidOperationException("Pareie primeiro.") : await _http.GetFromJsonAsync<List<JanelaRemota>>("/api/janelas") ?? new();
    public async Task<List<MonitorInfo>> ListarMonitoresAsync() => _http is null ? throw new InvalidOperationException("Pareie primeiro.") : await _http.GetFromJsonAsync<List<MonitorInfo>>("/api/monitores") ?? new();
    public async Task<ResultadoLayout> OrganizarAsync(PedidoLayout layout, bool confirmado)
    {
        var http = _http ?? throw new InvalidOperationException("Pareie primeiro.");
        using var response = await http.PostAsJsonAsync("/api/janelas/layout", new { layout, confirmado });
        if (!response.IsSuccessStatusCode) { var erro = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(); throw new InvalidOperationException(erro?.GetValueOrDefault("mensagem") ?? "Não foi possível organizar as janelas."); }
        return await response.Content.ReadFromJsonAsync<ResultadoLayout>() ?? throw new InvalidOperationException("Resposta de layout inválida.");
    }
    public async Task<byte[]> TelaAsync(CancellationToken ct, string? monitorId = null)
    {
        var http = _http ?? throw new InvalidOperationException("Pareie primeiro.");
        using var response = await http.GetAsync("/api/tela" + (monitorId is null ? "" : "?monitor=" + Uri.EscapeDataString(monitorId)), ct);
        if (!response.IsSuccessStatusCode) { var erro = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct); throw new InvalidOperationException(erro?.GetValueOrDefault("mensagem") ?? "Tela indisponível."); }
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
    public async Task AcordarAsync()
    {
        if (string.IsNullOrEmpty(Identidade?.Mac)) throw new InvalidOperationException("Pareie novamente para registrar o MAC do PC.");
        await AcordarPcAsync(Identidade.Mac, Identidade.Broadcast);
        EstadoMudou?.Invoke(this, "Pacote enviado · aguardando o PC");
    }

    public async Task<List<AppRemoto>> ListarAppsAsync()
    {
        if (_http is null) throw new InvalidOperationException("Pareie primeiro.");
        return await _http.GetFromJsonAsync<List<AppRemoto>>("/api/apps") ?? new();
    }
    public async Task<List<AppRemoto>> ListarDisponiveisAsync() => _http is null ? throw new InvalidOperationException("Pareie primeiro.") : await _http.GetFromJsonAsync<List<AppRemoto>>("/api/apps/disponiveis") ?? new();
    public async Task SalvarAppsAsync(string[] ids)
    { var http = _http ?? throw new InvalidOperationException("Pareie primeiro."); using var result = await http.PostAsJsonAsync("/api/apps/selecao", new { ids }); if (!result.IsSuccessStatusCode) { var erro = await result.Content.ReadFromJsonAsync<Dictionary<string, string>>(); throw new InvalidOperationException(erro?.GetValueOrDefault("mensagem") ?? "Não foi possível salvar a seleção."); } }
    public async Task<string> ComandoAsync(string nome, string? argumento = null, bool confirmado = false)
    {
        if (_http is null) throw new InvalidOperationException("Pareie primeiro.");
        using var resposta = await _http.PostAsJsonAsync("/api/comando", new { nome, argumento, confirmado });
        var corpo = await resposta.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        if (!resposta.IsSuccessStatusCode) throw new InvalidOperationException(corpo?.GetValueOrDefault("mensagem") ?? $"O PC respondeu HTTP {(int)resposta.StatusCode}.");
        return corpo?.GetValueOrDefault("mensagem") ?? "Sem resposta do PC.";
    }
    /// <summary>Redescoberta automática: UDP broadcast JARVIS_DISCOVER (porta 5187).</summary>
    public static async Task<List<(string Ip, string Nome)>> DescobrirAsync(int timeoutMs = 2500, CancellationToken ct = default)
        => (await DescobertaRede.ProcurarAsync(timeoutMs, ct)).Select(p => (p.Ip, p.Nome)).ToList();
    public void Esquecer()
    {
        EspelhoCelular.Parar(); DispositivoCompanheiro.Parar();
        MidiaNotificacao.Parar(); Notificacoes.Reagendar([]);
        _versao++;
        PararMonitor(); Identidade = null; Telemetria = null; _pin = _token = null;
        SecureStorage.Remove("jarvis_identidade");
        SecureStorage.Remove(ChaveToken);
        SecureStorage.Remove(ChaveBase);
        SecureStorage.Remove(ChavePin);
        SecureStorage.Remove(ChaveRemota); EnderecoRemoto = null;
        SecureStorage.Remove(ChaveEnderecos); _enderecos = [];
        _http?.Dispose(); _http = null; BaseUrl = null;
        Conectado = false;
        EstadoMudou?.Invoke(this, "PC removido");
    }

    public async Task ConectarEnderecoAsync(string endereco)
    {
        if (_pin is null || _token is null) throw new InvalidOperationException("Pareie com o PC na rede local primeiro.");
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length > 0 || uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0 || EnderecosJarvis.Tailscale(endereco)) throw new ArgumentException("Use o endereço HTTPS direto do PC, como https://[IPv6]:5189 ou seu domínio.");
        var pin = _pin; var token = _token;
        using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, cert, _, _) => cert is not null && Convert.ToHexString(SHA256.HashData(cert.RawData)).Equals(pin, StringComparison.OrdinalIgnoreCase) }) { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(5) };
        http.DefaultRequestHeaders.Add("X-Jarvis-Token", token);
        using var response = await http.GetAsync("/api/identidade"); response.EnsureSuccessStatusCode();
        if (_pin != pin || _token != token) return;
        BaseUrl = uri.GetLeftPart(UriPartial.Authority); await SecureStorage.SetAsync(ChaveBase, BaseUrl);
        EnderecoRemoto = BaseUrl; await SecureStorage.SetAsync(ChaveRemota, BaseUrl);
        if (_pin != pin || _token != token) return;
        MontarHttp(pin, token); await TestarAsync();
    }

    /// <summary>Wake-on-LAN: magic packet em broadcast, com o MAC do pareamento.</summary>
    public static async Task AcordarPcAsync(string mac, string broadcast = "255.255.255.255")
    {
        var pacote = WakeOnLan.CriarPacote(mac);
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;
        foreach (var destino in new[] { broadcast, "255.255.255.255" }.Distinct())
            for (var i = 0; i < 3; i++) await udp.SendAsync(pacote, pacote.Length, new System.Net.IPEndPoint(System.Net.IPAddress.Parse(destino), 9));
    }
}

public sealed record AppRemoto(string Id, string Nome, bool Aberto, bool EhJogo, string ApelidoVoz = "");

