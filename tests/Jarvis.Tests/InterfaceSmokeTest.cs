using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Jarvis.Core;
using Jarvis.Desktop.Views.Pages;

namespace Jarvis.Tests;

public sealed class InterfaceSmokeTest
{
    [Fact]
    public void Paginas_Principais_CriamLayoutSemRecursaoOuFalhaDeRecursos()
    {
        Exception? falha = null;
        var thread = new Thread(() =>
        {
            try
            {
                var resourceRoot = new DirectoryInfo(AppContext.BaseDirectory);
                while (resourceRoot is not null && !File.Exists(Path.Combine(resourceRoot.FullName, "Jarvis.sln"))) resourceRoot = resourceRoot.Parent;
                var source = System.Xml.Linq.XDocument.Load(Path.Combine(resourceRoot!.FullName, "src", "Jarvis.Desktop", "App.xaml"));
                System.Xml.Linq.XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
                var resources = source.Root!.Element(presentation + "Application.Resources")!.Elements().Single();
                resources.SetAttributeValue(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml");
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(resources.ToString());
                Jarvis.Desktop.Services.TemaDesktop.Iniciar();
                var temaAnterior = TemasJarvis.Estado;
                try
                {
                    var textoTema = new TextBlock { Text = "Herda a cor do botão" }; var botaoTema = new Button { Content = textoTema, Foreground = new SolidColorBrush(Color.FromRgb(79, 214, 255)) };
                    botaoTema.Measure(new Size(300, 50)); botaoTema.Arrange(new Rect(0, 0, 300, 50)); botaoTema.ApplyTemplate();
                    TemasJarvis.Escolher("ambar"); Jarvis.Desktop.Services.TemaDesktop.Aplicar(botaoTema);
                    Assert.NotEqual(Color.FromRgb(79, 214, 255), ((SolidColorBrush)botaoTema.Foreground).Color);
                    botaoTema.Foreground = new SolidColorBrush(Color.FromRgb(79, 214, 255)); Jarvis.Desktop.Services.TemaDesktop.Aplicar(botaoTema);
                    TemasJarvis.Escolher("azul"); Jarvis.Desktop.Services.TemaDesktop.Aplicar(botaoTema);
                    var mapped = TemasJarvis.Colorir(0x4FD6FF);
                    Assert.Equal(Color.FromRgb((byte)(mapped >> 16), (byte)(mapped >> 8), (byte)mapped), ((SolidColorBrush)botaoTema.Foreground).Color);
                }
                finally { TemasJarvis.Sincronizar(temaAnterior); }
                var raiz = new DirectoryInfo(AppContext.BaseDirectory);
                while (raiz is not null && !File.Exists(Path.Combine(raiz.FullName, "Jarvis.sln"))) raiz = raiz.Parent;
                var pasta = Path.Combine(raiz!.FullName, "artifacts", "preview"); Directory.CreateDirectory(pasta);
                foreach (var tipo in new[] { PageType.Nucleo, PageType.Conversa, PageType.Rotinas, PageType.PreparacaoVoz, PageType.ConversaIa, PageType.Atualizacoes, PageType.ResumoDia, PageType.Diagnostico, PageType.Apps, PageType.Emails, PageType.Configuracoes, PageType.ModoJogo, PageType.Agenda, PageType.Seguranca, PageType.Celular })
                {
                    var page = FabricaPaginas.Criar(tipo);
                    page.Width = 1180; page.Height = 680;
                    page.Measure(new Size(1180, 680)); page.Arrange(new Rect(0, 0, 1180, 680)); page.UpdateLayout();
                    if (tipo == PageType.Configuracoes) {
                        page.ApplyTemplate();
                        Assert.DoesNotContain(Descendentes(page).OfType<TextBlock>(), t => t.Text.StartsWith("Tema sincronizado"));
                        Assert.DoesNotContain(Descendentes(page).OfType<ComboBox>(), c => ReferenceEquals(c.ItemsSource, TemasJarvis.Catalogo));
                    }

                    Assert.True(page.DesiredSize.Width > 0);
                    if (tipo == PageType.Nucleo) { Assert.Single(Descendentes(page).OfType<Jarvis.Desktop.Views.AudioOrbView>()); Assert.Empty(Descendentes(page).OfType<Jarvis.Desktop.Views.Reactor3DView>()); Assert.DoesNotContain(Descendentes(page).OfType<Button>(), b => b.Content is string s && s.Contains("Desmontar reator")); }
                    Jarvis.Desktop.Services.TemaDesktop.Aplicar(page);
                    var auditTheme = TemasJarvis.Estado;
                    try {
                        TemasJarvis.Escolher("ambar"); Jarvis.Desktop.Services.TemaDesktop.Aplicar(page);
                        foreach (var element in Descendentes(page).OfType<FrameworkElement>()) {
                            var brushes = element switch {
                                Border b => new[] { b.Background, b.BorderBrush },
                                Control c => new[] { c.Background, c.BorderBrush, c.Foreground },
                                TextBlock text => new[] { text.Foreground },
                                Panel panel => new[] { panel.Background },
                                System.Windows.Shapes.Shape shape => new[] { shape.Fill, shape.Stroke }, _ => Array.Empty<Brush>() };
                            foreach (var brush in brushes.OfType<SolidColorBrush>().Where(b => b.Color.A > 0)) {
                                var c = brush.Color; var rgb = (uint)((c.R << 16) | (c.G << 8) | c.B);
                                Assert.Equal(rgb, TemasJarvis.Colorir(rgb));
                            }
                        }
                    } finally { TemasJarvis.Sincronizar(auditTheme); Jarvis.Desktop.Services.TemaDesktop.Aplicar(page); }
                    // Prévia offline: mesma geometria do Skia, sem depender da janela nativa.
                    foreach (var reactor in Descendentes(page).OfType<Jarvis.Desktop.Views.ReactorView>().ToArray())
                    {
                        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(600, 600));
                        Jarvis.Desktop.Renderers.SkiaReactorRenderer.Desenhar(surface.Canvas, 600, 600, ReactorState.Repouso, .05, 1);
                        using var snapshot = surface.Snapshot();
                        using var data = snapshot.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        using var stream = new MemoryStream(data.ToArray());
                        var img = new BitmapImage(); img.BeginInit(); img.CacheOption = BitmapCacheOption.OnLoad; img.StreamSource = stream; img.EndInit(); img.Freeze();
                        reactor.Content = new Image { Source = img };
                    }
                    foreach (var reactor in Descendentes(page).OfType<Jarvis.Desktop.Views.AudioOrbView>().ToArray())
                    {
                        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(800, 800)); Jarvis.Desktop.Renderers.AudioOrbRenderer.Desenhar(surface.Canvas, 800, 800, 4, .1, ReactorState.Repouso);
                        using var snapshot = surface.Snapshot(); using var data = snapshot.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100); using var stream = new MemoryStream(data.ToArray());
                        var img = new BitmapImage(); img.BeginInit(); img.CacheOption = BitmapCacheOption.OnLoad; img.StreamSource = stream; img.EndInit(); img.Freeze(); reactor.Children.Clear(); reactor.Children.Add(new Image { Source = img });
                    }
                    var bitmap = new RenderTargetBitmap(2360, 1360, 192, 192, PixelFormats.Pbgra32);
                    var host = new Border { Background = (Brush)app.Resources["FundoAbismo"], Child = page, Width = 1180, Height = 680 };
                    host.Measure(new Size(1180, 680)); host.Arrange(new Rect(0, 0, 1180, 680)); host.UpdateLayout(); bitmap.Render(host);
                    var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
                    using var file = File.Create(Path.Combine(pasta, tipo + ".png")); png.Save(file);
                    if (tipo == PageType.Nucleo) {
                        page.Width = 760; page.Height = 900; host.Width = 760; host.Height = 900; host.Measure(new Size(760, 900)); host.Arrange(new Rect(0, 0, 760, 900)); host.UpdateLayout();
                        var header = Descendentes(page).OfType<TextBlock>().Single(t => t.Text == "E-MAILS");
                        var right = Descendentes(page).OfType<TextBlock>().Single(t => t.Text == "VER CAIXA ↗");
                        Assert.True(header.TranslatePoint(new Point(0, 0), page).X >= 0);
                        Assert.True(right.TranslatePoint(new Point(right.ActualWidth, 0), page).X <= 760);
                        var narrow = new RenderTargetBitmap(760, 900, 96, 96, PixelFormats.Pbgra32); narrow.Render(host);
                        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(narrow)); using var narrowFile = File.Create(Path.Combine(pasta, "Nucleo-responsivo.png")); encoder.Save(narrowFile);
                    }
                    page.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                }
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var originalTheme = TemasJarvis.Estado;
                var themedButton = new Button { Content = "Atualização dinâmica", Foreground = new SolidColorBrush(Color.FromRgb(79, 214, 255)) };
                var themeHost = new Window { Content = themedButton, Width = 400, Height = 150, ShowActivated = false };
                try {
                    themeHost.Show();
                    TemasJarvis.Escolher("ambar"); Jarvis.Desktop.Services.TemaDesktop.Aplicar(themeHost);
                    themedButton.Foreground = new SolidColorBrush(Color.FromRgb(79, 214, 255));
                    var frame = new System.Windows.Threading.DispatcherFrame();
                    themeHost.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
                    System.Windows.Threading.Dispatcher.PushFrame(frame);
                    var expected = TemasJarvis.Rgb(TemasJarvis.Atual.Acento);
                    Assert.Equal(Color.FromRgb((byte)(expected >> 16), (byte)(expected >> 8), (byte)expected), ((SolidColorBrush)themedButton.Foreground).Color);
                    TemasJarvis.Escolher("prata"); Jarvis.Desktop.Services.TemaDesktop.Aplicar(themeHost);
                    Assert.NotEqual(Color.FromRgb(79, 214, 255), ((SolidColorBrush)themedButton.Foreground).Color);
                    var boundCard = new Border();
                    boundCard.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding { Source = new SolidColorBrush(Color.FromRgb(79, 214, 255)) });
                    Jarvis.Desktop.Services.TemaDesktop.Aplicar(boundCard);
                    Assert.True(System.Windows.Data.BindingOperations.IsDataBound(boundCard, Border.BackgroundProperty));
                    Assert.NotEqual(Color.FromRgb(79, 214, 255), ((SolidColorBrush)boundCard.Background).Color);
                    var resourcesFrame = new System.Windows.Threading.DispatcherFrame();
                    themeHost.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() => resourcesFrame.Continue = false));
                    System.Windows.Threading.Dispatcher.PushFrame(resourcesFrame);
                    Assert.Equal(((SolidColorBrush)app.Resources["AcaoCiano"]).Color, ((SolidColorBrush)themedButton.Foreground).Color);
                    var styledButton = new Button { Content = "Estilo primário", Style = (Style)app.Resources["BotaoPrimario"] };
                    themeHost.Content = styledButton; themeHost.UpdateLayout();
                    Assert.Equal((Color)ColorConverter.ConvertFromString(Theme.VerdeSistema), ((SolidColorBrush)styledButton.Background).Color);
                    // Um cartão criado com recursos já coloridos deve acompanhar
                    // as próximas trocas, incluindo a volta para o azul.
                    var lateCard = new Border { Background = (Brush)app.Resources["PainelVidroEscuroFundo"] };
                    Jarvis.Desktop.Services.TemaDesktop.Aplicar(lateCard);
                    foreach (var id in new[] { "ambar", "esmeralda", "azul", "prata" }) {
                        TemasJarvis.Escolher(id);
                        Jarvis.Desktop.Services.TemaDesktop.Aplicar(lateCard);
                        Assert.Equal((Color)ColorConverter.ConvertFromString(TemasJarvis.Atual.Painel), ((SolidColorBrush)lateCard.Background).Color);
                        foreach (var blue in new uint[] { 0x0B202F, 0x0D3348, 0x0F2E45, 0x1B2A38 })
                            Assert.Equal(TemasJarvis.Rgb(TemasJarvis.Atual.Painel), TemasJarvis.Colorir(blue));
                    }
                    var liveSettings = FabricaPaginas.Criar(PageType.Configuracoes);
                    themeHost.Content = liveSettings; themeHost.Width = 1180; themeHost.Height = 680; themeHost.UpdateLayout();
                    var layoutFrame = new System.Windows.Threading.DispatcherFrame();
                    themeHost.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() => layoutFrame.Continue = false));
                    System.Windows.Threading.Dispatcher.PushFrame(layoutFrame);
                    foreach (var input in Descendentes(liveSettings).OfType<TextBox>()) {
                        Assert.NotEqual(Color.FromRgb(8, 20, 34), ((SolidColorBrush)input.Background).Color);
                        Assert.NotEqual(Colors.Black, ((SolidColorBrush)input.Foreground).Color);
                    }
                    var settingsBitmap = new RenderTargetBitmap(1180, 680, 96, 96, PixelFormats.Pbgra32); settingsBitmap.Render(liveSettings);
                    var settingsPng = new PngBitmapEncoder(); settingsPng.Frames.Add(BitmapFrame.Create(settingsBitmap));
                    using (var file = File.Create(Path.Combine(pasta, "Configuracoes202-ao-vivo.png"))) settingsPng.Save(file);

                } finally { themeHost.Close(); TemasJarvis.Sincronizar(originalTheme); }
                var beforeShellTheme = TemasJarvis.Estado;
                var shell = new Jarvis.Desktop.MainWindow();
                ((TextBlock)shell.FindName("TxtStatusCelular")).Text = "Prévia da interface";
                var visual = (FrameworkElement)shell.Content;
                visual.Width = 1440; visual.Height = 900;
                visual.Measure(new Size(1440, 900)); visual.Arrange(new Rect(0, 0, 1440, 900)); visual.UpdateLayout();
                var shellBitmap = new RenderTargetBitmap(2880, 1800, 192, 192, PixelFormats.Pbgra32);
                shellBitmap.Render(visual);
                // Testa a resposta nativa usada pelo Windows para arrastar, inclusive áreas vazias.
                visual.ClearValue(FrameworkElement.WidthProperty); visual.ClearValue(FrameworkElement.HeightProperty);
                shell.Left = 100; shell.Top = 100;
                shell.ShowActivated = false; shell.Show(); shell.UpdateLayout();
                var hwnd = new WindowInteropHelper(shell).EnsureHandle();
                nint Hit(Point point)
                {
                    var screen = shell.PointToScreen(point);
                    return SendMessage(hwnd, 0x0084, 0, (nint)(((int)screen.X & 0xffff) | ((int)screen.Y << 16)));
                }
                try
                {
                    var shellTheme = TemasJarvis.Estado;
                    try {
                        TemasJarvis.Escolher("prata");
                        var colorsFrame = new System.Windows.Threading.DispatcherFrame();
                        shell.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() => colorsFrame.Continue = false));
                        System.Windows.Threading.Dispatcher.PushFrame(colorsFrame);
                        var remnants = new List<string>();
                        foreach (var obj in Descendentes(shell)) {
                            var props = obj switch {
                                Border => new[] { Border.BackgroundProperty, Border.BorderBrushProperty },
                                TextBlock => new[] { TextBlock.ForegroundProperty },
                                Control => new[] { Control.BackgroundProperty, Control.ForegroundProperty, Control.BorderBrushProperty },
                                System.Windows.Shapes.Shape => new[] { System.Windows.Shapes.Shape.FillProperty, System.Windows.Shapes.Shape.StrokeProperty },
                                Panel => new[] { Panel.BackgroundProperty }, _ => Array.Empty<DependencyProperty>() };
                            foreach (var prop in props) if (obj.GetValue(prop) is SolidColorBrush brush) {
                                var c = brush.Color;
                                if (c.A > 0 && c.B > c.R * 1.18 && c.B >= c.G * .82 && c.B - c.R > 10 && ((c.B - c.R) / (double)c.B >= .35)) remnants.Add(obj.GetType().Name + " " + (obj as FrameworkElement)?.Name + " " + prop.Name + " " + c);
                            }
                        }
                        File.WriteAllLines(Path.Combine(pasta, "AzuisRestantes202.txt"), remnants);
                        Assert.Empty(remnants);
                    } finally { TemasJarvis.Sincronizar(shellTheme); }
                    TemasJarvis.Escolher("azul");
                    var previewFrame = new System.Windows.Threading.DispatcherFrame();
                    shell.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() => previewFrame.Continue = false)); System.Windows.Threading.Dispatcher.PushFrame(previewFrame);
                    foreach (var r in Descendentes(shell).OfType<Jarvis.Desktop.Views.AudioOrbView>().ToArray()) {
                        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(800,800)); Jarvis.Desktop.Renderers.AudioOrbRenderer.Desenhar(surface.Canvas,800,800,3,.03,ReactorState.Repouso);
                        using var snap = surface.Snapshot(); using var png = snap.Encode(SkiaSharp.SKEncodedImageFormat.Png,100); using var stream = new MemoryStream(png.ToArray());
                        var picture = new BitmapImage(); picture.BeginInit(); picture.CacheOption = BitmapCacheOption.OnLoad; picture.StreamSource = stream; picture.EndInit(); picture.Freeze(); r.Children.Clear(); r.Children.Add(new Image { Source = picture });
                    }
                    shell.UpdateLayout();
                    var dock = (Jarvis.Desktop.Views.SidebarControl)shell.FindName("BarraLateral");
                    var dockLabels = Descendentes(dock).OfType<TextBlock>().ToArray();
                    File.WriteAllLines(Path.Combine(pasta,"Mark-IV-dock.txt"),dockLabels.Select(t => $"{t.Text}: {t.ActualWidth}x{t.ActualHeight} {t.Foreground} {t.Visibility}"));
                    Assert.Equal(11,dockLabels.Length); Assert.All(dockLabels, t => Assert.True(t.ActualWidth > 0 && t.ActualHeight > 0 && t.Foreground is not null));
                    var mark = new RenderTargetBitmap(1440,900,96,96,PixelFormats.Pbgra32); mark.Render(visual); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(mark)); using (var file = File.Create(Path.Combine(pasta,"Mark-IV-PC.png"))) encoder.Save(file);
                    Assert.Equal((nint)2, Hit(new Point(220, 42))); // HTCAPTION: título e barra vazia arrastáveis.
                    Assert.Null(shell.FindName("BuscaComando"));
                    Assert.DoesNotContain(Descendentes(shell).OfType<Button>(), b => b.Content is string content && content.Contains("Abrir conversa"));
                    Assert.Contains(dockLabels, t => t.Text == "Modo jogo");
                    var voz = (Border)shell.FindName("BarraVoz");
                    var key = new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, PresentationSource.FromVisual(shell), 0, System.Windows.Input.Key.F11) { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent };
                    shell.RaiseEvent(key); shell.UpdateLayout();
                    var bottom = voz.TranslatePoint(new Point(0, voz.ActualHeight), shell);
                    Assert.True(bottom.Y <= shell.ActualHeight && voz.ActualWidth > 300, "Barra de voz deve caber inteira em F11.");
                    shell.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, PresentationSource.FromVisual(shell), 0, System.Windows.Input.Key.F11) { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent });
                }
                finally { shell.Close(); TemasJarvis.Sincronizar(beforeShellTheme); }
                var shellPng = new PngBitmapEncoder(); shellPng.Frames.Add(BitmapFrame.Create(shellBitmap));
                using (var file = File.Create(Path.Combine(pasta, "CentralCompleta.png"))) shellPng.Save(file);
                shell.Close(); app.Shutdown();
            }
            catch (Exception ex) { falha = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A construção das telas não deve bloquear nem recorrer indefinidamente.");
        Assert.Null(falha);
    }
    private static IEnumerable<DependencyObject> Descendentes(DependencyObject pai)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(pai); i++)
        {
            var filho = VisualTreeHelper.GetChild(pai, i); yield return filho;
            foreach (var neto in Descendentes(filho)) yield return neto;
        }
    }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint hwnd, uint message, nint wParam, nint lParam);
}
