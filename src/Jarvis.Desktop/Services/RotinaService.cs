using System.Text.Json;
using Jarvis.Core;
using Jarvis.Data;
namespace Jarvis.Desktop.Services;
public sealed class RotinaService : IRotinaService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public static PassoRotina[] Validar(string json) => PlanoRotina.Validar(json);
    public async Task ExecutarAsync(string passosJson, IProgress<ProgressoRotina> progresso, CancellationToken ct)
    {
        var passos = Validar(passosJson);
        if (!await _gate.WaitAsync(0, ct)) throw new InvalidOperationException("Já existe uma rotina em execução.");
        try
        {
            var pc = new ControleRemotoPc();
            // Validar todas as permissões e aplicativos antes da primeira ação.
            foreach (var p in passos)
            {
                var tipo = p.Comando == "abrir_app" ? TipoAcao.AbrirApp : p.Comando == "layout" ? TipoAcao.ControlarJanela : (TipoAcao?)null;
                if (tipo is not null && new Permissoes().Obter(tipo.Value) != Permissao.Sozinho) throw new InvalidOperationException("Uma ação exige confirmação ou está bloqueada em Segurança. Execute-a individualmente.");
                if (p.Comando == "abrir_app")
                {
                    var app = pc.Apps().FirstOrDefault(a => a.Id == p.Argumento);
                    if (app is null || app.EhJogo) throw new ArgumentException("Aplicativo indisponível ou jogo exige confirmação individual.");
                }
            }
            for (var i = 0; i < passos.Length; i++)
            {
                ct.ThrowIfCancellationRequested(); var p = passos[i];
                progresso.Report(new(i + 1, passos.Length, PlanoRotina.Descrever(p) + "…", false));
                if (p.Comando == "esperar") await Task.Delay(TimeSpan.FromSeconds(int.Parse(p.Argumento!)), ct);
                else
                {
                    var result = await pc.ExecutarAsync(p.Comando, p.Argumento, false);
                    progresso.Report(new(i + 1, passos.Length, result, false));
                    if (result.Contains("bloquead", StringComparison.OrdinalIgnoreCase) || result.Contains("Confirme") || result.StartsWith("Abra o Spotify") || result.StartsWith("Aplicativo não encontrado")) throw new InvalidOperationException(result);
                }
            }
            progresso.Report(new(passos.Length, passos.Length, "Rotina concluída.", true));
        }
        finally { _gate.Release(); }
    }
}
