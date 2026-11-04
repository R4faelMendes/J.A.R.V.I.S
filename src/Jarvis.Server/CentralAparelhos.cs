using System.Security.Cryptography;
using System.Text;
using Jarvis.Core;
namespace Jarvis.Server;

/// <summary>Estado volátil por token pareado. Mantém somente o último quadro de cada aparelho.</summary>
public sealed class CentralAparelhos(TimeProvider? time = null) : ICentralAparelhos
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly object _gate = new();
    private readonly Dictionary<string, Aparelho> _items = new();
    private sealed class Aparelho(string id, string nome)
    {
        public string Id = id, Nome = nome;
        public DadosAparelho Dados = new("", "", null, null, null, null, "", false);
        public DateTimeOffset Visto;
        public Guid? Sessao;
        public DateTimeOffset UltimoQuadro;
        public ImagemAparelho? Quadro;
        public string Localizacao = "Nenhuma solicitação";
        public DateTimeOffset? SomAte;
        public readonly List<OrdemAparelho> Ordens = [];
        public readonly List<EntradaCelularPendente> Entradas = [];
        public readonly SemaphoreSlim Sinal = new(0, 1);
        public string Controle = "Aguardando o controle do Android.";
    }
    private DateTimeOffset Agora => _time.GetUtcNow();
    public static string IdToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))[..24];
    private Aparelho Obter(string id) => _items.TryGetValue(id, out var item) ? item : throw new ArgumentException("Aparelho indisponível. Abra o Jarvis no celular atualizado.");
    private bool Online(Aparelho a) => Agora - a.Visto < TimeSpan.FromSeconds(25);
    private void AtualizarPendencias(Aparelho a)
    {
        a.Ordens.RemoveAll(o => o.Expira <= Agora);
        a.Entradas.RemoveAll(o => o.Expira <= Agora || o.Entrada.Sessao != a.Sessao);
        if (a.Localizacao == "Solicitação enviada" && !a.Ordens.Any(o => o.Acao == "localizar")) a.Localizacao = "Sem confirmação do telefone";
        if (a.SomAte is { } fim && Agora >= fim) { a.SomAte = null; a.Localizacao = "Toque concluído"; }
    }
    private bool Espelhando(Aparelho a)
    {
        if (a.Sessao is not null && Agora - a.UltimoQuadro > TimeSpan.FromSeconds(10)) { a.Sessao = null; a.Quadro = null; a.Entradas.Clear(); }
        return a.Sessao is not null && Online(a);
    }
    private static void Validar(DadosAparelho d)
    {
        if (d.Modelo is null || d.Sistema is null || d.Conexao is null || d.Modelo.Length > 100 || d.Sistema.Length > 64 || d.Conexao.Length > 40 || d.Bateria is < 0 or > 100 || d.ArmazenamentoLivre is < 0 || d.ArmazenamentoTotal is < 0 || d.ArmazenamentoLivre > d.ArmazenamentoTotal)
            throw new ArgumentException("Dados do aparelho inválidos.");
    }
    public OrdemAparelho[] Pulso(string token, string nome, DadosAparelho dados)
    {
        Validar(dados); var id = IdToken(token);
        lock (_gate)
        {
            if (!_items.TryGetValue(id, out var a))
            {
                if (_items.Count >= 8)
                {
                    var old = _items.Values.Where(v => !Online(v)).OrderBy(v => v.Visto).FirstOrDefault();
                    if (old is null) throw new InvalidOperationException("Limite de aparelhos ativos atingido.");
                    _items.Remove(old.Id);
                }
                _items[id] = a = new(id, nome);
            }
            a.Dados = dados; a.Visto = Agora; a.Nome = nome;
            AtualizarPendencias(a); Espelhando(a);
            return a.Ordens.ToArray();
        }
    }
    public IReadOnlyList<AparelhoPainel> Listar()
    {
        lock (_gate)
        {
            foreach (var a in _items.Values) AtualizarPendencias(a);
            return _items.Values.OrderByDescending(a => a.Visto).Select(a => new AparelhoPainel(a.Id, a.Nome, a.Dados, a.Visto, Online(a), Espelhando(a), a.Localizacao, a.Sessao, a.Controle)).ToArray();
        }
    }
    public void Encontrar(string id)
    {
        lock (_gate)
        {
            var a = Obter(id); if (!Online(a)) throw new InvalidOperationException("Celular offline. O Jarvis precisa estar conectado no telefone para receber o pedido.");
            a.Ordens.RemoveAll(o => o.Acao is "localizar" or "silenciar");
            a.SomAte = null; a.Ordens.Add(new(Guid.NewGuid(), "localizar", Agora.AddSeconds(20))); a.Localizacao = "Solicitação enviada";
        }
    }
    public void Silenciar(string id)
    {
        lock (_gate) { var a = Obter(id); a.SomAte = null; a.Ordens.RemoveAll(o => o.Acao is "localizar" or "silenciar"); a.Ordens.Add(new(Guid.NewGuid(), "silenciar", Agora.AddSeconds(20))); a.Localizacao = "Parada solicitada"; }
    }
    public void Confirmar(string token, ConfirmacaoAparelho ack)
    {
        lock (_gate)
        {
            var a = Obter(IdToken(token)); var order = a.Ordens.FirstOrDefault(o => o.Id == ack.Id && o.Expira > Agora);
            if (order is null) return;
            if (ack.Estado is not ("tocando" or "silenciado" or "erro" or "parado")) throw new ArgumentException("Confirmação inválida.");
            a.Ordens.Remove(order);
            if (order.Acao == "localizar") a.Localizacao = ack.Estado == "tocando" ? "Som confirmado no celular · até 20 s" : "O telefone não conseguiu tocar";
            if (order.Acao == "localizar" && ack.Estado == "tocando") a.SomAte = Agora.AddSeconds(20);
            if (order.Acao == "silenciar") a.Localizacao = "Som interrompido no celular";
        }
    }
    public Guid IniciarEspelho(string token)
    {
        lock (_gate) { var a = Obter(IdToken(token)); a.Ordens.RemoveAll(o => o.Acao == "parar-espelho"); a.Entradas.Clear(); a.Quadro = null; a.Sessao = Guid.NewGuid(); a.UltimoQuadro = Agora; return a.Sessao.Value; }
    }
    public void Receber(string token, Guid sessao, byte[] jpeg, int rotacao = 0)
    {
        var (w, h) = JpegAparelho.Validar(jpeg);
        if (rotacao is < 0 or > 3) throw new ArgumentException("Rotação inválida.");
        lock (_gate) { var a = Obter(IdToken(token)); Espelhando(a); if (a.Sessao != sessao) throw new InvalidOperationException("Espelhamento encerrado. Autorize uma nova sessão no celular.");
            if (a.Quadro is { } old && (old.Largura != w || old.Altura != h || old.Rotacao != rotacao)) a.Entradas.Clear();
            a.Quadro = new(jpeg, w, h, Agora, rotacao); a.Visto = a.UltimoQuadro = Agora; }
    }
    public ImagemAparelho? Quadro(string id)
    {
        lock (_gate) { var a = Obter(id); if (!Espelhando(a) || a.Quadro is null || Agora - a.Quadro.Instante > TimeSpan.FromSeconds(3)) { a.Quadro = null; return null; } return a.Quadro; }
    }
    public void PararEspelho(string id)
    {
        lock (_gate) { var a = Obter(id); a.Sessao = null; a.Quadro = null; a.Entradas.Clear(); a.Ordens.RemoveAll(o => o.Acao == "parar-espelho"); a.Ordens.Add(new(Guid.NewGuid(), "parar-espelho", Agora.AddSeconds(20))); if (a.Sinal.CurrentCount == 0) a.Sinal.Release(); }
    }
    public void Encerrar(string token, Guid session)
    {
        lock (_gate) { var a = Obter(IdToken(token)); if (a.Sessao == session) { a.Sessao = null; a.Quadro = null; a.Entradas.Clear(); if (a.Sinal.CurrentCount == 0) a.Sinal.Release(); } }
    }
    public void Controlar(string id, EntradaCelular entrada)
    {
        entrada.Validar();
        lock (_gate)
        {
            var a = Obter(id); AtualizarPendencias(a);
            if (!a.Dados.ControleSuportado) throw new InvalidOperationException("O controle do celular pelo PC não está incluído neste instalador.");
            if (!a.Dados.ControleAtivo) throw new InvalidOperationException("Ative Jarvis em Acessibilidade no Android uma vez para usar mouse e teclado.");
            if (!Espelhando(a) || entrada.Sessao != a.Sessao || a.Quadro is not { } frame || Agora - frame.Instante > TimeSpan.FromSeconds(3)) throw new InvalidOperationException("Inicie a tela do celular antes de controlar.");
            if (entrada.Largura != frame.Largura || entrada.Altura != frame.Altura || entrada.Rotacao != frame.Rotacao) throw new InvalidOperationException("A tela girou. Aguarde a imagem atualizada.");
            if (a.Entradas.Count >= 24) throw new InvalidOperationException("Aguarde o celular concluir os comandos anteriores.");
            a.Entradas.Add(new(Guid.NewGuid(), entrada, Agora.AddSeconds(5))); if (a.Sinal.CurrentCount == 0) a.Sinal.Release();
        }
    }
    public async Task<EntradaCelularPendente[]> PuxarEntradasAsync(string token, Guid sessao, CancellationToken ct)
    {
        Aparelho a;
        lock (_gate) { a = Obter(IdToken(token)); if (!Espelhando(a) || a.Sessao != sessao) throw new InvalidOperationException("Sessão encerrada."); AtualizarPendencias(a); if (a.Entradas.Count > 0) return a.Entradas.Take(16).ToArray(); }
        await a.Sinal.WaitAsync(TimeSpan.FromSeconds(1), ct);
        lock (_gate) { if (!Espelhando(a) || a.Sessao != sessao) throw new InvalidOperationException("Sessão encerrada."); AtualizarPendencias(a); return a.Entradas.Take(16).ToArray(); }
    }
    public void ConfirmarEntrada(string token, ConfirmacaoEntradaCelular ack)
    {
        if (ack.Estado is not ("ok" or "recusado" or "expirado")) throw new ArgumentException("Resultado inválido.");
        lock (_gate) { var a = Obter(IdToken(token)); var count = a.Entradas.RemoveAll(e => e.Id == ack.Id); if (count > 0) a.Controle = ack.Estado == "ok" ? "Comando recebido pelo Android." : "Comando recusado ou expirado. Confira Acessibilidade, campo de texto e tela desbloqueada."; }
    }
}
