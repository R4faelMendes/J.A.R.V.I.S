namespace Jarvis.Mobile.Views;
public sealed class ControlePage : ContentPage
{
    private readonly Label _status = new() { Margin = new Thickness(0, 4) };
    private bool _busy;
    private readonly MidiaCard _player = new();
    protected override void OnAppearing() { base.OnAppearing(); _player.Ativar(true); }
    protected override void OnDisappearing() { _player.Ativar(false); base.OnDisappearing(); }
    public ControlePage()
    {
        Title = "Controles";
        Shell.SetNavBarIsVisible(this, false);
        var stack = new VerticalStackLayout { Padding = new Thickness(20, 16, 20, 28), Spacing = 12, MaximumWidthRequest = 760 };
        stack.Add(new Label { Text = "Controle remoto", Style = (Style)Application.Current.Resources["Titulo"] }); stack.Add(_status);
        var windowsPage = new Button { Text = "Organizar janelas" }; windowsPage.Clicked += async (_, _) => await Shell.Current.GoToAsync("janelas"); stack.Add(windowsPage);
        var screenPage = new Button { Text = "Tela ao vivo" }; screenPage.Clicked += async (_, _) => await Shell.Current.GoToAsync("tela"); stack.Add(screenPage);
        stack.Add(new Label { Text = "Mídia" });
        stack.Add(_player);
        stack.Add(new Label { Text = "Volume do computador" });
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 40 }; var volume = new Label { Text = "40%" };
        slider.ValueChanged += (_, e) => volume.Text = $"{e.NewValue:0}%";
        stack.Add(slider); stack.Add(volume);
        var set = new Button { Text = "Aplicar volume" }; set.Clicked += async (_, _) => await Execute("volume", ((int)slider.Value).ToString()); stack.Add(set);
        stack.Add(new Label { Text = "Energia e sessão" });
        stack.Add(Button("Bloquear sessão", "bloquear", confirm: true)); stack.Add(Button("Suspender PC", "suspender", confirm: true)); stack.Add(Button("Reiniciar PC em 30 segundos", "reiniciar", confirm: true));
        Content = new ScrollView { Content = stack };
    }
    private Button Button(string text, string command, string? arg = null, bool confirm = false)
    {
        var button = new Button { Text = text, Margin = new Thickness(0, 0, 8, 4), MinimumHeightRequest = 48 };
        button.Clicked += async (_, _) => await Execute(command, arg, confirm); return button;
    }
    private async Task Execute(string command, string? arg = null, bool confirm = false)
    {
        if (_busy) return; _busy = true;
        try
        {
            if (confirm && !await DisplayAlert("Confirmar no PC", "Executar esta ação no computador? Salve seu trabalho antes de continuar.", "Executar", "Cancelar")) return;
            var result = await App.Conexao.ComandoAsync(command, arg, confirm);
            if (result.StartsWith("Confirme") && !confirm && await DisplayAlert("Permissão do PC", "Confirmar a ação solicitada?", "Confirmar", "Cancelar")) result = await App.Conexao.ComandoAsync(command, arg, true);
            _status.Text = result;
        }
        catch (Exception ex) { _status.Text = ex.Message; }
        finally { _busy = false; }
    }
}
