namespace Jarvis.Mobile.Views;

public partial class MaisPage : ContentPage
{
    public MaisPage() => InitializeComponent();
    private async void AdicionarWidget(object? s, EventArgs e)
    {
#if ANDROID
        if (JarvisWidget.SolicitarFixacao()) return;
#endif
        await DisplayAlert("Widget Jarvis", "Na tela inicial do celular, mantenha um espaço vazio pressionado, abra Widgets e escolha Jarvis · Agenda.", "OK");
    }
    private async void Ir(object? s, EventArgs e)
    {
        var destino = (s as Button)?.ClassId ?? "";
        if (destino == "falar") await Shell.Current.GoToAsync("falar");
        else if (destino == "parear") await Shell.Current.GoToAsync("parear");
        else if (destino is "janelas" or "tela") await Shell.Current.GoToAsync(destino);
        else if (destino == "vpn") await Shell.Current.GoToAsync("rede-privada");
        else if (destino == "avancado") await Navigation.PushAsync(new AvancadasPage());
        else if (destino == "aparelho") await Shell.Current.GoToAsync("aparelho");
    }
    private void Esquecer(object? s, EventArgs e)
    {
        App.Conexao.Esquecer();
#if ANDROID
        JarvisWidget.PublicarAgenda([]);
#endif
        DisplayAlert("Jarvis", "PC removido.", "OK");
    }
}

