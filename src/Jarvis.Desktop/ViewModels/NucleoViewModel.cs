using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jarvis.Core.Abstracoes;

namespace Jarvis.Desktop.ViewModels;

/// <summary>
/// Estado compartilhado do Núcleo: saudação por horário, estado do reator,
/// transcrição ao vivo e os comandos Falar / Ver conversa.
/// Na fase 2 o IServicoVoz real entra aqui; por enquanto é simulação local.
/// </summary>
public sealed partial class NucleoViewModel : ObservableObject
{
    [ObservableProperty]
    private EstadoReator _estado = EstadoReator.Repouso;

    [ObservableProperty]
    private string _saudacao = SaudacaoPorHorario("usuário");

    [ObservableProperty]
    private string _transcricao = string.Empty;

    [ObservableProperty]
    private bool _ouvindo;

    public string RotuloEstado => Estado switch
    {
        EstadoReator.Ouvindo => "Ouvindo",
        EstadoReator.Pensando => "Pensando",
        EstadoReator.Falando => "Falando",
        EstadoReator.Alerta => "Alerta",
        EstadoReator.Mudo => "Mudo",
        EstadoReator.Economico => "Modo econômico",
        _ => "Em repouso",
    };

    partial void OnEstadoChanged(EstadoReator value)
    {
        OnPropertyChanged(nameof(RotuloEstado));
        Ouvindo = value == EstadoReator.Ouvindo;
    }

    public static string SaudacaoPorHorario(string nome)
    {
        var hora = DateTime.Now.Hour;
        var periodo = hora switch
        {
            < 5 => "Boa madrugada",
            < 12 => "Bom dia",
            < 18 => "Boa tarde",
            _ => "Boa noite",
        };
        return $"{periodo}, {nome}";
    }

    [RelayCommand]
    private async Task AlternarEscutaAsync()
    {
        if (Estado == EstadoReator.Ouvindo)
        {
            Estado = EstadoReator.Pensando;
            await Task.Delay(900);
            Estado = EstadoReator.Falando;
            await Task.Delay(1200);
            Estado = EstadoReator.Repouso;
        }
        else
        {
            Transcricao = string.Empty;
            Estado = EstadoReator.Ouvindo;
        }
    }
}
