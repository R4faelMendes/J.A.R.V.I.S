using Jarvis.Core.Abstracoes;

namespace Jarvis.Voice.PalavraAtivacao;

/// <summary>
/// Palavra de ativação "Jarvis" via Vosk pt-BR pequeno, gramática restrita.
/// Modelo fica em %LOCALAPPDATA%\Jarvis\modelos\vosk-pt (bootstrap baixa).
/// Sem teste de microfone nesta entrega, só a estrutura pronta.
/// </summary>
public sealed class DetectorPalavraAtivacao : IDisposable
{
    public event EventHandler<float>? ConfiancaMudou;
    public bool Ativo { get; private set; }

    public Task IniciarAsync(string pastaModelo, string[] gramatica, CancellationToken ct = default)
    {
        Ativo = true;
        // Fase 2 final: carregar Vosk.Model(pastaModelo) + reconhecedor com gramática.
        return Task.CompletedTask;
    }

    public void ProcessarAmostra(ReadOnlySpan<short> audio16kHz)
    {
        // Fase 2 final: alimentar o reconhecedor; disparar ao detectar "jarvis".
    }

    public void Parar() => Ativo = false;
    public void Dispose() => Parar();
}
