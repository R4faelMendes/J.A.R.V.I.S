namespace Jarvis.Mobile.Views;

public partial class DesempenhoPage : ContentPage
{
    public DesempenhoPage() => InitializeComponent();
    protected override void OnAppearing() { base.OnAppearing(); App.Conexao.TelemetriaMudou += Telemetria; App.Conexao.IniciarMonitor(); if (App.Conexao.Telemetria is { } t) Telemetria(this, t); }
    protected override void OnDisappearing() { App.Conexao.TelemetriaMudou -= Telemetria; base.OnDisappearing(); }
    private void Telemetria(object? sender, Jarvis.Core.TelemetriaPc t) => MainThread.BeginInvokeOnMainThread(() =>
    {
        static string Valor(double? v, string unidade = "%") => v.HasValue ? $"{v:0}{unidade}" : "Indisponível";
        var ram = t.MemoriaTotalBytes is > 0 && t.MemoriaUsadaBytes.HasValue ? $"{t.MemoriaUsadaBytes.Value / 1073741824d:0.0} GB de {t.MemoriaTotalBytes.Value / 1073741824d:0.0} GB" : "";
        Medidores.Text = $"CPU    {Valor(t.Cpu)}\n\nRAM    {Valor(t.Memoria)}\n{ram}\n\nGPU    {Valor(t.Gpu)}\n\nDISCO    {Valor(t.Disco)}\n\nTEMPERATURA    {Valor(t.Temperatura, " °C")}\n\nAtualizado às {t.Instante.ToLocalTime():HH:mm:ss}";
    });
    private async void Atualizar(object? s, EventArgs e)
    {
        try { await App.Conexao.TestarAsync(); if (App.Conexao.Telemetria is { } t) Telemetria(this, t); else Medidores.Text = "Telemetria indisponível. Confira a conexão com o PC."; }
        catch (Exception ex) { Medidores.Text = ex.Message; }
    }
}
