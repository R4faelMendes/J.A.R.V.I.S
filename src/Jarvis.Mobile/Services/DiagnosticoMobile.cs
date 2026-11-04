namespace Jarvis.Mobile.Services;
public static class DiagnosticoMobile
{
    private static string Arquivo => Path.Combine(FileSystem.AppDataDirectory, "ultima-falha-app.txt");
    public static void Iniciar()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Registrar(e.ExceptionObject as Exception);
#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => Registrar(e.Exception);
#endif
    }
    private static void Registrar(Exception? e)
    {
        try { var texto = $"{DateTimeOffset.Now:O}\nJarvis {AppInfo.Current.VersionString}\n{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} · Android {DeviceInfo.Current.VersionString}\n{e?.GetType().FullName}\n{e?.StackTrace}"; File.WriteAllText(Arquivo, texto[..Math.Min(8000, texto.Length)]); } catch { }
    }
    public static async Task CompartilharAsync()
    {
        var destino = Path.Combine(FileSystem.CacheDirectory, "Jarvis-diagnostico.txt");
        var erro = File.Exists(Arquivo) ? await File.ReadAllTextAsync(Arquivo) : "Nenhum fechamento inesperado registrado nesta versão.";
        await File.WriteAllTextAsync(destino, $"Jarvis {AppInfo.Current.VersionString}\nAndroid {DeviceInfo.Current.VersionString}\nEspelho: {EspelhoCelular.Estado}\nConexão: {App.Conexao.UltimoDiagnostico}\n\n{erro}");
        await Share.Default.RequestAsync(new ShareFileRequest("Diagnóstico do Jarvis", new ShareFile(destino)));
    }
}
