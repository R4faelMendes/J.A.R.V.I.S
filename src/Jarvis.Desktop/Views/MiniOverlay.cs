// JARVIS Desktop — Mini Jarvis: overlay 320x120 sempre no topo + bandeja.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Jarvis.Desktop.Views;

namespace Jarvis.Desktop.Views;

/// <summary>Janela flutuante com reator pequeno e uma linha de transcrição.</summary>
public sealed class MiniOverlay : Window
{
    private readonly TextBlock _linha;
    private readonly DispatcherTimer _sumir;

    public MiniOverlay()
    {
        Width = 320; Height = 120;
        Topmost = true;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x0C, 0x1B, 0x2E));
        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        grade.ColumnDefinitions.Add(new ColumnDefinition());
        var reator = new ReactorView { Width = 88, Height = 88, UsarSkia = false };
        Grid.SetColumn(reator, 0);
        grade.Children.Add(reator);
        _linha = new TextBlock
        {
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xE6, 0xF6, 0xFF)),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8),
        };
        Grid.SetColumn(_linha, 1);
        grade.Children.Add(_linha);
        Content = grade;
        MouseLeftButtonDown += (_, _) => DragMove();
        MouseDoubleClick += (_, _) => { Hide(); Owner?.Show(); Owner?.Activate(); };
        _sumir = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _sumir.Tick += (_, _) => Hide();
        Left = SystemParameters.WorkArea.Right - Width - 16;
        Top = SystemParameters.WorkArea.Bottom - Height - 16;
    }

    /// <summary>Mostra uma linha e some após 5 s de silêncio.</summary>
    public void Mostrar(string texto)
    {
        _linha.Text = texto;
        if (!IsVisible) Show();
        Activate();
        _sumir.Stop();
        _sumir.Start();
    }
}

/// <summary>Ícone da bandeja: Abrir, Silenciar, Sair (WPF nativo).</summary>
public sealed class TrayService : IDisposable
{
    private readonly Hardcodet.Wpf.TaskbarNotification.TaskbarIcon _icone;

    public event EventHandler? AoAbrir;
    public event EventHandler<bool>? AoSilenciar;

    public TrayService()
    {
        _icone = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon
        {
            ToolTipText = "Jarvis",
            IconSource = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Jarvis.Desktop;component/Assets/jarvis.ico")),
        };
        var menu = new ContextMenu();
        var abrir = new MenuItem { Header = "Abrir" };
        abrir.Click += (_, _) => AoAbrir?.Invoke(this, EventArgs.Empty);
        var mudo = new MenuItem { Header = "Silenciar", IsCheckable = true };
        mudo.Checked += (_, _) => AoSilenciar?.Invoke(this, true);
        mudo.Unchecked += (_, _) => AoSilenciar?.Invoke(this, false);
        var sair = new MenuItem { Header = "Sair" };
        sair.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(abrir);
        menu.Items.Add(mudo);
        menu.Items.Add(sair);
        _icone.ContextMenu = menu;
        _icone.TrayMouseDoubleClick += (_, _) => AoAbrir?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => _icone.Dispose();
}
