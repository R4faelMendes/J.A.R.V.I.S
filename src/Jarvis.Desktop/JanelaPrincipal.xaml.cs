using System.Windows;
using System.Windows.Controls;
using Jarvis.Core.Abstracoes;

namespace Jarvis.Desktop;

public partial class JanelaPrincipal : Window
{
    private readonly (string Titulo, string Icone)[] _telas =
    [
        ("Núcleo", "◉"),
        ("Conversa", "💬"),
        ("Apps e janelas", "▦"),
        ("Desempenho", "◔"),
        ("Agenda", "📅"),
        ("E-mails", "✉"),
        ("Rotinas", "⚡"),
        ("Celular", "📱"),
        ("Segurança", "🛡"),
        ("Configurações", "⚙"),
    ];

    public JanelaPrincipal()
    {
        InitializeComponent();
        MontarNavegacao();
        MostrarNucleo();
    }

    private void MontarNavegacao()
    {
        foreach (var (titulo, icone) in _telas)
        {
            var botao = new Button
            {
                Content = icone,
                ToolTip = titulo,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 18,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            botao.Click += (_, _) => MostrarTela(titulo);
            ItensNavegacao.Items.Add(botao);
        }
    }

    private void IrNucleo(object sender, RoutedEventArgs e) => MostrarNucleo();

    private void AlternarEscuta(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        RotuloEstado.Text = ReatorDock.Estado == EstadoReator.Ouvindo ? "Em repouso" : "Ouvindo";
        ReatorDock.Estado = ReatorDock.Estado == EstadoReator.Ouvindo
            ? EstadoReator.Repouso
            : EstadoReator.Ouvindo;
    }

    private void MostrarNucleo() => MostrarTela("Núcleo");

    private void MostrarTela(string titulo)
    {
        Conteudo.Content = new Telas.TelaBase(titulo);
    }
}
