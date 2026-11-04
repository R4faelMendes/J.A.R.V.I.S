using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Jarvis.Core;
namespace Jarvis.Sistema;

/// <summary>Desempenho do Windows e economia do Jarvis com restauração do estado anterior.</summary>
public sealed class ModoJogoService : IModoJogo
{
    private static readonly HashSet<string> Permitidos = new(StringComparer.OrdinalIgnoreCase)
        { "Discord", "Spotify", "ms-teams", "Teams", "Telegram", "OneDrive", "Widgets", "PhoneExperienceHost", "YourPhone", "GameBar" };
    private readonly SemaphoreSlim _mutex = new(1);
    private readonly IPlanoEnergiaJogo _energia;
    private readonly IInstrumentacaoJogo? _instrumentacao;
    private bool _economiaAnterior;
    public ModoJogoService(IPlanoEnergiaJogo? energia = null, IInstrumentacaoJogo? instrumentacao = null) { _energia = energia ?? new PlanoEnergiaJogo(); _instrumentacao = instrumentacao; }
    public bool Ativo { get; private set; }
    public IReadOnlyList<AppSegundoPlano> ListarCandidatos()
    {
        var result = new List<AppSegundoPlano>();
        using var current = Process.GetCurrentProcess(); var session = current.SessionId;
        foreach (var process in Process.GetProcesses()) using (process) {
            try { if (Permitidos.Contains(process.ProcessName) && process.SessionId == session)
                result.Add(new(process.Id, process.ProcessName)); } catch (InvalidOperationException) { } catch (Win32Exception) { }
        }
        return result.OrderBy(p => p.Nome).ToArray();
    }
    public async Task<ResultadoModoJogo> AtivarAsync(IReadOnlyCollection<int> processos)
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        try {
            if (Ativo) return new(true, "O modo de jogo já está ativo.");
            var candidatos = ListarCandidatos().ToDictionary(p => p.Id);
            if (processos.Any(id => !candidatos.ContainsKey(id))) return new(false, "A lista de aplicativos mudou. Atualize e selecione novamente.");
            _economiaAnterior = ConfiguracaoApp.ModoEconomico;
            if (!_energia.Ativar()) return new(false, "O Windows não disponibilizou um modo de alto desempenho. Nenhum app foi fechado.");
            Ativo = true;
            ConfiguracaoApp.ModoEconomico = true; ConfiguracaoApp.NotificarAlteracao();
            var fechados = 0; var mantidos = 0;
            foreach (var id in processos.Distinct()) {
                try {
                    using var p = Process.GetProcessById(id);
                    if (!p.ProcessName.Equals(candidatos[id].Nome, StringComparison.OrdinalIgnoreCase) || !Permitidos.Contains(p.ProcessName)) { mantidos++; continue; }
                    // Janelas recebem fechamento normal, preservando avisos de trabalho não salvo.
                    if (p.MainWindowHandle != IntPtr.Zero) {
                        if (p.CloseMainWindow()) { var done = await Task.Run(() => p.WaitForExit(2500)).ConfigureAwait(false); if (done) fechados++; else mantidos++; }
                        else mantidos++;
                    } else { p.Kill(); await p.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false); fechados++; }
                } catch (ArgumentException) { } catch (InvalidOperationException) { } catch (Win32Exception) { mantidos++; } catch (TimeoutException) { mantidos++; }
            }
            return new(true, $"Modo de jogo ativo: Windows em alto desempenho e Jarvis em economia. {fechados} processos fechados; {mantidos} mantidos. Ao sair, o plano anterior será restaurado.");
        } finally { _mutex.Release(); }
    }
    public async Task<ResultadoModoJogo> DesativarAsync()
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        try {
            if (!Ativo) return new(false, "O modo de jogo está desativado.");
            _instrumentacao?.Restaurar();
            if (!_energia.Restaurar()) return new(true, "Não foi possível restaurar o plano anterior. Tente desativar novamente.");
            ConfiguracaoApp.ModoEconomico = _economiaAnterior; ConfiguracaoApp.NotificarAlteracao(); Ativo = false;
            return new(false, "Modo de jogo encerrado. Plano de energia e economia do Jarvis restaurados.");
        } finally { _mutex.Release(); }
    }
}
