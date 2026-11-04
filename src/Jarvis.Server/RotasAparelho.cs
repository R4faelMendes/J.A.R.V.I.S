using Jarvis.Core;
namespace Jarvis.Server;
internal static class RotasAparelho
{
    public static void Mapear(WebApplication app)
    {
        static string Token(HttpContext c) => c.Request.Headers["X-Jarvis-Token"].ToString();
        app.MapPost("/api/celular/pulso", (DadosAparelho dados, HttpContext ctx, CentralAparelhos central, Aparelhos pares) =>
        {
            try
            {
                pares.Validar(Token(ctx), out var nome); var orders = central.Pulso(Token(ctx), nome, dados);
                PresencaRemota.Registrar(nome, dados.Bateria?.ToString(System.Globalization.CultureInfo.InvariantCulture), dados.Conexao);
                return Results.Ok(orders);
            }
            catch (ArgumentException e) { return Results.BadRequest(new { mensagem = e.Message }); }
            catch (InvalidOperationException e) { return Results.Conflict(new { mensagem = e.Message }); }
        });
        app.MapPost("/api/celular/confirmar", (ConfirmacaoAparelho ack, HttpContext ctx, CentralAparelhos central) =>
        {
            try { central.Confirmar(Token(ctx), ack); return Results.Ok(); }
            catch (ArgumentException e) { return Results.BadRequest(new { mensagem = e.Message }); }
        });
        app.MapPost("/api/celular/tela/iniciar", (HttpContext ctx, CentralAparelhos central) =>
        {
            try { return Results.Ok(new SessaoEspelho(central.IniciarEspelho(Token(ctx)))); }
            catch (ArgumentException e) { return Results.BadRequest(new { mensagem = e.Message }); }
        });
        app.MapPost("/api/celular/tela/parar", (SessaoEspelho sessao, HttpContext ctx, CentralAparelhos central) =>
        {
            try { central.Encerrar(Token(ctx), sessao.Id); return Results.Ok(); }
            catch (ArgumentException e) { return Results.BadRequest(new { mensagem = e.Message }); }
        });
        app.MapPost("/api/celular/tela/quadro", async (HttpContext ctx, CentralAparelhos central) =>
        {
            ctx.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(ctx.Request.Query["sessao"], out var sessao)) return Results.BadRequest();
            if (ctx.Request.ContentLength > JpegAparelho.LimiteBytes) return Results.StatusCode(413);
            using var buffer = new MemoryStream(); var chunk = new byte[32768]; int n;
            while ((n = await ctx.Request.Body.ReadAsync(chunk, ctx.RequestAborted)) > 0)
            {
                if (buffer.Length + n > JpegAparelho.LimiteBytes) return Results.StatusCode(413);
                buffer.Write(chunk, 0, n);
            }
            try { var rotacao = int.TryParse(ctx.Request.Query["rotacao"], out var rotation) ? rotation : 0; central.Receber(Token(ctx), sessao, buffer.ToArray(), rotacao); return Results.Ok(); }
            catch (ArgumentException e) { return Results.BadRequest(new { mensagem = e.Message }); }
            catch (InvalidOperationException e) { return Results.Conflict(new { mensagem = e.Message }); }
        }).RequireRateLimiting("quadro-celular");
        app.MapGet("/api/celular/tela/entradas", async (HttpContext ctx, CentralAparelhos central) =>
        {
            ctx.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(ctx.Request.Query["sessao"], out var session)) return Results.BadRequest();
            try { return Results.Ok(await central.PuxarEntradasAsync(Token(ctx), session, ctx.RequestAborted)); }
            catch (ArgumentException) { return Results.BadRequest(); } catch (InvalidOperationException) { return Results.Conflict(); }
        });
        app.MapPost("/api/celular/tela/entrada/confirmar", (ConfirmacaoEntradaCelular ack, HttpContext ctx, CentralAparelhos central) =>
        {
            try { central.ConfirmarEntrada(Token(ctx), ack); return Results.Ok(); } catch (ArgumentException) { return Results.BadRequest(); }
        });
    }
}
