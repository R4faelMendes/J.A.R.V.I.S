using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jarvis.Core;

namespace Jarvis.Mobile.Services;

public sealed partial class ConexaoPc
{
    private const string ChavePcs = "jarvis_pcs_v1";
    private const string ChaveFavorito = "jarvis_pc_favorito";
    private readonly SemaphoreSlim _catalogo = new(1, 1);
    public string? PcAtivo => _pin is null ? null : IdDoPin(_pin);
    public string? Favorito => Preferences.Default.Get(ChaveFavorito, "") is { Length: > 0 } id ? id : null;
    public void DefinirFavorito(string? id)
    {
        if (id is null) Preferences.Default.Remove(ChaveFavorito);
        else Preferences.Default.Set(ChaveFavorito, id);
    }

    public async Task<PcSalvo[]> ListarPcsAsync()
    {
        await SalvarPcAtivoAsync();
        return (await LerPcsAsync()).Select(p => new PcSalvo(p.Id, p.Nome, p.UltimaResposta, !string.IsNullOrWhiteSpace(p.Identidade?.Mac)))
            .OrderByDescending(p => p.Id == Favorito).ThenBy(p => p.Nome).ToArray();
    }

    public async Task<bool> VerificarPcSalvoAsync(string id)
    {
        var pc = (await LerPcsAsync()).Find(p => p.Id == id);
        if (pc is null) return false;
        var enderecos = RedeDireta.Filtrar(EnderecosJarvis.Reunir([pc.Endereco, pc.Remoto], pc.Enderecos));
        var resultado = await ConexaoPareada.ProcurarAsync(enderecos, pc.Pin, pc.Token);
        if (!resultado.Any(r => r.Conectado)) return false;

        await _catalogo.WaitAsync();
        try
        {
            var pcs = await LerPcsAsync();
            var index = pcs.FindIndex(p => p.Id == id && p.Token == pc.Token);
            if (index >= 0)
            {
                pcs[index] = pcs[index] with { UltimaResposta = DateTimeOffset.UtcNow };
                await GravarPcsAsync(pcs);
            }
        }
        finally { _catalogo.Release(); }
        return true;
    }

    public async Task SalvarPcAtivoAsync()
    {
        if (_pin is not { } pin || _token is not { } token || BaseUrl is not { } endereco) return;
        var id = IdDoPin(pin);
        var identidade = Identidade;
        var ultimaResposta = _ultimaRespostaPc;
        var enderecos = _enderecos;
        var remoto = EnderecoRemoto;
        await _catalogo.WaitAsync();
        try
        {
            if (PcAtivo != id) return;
            var pcs = await LerPcsAsync();
            var anterior = pcs.Find(p => p.Id == id);
            var nome = anterior?.Nome ?? identidade?.Nome ?? "Computador Jarvis";
            pcs.RemoveAll(p => p.Id == id);
            pcs.Add(new(id, nome, endereco, pin, token, remoto, enderecos, identidade,
                ultimaResposta ?? anterior?.UltimaResposta));
            await GravarPcsAsync(pcs);
        }
        finally { _catalogo.Release(); }
    }

    public async Task SelecionarPcAsync(string id)
    {
        if (PcAtivo == id) return;
        await SalvarPcAtivoAsync();
        var pc = (await LerPcsAsync()).Find(p => p.Id == id) ??
            throw new InvalidOperationException("Pareie este computador neste aparelho para conectar.");
        await _carregamento.WaitAsync();
        try
        {
            PararMonitor();
            await _gate.WaitAsync();
            try
            {
                Esquecer(manterPreferencias: true);
                await SecureStorage.SetAsync(ChaveBase, pc.Endereco);
                await SecureStorage.SetAsync(ChavePin, pc.Pin);
                await SecureStorage.SetAsync(ChaveToken, pc.Token);
                await SecureStorage.SetAsync(ChaveEnderecos, JsonSerializer.Serialize(pc.Enderecos));
                if (pc.Remoto is not null) await SecureStorage.SetAsync(ChaveRemota, pc.Remoto);
                if (pc.Identidade is not null)
                    await SecureStorage.SetAsync("jarvis_identidade", JsonSerializer.Serialize(pc.Identidade));
            }
            finally { _gate.Release(); }
        }
        finally { _carregamento.Release(); }
        await CarregarAsync();
        IniciarMonitor();
        Notificacoes.Reagendar(App.Agenda.Listar());
        MidiaNotificacao.Restaurar();
    }

    public async Task RenomearPcAsync(string id, string nome)
    {
        await _catalogo.WaitAsync();
        try
        {
            var pcs = await LerPcsAsync();
            var index = pcs.FindIndex(p => p.Id == id);
            if (index < 0) return;
            pcs[index] = pcs[index] with { Nome = nome };
            await GravarPcsAsync(pcs);
        }
        finally { _catalogo.Release(); }
    }

    public async Task RemoverPcAsync(string id)
    {
        await _catalogo.WaitAsync();
        try
        {
            if (PcAtivo == id) Esquecer();
            var pcs = await LerPcsAsync();
            pcs.RemoveAll(p => p.Id == id);
            await GravarPcsAsync(pcs);
            if (Favorito == id) DefinirFavorito(null);
        }
        finally { _catalogo.Release(); }
    }

    public async Task AcordarSalvoAsync(string id)
    {
        var identidade = (await LerPcsAsync()).Find(p => p.Id == id)?.Identidade;
        if (identidade is null || string.IsNullOrWhiteSpace(identidade.Mac))
            throw new InvalidOperationException("Este PC ainda precisa de uma configuração Wake-on-LAN.");
        await AcordarPcAsync(identidade.Mac, identidade.Broadcast);
    }

    private static Task GravarPcsAsync(List<PareamentoSalvo> pcs) =>
        SecureStorage.SetAsync(ChavePcs, JsonSerializer.Serialize(pcs));

    private static async Task<List<PareamentoSalvo>> LerPcsAsync() =>
        JsonSerializer.Deserialize<List<PareamentoSalvo>>(await SecureStorage.GetAsync(ChavePcs) ?? "[]") ?? [];

    private static string IdDoPin(string pin) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pin.ToUpperInvariant())));

    private sealed record PareamentoSalvo(string Id, string Nome, string Endereco, string Pin, string Token,
        string? Remoto, string[] Enderecos, IdentidadePc? Identidade, DateTimeOffset? UltimaResposta);
}

public sealed record PcSalvo(string Id, string Nome, DateTimeOffset? UltimaResposta, bool PodeAcordar);
