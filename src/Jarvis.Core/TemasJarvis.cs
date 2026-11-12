namespace Jarvis.Core;

public sealed record PaletaJarvis(string Id, string Nome, string Fundo, string Painel, string Linha, string Acento, string EnergiaSecundaria);
public sealed record EstadoTema(string Id, long Revisao);

/// <summary>Identidade única. IDs antigos são aceitos apenas para compatibilidade com aparelhos já pareados.</summary>
public static class TemasJarvis
{
    public static PaletaJarvis Atual { get; } = new("jarvis", "Jarvis", Theme.Abismo, Theme.Vidro, Theme.Linha, Theme.VerdeSistema, Theme.Gelo);
    public static IReadOnlyList<PaletaJarvis> Catalogo { get; } = Array.AsReadOnly(new[] { Atual });
    public static EstadoTema Estado { get; } = new("jarvis", 0);
    // Compatibilidade com os renderizadores e clientes anteriores; não há troca de tema.
    public static event EventHandler? Alterado { add { } remove { } }
    public static EstadoTema Escolher(string id)
    {
        if (id is not ("jarvis" or "azul" or "ambar" or "violeta" or "esmeralda" or "prata" or "rubi"))
            throw new ArgumentException("Tema desconhecido.", nameof(id));
        return Estado;
    }
    public static EstadoTema Sincronizar(EstadoTema estado) => Escolher(estado.Id);
    public static uint Rgb(string hex) => Convert.ToUInt32(hex.TrimStart('#'), 16);
    public static uint CorBase(uint rgb) => rgb;

    /// <summary>Converte cores legadas em preto, branco e verde; mantém cores semânticas de alerta.</summary>
    public static uint Colorir(uint rgb)
    {
        if (rgb is 0x000000 or 0x101010 or 0x333333 or 0xFFFFFF or 0xBBBBBB or 0x00D800) return rgb;
        var role = rgb switch {
            0x030D16 or 0x050D18 or 0x172431 => Theme.Abismo,
            0x081B29 or 0x081422 or 0x1D2D3C or 0x243747 or 0x0B202F or 0x0D3348 or 0x0F2E45 or 0x1B2A38 => Theme.Vidro,
            0x1B3A57 or 0x3B5263 => Theme.Linha,
            0x4FD6FF or 0x82E0E6 or 0x00D9CF or 0x4DE3A1 or 0x35F29A => Theme.VerdeSistema,
            0xE6F6FF or 0xEDF5F5 or 0xE0FFFF => Theme.Gelo,
            0x8FA9BF or 0xA5BBC7 or 0x7892A5 or 0x96AFBF or 0x778899 or 0x708090 => Theme.Nevoa,
            _ => null
        };
        if (role is not null) return Rgb(role);
        var r = (int)(rgb >> 16 & 255); var g = (int)(rgb >> 8 & 255); var b = (int)(rgb & 255);
        if (!(b > r + 3 && b >= g * .80 && g >= r * .95)) return rgb;
        if (b < 110) {
            var gray = Math.Clamp((r + g + b) / 3, 8, 64);
            return (uint)((gray << 16) | (gray << 8) | gray);
        }
        return (b - r) / (double)b < .40 ? Rgb(Theme.Gelo) : Rgb(Theme.VerdeSistema);
    }
}
