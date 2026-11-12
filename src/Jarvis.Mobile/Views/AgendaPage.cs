using Jarvis.Core;
namespace Jarvis.Mobile.Views;
public sealed class AgendaPage : ContentPage
{
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _day = DateTime.Today;
    private readonly Grid _calendar = new() { ColumnSpacing = 3, RowSpacing = 4 };
    private readonly Label _heading = new() { FontSize = 21, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _status = new() { FontSize = 12, TextColor = Color.FromArgb("#7DBBD3") };
    private readonly VerticalStackLayout _events = new() { Spacing = 10 };
    private readonly Button _previous = new() { Text = "‹" };
    private readonly VerticalStackLayout _board = new() { Spacing = 16 };
    private bool _quadro = true;
    public AgendaPage()
    {
        Title = "Agenda";
        var root = new VerticalStackLayout { Padding = 18, Spacing = 14 };
        root.Add(new Label { Text = "Seu tempo, organizado", FontSize = 26 }); root.Add(_status);
        var nav = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        _previous.Clicked += (_, _) => { _month = _month.AddMonths(-1); Render(); }; nav.Add(_previous, 0); nav.Add(_heading, 1);
        var next = new Button { Text = "›" }; next.Clicked += (_, _) => { _month = _month.AddMonths(1); Render(); }; nav.Add(next, 2); root.Add(nav);
        var view = new Button { Text = "Alternar quadro / calendário" }; view.Clicked += (_, _) => { _quadro = !_quadro; nav.IsVisible = !_quadro; Render(); }; root.Add(view); nav.IsVisible = false; root.Add(_board);
        for (var i = 0; i < 7; i++) _calendar.ColumnDefinitions.Add(new(GridLength.Star)); for (var i = 0; i < 7; i++) _calendar.RowDefinitions.Add(new(GridLength.Auto)); root.Add(_calendar);
        var actions = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } }; var add = new Button { Text = "+ Compromisso" }; add.Clicked += async (_, _) => await EditarAsync(null); actions.Add(add, 0);
        var sync = new Button { Text = "Sincronizar" }; sync.Clicked += async (_, _) => { sync.IsEnabled = false; _status.Text = await App.Agenda.SincronizarAsync(); Render(); sync.IsEnabled = true; }; actions.Add(sync, 1); root.Add(actions);
        var notifications = new Button { Text = "Ativar notificações e alarmes" }; notifications.Clicked += async (_, _) => { await Services.Notificacoes.AtivarAsync(true); _status.Text = "Alertas locais ativados conforme as permissões do Android."; }; root.Add(notifications); root.Add(_events); Content = new ScrollView { Content = root };
    }
    protected override async void OnAppearing() { base.OnAppearing(); Render(); _status.Text = await App.Agenda.SincronizarAsync(); Render(); }
    private void Render()
    {
        _calendar.IsVisible = _events.IsVisible = !_quadro; _board.IsVisible = _quadro;
        RenderBoard();
        if (_day < DateTime.Today) _day = DateTime.Today;
        _previous.IsEnabled = _month > new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var items = App.Agenda.Listar().Where(c => !c.Excluido || App.Agenda.Pendente(c.Id)).ToArray(); _heading.Text = _month.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")); _calendar.Clear();
        var weekdays = new[] { "S", "T", "Q", "Q", "S", "S", "D" }; for (var i = 0; i < 7; i++) _calendar.Add(new Label { Text = weekdays[i], HorizontalTextAlignment = TextAlignment.Center, TextColor = Colors.LightSlateGray }, i, 0);
        var offset = ((int)_month.DayOfWeek + 6) % 7;
        for (var day = 1; day <= DateTime.DaysInMonth(_month.Year, _month.Month); day++)
        {
            var date = _month.AddDays(day - 1); var has = items.Any(c => c.Inicio.LocalDateTime.Date == date);
            var valid = AgendaDatas.PodeSelecionar(date, DateTime.Today);
            var button = new Button { Text = day.ToString() + (has ? " •" : ""), IsEnabled = valid, Opacity = valid ? 1 : .28, FontSize = 12, Padding = 0, MinimumHeightRequest = 42, CornerRadius = 5, BackgroundColor = date == _day ? Color.FromArgb("#146987") : Color.FromArgb("#0A1D2C") }; button.Clicked += (_, _) => { if (!AgendaDatas.PodeSelecionar(date, DateTime.Today)) return; _day = date; Render(); }; var index = offset + day - 1; _calendar.Add(button, index % 7, index / 7 + 1);
        }
        _events.Clear(); _events.Add(new Label { Text = _day.ToString("dd/MM/yyyy"), FontSize = 20 });
        var selected = items.Where(c => c.Inicio.LocalDateTime.Date <= _day && c.Fim.LocalDateTime.Date >= _day).ToArray();
        if (selected.Length == 0) _events.Add(new Label { Text = "Sem compromissos neste dia." });
        foreach (var item in selected)
        {
            var card = new VerticalStackLayout { Padding = 16, Spacing = 8, BackgroundColor = Color.FromArgb("#0A1D2C") };
            card.Add(new Label { Text = item.Titulo, FontSize = 18 }); card.Add(new Label { Text = $"{item.Inicio.LocalDateTime:HH:mm} – {item.Fim.LocalDateTime:HH:mm}" + (App.Agenda.Pendente(item.Id) ? " · pendente de sincronização" : ""), FontSize = 12 });
            card.Add(new Label { Text = item.AvisoMinutos < 0 ? "Sem lembrete" : $"Aviso {item.AvisoMinutos} minutos antes · offline", FontSize = 12, TextColor = Color.FromArgb("#00D800") }); if (!string.IsNullOrWhiteSpace(item.Notas)) card.Add(new Label { Text = item.Notas });
            var edit = new Button { Text = "Editar / excluir" }; edit.Clicked += async (_, _) => await EditarAsync(item); card.Add(edit);
            if (App.Agenda.Pendente(item.Id)) { var resolve = new Button { Text = "Resolver conflito / consultar versão do PC" }; resolve.Clicked += async (_, _) => { var answer = await DisplayActionSheet("Resolver edição pendente", "Cancelar", null, "Usar versão do PC", "Preservar minha edição como cópia"); if (answer is not ("Usar versão do PC" or "Preservar minha edição como cópia")) return; try { await App.Agenda.ResolverAsync(item.Id, answer.StartsWith("Preservar")); _status.Text = await App.Agenda.SincronizarAsync(); Render(); } catch (Exception ex) { _status.Text = ex.Message; } }; card.Add(resolve); }
            _events.Add(card);
        }
    }
    private Task EditarAsync(Compromisso? item) => Navigation.PushAsync(new EditorCompromissoPage(item, _day));
    private void RenderBoard()
    {
        _board.Clear(); if (!_quadro) return;
        var items = App.Agenda.Listar().Where(c => !c.Excluido).ToArray();
        foreach (var etapa in Enum.GetValues<EtapaCompromisso>()) {
            var cards = items.Where(c => c.Etapa == etapa).OrderBy(c => c.Inicio).ToArray();
            var column = new VerticalStackLayout { Spacing = 10 };
            column.Add(new Label { Text = (etapa == EtapaCompromisso.Planejado ? "A fazer" : etapa == EtapaCompromisso.EmAndamento ? "Em andamento" : "Concluídos") + "  ·  " + cards.Length, FontSize = 20, FontAttributes = FontAttributes.Bold });
            foreach (var item in cards) {
                var content = new VerticalStackLayout { Spacing = 10 };
                content.Add(new Label { Text = item.Inicio.LocalDateTime.ToString("dd/MM · HH:mm") + (App.Agenda.Pendente(item.Id) ? " · pendente" : ""), FontSize = 11, TextColor = Color.FromArgb(TemasJarvis.Atual.Acento) });
                content.Add(new Label { Text = item.Titulo, FontSize = 18, FontAttributes = FontAttributes.Bold });
                if (!string.IsNullOrWhiteSpace(item.Notas)) content.Add(new Label { Text = item.Notas, FontSize = 12, MaxLines = 3 });
                var actions = new HorizontalStackLayout { Spacing = 8 }; var edit = new Button { Text = "Editar", FontSize = 12 }; edit.Clicked += async (_, _) => await EditarAsync(item); actions.Add(edit);
                var move = new Button { Text = etapa == EtapaCompromisso.Planejado ? "Iniciar →" : etapa == EtapaCompromisso.EmAndamento ? "Concluir ✓" : "Reabrir", FontSize = 12 };
                move.Clicked += async (_, _) => { try { await App.Agenda.SalvarAsync(item with { Etapa = etapa == EtapaCompromisso.Concluido ? EtapaCompromisso.Planejado : (EtapaCompromisso)((int)etapa + 1) }); Render(); _status.Text = await App.Agenda.SincronizarAsync(); Render(); } catch (Exception ex) { _status.Text = ex.Message; } }; actions.Add(move); content.Add(actions);
                column.Add(new Border { Content = content, Padding = 16, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 }, BackgroundColor = Color.FromArgb(TemasJarvis.Atual.Painel), Stroke = Color.FromArgb(TemasJarvis.Atual.Linha) });
            }
            if (cards.Length == 0) column.Add(new Label { Text = "Nenhum cartão nesta lista.", FontSize = 12 });
            _board.Add(column);
        }
    }
}

internal sealed class EditorCompromissoPage : ContentPage
{
    public EditorCompromissoPage(Compromisso? item, DateTime day)
    {
        Title = item is null ? "Novo compromisso" : "Editar compromisso";
        var start = item?.Inicio.LocalDateTime ?? day.Date.AddHours(DateTime.Now.Hour + 1); var end = item?.Fim.LocalDateTime ?? start.AddHours(1);
        var title = new Entry { Placeholder = "Título", Text = item?.Titulo, MaxLength = 160 }; var date = new DatePicker { MinimumDate = DateTime.Today, Date = start.Date < DateTime.Today ? DateTime.Today : start.Date }; var time = new TimePicker { Time = start.TimeOfDay, Format = "HH:mm" }; var until = new DatePicker { MinimumDate = DateTime.Today, Date = end.Date < DateTime.Today ? DateTime.Today : end.Date }; var endTime = new TimePicker { Time = end.TimeOfDay, Format = "HH:mm" };
        var values = new[] { -1, 0, 5, 10, 15, 30, 60, 1440 }; var reminder = new Picker { Title = "Lembrete", ItemsSource = values.Select(v => v < 0 ? "Sem lembrete" : $"{v} minutos antes").ToArray(), SelectedIndex = Array.IndexOf(values, item?.AvisoMinutos ?? 15) };
        var notes = new Editor { Placeholder = "Notas", Text = item?.Notas, HeightRequest = 100, MaxLength = 4000 }; var status = new Label { TextColor = Colors.Orange }; var save = new Button { Text = "Salvar compromisso" };
        var root = new VerticalStackLayout { Padding = 20, Spacing = 12 }; root.Add(title); root.Add(new Label { Text = "Início" }); root.Add(date); root.Add(time); root.Add(new Label { Text = "Fim" }); root.Add(until); root.Add(endTime); root.Add(reminder); root.Add(notes); root.Add(status); root.Add(save);
        save.Clicked += async (_, _) => { save.IsEnabled = false; try { var value = new Compromisso(item?.Id ?? Guid.NewGuid().ToString(), title.Text ?? "", new DateTimeOffset(date.Date + time.Time), new DateTimeOffset(until.Date + endTime.Time), values[Math.Max(0, reminder.SelectedIndex)], notes.Text ?? "", item?.Versao ?? 0, Etapa: item?.Etapa ?? EtapaCompromisso.Planejado); await App.Agenda.SalvarAsync(value); await Services.Notificacoes.AtivarAsync(); await App.Agenda.SincronizarAsync(); await Navigation.PopAsync(); } catch (Exception ex) { status.Text = ex.Message; } finally { save.IsEnabled = true; } };
        if (item is not null) { var remove = new Button { Text = "Excluir compromisso", TextColor = Colors.OrangeRed }; remove.Clicked += async (_, _) => { if (!await DisplayAlert("Agenda", "Excluir este compromisso?", "Excluir", "Cancelar")) return; await App.Agenda.SalvarAsync(item with { Excluido = true }); await App.Agenda.SincronizarAsync(); await Navigation.PopAsync(); }; root.Add(remove); }
        Content = new ScrollView { Content = root };
    }
}

