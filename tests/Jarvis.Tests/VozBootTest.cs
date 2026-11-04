// Diagnóstico: boot do MotorVoz fora da UI (revela exceção escondida pelo catch).
using Jarvis.Desktop.Services;

namespace Jarvis.Tests;

public sealed class VozBootTest
{
    [Fact]
    public void Iniciar_CriaLogSemErro()
    {
        var log = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Jarvis", "voz.log");

        var modelos = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "Jarvis.Desktop", "..", "..", "models"));
        // Caminho robusto: sobe até a raiz Jarvis e entra em models.
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "models")))
            dir = dir.Parent;
        modelos = Path.Combine(dir!.FullName, "models");
        using var motor = new MotorVoz(modelos);
        var ex = Record.Exception(() => motor.Iniciar());
        Assert.Null(ex);
        Assert.True(File.Exists(log), "voz.log deveria existir após Iniciar()");
        var texto = File.ReadAllText(log);
        Assert.Contains("microfone aberto", texto);
    }
}
