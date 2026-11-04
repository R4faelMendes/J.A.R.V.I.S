using SkiaSharp;
using ZXing;
namespace Jarvis.Mobile.Services;

/// <summary>Adaptador QR sobre pixels, sem pacote que altere a versão nativa do Skia.</summary>
public static class DecodificadorQr
{
    public static string? Ler(SKBitmap foto)
    {
        var escala = Math.Min(1, 2048d / Math.Max(foto.Width, foto.Height));
        var largura = Math.Max(1, (int)(foto.Width * escala)); var altura = Math.Max(1, (int)(foto.Height * escala));
        using var pixels = new SKBitmap(largura, altura, SKColorType.Bgra8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(pixels)) { canvas.Clear(SKColors.White); canvas.DrawBitmap(foto, new SKRect(0, 0, largura, altura)); }
        var leitor = new BarcodeReaderGeneric { AutoRotate = true };
        leitor.Options.TryHarder = true;
        leitor.Options.PossibleFormats = new[] { BarcodeFormat.QR_CODE };
        return leitor.Decode(new RGBLuminanceSource(pixels.Bytes, largura, altura, RGBLuminanceSource.BitmapFormat.BGRA32))?.Text;
    }
}
