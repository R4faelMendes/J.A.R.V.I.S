using Jarvis.Ai;
using Jarvis.Voice;
namespace Jarvis.Desktop.Services;
public static class EscolhaTranscricao
{
    public static string Selecionar(string whisper, string vosk, string wake, bool exigirAtivacao)
    {
        bool Valido(string texto) => !string.IsNullOrWhiteSpace(texto) && (!exigirAtivacao || WakeWordMatcher.AtivacaoNoInicio(texto, wake)) &&
            !string.IsNullOrWhiteSpace(WakeWordMatcher.StripWakeWord(texto, wake)) && RoteadorIntencoes.Rotear(WakeWordMatcher.StripWakeWord(texto, wake)) is not null;
        if (Valido(whisper)) return whisper;
        if (Valido(vosk)) return vosk;
        if (string.IsNullOrWhiteSpace(whisper)) return vosk;
        return whisper;
    }
}
