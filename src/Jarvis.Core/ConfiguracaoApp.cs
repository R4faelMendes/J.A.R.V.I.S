// JARVIS.Core — Configurações globais de aparência, voz, comportamento e usuário.
using Microsoft.Win32;

namespace Jarvis.Core;

public static class ConfiguracaoApp
{
    public static string NomeUsuario { get; set; } = "Rafael";
    public static string IniciaisUsuario { get; set; } = "RM";
    public static string CorReatorHex { get; set; } = Theme.CianoHolo;
    public static bool ModoEconomico { get; set; } = false;
    public static bool ModoDia { get; set; } = false;
    public static string Voz { get; set; } = "Faber · Piper PT-BR";
    public static string ModeloVoz { get; set; } = "pt_BR-faber-medium.onnx";
    public static bool LerEmailsAoAbrir { get; set; } = true;
    public static bool WakeWordAtiva { get; set; } = true;
    public static double VelocidadeVoz { get; set; } = 1.0;
    public static double SensibilidadeMic { get; set; } = 0.7;
    public static bool ReducaoRuido { get; set; } = true;
    public static string PalavraAtivacao { get; set; } = "Jarvis";
    public static bool OuvirJanelaFechada { get; set; } = false;
    public static bool ReduzirAnimacoes { get; set; } = false;
    public static bool IniciarComWindows { get; set; } = true;
    public static string MicrofonePreferido { get; set; } = "";

    private sealed record Preferencias(string NomeUsuario, string IniciaisUsuario, string CorReatorHex,
        string ModeloVoz, double VelocidadeVoz, bool LerEmailsAoAbrir, bool WakeWordAtiva, bool ModoEconomico, bool ReduzirAnimacoes = false, string MicrofonePreferido = "", double SensibilidadeMic = .7);
    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "preferencias.json");
    static ConfiguracaoApp()
    {
        try
        {
            var p = System.Text.Json.JsonSerializer.Deserialize<Preferencias>(File.ReadAllText(Arquivo));
            if (p is not null)
            {
                NomeUsuario = p.NomeUsuario; IniciaisUsuario = p.IniciaisUsuario;
                CorReatorHex = System.Text.RegularExpressions.Regex.IsMatch(p.CorReatorHex ?? "", "^#[0-9a-fA-F]{6}$") ? p.CorReatorHex! : Theme.CianoHolo;
                ModeloVoz = Path.GetFileName(p.ModeloVoz ?? "pt_BR-faber-medium.onnx");
                VelocidadeVoz = Math.Clamp(p.VelocidadeVoz, .7, 1.3);
                LerEmailsAoAbrir = p.LerEmailsAoAbrir; WakeWordAtiva = p.WakeWordAtiva; ModoEconomico = p.ModoEconomico;
                ReduzirAnimacoes = p.ReduzirAnimacoes;
                MicrofonePreferido = p.MicrofonePreferido ?? ""; SensibilidadeMic = Math.Clamp(p.SensibilidadeMic, .1, 1);
            }
        }
        catch { }
        IniciarComWindows = VerificarInicializacaoAutomatica();
    }
    private static void Salvar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!);
        File.WriteAllText(Arquivo + ".tmp", System.Text.Json.JsonSerializer.Serialize(new Preferencias(NomeUsuario, IniciaisUsuario, CorReatorHex, ModeloVoz, VelocidadeVoz, LerEmailsAoAbrir, WakeWordAtiva, ModoEconomico, ReduzirAnimacoes, MicrofonePreferido, SensibilidadeMic)));
        File.Move(Arquivo + ".tmp", Arquivo, true);
    }
    public static event EventHandler? ConfiguracoesAlteradas;

    public static void NotificarAlteracao()
    {
        Salvar();
        ConfiguracoesAlteradas?.Invoke(null, EventArgs.Empty);
    }

    public static void ConfigurarInicializacaoAutomatica(bool ativar)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key is null) return;
            var path = Environment.ProcessPath;
            if (ativar && !string.IsNullOrEmpty(path))
            {
                key.SetValue("JarvisAssistente", $"\"{path}\" --startup");
            }
            else
            {
                key.DeleteValue("JarvisAssistente", false);
            }
            IniciarComWindows = ativar;
        }
        catch { }
    }

    public static bool VerificarInicializacaoAutomatica()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("JarvisAssistente") != null;
        }
        catch { return false; }
    }
}
