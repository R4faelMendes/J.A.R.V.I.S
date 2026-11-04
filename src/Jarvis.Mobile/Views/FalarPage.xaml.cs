namespace Jarvis.Mobile.Views;

public partial class FalarPage : ContentPage
{
    private CancellationTokenSource? _gravacao;
    private CancellationTokenSource? _pagina;
    public FalarPage() => InitializeComponent();
    protected override void OnAppearing() { base.OnAppearing(); _pagina = new(); Reator.Animar(true); }
    protected override void OnDisappearing() { _pagina?.Cancel(); _gravacao?.Cancel(); Reator.Animar(false); base.OnDisappearing(); }
    private async void Falar(object? s, EventArgs e)
    {
        if (_gravacao is not null) { _gravacao.Cancel(); return; }
        string? texto;
        try
        {
#if ANDROID
            if (!App.Conexao.Conectado) { await App.Conexao.VerificarAutomaticamenteAsync(); if (!App.Conexao.Conectado) throw new InvalidOperationException("Conecte o celular ao PC para usar o Whisper local."); }
            _gravacao = new(); BotaoFalar.Text = "Concluir fala"; Transcricao.Text = "Ouvindo… fale seu comando. Uma pausa conclui a gravação.";
            var pcm = await Services.MicrofoneJarvis.GravarAsync(n => MainThread.BeginInvokeOnMainThread(() => Reator.Nivel = n), _gravacao.Token);
            _gravacao.Dispose(); _gravacao = null; BotaoFalar.IsEnabled = false; Transcricao.Text = "Transcrevendo no seu PC, sem chave de API…";
            texto = await App.Conexao.TranscreverAudioAsync(pcm, _pagina?.Token ?? default);
#else
            throw new PlatformNotSupportedException("Use o microfone no aplicativo Android ou os controles do painel.");
#endif
            if (_pagina?.IsCancellationRequested != true) await ExecutarAsync(texto);
        }
        catch (OperationCanceledException) { Transcricao.Text = "Gravação encerrada."; }
        catch (Exception ex) { Transcricao.Text = ex.Message; }
        finally { _gravacao?.Dispose(); _gravacao = null; BotaoFalar.Text = "Falar com Jarvis"; BotaoFalar.IsEnabled = true; }
    }
    private async Task ExecutarAsync(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) { Transcricao.Text = "Não reconheci uma frase. Tente falar mais perto do microfone."; return; }
        Transcricao.Text = "Você: " + texto;
        try
        {
            var pedido = Jarvis.Core.ComandoRemotoVoz.Traduzir(texto);
            if (pedido is null) { Transcricao.Text = "Use: pausar música, próxima faixa, volume 40, abrir calculadora, bloquear PC ou ler desempenho."; return; }
            var argumento = pedido.Argumento;
            if (pedido.Nome is "abrir_app" or "fechar_app")
            {
                var apps = await App.Conexao.ListarAppsAsync();
                var match = Jarvis.Core.ComandoRemotoVoz.EncontrarApp(apps.Select(a => new Jarvis.Core.AppInfo(a.Id, a.Nome, a.ApelidoVoz, a.Aberto, a.EhJogo)), argumento ?? "");
                if (match is null) { Transcricao.Text = "Não identifiquei um único aplicativo em ‘" + argumento + "’. Confira seus apps selecionados ou escolha o aplicativo na aba Apps."; return; }
                argumento = match.Id;
            }
            if (pedido.Risco && !await DisplayAlert("Confirmar comando", $"Executar no PC: {texto}?", "Executar", "Cancelar")) return;
            var resp = await App.Conexao.ComandoAsync(pedido.Nome, argumento, pedido.Risco);
            Transcricao.Text = "Jarvis: " + resp;
            if (_pagina?.IsCancellationRequested != true) { try { await TextToSpeech.Default.SpeakAsync(resp, cancelToken: _pagina?.Token ?? default); } catch { } }
        }
        catch (Exception ex) { Transcricao.Text = ex.Message; }
    }
}
