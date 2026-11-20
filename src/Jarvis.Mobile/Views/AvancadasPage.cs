namespace Jarvis.Mobile.Views;

public sealed class AvancadasPage : ContentPage
{
    public AvancadasPage()
    {
        Title = "Opções avançadas";
        var stack = new VerticalStackLayout { Padding = new Thickness(20, 16, 20, 28), MaximumWidthRequest = 760, Spacing = 14 };
        stack.Add(new Label { Text = "Opções avançadas", Style = (Style)Application.Current.Resources["Titulo"] });
        stack.Add(new Label { Text = "Configure estes recursos depois de parear o PC. Espelhamento e entrada remota continuam sujeitos às permissões dos aparelhos." });
        foreach (var (titulo, rota) in new[] { ("Janelas e grades", "janelas"), ("Tela do PC ao vivo", "tela"), ("Rede direta / internet", "rede-privada"), ("Painel e espelhamento do aparelho", "aparelho") }) {
            var button = new Button { Text = titulo };
            button.Clicked += async (_, _) => await Shell.Current.GoToAsync(rota);
            stack.Add(button);
        }
        Content = new ScrollView { Content = stack };
    }
}
