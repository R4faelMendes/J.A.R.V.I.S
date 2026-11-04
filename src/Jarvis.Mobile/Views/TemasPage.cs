using Jarvis.Core;
namespace Jarvis.Mobile.Views;
public sealed class TemasPage : ContentPage
{
    private readonly Label _status = new();
    public TemasPage()
    {
        Title = "Aparência"; var stack = new VerticalStackLayout { Padding = 22, Spacing = 14 };
        stack.Add(new Label { Text = "Uma identidade, dois aparelhos", FontSize = 25 });
        stack.Add(new Label { Text = "Escolha o tema da interface e do reator. O PC e este celular mudam juntos quando conectados." });
        stack.Add(new ReactorArt { HeightRequest = 260 });
        foreach (var p in TemasJarvis.Catalogo)
        {
            var b = new Button { Text = p.Nome, TextColor = Color.FromArgb(p.Acento), BackgroundColor = Color.FromArgb(p.Painel), BorderColor = Color.FromArgb(p.Linha), BorderWidth = 1 };
            b.Clicked += async (_, _) => { b.IsEnabled = false; try { await App.Conexao.EscolherTemaAsync(p.Id); _status.Text = "Tema aplicado ao PC e ao celular."; } catch (Exception e) { _status.Text = e.Message; } finally { b.IsEnabled = true; } }; stack.Add(b);
        }
        stack.Add(_status); Content = new ScrollView { Content = stack };
    }
}
