namespace Jarvis.Core;
public sealed record SensorJogo(string Nome, string Unidade, double? Valor);
public sealed record ControleVentoinha(string Id, string Nome, float Minimo, float Maximo);
public sealed record AmostraJogo(DateTimeOffset Instante, IReadOnlyList<SensorJogo> Sensores, IReadOnlyList<ControleVentoinha> Ventoinhas, string Hardware);
public interface IInstrumentacaoJogo : IDisposable
{
    Task<AmostraJogo> LerAsync(CancellationToken ct = default);
    Task DefinirVentoinhaAsync(string id, float? porcentagem);
    void Restaurar();
}
