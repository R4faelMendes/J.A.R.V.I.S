namespace Jarvis.Mobile.Views;
public sealed class EscolherAppsPage : ContentPage
{
    private readonly VerticalStackLayout _lista = new() { Spacing = 10 };
    private readonly Label _status = new() { Text = "Buscando aplicativos válidos…" };
    private readonly Dictionary<string, CheckBox> _checks = new();
    public EscolherAppsPage()
    {
        Title = "Meus aplicativos";
        var root = new Grid { Padding = 18, RowSpacing = 12, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        root.Add(_status, 0, 0); root.Add(new ScrollView { Content = _lista }, 0, 1);
        var save = new Button { Text = "Salvar seleção" }; save.Clicked += async (_, _) => { save.IsEnabled = false; try { await App.Conexao.SalvarAppsAsync(_checks.Where(c => c.Value.IsChecked).Select(c => c.Key).ToArray()); await Navigation.PopAsync(); } catch (Exception ex) { _status.Text = ex.Message; } finally { save.IsEnabled = true; } }; root.Add(save, 0, 2); Content = root;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var meus = (await App.Conexao.ListarAppsAsync()).Select(a => a.Id).ToHashSet(); var apps = await App.Conexao.ListarDisponiveisAsync(); _lista.Clear(); _checks.Clear();
            foreach (var app in apps) { var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 10 }; var check = new CheckBox { IsChecked = meus.Contains(app.Id) }; _checks[app.Id] = check; row.Add(check, 0); row.Add(new Label { Text = app.Nome, VerticalOptions = LayoutOptions.Center, MaxLines = 2 }, 1); _lista.Add(row); }
            _status.Text = "Marque somente os apps que você usa. A seleção vale no PC e no celular.";
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }
}
