using Jarvis.Core;
namespace Jarvis.Mobile.Services;
public static class DispositivoCompanheiro
{
    public static string Estado { get; internal set; } = "Abra o painel para conectar o aparelho.";
    public static bool Ativo
    {
        get
        {
#if ANDROID
            return AparelhoService.Ativo;
#else
            return false;
#endif
        }
    }
    public static DadosAparelho Coletar()
    {
        var (bateria, carregando) = LerBateria(); long? livre = null, total = null;
#if ANDROID
        try { using var fs = new Android.OS.StatFs(Android.OS.Environment.DataDirectory!.AbsolutePath); livre = fs.AvailableBytes; total = fs.TotalBytes; } catch { }
#endif
        var conexao = Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.WiFi) ? "Wi-Fi" : Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.Cellular) ? "Dados móveis" : "Outra rede";
        var vpn = false;
        return new(DeviceInfo.Current.Manufacturer + " " + DeviceInfo.Current.Model, DeviceInfo.Current.Platform + " " + DeviceInfo.Current.VersionString, bateria, carregando, livre, total, conexao, vpn, ControleDisponivel, ControleSuportado);
    }
    public static bool ControleSuportado
    {
        get {
#if ANDROID
            return ControleCelularAndroid.Suportado;
#else
            return false;
#endif
        }
    }
    public static bool ControleDisponivel
    {
        get
        {
#if ANDROID
            return ControleCelularAndroid.Ativo;
#else
            return false;
#endif
        }
    }
    public static void ConfigurarControle()
    {
#if ANDROID
        ControleCelularAndroid.Configurar();
#endif
    }
    public static (int? Percentual, bool? Carregando) LerBateria()
    {
        try
        {
#if ANDROID
            // ACTION_BATTERY_CHANGED fornece a leitura atual sem BATTERY_STATS (permissão de sistema).
            using var filter = new Android.Content.IntentFilter(Android.Content.Intent.ActionBatteryChanged);
            using var reading = Android.App.Application.Context.RegisterReceiver(null, filter);
            if (reading is null) return (null, null);
            var level = BateriaAparelho.Percentual(reading.GetIntExtra("level", -1), reading.GetIntExtra("scale", -1));
            var state = reading.GetIntExtra("status", -1); var plugged = reading.GetIntExtra("plugged", -1);
            bool? charging = state is 2 or 5 ? true : plugged > 0 ? true : plugged == 0 ? false : null;
            return (level, charging);
#else
            var level = Battery.Default.ChargeLevel; var state = Battery.Default.State;
            return (double.IsFinite(level) && level >= 0 ? (int)Math.Round(Math.Clamp(level, 0, 1) * 100) : null,
                state is BatteryState.Charging or BatteryState.Full ? true : state is BatteryState.Discharging or BatteryState.NotCharging ? false : null);
#endif
        }
        catch { return (null, null); }
    }
    public static void Restaurar()
    {
        if (Ativo || App.Conexao.BaseUrl is null || !Preferences.Get("painel_aparelho", true) || !App.EmPrimeiroPlano) return;
#if ANDROID
        try { AparelhoService.Iniciar(); } catch { Estado = "Abra Meu aparelho e ative a conexão em segundo plano."; }
#endif
    }
    public static async Task AtivarAsync()
    {
        if (App.Conexao.BaseUrl is null) throw new InvalidOperationException("Pareie com o PC primeiro.");
        await Notificacoes.AtivarAsync(); Preferences.Set("painel_aparelho", true); Restaurar();
    }
    public static void Parar(bool manterPreferencia = false)
    {
        if (!manterPreferencia) Preferences.Set("painel_aparelho", false);
#if ANDROID
        AparelhoService.Parar();
#endif
    }
}
public static class EspelhoCelular
{
    public static bool Compatibilidade { get => DispositivoCompanheiro.ControleSuportado && Preferences.Get("captura_compativel", false); set => Preferences.Set("captura_compativel", value); }
    public static bool Ativo
    {
        get
        {
#if ANDROID
            return EspelhoCelularService.Ativo;
#else
            return false;
#endif
        }
    }
    public static string Estado { get; internal set; } = "Compartilhamento parado.";
    public static async Task IniciarAsync()
    {
        if (App.Conexao.BaseUrl is null) throw new InvalidOperationException("Pareie com o PC primeiro.");
#if ANDROID
        if (Ativo) return;
        if (Platform.CurrentActivity is not MainActivity activity) throw new InvalidOperationException("Abra o Jarvis no celular para autorizar.");
        await DispositivoCompanheiro.AtivarAsync();
        await activity.AutorizarEspelhoAsync();
#else
        await Task.CompletedTask; throw new NotSupportedException("Espelhamento disponível no Android.");
#endif
    }
    public static void Parar()
    {
#if ANDROID
        EspelhoCelularService.Parar();
#endif
    }
}
