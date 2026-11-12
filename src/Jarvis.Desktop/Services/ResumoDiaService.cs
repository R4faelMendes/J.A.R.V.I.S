using System.Text;
using Jarvis.Core;
namespace Jarvis.Desktop.Services;

public sealed class ResumoDiaService(IAgendaService agenda, IEmailService email) : IResumoDia
{
    public async Task<string> GerarAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var agora = DateTimeOffset.Now;
        var texto = new StringBuilder($"Resumo de {agora:dd/MM/yyyy}, consultado às {agora:HH:mm}.\n\nFonte: agenda local deste computador.\n");
        var itens = agenda.Listar().Where(c => !c.Excluido && c.Etapa != EtapaCompromisso.Concluido && c.Fim >= agora && c.Inicio.ToLocalTime().Date == agora.LocalDateTime.Date).OrderBy(c => c.Inicio).ToArray();
        if (itens.Length == 0) texto.AppendLine("Nenhum compromisso pendente para hoje.");
        foreach (var c in itens) texto.AppendLine($"{c.Inicio.ToLocalTime():HH:mm}–{c.Fim.ToLocalTime():HH:mm}: {c.Titulo}");
        texto.AppendLine("\nFonte: caixa de entrada da conta conectada.");
        if (!email.Conectado) texto.AppendLine("E-mail desconectado. Conecte sua conta para incluir mensagens.");
        else
        {
            try
            {
                var caixa = await email.SincronizarAsync(ct);
                texto.AppendLine($"{caixa.NaoLidos} mensagens não lidas. Sincronização: {DateTimeOffset.Now:HH:mm}.");
                foreach (var m in caixa.Mensagens.Where(m => m.NaoLido).OrderByDescending(m => m.Data).Take(5)) texto.AppendLine($"• {m.Remetente}: {m.Assunto}");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { texto.AppendLine("Não foi possível atualizar o e-mail agora. Tente sincronizar na página E-mails."); }
        }
        return texto.ToString();
    }
}
