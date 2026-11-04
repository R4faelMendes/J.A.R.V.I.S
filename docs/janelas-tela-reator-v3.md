# Reator animado, janelas e tela ao vivo — versão 1.3

## Como usar

### Animação

O reator mantém a arte detalhada e agora desenha órbitas de energia, pontos luminosos e filamentos de plasma em movimento, em vez de somente aumentar/diminuir a imagem. Desktop e Android usam a mesma geometria temporal de `ReactorFluxo`, desenhada por WPF e Skia, respectivamente. No desktop, a energia reage ao áudio. Não é um vídeo nem rotação independente dos anéis metálicos da ilustração.

A animação para quando a tela fica oculta, o aplicativo móvel vai para segundo plano, ou o desktop usa modo econômico/animações reduzidas. A tela Falar do celular usa o mesmo reator do início.

### Janelas e grades

- **PC:** Aplicativos → Janelas abertas. Atualize a lista, marque as janelas, escolha o monitor e clique na grade. A ordem exibida da lista determina as posições.
- **Android:** Início → Janelas, ou Controles → Janelas e grades. Marque as janelas na ordem desejada; o app mostra Posição 1, 2 etc. Escolha monitor e grade e aplique. Desmarque/marque para mudar a ordem.
- Duas colunas e Foco lateral aceitam até duas janelas; Três colunas até três; Grade 2 × 2 até quatro. Seleções vazias, repetidas, acima da capacidade, monitores removidos ou janelas fechadas geram mensagem em vez de mover uma seleção inesperada.
- Focar/restaurar, minimizar, maximizar e fechar operam sobre a janela escolhida. Fechar solicita WM_CLOSE; não mata o processo. Um documento não salvo pode abrir uma pergunta no PC, visível pela tela ao vivo.
- Para abrir uma janela de aplicativo, use **Abrir aplicativo**, que leva ao catálogo do PC. A abertura é por aplicativo; esta versão não força múltiplas instâncias de programas que só permitem uma.
- Janelas internas do shell, ferramentas, janelas ocultas em outro desktop e o próprio Jarvis são filtrados. A listagem não é limitada às primeiras seis.

O posicionamento usa área útil do monitor, coordenadas físicas com contexto de DPI por monitor, margens, bordas visíveis DWM e preservação da ordem Z. Os mesmos cálculos atendem PC e celular. Apps elevados podem recusar ações de um Jarvis sem elevação; programas com tamanho mínimo podem não caber na célula. Esses casos não são resolvidos encerrando processos à força.

### Tela principal ao vivo

1. No PC, entre em **Dispositivos** e marque **Compartilhar minha tela principal com o app pareado**.
2. No Android atualizado, abra **Janelas → Ver tela principal ao vivo**, ou **Mais → Tela principal ao vivo**.
3. Toque em **Iniciar visualização**. Para encerrar, toque em Parar, saia da página/app ou desmarque o compartilhamento no PC.

É visualização da tela principal, sem áudio, teclado, mouse remoto ou gravação. As ações de janelas continuam disponíveis na página própria. A imagem se adapta à área disponível e à orientação do telefone.

O transporte é JPEG sob demanda em HTTPS, pelo mesmo token e certificado fixado do pareamento. A taxa alvo é até 2,5 quadros/s; dimensão máxima 1280 × 720, preservando proporção. Rede, resolução e carga do PC afetam fluidez/latência. Não é transmissão de jogos a 30/60 FPS. As imagens ficam em memória; respostas têm `Cache-Control: no-store`. Não há captura quando ninguém solicita quadros. Uma captura recente pode ser reutilizada por 300 ms entre clientes.

O compartilhamento começa desativado a cada execução do PC e não pode ser ativado remotamente. Tela bloqueada ou desktop protegido pausa a captura. Conteúdo protegido por DRM ou alguns aplicativos gráficos podem aparecer pretos. Dispositivos pareados compartilham a mesma permissão; revogue aparelhos desconhecidos no PC.

## Implementação e validação

- Contratos: `Jarvis.Core/OrganizacaoJanelas.cs`; geometria da animação: `Jarvis.Core/ReactorFluxo.cs`.
- Native Windows: `Jarvis.System/WindowController.cs` e `TransmissaoTela.cs`.
- API autenticada: GET `/api/monitores`, POST `/api/janelas/layout`, GET `/api/tela`; novo comando fixo `fechar_janela`.
- Android: `JanelasPage`, `TelaPage`, `ReactorArt`; versão 1.3, código 5. APK de entrega em `artifacts/entrega/Jarvis.apk`.
- Testes adicionados: grades em monitor com origem negativa e dimensões ímpares; validação de capacidade; movimento do reator; captura desligada; proteção HTTPS/token, revogação e ausência de cache; ordenação de seleção pela API; duas janelas WPF de teste reais organizadas/fechadas e captura JPEG real, sem gravar a tela em disco.
- Validação final: solução Release compilada com zero erros e 23 avisos; 99 testes aprovados, sem falhas ou ignorados. Os avisos incluem pacotes anteriores e APIs Skia com alternativa mais recente.
- Inicialização real com `--startup`, criação da janela principal e encerramento normal aprovados; servidor HTTPS iniciou nas verificações de execução.
- APK confirmado pelo aapt: versão 1.3, código 5; 28.080.192 bytes. SHA256 `40364B1FBECB9F7FCEEABA25EC95899CD831015B5230B6B169E9D46015622F70`. Cópias de entrega e da pasta mobile do desktop idênticas.
- Logs finais: `artifacts/build-janelas-entrega.log`, `artifacts/tests-janelas-entrega.log`, `artifacts/startup-janelas-v3.log`, `artifacts/apk-janelas-v3.json`.

Ainda depende de teste físico: instalar/atualizar APK em Android, validar orientação e legibilidade no aparelho, parear na rede real, conferir latência e permissões de aplicativos particulares, testar monitores com escalas diferentes. Não havia Android conectado à máquina durante a implementação.

Referências técnicas: [SetThreadDpiAwarenessContext](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setthreaddpiawarenesscontext), [DPI no desktop Windows](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows), [Graphics.CopyFromScreen](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.copyfromscreen).
