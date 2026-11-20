namespace Jarvis.Mobile.Views;

public partial class DesempenhoPage : ContentPage
{
    private CancellationTokenSource? _visibilidade;

    public DesempenhoPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _visibilidade = new();
        App.Conexao.TelemetriaMudou += Telemetria;
        App.Conexao.IniciarMonitor();
        await AtualizarAsync();
    }

    protected override void OnDisappearing()
    {
        App.Conexao.TelemetriaMudou -= Telemetria;
        _visibilidade?.Cancel();
        _visibilidade?.Dispose();
        _visibilidade = null;
        base.OnDisappearing();
    }

    private void Telemetria(object? sender, Jarvis.Core.TelemetriaPc t)
    {
        var visita = _visibilidade;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (visita is null || !ReferenceEquals(visita, _visibilidade) || !App.EmPrimeiroPlano) return;
            static string Valor(double? v, string unidade = "%") => v.HasValue ? $"{v:0}{unidade}" : "Indisponível";
            var ram = t.MemoriaTotalBytes is > 0 && t.MemoriaUsadaBytes.HasValue ? $"{t.MemoriaUsadaBytes.Value / 1073741824d:0.0} GB de {t.MemoriaTotalBytes.Value / 1073741824d:0.0} GB" : "";
            Medidores.Text = $"CPU    {Valor(t.Cpu)}\n\nRAM    {Valor(t.Memoria)}\n{ram}\n\nGPU    {Valor(t.Gpu)}\n\nDISCO    {Valor(t.Disco)}\n\nTEMPERATURA    {Valor(t.Temperatura, " °C")}\n\nAtualizado às {t.Instante.ToLocalTime():HH:mm:ss}";
        });
    }

    private async void Atualizar(object? s, EventArgs e) => await AtualizarAsync();

    private async Task AtualizarAsync()
    {
        if (_visibilidade is not { } visita) return;
        var ct = visita.Token;
        try
        {
            await App.Conexao.AtualizarTelemetriaAsync(ct);
            if (!ct.IsCancellationRequested && (!App.Conexao.Conectado || App.Conexao.Telemetria is null))
                Medidores.Text = "Telemetria indisponível. Confira a conexão com o PC.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { if (!ct.IsCancellationRequested) Medidores.Text = ex.Message; }
    }
}
