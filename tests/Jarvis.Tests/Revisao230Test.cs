using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Ai;
namespace Jarvis.Tests;

public sealed class Revisao230Test
{
    [Fact] public void PaletaRosaRemovidaEConfiguracaoAntigaMigrada() {
        var anterior = TemasJarvis.Estado;
        try { Assert.DoesNotContain(TemasJarvis.Catalogo, p => p.Id == "rubi"); TemasJarvis.Sincronizar(new("rubi", anterior.Revisao + 1)); Assert.Equal("ambar", TemasJarvis.Estado.Id); }
        finally { TemasJarvis.Sincronizar(anterior); }
    }
    [Fact] public void TonsAzuisIncluindoDetalhesSuavesAcompanhamTema() {
        var anterior = TemasJarvis.Estado;
        try { TemasJarvis.Escolher("ambar"); foreach (var rgb in new uint[] { 0x8FA9BF, 0xA5BBC7, 0x778899, 0x708090, 0x00BFFF, 0xE0FFFF, 0x1D2D3C, 0x146987 }) {
            var c = TemasJarvis.Colorir(rgb); Assert.NotEqual(rgb, c); Assert.True((c >> 16 & 255) >= (c & 255), $"{rgb:X6} continuou azulado: {c:X6}");
        } } finally { TemasJarvis.Sincronizar(anterior); }
    }
    [Fact] public void WidgetMostraProximosCompromissosNaoConcluidosNemExcluidos() {
        var now = DateTimeOffset.Now; Compromisso Item(int hour, EtapaCompromisso etapa = EtapaCompromisso.Planejado) => new(Guid.NewGuid().ToString(), "Compromisso", now.AddHours(hour), now.AddHours(hour + 1), Etapa: etapa);
        var upcoming = Item(2); var active = Item(-1); var result = AgendaWidget.Proximos([Item(4, EtapaCompromisso.Concluido), Item(5) with { Excluido = true }, Item(-4), upcoming, active], now);
        Assert.Equal(new[] { active.Id, upcoming.Id }, result.Select(c => c.Id));
    }
    [Fact] public void EtapaDoQuadroPersisteESincronizaSemPerderDados() {
        var folder = Path.Combine(Path.GetTempPath(), "jarvis-230-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try { var agenda = new AgendaService(Path.Combine(folder, "agenda.db")); var start = DateTimeOffset.Now.AddHours(1); var saved = agenda.Salvar(new(Guid.NewGuid().ToString(), "Reunião", start, start.AddHours(1)));
            var moved = agenda.Salvar(saved with { Etapa = EtapaCompromisso.Concluido }); Assert.Equal(EtapaCompromisso.Concluido, agenda.Listar().Single().Etapa); Assert.Null(moved.AlertaEm); Assert.Equal(saved.Versao + 1, moved.Versao); Assert.Equal(saved.Inicio, moved.Inicio);
            Assert.Throws<ConflitoAgendaException>(() => agenda.Salvar(saved with { Etapa = EtapaCompromisso.EmAndamento }));
        } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(folder, true); }
    }
    [Theory] [InlineData("abcd efgh ijkl mnop", "abcdefghijklmnop")] [InlineData("abcdefghijklmnop", "abcdefghijklmnop")]
    public void SenhaDeAppAceitaEspacosDoGoogle(string value, string expected) => Assert.Equal(expected, EmailImapService.NormalizarSenha(value));
    [Theory] [InlineData("senha-normal")] [InlineData("")] [InlineData("1234567890123456")]
    public void SenhaNormalNaoEConfundidaComSenhaDeApp(string value) => Assert.Throws<ArgumentException>(() => EmailImapService.NormalizarSenha(value));
    [Fact] public async Task MonitorUsaSomenteSensoresReaisComFallbackParaValoresAusentes() {
        using var monitor = new Jarvis.Sistema.InstrumentacaoJogoService();
        var sample = await monitor.LerAsync().WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Contains(sample.Sensores, s => s.Nome == "CPU"); Assert.Contains(sample.Sensores, s => s.Nome == "RAM");
        Assert.All(sample.Sensores, s => Assert.True(s.Valor is null || double.IsFinite(s.Valor.Value)));
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "Jarvis.sln"))) root = root.Parent;
        File.WriteAllText(Path.Combine(root!.FullName, "artifacts", "sensores-230.json"), System.Text.Json.JsonSerializer.Serialize(sample, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
}
