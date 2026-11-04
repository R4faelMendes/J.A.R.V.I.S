using SkiaSharp;

namespace Jarvis.Mobile.Views;

public partial class ParearPage : ContentPage
{
    public ParearPage() => InitializeComponent();
    private CancellationTokenSource? _busca;
    private bool _procurando;
    private List<(string Ip, string Nome)> _pcs = [];

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (App.LinkPareamento is { } link) { App.LinkPareamento = null; await ConcluirAsync(link); return; }
        _busca?.Cancel(); _busca = new(); _ = ProcurarLoopAsync(_busca.Token);
    }
    protected override void OnDisappearing() { _busca?.Cancel(); base.OnDisappearing(); }
    private async Task ProcurarLoopAsync(CancellationToken ct)
    {
        try { while (!ct.IsCancellationRequested) { await BuscarAsync(ct); if (_pcs.Count > 0) return; await Task.Delay(8000, ct); } }
        catch (OperationCanceledException) { }
    }
    private void SelecionarPc(object? sender, EventArgs e)
    {
        if (Computadores.SelectedIndex is var i && i >= 0 && i < _pcs.Count) Ip.Text = _pcs[i].Ip;
    }
    private async Task BuscarAsync(CancellationToken ct = default)
    {
        if (_procurando) return; _procurando = true; Buscar.IsEnabled = false; BuscaStatus.Text = "Procurando o Jarvis neste Wi-Fi…";
        try
        {
            _pcs = await Services.ConexaoPc.DescobrirAsync(2500, ct); if (ct.IsCancellationRequested) return;
            Computadores.ItemsSource = _pcs.Select(p => $"{p.Nome} · {p.Ip}").ToArray(); Computadores.IsVisible = _pcs.Count > 1;
            if (_pcs.Count > 0)
            {
                Computadores.SelectedIndex = 0; Ip.Text = _pcs[0].Ip;
                BuscaStatus.Text = _pcs.Count == 1 ? $"{_pcs[0].Nome} encontrado automaticamente" : $"{_pcs.Count} PCs encontrados · escolha o seu";
                Msg.Text = "Digite apenas o PIN de seis dígitos exibido em Dispositivos no PC. Nas próximas vezes a conexão será automática.";
            }
            else BuscaStatus.Text = "Nenhum PC respondeu. Mantenha o Jarvis aberto e ambos na mesma rede Wi-Fi. Você também pode usar o QR.";
        }
        catch (OperationCanceledException) { }
        catch { BuscaStatus.Text = "Busca indisponível nesta rede. Use o QR ou informe o endereço do PC."; }
        finally { _procurando = false; Buscar.IsEnabled = true; }
    }
    private async void Fotografar(object? s, EventArgs e)
    {
        Msg.Text = "";
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                Msg.Text = "Câmera indisponível. Digite código + IP.";
                return;
            }
            if (await Permissions.RequestAsync<Permissions.Camera>() != PermissionStatus.Granted) { Msg.Text = "Permita a câmera para fotografar o QR, ou use o PIN de seis dígitos."; return; }
            var foto = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions { Title = "Fotografe o QR" });
            if (foto is null) return;
            await using var stream = await foto.OpenReadAsync();
            using var bmp = SKBitmap.Decode(stream);
            if (bmp is null) { Msg.Text = "Não li a foto. Tente de novo."; return; }
            var textoQr = Services.DecodificadorQr.Ler(bmp) ?? "";
            if (Uri.TryCreate(textoQr, UriKind.Absolute, out var urlQr) && urlQr.AbsolutePath == "/mobile") textoQr = Uri.UnescapeDataString(urlQr.Fragment.TrimStart('#'));
            if (!textoQr.StartsWith("jarvis://parear?"))
            {
                Msg.Text = "QR inválido. Confira se é o QR do Jarvis.";
                return;
            }
            await ConcluirAsync(textoQr);
        }
        catch (Exception ex) { Msg.Text = ex.Message; }
    }

    private async Task ConcluirAsync(string qr)
    {
        Msg.Text = "QR lido. Pareando...";
        try
        {
            var pars = new Dictionary<string, string>();
            foreach (var par in new Uri(qr).Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = par.Split('=', 2);
                if (kv.Length == 2) pars[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1]);
            }
            var ok = await App.Conexao.ParearAsync(
                pars.GetValueOrDefault("ip", ""), int.TryParse(pars.GetValueOrDefault("porta"), out var p) ? p : 5189,
                pars.GetValueOrDefault("token", ""), pars.GetValueOrDefault("pin", ""), DeviceInfo.Current.Model, pars.GetValueOrDefault("ips", "").Split(',', StringSplitOptions.RemoveEmptyEntries));
            if (!ok) { Msg.Text = "Código expirado. Gere um novo no PC."; return; }
            await DisplayAlert("Jarvis", "Conectado ao PC!", "OK");
            await Shell.Current.GoToAsync("//inicio");
        }
        catch (Exception ex) { Msg.Text = ex.Message; }
    }

    private async void Descobrir(object? s, EventArgs e)
    {
        await BuscarAsync(_busca?.Token ?? default);
    }

    private async void ParearCodigo(object? s, EventArgs e)
    {
        Msg.Text = "Conectando ao PC...";
        try
        {
            var cod = Codigo.Text?.Trim().Replace(" ", "") ?? "";
            var ipPc = Ip.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(cod) || string.IsNullOrWhiteSpace(ipPc))
            {
                Msg.Text = "Digite o código de 6 dígitos e o IP do PC.";
                return;
            }
            var ok = await App.Conexao.ParearPorCodigoAsync(ipPc, 5189, cod, "Celular");
            if (!ok)
            {
                Msg.Text = "Código inválido ou expirado. Gere um novo no PC.";
                return;
            }
            await DisplayAlert("Jarvis", "Conectado ao PC com sucesso!", "OK");
            await Shell.Current.GoToAsync("//inicio");
        }
        catch (Exception ex)
        {
            Msg.Text = "Erro: " + ex.Message;
        }
    }
}
