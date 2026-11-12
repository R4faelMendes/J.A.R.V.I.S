namespace Jarvis.Voice;

public static class ModeloVosk
{
    public static bool Completo(string pasta)
    {
        bool Arquivo(string nome, long tamanho) => File.Exists(Path.Combine(pasta, nome)) && new FileInfo(Path.Combine(pasta, nome)).Length >= tamanho;
        // O modelo português 0.3 oficial é anterior ao layout am/conf/graph.
        var legado = Arquivo("final.mdl", 100_000) && Arquivo("mfcc.conf", 10) && Arquivo("Gr.fst", 100_000) && Arquivo("HCLr.fst", 100_000);
        var atual = Arquivo("am/final.mdl", 100_000) && Arquivo("conf/model.conf", 10) &&
            (Arquivo("graph/HCLG.fst", 100_000) || Arquivo("graph/Gr.fst", 100_000) && Arquivo("graph/HCLr.fst", 100_000));
        return legado || atual;
    }
}
