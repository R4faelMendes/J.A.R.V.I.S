using Jarvis.Mobile.Services;
namespace Jarvis.Mobile.Views;
public partial class AppsPage : ContentPage
{
    private List<AppRemoto> _apps = new();
    private string _busca = "";
    private bool _ocupado;
    public AppsPage() => InitializeComponent();
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { Estado.Text = "Consultando o computador…"; _apps = await App.Conexao.ListarAppsAsync(); Atualizar(); Estado.Text = $"{_apps.Count} aplicativos do PC"; }
        catch { Estado.Text = "Não consegui consultar o PC. Confira o pareamento e a rede."; }
    }
    private void Atualizar() => Lista.ItemsSource = _apps.Where(a => a.Nome.Contains(_busca, StringComparison.OrdinalIgnoreCase)).ToList();
    private void Filtrar(object? s, TextChangedEventArgs e) { _busca = e.NewTextValue ?? ""; Atualizar(); }
    private async void EscolherApps(object? s, EventArgs e) => await Navigation.PushAsync(new EscolherAppsPage());
    private async void Abrir(object? s, EventArgs e) => await Executar(s, false);
    private async void Fechar(object? s, EventArgs e) => await Executar(s, true);
    private async Task Executar(object? sender, bool fechar)
    {
        if (_ocupado || (sender as Button)?.BindingContext is not AppRemoto app) return;
        _ocupado = true;
        try
        {
            if (!await DisplayAlert("Jarvis", fechar ? $"Fechar {app.Nome}? Salve seu trabalho no PC antes de continuar." : $"Abrir {app.Nome} no computador?", "Confirmar", "Cancelar")) return;
            Estado.Text = await App.Conexao.ComandoAsync(fechar ? "fechar_app" : "abrir_app", app.Id, true);
        }
        catch { Estado.Text = "Não foi possível executar. Confira se o PC está conectado."; }
        finally { _ocupado = false; }
    }
}
