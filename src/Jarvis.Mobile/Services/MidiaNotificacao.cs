namespace Jarvis.Mobile.Services;
public static class MidiaNotificacao
{
    public static bool EmExecucao
    {
        get
        {
#if ANDROID
            return SpotifyRemotoService.Ativo;
#else
            return false;
#endif
        }
    }
    public static async Task AtivarAsync()
    {
        if (App.Conexao.BaseUrl is null) throw new InvalidOperationException("Pareie com o PC primeiro.");
        await Notificacoes.AtivarAsync(); Preferences.Set("controle_spotify", true);
#if ANDROID
        SpotifyRemotoService.Iniciar();
#endif
    }
    public static void Restaurar()
    {
#if ANDROID
        if (Preferences.Get("controle_spotify", false) && App.Conexao.BaseUrl is not null) SpotifyRemotoService.Iniciar();
#endif
    }
    public static void Parar()
    {
        Preferences.Set("controle_spotify", false);
#if ANDROID
        SpotifyRemotoService.Parar();
#endif
    }
}
