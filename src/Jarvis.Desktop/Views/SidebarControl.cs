using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Jarvis.Core;
namespace Jarvis.Desktop.Views;
public sealed class SidebarControl : UserControl
{
    public static readonly DependencyProperty PaginaAtualProperty = DependencyProperty.Register(nameof(PaginaAtual), typeof(PageType), typeof(SidebarControl), new PropertyMetadata(PageType.Nucleo, (d, _) => ((SidebarControl)d).Atualizar()));
    public PageType PaginaAtual { get => (PageType)GetValue(PaginaAtualProperty); set => SetValue(PaginaAtualProperty, value); }
    public event EventHandler<PageType>? Navegar;
    private readonly Dictionary<PageType, Button> _items = new();
    public SidebarControl()
    {
        var column = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var (page, label, icon) in new[] { (PageType.Nucleo, "Início", "sparkles"), (PageType.ModoJogo, "Modo jogo", "gauge"), (PageType.Apps, "Aplicativos", "layout-grid"), (PageType.Desempenho, "Desempenho", "gauge"), (PageType.Emails, "E-mails", "mail"), (PageType.Agenda, "Agenda", "calendar-days"), (PageType.Celular, "Dispositivos", "smartphone"), (PageType.Seguranca, "Segurança", "shield-check"), (PageType.Configuracoes, "Configurações", "settings") })
        {
            var content = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center }; content.Children.Add(Icones.Criar(icon, 21, new SolidColorBrush(Color.FromRgb(150,175,191)))); content.Children.Add(new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.FromRgb(150,175,191)), Margin = new Thickness(0, 7, 0, 0), HorizontalAlignment = HorizontalAlignment.Center, FontSize = 9 });
            var button = new Button { Content = content, HorizontalContentAlignment = HorizontalAlignment.Center, Width = 86, Height = 64, Padding = new Thickness(5), Margin = new Thickness(2, 5, 2, 5), Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = new SolidColorBrush(Color.FromRgb(150, 175, 191)), ToolTip = label };
            button.Click += (_, _) => { PaginaAtual = page; Navegar?.Invoke(this, page); }; AutomationProperties.SetName(button, label); _items.Add(page, button); column.Children.Add(button);
        }
        Content = new ScrollViewer { Content = column, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled }; Atualizar();
    }
    private void Atualizar() { foreach (var (page, button) in _items) { button.Background = page == PaginaAtual ? new SolidColorBrush(Color.FromRgb(9, 38, 55)) : Brushes.Transparent; button.Foreground = page == PaginaAtual ? new SolidColorBrush(Color.FromRgb(79, 214, 255)) : new SolidColorBrush(Color.FromRgb(150, 175, 191)); button.BorderThickness = page == PaginaAtual ? new Thickness(0, 2, 0, 0) : new Thickness(0); button.BorderBrush = Brushes.DeepSkyBlue;
        if (button.Content is StackPanel panel) {
            foreach (var text in panel.Children.OfType<TextBlock>()) text.Foreground = button.Foreground;
            foreach (var grid in panel.Children.OfType<Grid>()) foreach (var shape in grid.Children.OfType<System.Windows.Shapes.Shape>()) { if (shape.Stroke is not null) shape.Stroke = button.Foreground; else if (shape.Fill is not null) shape.Fill = button.Foreground; }
        } } }
    protected override void OnKeyDown(KeyEventArgs e) { var keys = _items.Keys.ToArray(); var i = Array.IndexOf(keys, PaginaAtual); if (e.Key is Key.Down or Key.Up) { PaginaAtual = keys[Math.Clamp(i + (e.Key == Key.Down ? 1 : -1), 0, keys.Length - 1)]; Navegar?.Invoke(this, PaginaAtual); e.Handled = true; } base.OnKeyDown(e); }
}


