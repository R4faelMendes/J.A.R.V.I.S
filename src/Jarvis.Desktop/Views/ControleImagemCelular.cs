using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Jarvis.Core;
namespace Jarvis.Desktop.Views;
internal sealed record ContextoImagemCelular(string Id, Guid Sessao, ImagemAparelho Imagem);
internal static class ControleImagemCelular
{
    public static void Conectar(Image image, Func<ContextoImagemCelular?> contexto, Action<EntradaCelular> enviar, Action<string> status)
    {
        image.Focusable = true; image.Cursor = Cursors.Hand; image.ToolTip = "Clique e use o teclado · arraste para deslizar · botão direito para segurar · roda do mouse para rolar";
        (PontoTela Ponto, ContextoImagemCelular Contexto, long Inicio)? down = null;
        PontoTela? Position(Point point, ContextoImagemCelular ctx) => CoordenadasTela.Normalizar(point.X, point.Y, image.ActualWidth, image.ActualHeight, ctx.Imagem.Largura, ctx.Imagem.Altura);
        EntradaCelular Pedido(ContextoImagemCelular ctx, string action, double x = .5, double y = .5, double toX = .5, double toY = .5, string? text = null, string? key = null) => new(ctx.Sessao, action, ctx.Imagem.Largura, ctx.Imagem.Altura, ctx.Imagem.Rotacao, x, y, toX, toY, Texto: text, Tecla: key);
        void Send(EntradaCelular input) { try { enviar(input); } catch (Exception e) { status(e.Message); } }
        image.MouseDown += (_, e) =>
        {
            if (e.ChangedButton is not (MouseButton.Left or MouseButton.Right) || contexto() is not { } ctx || Position(e.GetPosition(image), ctx) is not { } p) return;
            image.Focus(); image.CaptureMouse(); down = (p, ctx, Environment.TickCount64); e.Handled = true;
        };
        image.MouseUp += (_, e) =>
        {
            var saved = down; down = null; image.ReleaseMouseCapture();
            if (saved is not { } start || contexto() is not { } ctx || ctx.Id != start.Contexto.Id || ctx.Sessao != start.Contexto.Sessao || ctx.Imagem.Rotacao != start.Contexto.Imagem.Rotacao || Position(e.GetPosition(image), ctx) is not { } end) return;
            var moved = Math.Abs(start.Ponto.X - end.X) + Math.Abs(start.Ponto.Y - end.Y) > .018;
            Send(Pedido(ctx, moved ? "arrastar" : e.ChangedButton == MouseButton.Right || Environment.TickCount64 - start.Inicio > 600 ? "segurar" : "tocar", start.Ponto.X, start.Ponto.Y, end.X, end.Y)); e.Handled = true;
        };
        image.LostMouseCapture += (_, _) => down = null;
        image.MouseWheel += (_, e) => { if (contexto() is not { } ctx) return; Send(Pedido(ctx, "rolar", .5, e.Delta > 0 ? .35 : .7, .5, e.Delta > 0 ? .7 : .35)); e.Handled = true; };
        image.PreviewTextInput += (_, e) => { if (contexto() is not { } ctx || string.IsNullOrEmpty(e.Text) || e.Text.Any(char.IsControl)) return; Send(Pedido(ctx, "texto", text: e.Text)); e.Handled = true; };
        image.PreviewKeyDown += (_, e) =>
        {
            if (contexto() is not { } ctx) return;
            if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { try { var text = Clipboard.GetText(); if (text.Length > 0) Send(Pedido(ctx, "texto", text: text[..Math.Min(text.Length, 1000)])); } catch (Exception ex) { status(ex.Message); } e.Handled = true; return; }
            var key = e.Key switch { Key.Return => "Enter", Key.Back => "Backspace", Key.Escape => "Escape", Key.Tab => "Tab", Key.Left => "Left", Key.Right => "Right", Key.Up => "Up", Key.Down => "Down", _ => null };
            if (key is not null) { Send(Pedido(ctx, "tecla", key: key)); e.Handled = true; }
        };
    }
}
