using Jarvis.Voice;
namespace Jarvis.Tests;

public sealed class ModeloVoskAusenteTest
{
    [Fact]
    public void ModeloAusenteOuIncompletoFalhaAntesDaBibliotecaNativa()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "jarvis-modelo-" + Guid.NewGuid().ToString("N"));
        using var voz = new VoiceService();
        using var wake = new WakeWordListener(voz);
        Assert.Throws<DirectoryNotFoundException>(() => wake.Carregar(pasta));
        Directory.CreateDirectory(pasta);
        try { Assert.Throws<FileNotFoundException>(() => wake.Carregar(pasta)); }
        finally { Directory.Delete(pasta); }
    }
}
