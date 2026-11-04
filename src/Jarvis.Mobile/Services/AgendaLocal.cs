using System.Text.Json;
using Jarvis.Core;
namespace Jarvis.Mobile.Services;

public sealed class AgendaLocal
{
    private sealed record Cache(List<Compromisso> Itens, HashSet<string> Pendentes);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _arquivoGate = new();
    private string Arquivo => Path.Combine(FileSystem.AppDataDirectory, "agenda-" + App.Conexao.IdentificadorPareamento + ".json");
    private Cache Ler(string? arquivo = null) { lock (_arquivoGate) { try { return JsonSerializer.Deserialize<Cache>(File.ReadAllText(arquivo ?? Arquivo)) ?? new([], []); } catch { return new([], []); } } }
    private void Gravar(Cache cache, string? arquivo = null) { arquivo ??= Arquivo; if (arquivo != Arquivo) throw new InvalidOperationException("O computador pareado mudou. Abra a agenda novamente."); lock (_arquivoGate) { File.WriteAllText(arquivo + ".tmp", JsonSerializer.Serialize(cache)); File.Move(arquivo + ".tmp", arquivo, true); } Notificacoes.Reagendar(cache.Itens);
#if ANDROID
        JarvisWidget.PublicarAgenda(cache.Itens);
#endif
    }
    public IReadOnlyList<Compromisso> Listar() => Ler().Itens.OrderBy(c => c.Inicio).ToArray();
    public async Task SalvarAsync(Compromisso item)
    {
        item.Validar(); await _gate.WaitAsync();
        try { var cache = Ler(); var previous = cache.Itens.FirstOrDefault(c => c.Id == item.Id); if (previous is null || previous.Inicio != item.Inicio || previous.Fim != item.Fim) AgendaDatas.Validar(item, DateTime.Today); cache.Itens.RemoveAll(c => c.Id == item.Id); cache.Itens.Add(item); cache.Pendentes.Add(item.Id); Gravar(cache); }
        finally { _gate.Release(); }
    }
    public async Task<string> SincronizarAsync()
    {
        if (!await _gate.WaitAsync(0)) return "Sincronização em andamento.";
        var arquivo = Arquivo;
        try
        {
            var cache = Ler(arquivo);
            foreach (var id in cache.Pendentes.ToArray())
            {
                var item = cache.Itens.First(c => c.Id == id);
                if (arquivo != Arquivo) return "O computador pareado mudou. Abra a agenda novamente.";
                try { var saved = await App.Conexao.SalvarCompromissoAsync(item); cache.Itens.RemoveAll(c => c.Id == id); cache.Itens.Add(saved); cache.Pendentes.Remove(id); Gravar(cache, arquivo); }
                catch (ConflitoAgendaException) { Gravar(cache, arquivo); return "Conflito: um compromisso mudou no PC. Abra o item pendente e escolha manter a versão do PC ou copiar sua edição."; }
                catch (ArgumentException ex) { return "Edite o compromisso pendente: " + ex.Message; }
            }
            var remote = await App.Conexao.ListarAgendaAsync(); cache.Itens.Clear(); cache.Itens.AddRange(remote); Gravar(cache, arquivo);
            return "Agenda sincronizada · lembretes disponíveis offline";
        }
        catch (Exception) { Notificacoes.Reagendar(Ler().Itens); return "Offline · compromissos salvos e lembretes locais mantidos. Alterações aguardam sincronização."; }
        finally { _gate.Release(); }
    }
    public bool Pendente(string id) => Ler().Pendentes.Contains(id);
    public async Task ResolverAsync(string id, bool manterLocal)
    {
        await _gate.WaitAsync();
        try
        {
            var cache = Ler(); var remote = (await App.Conexao.ListarAgendaAsync()).FirstOrDefault(c => c.Id == id); var local = cache.Itens.First(c => c.Id == id);
            cache.Itens.RemoveAll(c => c.Id == id); cache.Pendentes.Remove(id); if (remote is not null) cache.Itens.Add(remote);
            if (manterLocal) { var copy = local with { Id = Guid.NewGuid().ToString(), Versao = 0, Excluido = false }; cache.Itens.Add(copy); cache.Pendentes.Add(copy.Id); } Gravar(cache);
        }
        finally { _gate.Release(); }
    }
}

public static class Notificacoes
{
    public static void Reagendar(IEnumerable<Compromisso> items)
    {
#if ANDROID
        AvisosAndroid.Reagendar(Android.App.Application.Context, items);
#endif
    }
    public static async Task AtivarAsync(bool pedirHorarioPreciso = false)
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33)) await Permissions.RequestAsync<PermissaoNotificacoes>();
        if (pedirHorarioPreciso) AvisosAndroid.PedirAlarmes();
#endif
        Reagendar(App.Agenda.Listar());
    }
#if ANDROID
    public sealed class PermissaoNotificacoes : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => OperatingSystem.IsAndroidVersionAtLeast(33) ? [(Android.Manifest.Permission.PostNotifications, true)] : [];
    }
#endif
}
