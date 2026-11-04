using System.Diagnostics;
using System.Net.WebSockets;
using Jarvis.Core;

namespace Jarvis.Server;

internal static class TelaStream
{
    private static readonly SemaphoreSlim Vagas = new(2, 2);
    public static async Task Executar(HttpContext ctx, IControleRemoto pc)
    {
        if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
        var query = ctx.Request.Query;
        int Number(string name, int fallback) => int.TryParse(query[name], out var n) ? n : fallback;
        var options = new OpcoesTransmissao(Number("fps", 60), Number("largura", 1280), Number("qualidade", 60));
        try { options.Validar(); } catch (ArgumentException) { ctx.Response.StatusCode = 400; return; }
        if (!await Vagas.WaitAsync(0, ctx.RequestAborted)) { ctx.Response.StatusCode = 429; return; }
        try
        {
            using var session = pc.AbrirTransmissao(query["monitor"].FirstOrDefault(), options);
            using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
            using var life = CancellationTokenSource.CreateLinkedTokenSource(ctx.RequestAborted);
            using var window = new SemaphoreSlim(2, 2);
            var acknowledgements = Task.Run(async () =>
            {
                var ack = new byte[1];
                try
                {
                    while (!life.IsCancellationRequested)
                    {
                        var received = await socket.ReceiveAsync(new ArraySegment<byte>(ack), life.Token);
                        if (received.MessageType == WebSocketMessageType.Close) break;
                        if (received.MessageType != WebSocketMessageType.Binary || received.Count != 1 || ack[0] != 1) break;
                        if (window.CurrentCount < 2) window.Release();
                    }
                }
                catch (OperationCanceledException) { }
                catch (WebSocketException) { }
                finally { life.Cancel(); }
            });
            try
            {
                while (!life.IsCancellationRequested)
                {
                    var frameTime = Stopwatch.StartNew();
                    if (!await window.WaitAsync(TimeSpan.FromSeconds(5), life.Token))
                        throw new InvalidOperationException("Celular sem responder. Reinicie a visualização.");
                    var frame = session.Capturar();
                    await socket.SendAsync(new ArraySegment<byte>(frame.Jpeg), WebSocketMessageType.Binary, true, life.Token);
                    var delay = TimeSpan.FromSeconds(1d / options.Fps) - frameTime.Elapsed;
                    if (delay > TimeSpan.Zero) await Task.Delay(delay, life.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            catch (InvalidOperationException ex)
            {
                if (socket.State == WebSocketState.Open)
                    await socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, ex.Message[..Math.Min(60, ex.Message.Length)], CancellationToken.None);
            }
            finally { life.Cancel(); await acknowledgements; }
        }
        catch (InvalidOperationException ex) when (!ctx.Response.HasStarted)
        { ctx.Response.StatusCode = 423; await ctx.Response.WriteAsJsonAsync(new { mensagem = ex.Message }); }
        finally { Vagas.Release(); }
    }
}
