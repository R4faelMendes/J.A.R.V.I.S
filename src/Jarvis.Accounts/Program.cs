using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var clientId = builder.Configuration["Google:ClientId"] ?? throw new InvalidOperationException("Google:ClientId ausente.");
var secret = builder.Configuration["Google:ClientSecret"] ?? throw new InvalidOperationException("Google:ClientSecret ausente.");
var origin = new Uri(builder.Configuration["Accounts:Origin"] ?? throw new InvalidOperationException("Accounts:Origin ausente."));
if (origin.Scheme != "https" || origin.AbsolutePath != "/" || origin.Query.Length != 0 || origin.UserInfo.Length != 0 || origin.Fragment.Length != 0)
    throw new InvalidOperationException("Accounts:Origin deve ser a origem pública HTTPS.");
var database = new SqliteConnectionStringBuilder { DataSource = builder.Configuration["Accounts:Database"] ?? "jarvis-accounts.db" }.ToString();
builder.Services.AddRateLimiter(o => o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
    RateLimitPartition.GetFixedWindowLimiter("api", _ => new FixedWindowRateLimiterOptions
    { PermitLimit = 600, Window = TimeSpan.FromMinutes(1) })));
builder.Services.AddAuthentication("browser").AddCookie("browser", o => o.Cookie.SecurePolicy = CookieSecurePolicy.Always)
    .AddOAuth("google", o =>
    {
        o.ClientId = clientId;
        o.ClientSecret = secret;
        o.CallbackPath = "/signin-google";
        o.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        o.TokenEndpoint = "https://oauth2.googleapis.com/token";
        o.UserInformationEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
        o.Scope.Add("openid");
        o.Scope.Add("email");
        o.UsePkce = true;
        o.Events.OnCreatingTicket = async c =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, o.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", c.AccessToken);
            using var response = await c.Backchannel.SendAsync(request, c.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(c.HttpContext.RequestAborted));
            var subject = json.RootElement.GetProperty("sub").GetString()!;
            c.Identity!.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
            if (json.RootElement.TryGetProperty("email", out var email) && email.GetString() is { } address)
                c.Identity.AddClaim(new Claim(ClaimTypes.Email, address));
        };
        o.Events.OnTicketReceived = async c =>
        {
            c.HandleResponse();
            var subject = c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (subject is null || !c.Properties!.Items.TryGetValue("login_id", out var id) || !Hex(id))
            {
                c.Response.StatusCode = 400;
                return;
            }
            using var db = Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "UPDATE login SET subject=$subject,email=$email WHERE id=$id AND expiry>$now AND subject IS NULL";
            cmd.Parameters.AddWithValue("$subject", subject);
            cmd.Parameters.AddWithValue("$email", (object?)c.Principal!.FindFirstValue(ClaimTypes.Email) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", id!);
            cmd.Parameters.AddWithValue("$now", Now());
            if (cmd.ExecuteNonQuery() != 1)
            {
                c.Response.StatusCode = 410;
                await c.Response.WriteAsync("Este pedido de login expirou. Inicie outro pelo Jarvis.");
                return;
            }
            await c.Response.WriteAsync("Conta Jarvis conectada. Você já pode voltar ao aplicativo.");
        };
        o.Events.OnRemoteFailure = c =>
        {
            c.HandleResponse();
            return c.Response.WriteAsync("O login com Google não foi concluído. Volte ao Jarvis e tente novamente.");
        };
    });

var app = builder.Build();
// Use a origem configurada ao gerar o callback, inclusive atrás de um proxy TLS.
app.Use(async (ctx, next) =>
{
    ctx.Request.Scheme = origin.Scheme;
    ctx.Request.Host = HostString.FromUriComponent(origin.Authority);
    ctx.Response.Headers.CacheControl = "no-store";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
app.UseRateLimiter();
app.UseAuthentication();

using (var db = Open())
using (var schema = db.CreateCommand())
{
    schema.CommandText = """
        PRAGMA journal_mode=WAL;
        CREATE TABLE IF NOT EXISTS login (id TEXT PRIMARY KEY, proof TEXT NOT NULL, expiry INTEGER NOT NULL, subject TEXT, email TEXT);
        CREATE TABLE IF NOT EXISTS sessions (token TEXT PRIMARY KEY, subject TEXT NOT NULL, email TEXT, expiry INTEGER NOT NULL);
        CREATE TABLE IF NOT EXISTS pcs (subject TEXT NOT NULL, id TEXT NOT NULL, name TEXT NOT NULL, PRIMARY KEY(subject,id));
        """;
    schema.ExecuteNonQuery();
}
app.MapGet("/health", () => Results.Ok(new { app = "jarvis-accounts" }));
app.MapPost("/v1/login", (LoginProof body) =>
{
    if (!Hex(body.Proof)) return Results.BadRequest();
    var id = Secret();
    using var db = Open();
    using var cmd = db.CreateCommand();
    cmd.CommandText = "DELETE FROM login WHERE expiry <= $now; DELETE FROM sessions WHERE expiry <= $now; INSERT INTO login VALUES ($id,$proof,$expiry,NULL,NULL);";
    cmd.Parameters.AddWithValue("$now", Now());
    cmd.Parameters.AddWithValue("$expiry", Now() + 300);
    cmd.Parameters.AddWithValue("$id", id);
    cmd.Parameters.AddWithValue("$proof", body.Proof);
    cmd.ExecuteNonQuery();
    return Results.Ok(new { Id = id, Url = new Uri(origin, "/v1/login/google?id=" + id) });
});
app.MapGet("/v1/login/google", (string id) =>
{
    if (!Hex(id)) return Results.BadRequest();
    using var db = Open();
    using var cmd = db.CreateCommand();
    cmd.CommandText = "SELECT 1 FROM login WHERE id=$id AND expiry>$now AND subject IS NULL";
    cmd.Parameters.AddWithValue("$id", id);
    cmd.Parameters.AddWithValue("$now", Now());
    if (cmd.ExecuteScalar() is null) return Results.NotFound();
    var properties = new AuthenticationProperties();
    properties.Items["login_id"] = id;
    return Results.Challenge(properties, ["google"]);
});
app.MapPost("/v1/login/{id}/token", (string id, LoginProof body) =>
{
    if (!Hex(id) || !Hex(body.Proof)) return Results.BadRequest();
    using var db = Open();
    using var transaction = db.BeginTransaction();
    using var cmd = db.CreateCommand();
    cmd.Transaction = transaction;
    cmd.CommandText = "DELETE FROM login WHERE id=$id AND proof=$proof AND expiry>$now AND subject IS NOT NULL RETURNING subject,email";
    cmd.Parameters.AddWithValue("$id", id);
    cmd.Parameters.AddWithValue("$proof", Hash(body.Proof));
    cmd.Parameters.AddWithValue("$now", Now());
    string subject;
    string? email;
    using (var row = cmd.ExecuteReader())
    {
        if (!row.Read()) return Results.NoContent();
        subject = row.GetString(0);
        email = row.IsDBNull(1) ? null : row.GetString(1);
    }
    var token = Secret();
    cmd.CommandText = "INSERT INTO sessions VALUES ($token,$subject,$email,$expiry)";
    cmd.Parameters.AddWithValue("$token", Hash(token));
    cmd.Parameters.AddWithValue("$subject", subject);
    cmd.Parameters.AddWithValue("$email", (object?)email ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$expiry", Now() + 30 * 24 * 3600);
    cmd.ExecuteNonQuery();
    transaction.Commit();
    return Results.Ok(new { Token = token, Email = email, AccountId = Hash(subject) });
});
app.MapGet("/v1/pcs", (HttpContext ctx) =>
{
    using var db = Open();
    if (Subject(ctx, db) is not { } subject) return Results.Unauthorized();
    using var cmd = db.CreateCommand();
    cmd.CommandText = "SELECT id,name FROM pcs WHERE subject=$subject ORDER BY name";
    cmd.Parameters.AddWithValue("$subject", subject);
    using var rows = cmd.ExecuteReader();
    var pcs = new List<SavedPc>();
    while (rows.Read()) pcs.Add(new(rows.GetString(0), rows.GetString(1)));
    return Results.Ok(pcs);
});
app.MapPut("/v1/pcs/{id}", (HttpContext ctx, string id, SavedName body) => UpdatePc(ctx, id, body.Name, false));
app.MapDelete("/v1/pcs/{id}", (HttpContext ctx, string id) => UpdatePc(ctx, id, null, true));
app.MapDelete("/v1/session", (HttpContext ctx) =>
{
    using var db = Open();
    using var cmd = db.CreateCommand();
    cmd.CommandText = "DELETE FROM sessions WHERE token=$token";
    cmd.Parameters.AddWithValue("$token", Hash(Token(ctx)));
    cmd.ExecuteNonQuery();
    return Results.NoContent();
});
app.Run();

IResult UpdatePc(HttpContext ctx, string id, string? name, bool delete)
{
    if (!Hex(id) || (!delete && (string.IsNullOrWhiteSpace(name) || name.Length > 100))) return Results.BadRequest();
    using var db = Open();
    if (Subject(ctx, db) is not { } subject) return Results.Unauthorized();
    using var cmd = db.CreateCommand();
    cmd.CommandText = delete ? "DELETE FROM pcs WHERE subject=$subject AND id=$id" :
        """
        INSERT INTO pcs SELECT $subject,$id,$name
        WHERE EXISTS (SELECT 1 FROM pcs WHERE subject=$subject AND id=$id)
           OR (SELECT COUNT(*) FROM pcs WHERE subject=$subject)<100
        ON CONFLICT(subject,id) DO UPDATE SET name=excluded.name
        """;
    cmd.Parameters.AddWithValue("$subject", subject);
    cmd.Parameters.AddWithValue("$id", id);
    if (!delete) cmd.Parameters.AddWithValue("$name", name!.Trim());
    var changed = cmd.ExecuteNonQuery();
    return delete || changed == 1 ? Results.NoContent() : Results.Conflict();
}

SqliteConnection Open()
{
    var db = new SqliteConnection(database);
    db.Open();
    return db;
}
static string? Subject(HttpContext ctx, SqliteConnection db)
{
    var token = Token(ctx);
    if (!Hex(token)) return null;
    using var cmd = db.CreateCommand();
    cmd.CommandText = "SELECT subject FROM sessions WHERE token=$token AND expiry>$now";
    cmd.Parameters.AddWithValue("$token", Hash(token));
    cmd.Parameters.AddWithValue("$now", Now());
    return cmd.ExecuteScalar() as string;
}
static string Token(HttpContext ctx) =>
    AuthenticationHeaderValue.TryParse(ctx.Request.Headers.Authorization, out var h) &&
    h.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ? h.Parameter ?? "" : "";
static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
static string Secret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
static string Hash(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
static bool Hex(string? s) => s is { Length: 64 } && s.All(Uri.IsHexDigit);

record LoginProof(string Proof);
record SavedName(string Name);
record SavedPc(string Id, string Name);
