using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Jarvis.Core;
using Jarvis.Desktop.Services;
namespace Jarvis.Desktop.Views.Pages;
public sealed class AgendaPage : PaginaBase
{
    private readonly IAgendaService _agenda = ServicosAplicacao.Agenda;
    private readonly Grid _calendar = new() { Margin = new Thickness(0, 0, 22, 0) };
    private DateTime _day = DateTime.Today;
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private readonly StackPanel _items = new();
    public AgendaPage() : base("Seus compromissos", "Agenda") { }
    private readonly StackPanel _board = new();
    private readonly TextBox _search = new() { MinWidth = 180, MaxWidth = 320, ToolTip = "Buscar compromisso" };
    private bool _quadro = true;
    private Grid? _calendarRoot;
    protected override void Montar(Grid container)
    {
        container.RowDefinitions.Add(new() { Height = GridLength.Auto }); container.RowDefinitions.Add(new());
        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 18) };
        void Action(string label, Action click) { var b = new Button { Content = label, Margin = new Thickness(0, 0, 8, 8) }; b.Click += (_, _) => click(); toolbar.Children.Add(b); }
        Action("+ Novo cartão", () => Editar(null)); Action("Quadro", () => { _quadro = true; Carregar(); }); Action("Calendário", () => { _quadro = false; Carregar(); }); Action("Atualizar", Carregar);
        _search.TextChanged += (_, _) => DesenharQuadro(); toolbar.Children.Add(_search); container.Children.Add(toolbar);
        _calendarRoot = new Grid(); _calendarRoot.ColumnDefinitions.Add(new() { Width = new GridLength(2.4, GridUnitType.Star) }); _calendarRoot.ColumnDefinitions.Add(new() { MinWidth = 260 });
        _calendarRoot.Children.Add(_calendar); var details = new ScrollViewer { Content = _items, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; Grid.SetColumn(details, 1); _calendarRoot.Children.Add(details);
        Grid.SetRow(_calendarRoot, 1); container.Children.Add(_calendarRoot);
        Grid.SetRow(_board, 1); container.Children.Add(_board); Carregar();
    }
    private void Mover(Compromisso item, EtapaCompromisso etapa) { try { _agenda.Salvar(item with { Etapa = etapa }); Carregar(); } catch (Exception ex) { MostrarToast(ex.Message); } }
    private void DesenharQuadro()
    {
        _board.Children.Clear(); if (!_quadro) return;
        var items = _agenda.Listar().Where(c => !c.Excluido && (string.IsNullOrWhiteSpace(_search.Text) || (c.Titulo + " " + c.Notas).Contains(_search.Text, StringComparison.CurrentCultureIgnoreCase))).ToArray();
        var columns = new Grid();
        foreach (var etapa in Enum.GetValues<EtapaCompromisso>()) {
            columns.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 270 });
            var list = new StackPanel(); var values = items.Where(c => c.Etapa == etapa).ToArray();
            var title = etapa == EtapaCompromisso.Planejado ? "A fazer" : etapa == EtapaCompromisso.EmAndamento ? "Em andamento" : "Concluídos";
            list.Children.Add(new TextBlock { Text = $"{title}   {values.Length}", FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 4, 0, 18), Foreground = (Brush)Application.Current.Resources["TextoGelo"] });
            foreach (var item in values.OrderBy(c => c.Inicio)) {
                var body = new StackPanel();
                var date = new TextBlock { Text = item.Inicio.LocalDateTime.ToString("dd MMM · HH:mm", CultureInfo.GetCultureInfo("pt-BR")), FontSize = 11, Margin = new Thickness(0, 0, 0, 10) }; date.SetResourceReference(TextBlock.ForegroundProperty, item.Fim < DateTimeOffset.Now && etapa != EtapaCompromisso.Concluido ? "CorOuro" : "AcaoCiano"); body.Children.Add(date);
                body.Children.Add(new TextBlock { Text = item.Titulo, FontSize = 17, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["TextoGelo"] });
                if (!string.IsNullOrWhiteSpace(item.Notas)) body.Children.Add(new TextBlock { Text = item.Notas, TextWrapping = TextWrapping.Wrap, MaxHeight = 70, Margin = new Thickness(0, 10, 0, 12), Foreground = (Brush)Application.Current.Resources["TextoNevoa"] });
                var buttons = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
                var edit = new Button { Content = "Editar", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 6, 4) }; edit.Click += (_, _) => Editar(item); buttons.Children.Add(edit);
                var next = new Button { Content = etapa == EtapaCompromisso.Planejado ? "Iniciar →" : etapa == EtapaCompromisso.EmAndamento ? "Concluir ✓" : "Reabrir", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 4) }; next.Click += (_, _) => Mover(item, etapa == EtapaCompromisso.Concluido ? EtapaCompromisso.Planejado : (EtapaCompromisso)((int)etapa + 1)); buttons.Children.Add(next); body.Children.Add(buttons);
                var card = CriarCardVidro(conteudo: body); card.Padding = new Thickness(16); card.CornerRadius = new CornerRadius(12); card.Cursor = System.Windows.Input.Cursors.Hand;
                Point origin = default; card.PreviewMouseLeftButtonDown += (_, e) => origin = e.GetPosition(card);
                card.MouseMove += (_, e) => { var point = e.GetPosition(card); if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && (Math.Abs(point.X - origin.X) > 8 || Math.Abs(point.Y - origin.Y) > 8)) DragDrop.DoDragDrop(card, new DataObject("Jarvis.Compromisso", item.Id), DragDropEffects.Move); };
                list.Children.Add(card);
            }
            if (values.Length == 0) list.Children.Add(new TextBlock { Text = "Arraste um cartão para esta lista.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 10, 4, 20), Foreground = (Brush)Application.Current.Resources["TextoNevoa"] });
            var lane = new Border { Child = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 750 }, Padding = new Thickness(12), Margin = new Thickness(0, 0, 12, 0), CornerRadius = new CornerRadius(14), AllowDrop = true, MinHeight = 320 }; lane.SetResourceReference(Border.BackgroundProperty, "PainelVidroEscuroFundo");
            lane.DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent("Jarvis.Compromisso") ? DragDropEffects.Move : DragDropEffects.None; e.Handled = true; };
            lane.Drop += (_, e) => { if (e.Data.GetData("Jarvis.Compromisso") is string id && _agenda.Listar().FirstOrDefault(c => c.Id == id && !c.Excluido) is { } item) Mover(item, etapa); };
            Grid.SetColumn(lane, (int)etapa); columns.Children.Add(lane);
        }
        _board.Children.Add(new ScrollViewer { Content = columns, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
    private void Carregar()
    {
        if (_calendarRoot is not null) _calendarRoot.Visibility = _quadro ? Visibility.Collapsed : Visibility.Visible;
        _board.Visibility = _quadro ? Visibility.Visible : Visibility.Collapsed; DesenharQuadro();
        if (_day < DateTime.Today) _day = DateTime.Today;
        _items.Children.Clear(); var day = _day; DesenharCalendario();
        _items.Children.Add(new TextBlock { Text = day.ToString("dddd, dd 'de' MMMM", CultureInfo.GetCultureInfo("pt-BR")), FontSize = 22, Foreground = Brushes.LightCyan, Margin = new Thickness(0, 0, 0, 18) });
        var events = _agenda.Listar().Where(c => !c.Excluido && c.Inicio.LocalDateTime.Date <= day && c.Fim.LocalDateTime.Date >= day).ToArray();
        if (events.Length == 0) _items.Children.Add(new TextBlock { Text = "Nenhum compromisso neste dia.", Foreground = Brushes.LightSlateGray });
        foreach (var item in events)
        {
            var row = new StackPanel(); row.Children.Add(new TextBlock { Text = item.Titulo, FontSize = 18, Foreground = Brushes.LightCyan, TextWrapping = TextWrapping.Wrap });
            row.Children.Add(new TextBlock { Text = $"{item.Inicio.LocalDateTime:HH:mm} – {item.Fim.LocalDateTime:HH:mm} · " + (item.AvisoMinutos < 0 ? "Sem lembrete" : $"Lembrete {item.AvisoMinutos} min antes"), Foreground = Brushes.LightSlateGray, Margin = new Thickness(0, 8, 0, 8) });
            row.Children.Add(new TextBlock { Text = item.Notas, Foreground = Brushes.WhiteSmoke, TextWrapping = TextWrapping.Wrap });
            var actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) }; var edit = new Button { Content = "Editar", Margin = new Thickness(0, 0, 12, 0) }; edit.Click += (_, _) => Editar(item); actions.Children.Add(edit);
            var remove = new Button { Content = "Excluir" }; remove.Click += (_, _) => { if (MessageBox.Show($"Excluir {item.Titulo}?", "Agenda", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return; try { _agenda.Salvar(item with { Excluido = true }); Carregar(); } catch (Exception ex) { MessageBox.Show(ex.Message); } }; actions.Children.Add(remove); row.Children.Add(actions);
            _items.Children.Add(new Border { Child = row, Padding = new Thickness(18), Margin = new Thickness(0, 0, 0, 12), Background = new SolidColorBrush(Color.FromRgb(8, 28, 42)), CornerRadius = new CornerRadius(5) });
        }
    }
    private void DesenharCalendario()
    {
        _calendar.Children.Clear(); _calendar.RowDefinitions.Clear();
        _calendar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); _calendar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); _calendar.RowDefinitions.Add(new RowDefinition());
        var nav = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
        var previous = new Button { Content = "‹", Width = 40, Height = 40, Padding = new Thickness(0), IsEnabled = _month > new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) }; DockPanel.SetDock(previous, Dock.Left); previous.Click += (_, _) => { _month = _month.AddMonths(-1); DesenharCalendario(); }; nav.Children.Add(previous);
        var next = new Button { Content = "›", Width = 40, Height = 40, Padding = new Thickness(0) }; DockPanel.SetDock(next, Dock.Right); next.Click += (_, _) => { _month = _month.AddMonths(1); DesenharCalendario(); }; nav.Children.Add(next);
        nav.Children.Add(new TextBlock { Text = _month.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("pt-BR")), Foreground = Brushes.LightCyan, FontSize = 24, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center }); _calendar.Children.Add(nav);
        var weekdays = new System.Windows.Controls.Primitives.UniformGrid { Columns = 7, Height = 32 };
        foreach (var name in new[] { "SEG", "TER", "QUA", "QUI", "SEX", "SÁB", "DOM" }) weekdays.Children.Add(new TextBlock { Text = name, FontSize = 11, Foreground = Brushes.LightSlateGray, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetRow(weekdays, 1); _calendar.Children.Add(weekdays);
        var grid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 7, Rows = 6 };
        var events = _agenda.Listar().Where(c => !c.Excluido).ToArray();
        var offset = ((int)_month.DayOfWeek + 6) % 7;
        for (var i = 0; i < 42; i++)
        {
            var date = _month.AddDays(i - offset);
            var valid = AgendaDatas.PodeSelecionar(date, DateTime.Today);
            var daily = events.Where(c => c.Inicio.LocalDateTime.Date <= date && c.Fim.LocalDateTime.Date >= date).ToArray();
            var selected = date == _day; var today = date == DateTime.Today;
            var content = new StackPanel(); content.Children.Add(new TextBlock { Text = date.Day.ToString(), FontSize = 23, FontWeight = selected || today ? FontWeights.Bold : FontWeights.Normal });
            if (daily.Length > 0) content.Children.Add(new TextBlock { Text = $"● {daily.Length}", FontSize = 11, Foreground = selected ? Brushes.Black : (Brush)Application.Current.Resources["AcaoCiano"], Margin = new Thickness(0, 5, 0, 0) });
            if (today || selected) content.Children.Add(new TextBlock { Text = selected ? "SELECIONADO" : "HOJE", FontSize = 9, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0) });
            var button = new Button { Style = (Style)Application.Current.Resources["DiaAgenda"], Content = content, IsEnabled = valid, Opacity = valid ? 1 : .28, Padding = new Thickness(8), Margin = new Thickness(3), HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Top, ToolTip = valid ? date.ToString("dd/MM/yyyy") + (daily.Length > 0 ? "\n" + string.Join("\n", daily.Select(c => c.Titulo)) : "") : "Esta data já passou", Background = selected ? (Brush)Application.Current.Resources["AcaoCiano"] : (Brush)Application.Current.Resources["PainelVidroEscuroFundo"], BorderBrush = today || selected ? (Brush)Application.Current.Resources["AcaoCiano"] : (Brush)Application.Current.Resources["BordaLinha"], BorderThickness = new Thickness(today || selected ? 2 : 1), Foreground = selected ? Brushes.Black : (Brush)Application.Current.Resources[date.Month == _month.Month ? "TextoGelo" : "TextoNevoa"] };
            button.Click += (_, _) => { if (!AgendaDatas.PodeSelecionar(date, DateTime.Today)) return; _day = date; _month = new DateTime(date.Year, date.Month, 1); Carregar(); }; grid.Children.Add(button);
        }
        Grid.SetRow(grid, 2); _calendar.Children.Add(grid);
    }
    private void Editar(Compromisso? item)
    {
        var start = item?.Inicio.LocalDateTime ?? _day.Date.AddHours(DateTime.Now.Hour + 1);
        var end = item?.Fim.LocalDateTime ?? start.AddHours(1);
        var title = new TextBox { Text = item?.Titulo ?? "" }; var notes = new TextBox { Text = item?.Notas ?? "", AcceptsReturn = true, Height = 70, TextWrapping = TextWrapping.Wrap };
        var date = new DatePicker { Style = (Style)Application.Current.Resources["SeletorDataAgenda"], DisplayDateStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), SelectedDate = start.Date < DateTime.Today ? DateTime.Today : start.Date }; var until = new DatePicker { Style = (Style)Application.Current.Resources["SeletorDataAgenda"], DisplayDateStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), SelectedDate = end.Date < DateTime.Today ? DateTime.Today : end.Date }; var time = new TextBox { Text = start.ToString("HH:mm") }; var endTime = new TextBox { Text = end.ToString("HH:mm") };
        date.BlackoutDates.AddDatesInPast(); until.BlackoutDates.AddDatesInPast();
        var reminders = new[] { -1, 0, 5, 10, 15, 30, 60, 1440 }; var reminder = new ComboBox { ItemsSource = reminders.Select(m => m < 0 ? "Sem lembrete" : $"{m} minutos antes").ToArray(), SelectedIndex = Array.IndexOf(reminders, item?.AvisoMinutos ?? 15) };
        var panel = new StackPanel { Margin = new Thickness(24) };
        void Field(string name, UIElement control) { panel.Children.Add(new TextBlock { Text = name, Foreground = Brushes.LightCyan, Margin = new Thickness(0, 10, 0, 5) }); panel.Children.Add(control); }
        Field("Título", title); Field("Data inicial", date); Field("Horário inicial (HH:mm)", time); Field("Data final", until); Field("Horário final (HH:mm)", endTime); Field("Aviso no celular", reminder); Field("Notas", notes);
        var status = new TextBlock { Foreground = Brushes.Orange, TextWrapping = TextWrapping.Wrap }; panel.Children.Add(status); var save = new Button { Content = "Salvar compromisso", Margin = new Thickness(0, 18, 0, 0) }; panel.Children.Add(save);
        var dialog = new Window { Title = "Agenda Jarvis", Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, Width = 440, Height = 680, Content = new ScrollViewer { Content = panel }, Background = (Brush)Application.Current.Resources["FundoAbismo"] };
        save.Click += (_, _) => { try { if (!TimeSpan.TryParseExact(time.Text, @"hh\:mm", null, out var h) || !TimeSpan.TryParseExact(endTime.Text, @"hh\:mm", null, out var eh) || date.SelectedDate is null || until.SelectedDate is null) throw new ArgumentException("Confira as datas e horários."); var value = new Compromisso(item?.Id ?? Guid.NewGuid().ToString(), title.Text, new DateTimeOffset(date.SelectedDate.Value.Date + h), new DateTimeOffset(until.SelectedDate.Value.Date + eh), reminders[Math.Max(0, reminder.SelectedIndex)], notes.Text, item?.Versao ?? 0, Etapa: item?.Etapa ?? EtapaCompromisso.Planejado); _agenda.Salvar(value); dialog.Close(); Carregar(); } catch (Exception ex) { status.Text = ex.Message; } };
        dialog.ShowDialog();
    }
}
