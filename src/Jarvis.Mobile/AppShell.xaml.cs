namespace Jarvis.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("painel-atalhos", typeof(Views.PainelAtalhosPage));
        Routing.RegisterRoute("agenda", typeof(Views.AgendaPage));
        Routing.RegisterRoute("falar", typeof(Views.FalarPage));
        Routing.RegisterRoute("parear", typeof(Views.ParearPage));
        Routing.RegisterRoute("janelas", typeof(Views.JanelasPage));
        Routing.RegisterRoute("tela", typeof(Views.TelaPage));
        Routing.RegisterRoute("desempenho", typeof(Views.DesempenhoPage));
        Routing.RegisterRoute("rede-privada", typeof(Views.RedePrivadaPage));
        Routing.RegisterRoute("aparelho", typeof(Views.AparelhoPage));
    }
}
