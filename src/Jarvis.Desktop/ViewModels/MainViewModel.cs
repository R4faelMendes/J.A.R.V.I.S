// JARVIS Desktop — ViewModel principal (MVVM, CommunityToolkit).
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jarvis.Core;

namespace Jarvis.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private PageType _paginaAtual = PageType.Nucleo;
    [ObservableProperty] private ReactorState _estadoReator = ReactorState.Repouso;
    [ObservableProperty] private double _nivelAudio;
    [ObservableProperty] private string _statusConexao = "Online";
    [ObservableProperty] private bool _modoEconomico;

    public bool SkiaAtivo => !ModoEconomico;

    partial void OnModoEconomicoChanged(bool value) => OnPropertyChanged(nameof(SkiaAtivo));
    [ObservableProperty] private string _nomeUsuario = "Lucas";
    [ObservableProperty] private string _ultimaTranscricao = "";
    [ObservableProperty] private string _fpsTexto = "";

    [RelayCommand]
    private void Navegar(PageType pagina) => PaginaAtual = pagina;

    [RelayCommand]
    private void AtivarEscuta()
    {
        EstadoReator = EstadoReator == ReactorState.Ouvindo ? ReactorState.Repouso : ReactorState.Ouvindo;
        PedidoEscutaTecla?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? PedidoEscutaTecla;

    [RelayCommand]
    private void AlternarEconomico() => ModoEconomico = !ModoEconomico;
}
