namespace Jarvis.Mobile.Views;

public sealed class AvancadasPage : ContentPage
{
    public AvancadasPage()
    {
        Title = "Opções avançadas";
        var stack = new VerticalStackLayout { Padding = 20, Spacing = 14 };
        stack.Add(new Label { Text = "Rede e controle remoto", FontSize = 25 });
        stack.Add(new Label { Text = "Configure estes recursos depois de parear o PC. Espelhamento e entrada remota continuam sujeitos às permissões dos aparelhos." });
        foreach (var (titulo, rota) in new[] { ("Janelas e grades", "janelas"), ("Tela do PC ao vivo", "tela"), ("Rede direta / internet", "rede-privada"), ("Painel e espelhamento do aparelho", "aparelho") }) {
            var button = new Button { Text = titulo };
            button.Clicked += async (_, _) => await Shell.Current.GoToAsync(rota);
            stack.Add(button);
        }
        Content = new ScrollView { Content = stack };
    }
}
