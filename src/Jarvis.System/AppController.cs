using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Jarvis.Core;
using Microsoft.Win32;
namespace Jarvis.Sistema;

/// <summary>Launchers existentes e seleção explícita. Nunca executa uma linha de comando recebida.</summary>
public sealed class AppController : IAppController
{
    private sealed record AppEntry(string Id, string Nome, string Apelido, string Executavel, string Argumentos, bool EhJogo, string Icone);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, AppEntry> _catalogo = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();
    private readonly string Pasta;
    private string Selecao => Path.Combine(Pasta, "apps-selecionados.json");
    private string Pessoais => Path.Combine(Pasta, "apps-pessoais.json");
    private readonly HashSet<string> _padrao = new(StringComparer.OrdinalIgnoreCase);
    public AppController(string? pastaDados = null)
    {
        Pasta = pastaDados ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
        foreach (var (id, nome, exe, apelido) in new[] {
            ("chrome", "Google Chrome", "chrome.exe", "chrome"), ("edge", "Microsoft Edge", "msedge.exe", "edge"),
            ("spotify", "Spotify", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe"), "spotify"),
            ("discord", "Discord", "Discord.exe", "discord"), ("steam", "Steam", "steam.exe", "steam"),
            ("vscode", "Visual Studio Code", "Code.exe", "vs code"), ("notepad", "Bloco de notas", "notepad.exe", "bloco de notas"),
            ("calc", "Calculadora", "calc.exe", "calculadora"), ("explorer", "Explorador de arquivos", "explorer.exe", "explorador") })
        { var caminho = Resolver(exe); if (caminho is not null) { Adicionar(id, nome, apelido, caminho, "", false); _padrao.Add(id); } }
        Atalhos(); Abertos(); JogosSteam();
        try { foreach (var a in JsonSerializer.Deserialize<AppEntry[]>(File.ReadAllText(Pessoais)) ?? []) if (File.Exists(a.Executavel)) _catalogo[a.Id] = a; } catch { }
    }
    private static string? Resolver(string exe)
    {
        exe = Environment.ExpandEnvironmentVariables(exe.Trim().Trim('"'));
        if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return null;
        if (Path.IsPathRooted(exe)) return File.Exists(exe) ? Path.GetFullPath(exe) : null;
        foreach (var p in new[] { Environment.SystemDirectory, Environment.GetFolderPath(Environment.SpecialFolder.Windows) }.Concat((Environment.GetEnvironmentVariable("PATH") ?? "").Split(';')))
        { try { var path = Path.Combine(p.Trim('"'), exe); if (File.Exists(path)) return Path.GetFullPath(path); } catch { } }
        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        foreach (var prefix in new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\" })
        { try { using var key = root.OpenSubKey(prefix + exe); var path = (key?.GetValue("") as string)?.Trim('"'); if (path is not null && File.Exists(path)) return path; } catch { } }
        foreach (var baseDir in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) })
        foreach (var rel in new[] { $"Google/Chrome/Application/{exe}", $"Microsoft/Edge/Application/{exe}", $"Programs/Microsoft VS Code/{exe}", $"Steam/{exe}" })
        { var p = Path.Combine(baseDir, rel); if (File.Exists(p)) return p; }
        return null;
    }
    private static bool Utilitario(string nome)
    {
        var n = nome.ToLowerInvariant();
        return new[] { "uninstall", "desinstal", "setup", "installer", "update", "driver", "runtime", "redistribut", "sdk", "documentation", "readme", "help", "services", "crash", "repair", "register", "release notes", "debugger" }.Any(n.Contains);
    }
    private void Adicionar(string id, string nome, string apelido, string exe, string argumentos, bool jogo)
    {
        if (!File.Exists(exe) || Utilitario(nome) || Utilitario(Path.GetFileName(exe))) return;
        if (_catalogo.Values.Any(a => a.Executavel.Equals(exe, StringComparison.OrdinalIgnoreCase) && a.Argumentos == argumentos)) return;
        _catalogo[id] = new(id, nome, apelido, exe, argumentos, jogo, jogo ? "🎮" : "▣");
    }
    private void Atalhos()
    {
        object? shell = null;
        try
        {
            var tipo = Type.GetTypeFromProgID("WScript.Shell"); if (tipo is null) return; shell = Activator.CreateInstance(tipo);
            foreach (var pasta in new[] { Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) })
            {
                if (!Directory.Exists(pasta)) continue;
                foreach (var link in Directory.EnumerateFiles(pasta, "*.lnk", SearchOption.AllDirectories))
                {
                    object? atalho = null;
                    try
                    {
                        var nome = Path.GetFileNameWithoutExtension(link); if (Utilitario(nome)) continue;
                        atalho = ((dynamic)shell!).CreateShortcut(link); string destino = ((dynamic)atalho).TargetPath; string args = ((dynamic)atalho).Arguments;
                        var exe = Resolver(destino); if (exe is null) continue;
                        var id = "app-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(exe.ToLowerInvariant() + "|" + args)))[..16];
                        Adicionar(id, nome, nome.ToLowerInvariant(), exe, args, false);
                    }
                    catch { }
                    finally { if (atalho is not null && Marshal.IsComObject(atalho)) Marshal.FinalReleaseComObject(atalho); }
                }
            }
        }
        catch { }
        finally { if (shell is not null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell); }
    }
    private void Abertos()
    {
        foreach (var p in Process.GetProcesses())
        using (p)
        {
            try
            {
                if (p.MainWindowHandle == 0 || p.ProcessName.Equals("Jarvis.Desktop", StringComparison.OrdinalIgnoreCase)) continue;
                var exe = p.MainModule?.FileName; if (exe is null || !File.Exists(exe) || Utilitario(p.ProcessName)) continue;
                var existente = _catalogo.Values.FirstOrDefault(a => a.Executavel.Equals(exe, StringComparison.OrdinalIgnoreCase));
                if (existente is not null) { _padrao.Add(existente.Id); continue; }
                if (exe.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase)) continue;
                var id = "app-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(exe.ToLowerInvariant() + "|")))[..16];
                var nome = p.MainModule?.FileVersionInfo.ProductName; Adicionar(id, string.IsNullOrWhiteSpace(nome) ? p.ProcessName : nome, p.ProcessName.ToLowerInvariant(), exe, "", false); _padrao.Add(id);
            }
            catch { }
        }
    }
    private void JogosSteam()
    {
        var steam = _catalogo.Values.FirstOrDefault(a => a.Id == "steam"); if (steam is null) return;
        try { using var apps = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam\Apps"); if (apps is null) return;
            foreach (var sub in apps.GetSubKeyNames()) { using var jogo = apps.OpenSubKey(sub); if (jogo?.GetValue("Installed") is not int installed || installed != 1 || !uint.TryParse(sub, out _)) continue; if (jogo.GetValue("Name") is string nome) Adicionar("steam-" + sub, nome, nome.ToLowerInvariant(), steam.Executavel, "-applaunch " + sub, true); }
        } catch { }
    }
    private HashSet<string> Selecionados()
    { lock (Gate) { try { return (JsonSerializer.Deserialize<string[]>(File.ReadAllText(Selecao)) ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase); } catch { return new(_padrao, StringComparer.OrdinalIgnoreCase); } } }
    private static string Processo(AppEntry a) => Path.GetFileNameWithoutExtension(a.Executavel) switch { "calc" => "CalculatorApp", var nome => nome };
    private IReadOnlyList<AppInfo> Converter(IEnumerable<AppEntry> entradas)
    {
        var rodando = Process.GetProcesses().Select(p => { using (p) { try { return p.ProcessName; } catch { return ""; } } }).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return entradas.Where(a => File.Exists(a.Executavel)).Select(a => new AppInfo(a.Id, a.Nome, a.Apelido, !a.EhJogo && rodando.Contains(Processo(a)), a.EhJogo, a.Icone)).OrderByDescending(a => a.Aberto).ThenBy(a => a.Nome).ToArray();
    }
    public IReadOnlyList<AppInfo> ListarApps() { var ids = Selecionados(); return Converter(_catalogo.Values.Where(a => ids.Contains(a.Id))); }
    public IReadOnlyList<AppInfo> ListarDisponiveis() => Converter(_catalogo.Values);
    public void SelecionarApps(IReadOnlyCollection<string> ids)
    {
        if (ids.Count > 500 || ids.Any(id => !_catalogo.ContainsKey(id))) throw new ArgumentException("Aplicativo indisponível. Atualize a lista.");
        lock (Gate) { Directory.CreateDirectory(Pasta); File.WriteAllText(Selecao, JsonSerializer.Serialize(ids.Distinct().ToArray())); }
    }
    public void Abrir(string id)
    {
        if (!_catalogo.TryGetValue(id, out var app) || !File.Exists(app.Executavel)) throw new ArgumentException("Aplicativo indisponível. Atualize seus apps selecionados.");
        Process.Start(new ProcessStartInfo(app.Executavel) { Arguments = app.Argumentos, WorkingDirectory = Path.GetDirectoryName(app.Executavel), UseShellExecute = true });
    }
    public void Fechar(string id)
    {
        if (!_catalogo.TryGetValue(id, out var app)) throw new ArgumentException("Aplicativo indisponível.");
        if (app.EhJogo) throw new InvalidOperationException("Feche a janela específica do jogo em Janelas; o launcher não identifica qual processo do jogo está aberto.");
        foreach (var p in Process.GetProcessesByName(Processo(app))) using (p) { try { if (p.MainWindowHandle != 0) p.CloseMainWindow(); } catch { } }
    }
    public void ForcarFechamento(string id) => throw new InvalidOperationException("Use o gerenciador de processos do PC para encerramento forçado.");
    public void AdicionarApp(string nome, string executavel, string apelidoVoz, bool ehJogo = false)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 100 || string.IsNullOrWhiteSpace(apelidoVoz) || apelidoVoz.Length > 100) throw new ArgumentException("Informe um nome e apelido de até 100 caracteres.");
        var argumentos = "";
        if (Path.GetExtension(executavel).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(executavel)) throw new ArgumentException("O atalho não existe.");
            object? shell = null, link = null;
            try { shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!); link = ((dynamic)shell!).CreateShortcut(Path.GetFullPath(executavel)); executavel = ((dynamic)link).TargetPath; argumentos = ((dynamic)link).Arguments; }
            catch { throw new ArgumentException("Não foi possível ler este atalho. Escolha o executável do app."); }
            finally { if (link is not null && Marshal.IsComObject(link)) Marshal.FinalReleaseComObject(link); if (shell is not null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell); }
        }
        var exe = Resolver(executavel) ?? throw new ArgumentException("Selecione um executável .exe existente.");
        var repetido = _catalogo.Values.FirstOrDefault(a => a.Executavel.Equals(exe, StringComparison.OrdinalIgnoreCase) && a.Argumentos == argumentos);
        var id = repetido?.Id ?? "pessoal-" + Guid.NewGuid().ToString("N"); var app = new AppEntry(id, nome, apelidoVoz, exe, argumentos, ehJogo, "▣");
        lock (Gate) { Directory.CreateDirectory(Pasta); AppEntry[] existentes; try { existentes = JsonSerializer.Deserialize<AppEntry[]>(File.ReadAllText(Pessoais)) ?? []; } catch { existentes = []; } File.WriteAllText(Pessoais, JsonSerializer.Serialize(existentes.Where(a => a.Id != id).Append(app))); _catalogo[id] = app; var ids = Selecionados(); ids.Add(id); File.WriteAllText(Selecao, JsonSerializer.Serialize(ids)); }
    }
}
