using System.Text.Json;
namespace Jarvis.Core;

public sealed record PaletaJarvis(string Id, string Nome, string Fundo, string Painel, string Linha, string Acento, string EnergiaSecundaria);
public sealed record EstadoTema(string Id, long Revisao);
public static class TemasJarvis
{
    public static IReadOnlyList<PaletaJarvis> Catalogo { get; } = Array.AsReadOnly(new[] {
        new PaletaJarvis("azul", "Mark IV · ciano", "#172431", "#1D2D3C", "#3B5263", "#82E0E6", "#E7BA7B"),
        new PaletaJarvis("ambar", "Grafite · champagne", "#282621", "#33312C", "#655F52", "#DEC498", "#F5E8D0"),
        new PaletaJarvis("violeta", "Grafite · lavanda", "#202536", "#2A3043", "#505D75", "#B9B5EA", "#DFDCF8"),
        new PaletaJarvis("esmeralda", "Grafite · jade", "#1A2B30", "#243A3E", "#466367", "#91CCBE", "#D8ECE6"),
        new PaletaJarvis("prata", "Obsidiana · prata", "#242A33", "#303844", "#596575", "#D2DCE7", "#F5F7FA") });
    private static readonly object Gate = new();
    private static string Arquivo => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "tema.json");
    private static EstadoTema _estado = Ler();
    private static EstadoTema Ler() { try { var e = JsonSerializer.Deserialize<EstadoTema>(File.ReadAllText(Arquivo)); if (e?.Id == "rubi") return e with { Id = "ambar" }; if (e is not null && Catalogo.Any(p => p.Id == e.Id)) return e; } catch { } return new("azul", 0); }
    public static EstadoTema Estado { get { lock (Gate) return _estado; } }
    public static PaletaJarvis Atual => Catalogo.First(p => p.Id == Estado.Id);
    public static event EventHandler? Alterado;
    public static EstadoTema Escolher(string id) => Aplicar(new(id, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), true);
    public static EstadoTema Sincronizar(EstadoTema estado) => Aplicar(estado, false);
    private static EstadoTema Aplicar(EstadoTema novo, bool incrementar)
    {
        if (novo.Id == "rubi") novo = novo with { Id = "ambar" }; // Migração de aparelhos antigos.
        if (!Catalogo.Any(p => p.Id == novo.Id)) throw new ArgumentException("Tema desconhecido.");
        lock (Gate)
        {
            if (novo.Id == _estado.Id && (!incrementar || novo.Revisao == _estado.Revisao)) return _estado;
            if (incrementar) novo = novo with { Revisao = Math.Max(novo.Revisao, _estado.Revisao + 1) };
            Directory.CreateDirectory(Path.GetDirectoryName(Arquivo)!); File.WriteAllText(Arquivo + ".tmp", JsonSerializer.Serialize(novo)); File.Move(Arquivo + ".tmp", Arquivo, true); _estado = novo;
        }
        Alterado?.Invoke(null, EventArgs.Empty); return Estado;
    }
    public static uint Rgb(string hex) => Convert.ToUInt32(hex.TrimStart('#'), 16);
    // Recursos dinâmicos já podem chegar com a paleta atual. Guarde a função
    // da cor, para que controles criados após a primeira troca também mudem.
    public static uint CorBase(uint rgb)
    {
        var p = Atual;
        if (rgb == Rgb(p.Fundo)) return 0x172431;
        if (rgb == Rgb(p.Painel)) return 0x1D2D3C;
        if (rgb == Rgb(p.Linha)) return 0x3B5263;
        if (rgb == Rgb(p.Acento)) return 0x82E0E6;
        return rgb;
    }
    // Substitui somente a família azul/ciano da interface, preservando branco, metal e estados semânticos.
    public static uint Colorir(uint rgb)
    {
        var paleta = Atual;
        var role = rgb switch {
            0x030D16 or 0x050D18 or 0x172431 => paleta.Fundo,
            0x081B29 or 0x081422 or 0x1D2D3C or 0x243747 or 0x0B202F or 0x0D3348 or 0x0F2E45 or 0x1B2A38 => paleta.Painel,
            0x1B3A57 or 0x3B5263 => paleta.Linha,
            0x4FD6FF or 0x82E0E6 => paleta.Acento,
            0x8FA9BF or 0xA5BBC7 or 0x7892A5 or 0x96AFBF or 0x778899 or 0x708090 => null,
            _ => null };
        if (role is not null) return Rgb(role);
        if (rgb is 0xE6F6FF or 0xEDF5F5 or 0xFFFFFF) return rgb;
        if (rgb == Rgb(paleta.Fundo) || rgb == Rgb(paleta.Painel) || rgb == Rgb(paleta.Linha) || rgb == Rgb(paleta.Acento) || rgb == Rgb(paleta.EnergiaSecundaria)) return rgb;
        var r = (int)(rgb >> 16 & 255); var g = (int)(rgb >> 8 & 255); var b = (int)(rgb & 255);
        if (!(b > r + 3 && b >= g * .80 && g >= r * .95)) return rgb;
        if (b >= 110 && (b - r) / (double)b < .40) {
            var acento = Rgb(paleta.Acento);
            var gray = Math.Clamp((r + g + b) / 3, 130, 235);
            return (uint)(((int)(((acento >> 16) & 255) * .22 + gray * .78) << 16) | ((int)(((acento >> 8) & 255) * .22 + gray * .78) << 8) | (int)((acento & 255) * .22 + gray * .78));
        }
        if (b <= 26) return Rgb(Atual.Fundo);
        var baseRgb = Rgb(b < 70 ? Atual.Painel : b < 110 ? Atual.Linha : Atual.Acento);
        var rr = (int)(baseRgb >> 16 & 255); var gg = (int)(baseRgb >> 8 & 255); var bb = (int)(baseRgb & 255);
        var ratio = b < 70 ? Math.Clamp(b / 41d, .75, 1.25) : b < 110 ? .85 : Math.Clamp(b / 255d, .70, 1);
        return (uint)((Math.Clamp((int)(rr * ratio), 0, 255) << 16) | (Math.Clamp((int)(gg * ratio), 0, 255) << 8) | Math.Clamp((int)(bb * ratio), 0, 255));
    }
}
