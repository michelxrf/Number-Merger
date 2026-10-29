# Number Merge — Documentação do Estado Atual

> Última atualização: 2026-10-29  
> Projeto: `D:\Projects\Number Merger`  
> Unity: `6000.3.10f1`  
> Último commit incluído: `9d541ef bug fix`

## Sumário

1. [Visão geral](#1-visão-geral)
2. [Estado atual](#2-estado-atual)
3. [Tecnologias e configuração](#3-tecnologias-e-configuração)
4. [Estrutura de arquivos](#4-estrutura-de-arquivos)
5. [Arquitetura](#5-arquitetura)
6. [Regras do jogo](#6-regras-do-jogo)
7. [Fluxo de uma jogada](#7-fluxo-de-uma-jogada)
8. [Persistência](#8-persistência)
9. [Implementação por parte](#9-implementação-por-parte)
10. [Cena e layout](#10-cena-e-layout)
11. [Input e animações](#11-input-e-animações)
12. [Como executar e testar](#12-como-executar-e-testar)
13. [Verificações realizadas](#13-verificações-realizadas)
14. [Limitações e próximos passos](#14-limitações-e-próximos-passos)
15. [Referência rápida](#15-referência-rápida)
16. [Mudanças desde a versão anterior](#16-mudanças-desde-a-versão-anterior)

## 1. Visão geral

**Number Merge** é um protótipo de jogo de combinação de números em um tabuleiro vertical. O jogador recebe peças de valor `2` ou maior (conforme o estado do tabuleiro), escolhe uma coluna arrastando o ponteiro e solta a peça. Peças iguais adjacentes se combinam, geram uma peça maior e podem iniciar cascatas.

O estado atual é um **protótipo jogável**, com:

- Tabuleiro de 5 colunas e 7 linhas visíveis, mais uma linha de overflow.
- Geração de peças adaptativa à pressão do tabuleiro, com pesos e temperatura configuráveis em um único arquivo.
- Combinação por vizinhança ortogonal, com cascatas e gravidade.
- Pontuação e recorde, ambos persistidos com `PlayerPrefs`.
- Retomada da partida em andamento ao reabrir o jogo.
- Tela de game over com reinício.
- Overlay de debug (tecla F12).
- Música de fundo via `AudioManager`.

A cena `MainMenu` ainda é um placeholder com câmera. Não existe fluxo de menu.

## 2. Estado atual

### Funciona

- Inserção na célula vazia mais baixa da coluna.
- Rejeição de coluna cheia (a peça atual não é consumida).
- Combinação de 2 a 5 peças iguais adjacentes, com resultado limitado a `8192`.
- Prioridade por maior número de vizinhos iguais; empate resolvido aleatoriamente.
- Gravidade e múltiplas cascatas, com proteções contra progresso inválido, ciclos e loops longos.
- Geração de peças: elegibilidade e probabilidades base em `SpawnTable`, ajustadas pela pressão do tabuleiro (ver seção 6).
- Fila de duas peças sempre visível: atual (centro) e próxima (canto).
- Prévia da peça atual acompanhando a coluna escolhida durante o arraste.
- Pontuação somando o valor resultante de cada combinação.
- Recorde persistido em `PlayerPrefs`.
- Salvamento da partida após cada jogada válida; retomada ao abrir o jogo.
- Game over quando uma coluna fica com 8 peças; o save da partida é apagado.
- Tela de game over com pontos e recorde, e botão de reinício.
- Overlay de debug (F12) com fechar, reiniciar e limpar dados.
- Música de fundo tocada pelo `AudioManager` (persistente entre cenas).
- Fundo em tela cheia (`FullscreenBackground`) com ajuste "cover".
- Cena `Gameplay` como única cena em Build Settings.

### Pendente ou não implementado

- Cena `MainMenu` funcional e transição menu → gameplay. (O objeto `ContinueButton` existe na cena `Gameplay`, mas nenhum script do projeto o referencia.)
- Efeitos sonoros: `PlaySfx` e `PlayUi` existem, mas nada os chama ainda.
- Tipo de peça "bad tile": o campo `BadTileSprite` existe no tema, mas a mecânica não foi implementada.
- Teste automatizado do caminho real de toque no Editor.
- Testes automatizados para os demais sistemas de lógica. A cobertura atual é apenas a geração de peças (ver seção 13).
- Ajuste definitivo de responsividade e orientação.

## 3. Tecnologias e configuração

| Item | Configuração |
|---|---|
| Unity | `6000.3.10f1` |
| Render pipeline | URP 2D |
| UI | uGUI 2.0 com TextMeshPro (`TMP_Text`) |
| Fonte | `ShadowsIntoLight-Regular SDF` |
| Input | Input System (leitura direta de `Pointer.current` e `Keyboard.current`) |
| `activeInputHandler` | `1` (Input System apenas) |
| Canvas | Overlay |
| Referência do CanvasScaler | `1080×1920`, match `0.5` (valor anterior, não reverificado) |
| Áudio | `AudioMixer` com grupos Music, Sfx e Ui |
| Animações | LeanTween em `Assets/LeanTween` |
| Assembly | Sem `.asmdef`; código do jogo em `Assembly-CSharp` |
| Namespaces | `NumberMerge.Gameplay`, `NumberMerge.Managers`, `NumberMerge.Save`, `NumberMerge.UI`, `NumberMerge.Audio` |
| Testes | 72 testes EditMode próprios da geração de peças, em `Assets/!/Tests/Editor` (Unity Test Framework) |

Observação: a referência do CanvasScaler é retrato (`1080×1920`), mas o layout foi montado e verificado em landscape (`1920×1080`). Se o alvo final for retrato, o layout precisa ser revisto.

## 4. Estrutura de arquivos

```text
Assets/
  !/
    Audio/
      BGM/                  Música de fundo
      SFX/                  tile click.mp3 (ainda não utilizado)
      MainMixer.mixer       AudioMixer com os grupos Music, Sfx e Ui
    Fonts/                  ShadowsIntoLight (TTF e SDF)
    Icon/                   Ícones
    Prefabs/
      Tile.prefab           Prefab base do tile
      UI/DebugScreen.prefab Overlay de debug
    Scenes/
      Gameplay.unity        Cena jogável
      MainMenu.unity        Placeholder de menu
    Scripts/
      Audio/
        AudioManager.cs
      Gameplay/
        BoardController.cs, BoardModel.cs, BoardPosition.cs, BoardSnapshot.cs,
        CascadeResolver.cs, CascadeResult.cs, CascadeStep.cs,
        MergeMath.cs, MergeOutcome.cs, MergeResolver.cs, MoveResult.cs,
        RandomSource.cs, SpawnTable.cs,
        PieceGenerator.cs, PieceGenerationSettings.cs, BoardPressureCalculator.cs,
        PieceDifficultyEvaluator.cs, CandidateSelection.cs, PieceGenerationDecision.cs,
        PieceGenerationConfig.cs (ScriptableObject)
      Managers/
        ScoreService.cs
      Save/
        BoardState.cs, SaveSystem.cs
      UI/
        BoardView.cs, BoardViewConfig.cs (ScriptableObject), DebugScreen.cs,
        FullscreenBackground.cs, GameOverScreen.cs, GameplayScreen.cs, PieceQueueView.cs,
        TileTheme.cs, TileView.cs
    Data/
      BoardViewConfig.asset  Layout e durações de animação do tabuleiro (ScriptableObject)
    Settings/
      PieceGenerationConfig.asset  Balanceamento da geração de peças (ScriptableObject)
    Sprites/                Tiles, slot, fundos, partículas
    Themes/
      DefaultTileTheme.asset  Tema padrão do tabuleiro e das peças
    Tests/
      Editor/                 Testes EditMode da geração de peças (ver seção 13)
        TestBoards.cs, BoardPressureCalculatorTests.cs, PieceDifficultyEvaluatorTests.cs,
        CandidateSelectionTests.cs, PieceGeneratorTests.cs
  LeanTween/                Biblioteca de animações (inclui pasta Testing com exemplos próprios)
ProjectSettings/
  ProjectSettings.asset
  EditorBuildSettings.asset  Build: apenas Gameplay
```

Os testes próprios ficam em `Assets/!/Tests/Editor`, sem `.asmdef`. Eles compilam em `Assembly-CSharp-Editor`, porque um `.asmdef` não pode referenciar `Assembly-CSharp`. Não há outros scripts de Editor.

## 5. Arquitetura

A implementação separa lógica de jogo e apresentação:

```text
Pointer.current / Keyboard.current
      │
      ▼
GameplayScreen ──────────────► PieceGenerator ──► SpawnTable (elegibilidade e base)
      │                              ├──► BoardPressureCalculator (pressão)
      │                              ├──► PieceDifficultyEvaluator (pontuação)
      │                              └──► CandidateSelection (softmax) ──► SpawnTable.Pick
      │                              │
      │                              ▼
      ├──────────────────────► PieceQueueView
      │                              │
      ▼                              ▼
BoardController ─────────────► BoardView ──► TileView ──► TileTheme
      │                              │
      ├── BoardModel                 └── LeanTween / coroutines
      ├── CascadeResolver
      │     └── MergeResolver
      └── ScoreService

GameplayScreen ──► SaveSystem ──► PlayerPrefs (BoardState em JSON, recorde)
GameOverScreen / DebugScreen ──► SceneManager (recarrega Gameplay)
AudioManager (singleton, DontDestroyOnLoad) ──► AudioMixer
```

### Camadas

- **Gameplay puro:** `BoardModel`, `BoardController`, `CascadeResolver`, `MergeResolver`, `MergeMath`, `ScoreService`, `RandomSource`, e a geração de peças: `PieceGenerator`, `PieceGenerationSettings`, `SpawnTable`, `BoardPressureCalculator`, `PieceDifficultyEvaluator`, `CandidateSelection`, `PieceGenerationDecision`.
- **Persistência:** `SaveSystem`, `BoardState`.
- **Apresentação:** `GameplayScreen`, `BoardView`, `BoardViewConfig`, `PieceQueueView`, `TileView`, `TileTheme`, `GameOverScreen`, `DebugScreen`, `FullscreenBackground`.
- **Áudio:** `AudioManager`.
- **Dados de resultado:** `BoardSnapshot`, `MoveResult`, `CascadeResult`, `CascadeStep`, `MergeOutcome`, `BoardPosition`.

A lógica pura não depende de `MonoBehaviour` e recebe dependências por construtor (`IRandomSource` para aleatoriedade). `GameplayScreen` é o ponto de composição da cena.

## 6. Regras do jogo

### Tabuleiro

- **5 colunas** (`0..4`).
- **8 linhas totais** (`0..7`), contando de baixo para cima.
- **7 linhas visíveis** (`0..6`).
- A linha `7` é a linha de entrada e de overflow.
- A peça é inserida na célula vazia mais baixa da coluna.
- Uma coluna com 8 peças está cheia e rejeita novas peças.
- Após a resolução da jogada, se qualquer coluna tiver mais de 7 peças (ou seja, 8), o jogo termina.

### Peças

- A fila mantém `Current` (peça a ser jogada) e `Next` (próxima).
- `Advance()` promove `Next` para `Current` e gera um novo `Next` com base no tabuleiro **após** a jogada.
- Ao iniciar uma partida, `Current` e `Next` são gerados a partir do tabuleiro vazio.

### Geração de peças

A geração tem três camadas:

1. **Elegibilidade e probabilidades base** (`SpawnTable`): quais valores podem nascer e com qual peso histórico.
2. **Pressão do tabuleiro** (`BoardPressureCalculator`): quão perto o tabuleiro está do perigo.
3. **Pontuação e seleção** (`PieceDifficultyEvaluator`, `CandidateSelection`): ajusta as probabilidades pela pressão, favorecendo jogadas mais difíceis conforme o tabuleiro enche.

Os valores elegíveis são sempre os mesmos da camada 1. A camada 2 e 3 só mudam a probabilidade de cada um.

#### Elegibilidade e probabilidades base (`SpawnTable`)

A probabilidade de cada valor depende das peças presentes no tabuleiro:

1. **Teto:** o teto é `maior peça do tabuleiro × 2^SpawnCeilingLevelOffset`, limitado a 8192 (`SpawnTable.CalculateCeiling`). Nascem valores até o teto, inclusive. O padrão `SpawnCeilingLevelOffset = -1` dá metade da maior peça. `0` permite que a maior peça nasça; valores positivos permitem valores acima dela; valores negativos reduzem o teto, e um teto abaixo de 2 deixa só o `2`. Um tabuleiro vazio ignora o offset.
2. **Candidatos:** são elegíveis todas as potências de 2 até o teto, **estejam ou não no tabuleiro**, além de outros valores presentes no tabuleiro até o teto.
3. **Pesos:** cada valor presente no tabuleiro tem peso igual à quantidade dessa peça. Uma potência de 2 ausente do tabuleiro recebe o peso `AbsentValueWeight` (padrão 0,5, em unidades de uma peça). Com `AbsentValueWeight = 0`, só valores presentes no tabuleiro nascem, como antes.
4. **Mínimo do `2`:** o valor `2` sempre tem pelo menos **20%** de chance (`MinimumValueChance`), mesmo que não esteja no tabuleiro. Se a parcela proporcional for menor, o `2` recebe 20% e os demais dividem os 80% restantes proporcionalmente.
5. **Caso sem alternativas:** se o `2` for o único candidato possível, ele nasce com 100%.

Exemplos (teto = segunda maior peça, inclusive; pesos = contagem no tabuleiro, `AbsentValueWeight` = 0,5 para potências ausentes):

- Tabuleiro com `2` ×3, `4` ×1 e `8` ×1: teto = `4`. Candidatos: `2` (peso 3) e `4` (peso 1), com `2` sempre pelo menos 20%.
- Tabuleiro com `16` ×1 e `8` ×1: teto = `8`. Candidatos: `2` e `4` (ausentes, peso 0,5 cada) e `8` (peso 1).
- Tabuleiro com `4` ×2 e `2` ×1: a maior peça é `4`, então o teto é `2`. Só o `2` pode nascer (100%).

#### Pressão do tabuleiro

A pressão bruta é `0,50·H + 0,30·D + 0,20·I`, limitada a [0, 1]:

- **H (altura):** média de `min(altura da coluna, 7) / 7`.
- **D (densidade):** células visíveis ocupadas / 35. A linha de overflow não entra na contagem.
- **I (desequilíbrio):** `(altura máxima − altura mínima) / 7`.

A linha 7 é o limiar de perigo. Uma coluna que chega à linha de overflow conta como cheia, então o overflow nunca reduz a pressão.

A pressão bruta máxima alcançável em um tabuleiro empilhado (sempre o caso, pela gravidade) é **0,84**: quatro colunas cheias e uma vazia. Um tabuleiro com as 35 células visíveis ocupadas dá **0,80**. Por isso, o agendamento usa a pressão **normalizada** = bruta / 0,84, limitada a 1, para que o topo da escala seja alcançável.

#### Pontuação dos candidatos

Cada valor elegível recebe quatro componentes em [0, 1], em que valores maiores significam mais favorecido na seleção:

| Componente | Cálculo |
|---|---|
| **F, frequência** | Probabilidade base de `SpawnTable`, dividida pela maior probabilidade entre os candidatos. |
| **M, flexibilidade de combinação** | Média por coluna do valor de combinação disponível. O valor depende do número de participantes (2, 3, 4 e 5 peças: 0,6, 0,8, 0,9 e 1,0) e é maior em combinações mais altas na coluna. Favorece jogadas flexíveis. |
| **C, congestionamento** | Risco por coluna = fração preenchida após a jogada × (1 − 0,6 × valor da combinação). `C = 0,6 × menor risco + 0,4 × risco médio`. Favorece peças sem bom encaixe. |
| **P, progressão** | Posição do valor numa escala logarítmica entre `2` e o maior candidato elegível (0 para o menor, 1 para o maior). |

Pontuação final: `score = wF·F + wM·M + wC·C + wP·P`.

Os pesos são interpolados linearmente conforme a pressão normalizada, em três âncoras (0, 0,5 e 1). Os valores são os padrões do asset `PieceGenerationConfig`:

| Pressão | F | M | C | P |
|---|---:|---:|---:|---:|
| Baixa (0) | 0,45 | 0,40 | 0,15 | 0,00 |
| Moderada (0,5) | 0,25 | 0,35 | 0,30 | 0,10 |
| Alta (1) | 0,10 | 0,20 | 0,50 | 0,20 |

Efeito esperado: em pressão baixa, a geração favorece peças com boas combinações, para que o início seja tranquilo. Em pressão alta, o congestionamento domina, mas peças que combinam continuam possíveis, então a recuperação segue viável.

#### Seleção

- `peso(v) = exp((score(v) − maior score) / T)`, com `T = 0,8`. Valores com score maior têm peso maior.
- As probabilidades são normalizadas e misturadas com um piso uniforme de 1% (limitado a `1/n`), para que todo candidato legal continue possível.
- A escolha usa um único sorteio de `IRandomSource` por peça (`SpawnTable.Pick`).
- Se nenhum candidato for pontuado, a geração usa as probabilidades base, sem valores sem pontuação.

### Combinação

- Apenas peças iguais adjacentes **ortogonalmente** (cima, baixo, esquerda, direita).
- Participantes: mínimo 2, máximo 5 (a peça de origem mais até 4 vizinhos).
- Resultado: `valor × 2^(participantes − 1)`, ou seja, dobra a cada participante extra.
- Resultado máximo: `8192`. Uma combinação cujo resultado ultrapassaria `8192` não acontece; peças de valor `8192` não combinam.
- Prioridade: candidatos com mais vizinhos iguais são resolvidos primeiro.
- Empate: sorteio entre os candidatos da mesma prioridade.
- A peça de origem recebe o resultado e os vizinhos são removidos.
- Após cada combinação, a gravidade é aplicada e o processo continua até não haver mais combinações.

Exemplos:

| Peça | Participantes | Resultado | Pontos |
|---|---:|---:|---:|
| 2 | 2 | 4 | 4 |
| 2 | 3 | 8 | 8 |
| 2 | 4 | 16 | 16 |
| 2 | 5 | 32 | 32 |
| 4 | 2 | 8 | 8 |
| 4 | 3 | 16 | 16 |

### Pontuação e recorde

- Cada combinação adiciona o **valor resultante** à pontuação.
- A pontuação é acumulada por toda a cascata da jogada.
- `ScoreService` usa `long` e verifica overflow antes de somar (`CanAdd`/`TryAdd`). Antes de cada jogada, `BoardController` confirma que a pontuação suporta o pior caso possível (`MaxPossibleCascadePoints`: 8 linhas × 5 colunas × 8192).
- O recorde é atualizado sempre que a pontuação atual o supera e é salvo junto com a partida (ver seção 8).

### Game over

- Ocorre quando, após uma jogada, alguma coluna tem 8 peças.
- A tela de game over mostra pontos e recorde e oferece reinício.
- Ao acontecer, o save da partida em andamento é apagado: não é possível retomar uma partida terminada.

## 7. Fluxo de uma jogada

1. O jogador pressiona a tela. `GameplayScreen.Update` detecta `Pointer.current.press.wasPressedThisFrame`.
2. `BeginPreview` ativa a prévia, traz a peça atual para frente e muda o status para “Solte para inserir”.
3. O arraste chama `UpdatePreview`, que calcula a coluna mais próxima e move a peça com LeanTween.
4. Ao soltar, `CommitPreview` chama `PlayColumn(coluna)`.
5. `BoardController.TryPlay` bloqueia se o jogo acabou ou se a pontuação não suporta o pior caso, captura o estado antes, insere a peça e resolve a cascata.
6. `CascadeResolver.Resolve` aplica gravidade, resolve combinações uma a uma e devolve todas as etapas.
7. `BoardController` soma os pontos, atualiza o recorde e verifica overflow (`IsGameOver`).
8. Em caso de sucesso:
   - `PieceGenerator.Advance(board)` promove a próxima peça e gera uma nova, usando o tabuleiro já resolvido.
   - `PieceQueueView.Advance` promove a segunda peça e cria uma nova.
   - Se houve game over, o save é apagado imediatamente.
   - `BoardView.PlayMove` anima inserção, gravidade e combinações.
9. Ao terminar a animação, `SaveProgress` salva o recorde e o estado da partida (ou apaga o save em game over), o input volta ao jogador e a interface é atualizada.
10. Em caso de falha (coluna cheia):
    - A fila volta ao centro.
    - O status mostra “Coluna cheia”.
    - A peça atual não é consumida.

Durante animação, game over, overlay de debug aberto ou arraste sem coluna válida, o input é ignorado.

## 8. Persistência

Arquivo: `Assets/!/Scripts/Save/SaveSystem.cs`. Usa `PlayerPrefs`, que funciona em Windows, Android e WebGL.

| Chave | Conteúdo | Quando é salva | Quando é apagada |
|---|---|---|---|
| `NumberMerge.Record` | Recorde como texto (`long`) | Ao final de cada jogada e na inicialização | Pelo overlay de debug |
| `NumberMerge.Board` | `BoardState` em JSON | Na inicialização e ao final de cada jogada válida | Em game over, ou quando o dado é inválido |

`BoardState` guarda:

- Dimensões do tabuleiro (`ColumnCount`, `VisibleRowCount`).
- Todas as células, incluindo a linha de overflow, em ordem linha-major.
- Peça atual e próxima.
- Pontuação como texto (`JsonUtility` não suporta `long`).

Ao iniciar `Gameplay`:

1. Carrega o recorde.
2. Se houver um `BoardState` válido, com dimensões compatíveis e peças maiores que zero, restaura o tabuleiro, a fila e a pontuação. Se o estado restaurado já for game over, o save é apagado.
3. Se o dado for inválido ou incompatível, ele é descartado e uma nova partida começa.

Observação: o save é gravado depois que a animação termina. Se o jogo for fechado no meio de uma jogada, a partida é retomada a partir do estado anterior à jogada, com a mesma peça atual.

## 9. Implementação por parte

### 9.1 `BoardModel`

Arquivo: `Assets/!/Scripts/Gameplay/BoardModel.cs`

- Matriz `int[TotalRowCount, ColumnCount]`; `0` é célula vazia.
- `TryInsert` encontra a linha vazia mais baixa.
- `ApplyGravity` compacta as peças para baixo.
- `GetPieceCount`, `GetHeight`, `IsColumnFull`, `HasOverflow`.
- `TrySetValue` e `TryRemove` são usados pela resolução de combinações.
- Mantém contagem por valor (`GetValueCount`) e os dois maiores valores distintos (`HighestValue`, `SecondHighestValue`), usados pela `SpawnTable`.

A linha `0` é a base; a linha `7` é o overflow.

### 9.2 Snapshots e resultados

Arquivos: `BoardSnapshot.cs`, `MoveResult.cs`, `CascadeResult.cs`, `CascadeStep.cs`, `MergeOutcome.cs`, `BoardPosition.cs`.

- `BoardSnapshot` copia o tabuleiro para animar transições sem alterar o modelo vivo.
- `MoveResult` guarda: tabuleiro antes da jogada, após a inserção, coluna, valor, linha inserida, cascata completa e tabuleiro final.
- `CascadeStep` guarda snapshots antes da combinação, depois da combinação e depois da gravidade.
- `CascadeResult` guarda as combinações, o número de etapas, as etapas, o snapshot após a primeira gravidade e o tabuleiro final.
- `MergeOutcome` descreve origem, valor original, valor resultante, vizinhos removidos e quantidade de participantes.

### 9.3 Geração de peças

Arquivos: `PieceGenerator.cs`, `PieceGenerationSettings.cs`, `BoardPressureCalculator.cs`, `PieceDifficultyEvaluator.cs`, `CandidateSelection.cs`, `PieceGenerationDecision.cs`, `SpawnTable.cs`, `RandomSource.cs`

Pipeline de cada peça (`PieceGenerator.GenerateValue`):

1. `SpawnTable.Build(board)`: valores elegíveis e probabilidades base.
2. `BoardPressureCalculator.Calculate(board)`: pressão bruta e normalizada.
3. `PieceDifficultyEvaluator.Evaluate(...)`: componentes F, M, C, P e score de cada candidato, com os pesos da pressão.
4. `CandidateSelection.Compute(...)`: softmax com temperatura e piso uniforme.
5. `SpawnTable.Pick(...)`: um sorteio com `IRandomSource`.

Propriedades:

- `PieceGenerator` mantém a API anterior: `Reset(board)`, `Restore(current, next)`, `Advance(board)`, `Current` e `Next`.
- Não altera o tabuleiro. Consome um sorteio por peça (`Reset` consome dois, `Advance` consome um). A avaliação não consome aleatoriedade.
- As listas de trabalho são reaproveitadas entre jogadas, para evitar alocações a cada peça.
- Todas as classes da pipeline são puras (sem `MonoBehaviour`) e recebem dependências por construtor.
- `IRandomSource` permite substituir o gerador. `SystemRandomSource` usa `System.Random`. Com uma semente fixa, a sequência é reproduzível.

Diagnóstico:

- `RecordDecisions` (bool, padrão `false`): quando `true`, `LastDecision` guarda a pressão, os pesos, os componentes de cada candidato, a probabilidade e o valor escolhido.
- `LogDecisions` (existe só em editor e builds de desenvolvimento, padrão `false`): imprime cada decisão no console do Unity.

Balanceamento por asset. Todos os parâmetros ficam no asset `Assets/!/Settings/PieceGenerationConfig.asset` (tipo `PieceGenerationConfig`, ScriptableObject). Para criar outro, use Assets > Create > Number Merge > Piece Generation Config.

- `GameplayScreen` tem o campo `Piece Generation Config`. Ele passa `Settings` para `new PieceGenerator(random, settings)`. Se o campo estiver vazio, o jogo usa os valores padrão de `PieceGenerationSettings`.
- Alterar o asset no Inspector muda o jogo na próxima partida iniciada. Não é preciso mudar código.
- `OnValidate` do asset registra no console um erro com o nome do asset quando um valor é inválido. O `PieceGenerator` também recusa valores inválidos ao ser criado.
- Os campos têm tooltips e sliders (`Range`) no Inspector.

Parâmetros (`PieceGenerationSettings`, serializados no asset):

| Parâmetro | Padrão | Efeito |
|---|---|---|
| `HeightPressureWeight` / `DensityPressureWeight` / `ImbalancePressureWeight` | 0,50 / 0,30 / 0,20 | Peso de cada sinal da pressão. |
| `LowPressureWeights` / `ModeratePressureWeights` / `HighPressureWeights` | ver seção 6 | Pesos de F, M, C e P nas três âncoras. |
| `ModeratePressurePoint` | 0,5 | Posição da âncora moderada na pressão normalizada. |
| `AbsentValueWeight` | 0,5 | Peso de cada potência de 2 ausente do tabuleiro (em peças). `0` restaura a regra antiga de só nascer o que já está no tabuleiro. |
| `SpawnCeilingLevelOffset` | -1 | Teto de geração em níveis a partir da maior peça: `maior × 2^nível`, limitado a 8192. Negativo = abaixo da maior peça, positivo = acima. Faixa de -10 a 10. |
| `SpawnHardCeiling` | 0 | Teto fixo para peças geradas: nenhuma peça acima deste valor nasce. `0` = sem teto. Potência de 2 de 2 a 8192. Não limita o resultado das combinações. |
| `Temperature` | 0,8 | Nitidez da seleção. Valores menores tornam a escolha mais decisiva. |
| `MinimumCandidateProbability` | 0,01 | Piso uniforme por candidato, limitado a `1/n`. |
| `MergeParticipantValues` | 0,6 / 0,8 / 0,9 / 1,0 | Valor da combinação por número de participantes (2 a 5). |
| `MergeHeightBias` | 0,5 | Quanto uma combinação perto da base da coluna vale menos. |
| `MergeReliefFactor` | 0,6 | Quanto uma combinação reduz o risco de uma coluna. |
| `CongestionBestColumnWeight` | 0,6 | Peso da coluna mais segura no congestionamento (o restante é a média). |

O piso `SpawnTable.MinimumValueChance` (20% para o `2`) continua em `SpawnTable.cs`. Ele alimenta a probabilidade base F.

### 9.4 Combinação

Arquivos: `MergeMath.cs`, `MergeResolver.cs`

`MergeMath` centraliza:

- Valor máximo `8192`.
- Participantes mínimos (`2`) e máximos (`5`).
- Cálculo do resultado: `valor << (participantes − 1)`, rejeitado se passar de `8192`.

`MergeResolver.TryResolveOnce`:

1. Percorre todas as células com valor.
2. Coleta vizinhos ortogonais iguais.
3. Ignora candidatos cujo resultado não é válido.
4. Usa a maior prioridade (número de vizinhos) disponível.
5. Sorteia um candidato da prioridade (`IRandomSource`).
6. Aplica o resultado na origem e remove os vizinhos.

### 9.5 Cascatas

Arquivo: `CascadeResolver.cs`

1. Aplica gravidade inicial e captura o snapshot correspondente.
2. Resolve uma combinação por vez.
3. Verifica progresso: a quantidade de peças deve cair exatamente em `participantes − 1`; caso contrário, lança exceção.
4. Aplica gravidade.
5. Detecta ciclos por snapshot textual (lança exceção se o tabuleiro se repetir).
6. Limita a execução a `10000` etapas (`DefaultMaxSteps`).
7. Retorna `CascadeResult` com todas as etapas.

As proteções de progresso, ciclo e limite evitam loops infinitos em caso de bug.

### 9.6 `BoardController` e pontuação

Arquivos: `BoardController.cs`, `ScoreService.cs`

`BoardController.TryPlay(coluna, valor, out MoveResult)` é a operação principal:

- Bloqueia se o jogo acabou.
- Verifica se a pontuação suporta o pior caso possível.
- Insere a peça; se a coluna estiver cheia, retorna `false`.
- Resolve a cascata.
- Soma os valores resultantes e atualiza a pontuação.
- Define `IsGameOver` pelo overflow de qualquer coluna.
- Produz `MoveResult`.

Ao construir o controlador com um tabuleiro restaurado, `IsGameOver` já é calculado a partir do estado.

`ScoreService` mantém `CurrentScore` e `Record`, ambos `long`. Ao restaurar, o recorde nunca fica abaixo da pontuação restaurada.

### 9.7 `BoardView`

Arquivo: `Assets/!/Scripts/UI/BoardView.cs`

Responsável pela representação visual do tabuleiro:

- Cria as células de fundo (`Cell_c_r`) apenas para as linhas visíveis, em `EnsureBuilt`. A linha de overflow não tem célula de fundo.
- Tamanho da célula: o maior valor que cabe no `RectTransform` do próprio objeto, a menos que `CellSizeOverride` seja maior que zero. Espaçamento vem do config (`Spacing`).
- Cria e destrói `TileView` conforme os snapshots (`SetBoard`).
- `GetNearestColumn` converte posição local em coluna; `GetColumnCenterX` retorna o centro da coluna; `GetDropStartLocalY` retorna o ponto acima da linha de entrada.
- Durações das animações (inserção, combinação, pop e gravidade) vêm do config.

Configuração (`BoardViewConfig`, ScriptableObject):

- O `BoardView` tem o campo `Config` (`_config`), que aponta para `Assets/!/Data/BoardViewConfig.asset`. Sem config atribuído, o `BoardView` registra um erro e usa os valores padrão internos.
- Valores de grade: `ColumnCount` (5), `VisibleRowCount` (7), `Spacing` (padrão 6 px; o asset usado na cena `Gameplay` está em 3 px) e `CellSizeOverride` (0 = ajustar ao `RectTransform`).
- Durações em segundos: `InsertDuration` (0,18), `MergeDuration` (0,12), `PopDuration` (0,12) e `GravityDuration` (0,12).
- Os valores são copiados para campos de runtime em `Start`. Como o `GameplayScreen.Awake` lê `ColumnCount` e chama `Initialize` antes de `Start`, a configuração também é carregada no primeiro acesso (`EnsureConfigLoaded`), então a ordem de execução não afeta o resultado.
- Alterar o asset no Inspector muda o tabuleiro na próxima vez que a cena for carregada. Não é preciso mudar código.
- `TileTheme` e o prefab `Tile` continuam como referências diretas no `BoardView`, fora do config.

`PlayMove` executa:

1. Restaura o snapshot anterior.
2. Cria a peça inserida na posição inicial da prévia.
3. Anima a inserção (LeanTween).
4. Anima a gravidade até `AfterInitialGravity`.
5. Para cada `CascadeStep`, anima a combinação e a gravidade seguinte.
6. Sincroniza o grid com `FinalBoard` (`EnsureGridMatches`).

### 9.8 `TileView` e `TileTheme`

Arquivos: `TileView.cs`, `TileTheme.cs`, `Assets/!/Themes/DefaultTileTheme.asset`

`TileView`:

- Exibe o valor em um `TMP_Text` filho.
- Recebe o sprite de fundo do tema para o seu valor (`GetPieceSprite`) e a cor do texto do tema.
- Controla escala via `transform.localScale` e transparência via `CanvasGroup`.
- `ResetVisual()` restaura escala 1 e alpha 1.

`TileTheme` (ScriptableObject, menu `Number Merge/Tile Theme`):

- `SlotSprite`: sprite de cada célula vazia.
- `TextColor`: cor do valor em todas as peças.
- `PieceSprites`: sprites por ordem crescente a partir de `2` (índice 0 = 2, 1 = 4, 2 = 8, …).
- `FallbackPieceSprite`: usado para valores sem sprite próprio.
- `BadTileSprite`: reservado, ainda sem uso.

O prefab `Tile.prefab` contém um `Image` com `TileView` e `CanvasGroup`, e um filho `Label` com `TMP_Text`. O raycast está desativado.

### 9.9 `PieceQueueView`

Arquivo: `Assets/!/Scripts/UI/PieceQueueView.cs`

Responsável pela fila visual de duas peças:

- `Initialize` cria `QueueCurrent` (escala 1, centro acima do tabuleiro) e `QueueNext` (escala `0.7`, canto superior direito).
- `BeginPreview` cancela tweens, restaura o visual e coloca a peça atual na frente.
- `MovePreviewTo` move a peça atual com LeanTween `easeOutQuad`.
- `CancelPreview` devolve a peça ao centro.
- `Advance`:
  1. Destrói a peça atual.
  2. Promove a próxima peça para atual e a move ao centro com `easeOutBack`, escalando de `0.7` para `1`.
  3. Cria a nova próxima já visível em escala `0.7`.

Os dois tiles são criados em runtime; o `QueueRoot` não tem filhos salvos na cena.

### 9.10 `GameplayScreen`

Arquivo: `Assets/!/Scripts/UI/GameplayScreen.cs`

É o controlador da cena e o ponto de composição:

- Em `Awake`, carrega o jogo salvo ou inicia um novo, inicializa a `BoardView` e a fila, e salva o estado inicial.
- Lê `Pointer.current` em `Update` e ignora o input durante animação, game over ou com o overlay de debug aberto.
- Implementa prévia, arraste e commit.
- Expõe `PlayColumn(int)` para inserção direta (usada também em testes manuais).
- Atualiza pontuação e status.
- Executa a coroutine de animação da jogada e salva o progresso ao final dela.

Textos de status:

- “Toque e arraste para escolher a coluna”
- “Solte para inserir”
- “Resolvendo...”
- “Coluna cheia”

O texto de pontuação mostra pontos e recorde na mesma linha.

### 9.11 `GameOverScreen`

Arquivo: `Assets/!/Scripts/UI/GameOverScreen.cs`

- Mostra um overlay com “Pontos” e “Recorde”.
- O botão de reinício recarrega a cena `Gameplay`. Como o save foi apagado, a nova partida começa do zero.

### 9.12 `DebugScreen`

Arquivo: `Assets/!/Scripts/UI/DebugScreen.cs`

- Abre e fecha com **F12** (lido com `Keyboard.current`).
- Enquanto estiver aberto, bloqueia o input de jogo (`DebugScreen.IsOpen`).
- Botões: fechar, reiniciar a partida, e **limpar dados e reiniciar** (apaga o recorde e o save).

É um recurso de desenvolvimento. Não há controle para desligá-lo em build.

### 9.13 `AudioManager`

Arquivo: `Assets/!/Scripts/Audio/AudioManager.cs`

- Singleton (`AudioManager.Instance`) com `DontDestroyOnLoad`. Duplicatas se destroem.
- Cria três `AudioSource`, roteados para os grupos Music, Sfx e Ui de um `AudioMixer`.
- Toca a música de fundo (`_backgroundMusic`, em loop) ao iniciar. A cena `Gameplay` já referencia o mixer e o clipe.
- Métodos disponíveis: `PlayMusic`, `StopMusic`, `PlaySfx`, `PlayUi`, `SetVolume`, `GetVolume`. Volume é linear (0 a 1), convertido para decibéis. Os parâmetros do mixer são `MusicVolume`, `SfxVolume` e `UiVolume`.
- `PlaySfx` e `PlayUi` ainda não são chamados pelo jogo.

### 9.14 `FullscreenBackground`

Arquivo: `Assets/!/Scripts/UI/FullscreenBackground.cs`

- Em cada frame, posiciona o `SpriteRenderer` na frente da câmera e escala-o para cobrir toda a área visível (“cover”), mantendo a proporção.
- Funciona com câmera ortográfica ou em perspectiva. Usa a câmera atribuída ou `Camera.main`.
- Roda também no Editor (`[ExecuteAlways]`).

## 10. Cena e layout

### Objetos da cena `Gameplay`

Objetos identificados no arquivo da cena:

- `Main Camera`
- `GameCanvas`, com `Canvas`, `CanvasScaler`, `GraphicRaycaster` e `GameplayScreen`
- `BackgroundSprite` (com `FullscreenBackground`)
- `Board` (com `BoardView`)
- `Title`, `ScoreText`, `RecordText`, `StatusText`
- `QueueRoot` (com `PieceQueueView`)
- `GameOverScreen`, com `Overlay`, `Panel`, `ScoreText`, `RecordText`, `Title` e `RestartButton`
- Overlay de debug (`DebugScreen`)
- `AudioManager`
- `ContinueButton` (sem script associado no código atual)
- `EventSystem`, com `InputSystemUIInputModule`

O gameplay lê o ponteiro diretamente. O `EventSystem` existe para a UI de botões.

### Layout

- A posição e o tamanho do tabuleiro são definidos no `RectTransform` de `Board`.
- A posição da fila atual é derivada do topo do tabuleiro (`GetDropStartLocalY`), e a próxima fica deslocada para a direita e para cima (`+280` em X, `+75` em Y do ponto de entrada).
- As escalas são: atual `1`, próxima `0.7`.
- Os valores numéricos anteriores (por exemplo, o tabuleiro de `444×714` com células de `84 px`) não foram reverificados nesta atualização, porque o tamanho das células agora é calculado a partir do `RectTransform`.

## 11. Input e animações

### Input

- O projeto usa o novo Input System.
- `GameplayScreen.Update` lê `Pointer.current` (mouse e toque).
- Não há botões de coluna. A coluna é escolhida pela posição horizontal do ponteiro, convertida para coordenadas locais do tabuleiro e limitada entre `0` e `4`.
- A prévia só é atualizada quando a coluna muda.
- Soltar confirma a jogada. Se a coluna estiver cheia, a peça volta ao centro.

O caminho de input foi implementado com a API padrão do Input System, mas a simulação automatizada de toque no Editor não foi confiável. O teste de toque real deve ser manual.

### Animações

| Animação | Implementação |
|---|---|
| Prévia horizontal | LeanTween `move`, `easeOutQuad`, `0.12s` |
| Retorno ao centro | LeanTween `move`, `easeOutQuad`, `0.12s` |
| Promoção da fila | LeanTween `move` + `scale`, `easeOutBack`, `0.3s` |
| Inserção no tabuleiro | LeanTween `move` + `scale` + `value`, duração `InsertDuration` (0,18s) |
| Combinação | Coroutine com interpolação smoothstep, duração `MergeDuration` (0,12s) |
| Pop do resultado | Coroutine com interpolação, duração `PopDuration` (0,12s) |
| Gravidade | Coroutine com interpolação smoothstep, duração `GravityDuration` (0,12s) |

As durações marcadas acima vêm do `BoardViewConfig`.

As animações de combinação e gravidade continuam em coroutines com interpolação manual. Isso foi mantido porque o fluxo já estava estável.

## 12. Como executar e testar

1. Abrir o projeto no Unity `6000.3.10f1`.
2. Abrir `Assets/!/Scenes/Gameplay.unity`.
3. Entrar em Play Mode.
4. Pressionar e arrastar com mouse ou toque; soltar para inserir.
5. Verificar:
   - Fila inicial com duas peças.
   - Prévia acompanhando a coluna.
   - Inserção na coluna escolhida.
   - Fila avançando após uma jogada válida.
   - Combinações, cascatas e pontuação.
   - Coluna cheia mantendo a peça atual.
   - Recorde mantido após fechar e reabrir o jogo.
   - Partida retomada após fechar e reabrir o jogo.
   - Game over com pontos e recorde, e reinício.
   - F12 abrindo o overlay de debug, com “limpar dados e reiniciar”.

Testes automatizados:

1. Abrir Window > General > Test Runner.
2. Selecionar a aba EditMode e executar todos os testes, ou filtrar pela assembly `Assembly-CSharp-Editor`.
3. Resultado esperado: 72 testes aprovados (`BoardPressureCalculatorTests`, `PieceDifficultyEvaluatorTests`, `CandidateSelectionTests`, `PieceGeneratorTests`, `PieceGenerationConfigTests`).

Os testes exigem a janela do Editor em foco. Sem foco, a execução fica bloqueada.

Build: `EditorBuildSettings.asset` contém apenas `Assets/!/Scenes/Gameplay.unity`.

## 13. Verificações realizadas

As verificações abaixo foram feitas em 2026-10-28, com o Unity Editor conectado, após a mudança na geração de peças:

| Verificação | Estado |
|---|---|
| Compilação sem erros | Verificada: 0 erros, em `Assembly-CSharp` e `Assembly-CSharp-Editor` |
| Testes EditMode da geração de peças | 72 de 72 aprovados (assembly `Assembly-CSharp-Editor`) |
| Referências ausentes na cena | Não verificadas nesta atualização (último registro: 0, versão anterior) |
| Referências de inspector em `Gameplay.unity` | Presentes para `AudioManager`, `BoardView` e `PieceQueueView` (verificado por leitura do YAML) |
| Toque real no Editor | Pendente de teste manual |

## 14. Limitações e próximos passos

### Prioridade alta

1. Verificar referências ausentes na cena com o Unity aberto.
1a. Playtest da geração adaptativa. Ajustar primeiro `Temperature` e, depois, os pesos de congestionamento (alta pressão) e de flexibilidade (baixa pressão), no asset `PieceGenerationConfig`.
2. Testar toque real em dispositivo ou no Editor com mouse.
3. Definir a orientação e a resolução alvo, e revisar o CanvasScaler.
4. Testar um build com `Gameplay` (inclui o ciclo de save, game over e reinício).

### Prioridade média

5. Criar a cena de menu e a transição para o gameplay, ou remover o `MainMenu` e o `ContinueButton` se não forem usados.
6. Adicionar testes automatizados para `BoardModel`, `MergeResolver`, `CascadeResolver`, `ScoreService` e `BoardState`. A geração de peças já tem testes.
7. Ligar os efeitos sonoros (`PlaySfx`) ao drop, à combinação e ao game over.

### Prioridade baixa

8. Decidir se o “bad tile” entra no jogo ou se o campo `BadTileSprite` é removido.
9. Adicionar feedback visual de pontuação.
10. Avaliar pooling de tiles.
11. Remover assets não utilizados ou documentar seu propósito (por exemplo, `Particles`, `scrath.png` e `tile click.mp3`).
12. Desligar ou remover o overlay de debug em builds finais.

## 15. Referência rápida

| Arquivo | Responsabilidade |
|---|---|
| `Assets/!/Scripts/Gameplay/BoardModel.cs` | Estado e operações básicas do tabuleiro |
| `Assets/!/Scripts/Gameplay/BoardController.cs` | Orquestra jogada, cascata, pontuação e game over |
| `Assets/!/Scripts/Gameplay/CascadeResolver.cs` | Resolve cascatas com proteções |
| `Assets/!/Scripts/Gameplay/MergeResolver.cs` | Escolhe e aplica uma combinação |
| `Assets/!/Scripts/Gameplay/MergeMath.cs` | Fórmula e limites de combinação |
| `Assets/!/Scripts/Gameplay/PieceGenerator.cs` | Fila de peças (atual e próxima); orquestra a geração |
| `Assets/!/Scripts/Gameplay/PieceGenerationSettings.cs` | Todos os parâmetros de ajuste da geração |
| `Assets/!/Scripts/Gameplay/BoardPressureCalculator.cs` | Pressão do tabuleiro (bruta e normalizada) |
| `Assets/!/Scripts/Gameplay/PieceDifficultyEvaluator.cs` | Componentes F, M, C, P e score de cada candidato |
| `Assets/!/Scripts/Gameplay/CandidateSelection.cs` | Softmax e piso uniforme das probabilidades |
| `Assets/!/Scripts/Gameplay/PieceGenerationDecision.cs` | Registro de diagnóstico de cada geração |
| `Assets/!/Scripts/Gameplay/PieceGenerationConfig.cs` | ScriptableObject com os parâmetros de balanceamento |
| `Assets/!/Settings/PieceGenerationConfig.asset` | Asset de balanceamento usado pelo `GameplayScreen` |
| `Assets/!/Scripts/Gameplay/SpawnTable.cs` | Elegibilidade e probabilidades base |
| `Assets/!/Tests/Editor/` | Testes EditMode da geração de peças |
| `Assets/!/Scripts/Gameplay/RandomSource.cs` | Fonte de aleatoriedade substituível |
| `Assets/!/Scripts/Gameplay/BoardSnapshot.cs` | Cópia do tabuleiro para animação |
| `Assets/!/Scripts/Gameplay/MoveResult.cs` | Dados de uma jogada completa |
| `Assets/!/Scripts/Gameplay/CascadeResult.cs` | Resultado agregado da cascata |
| `Assets/!/Scripts/Gameplay/CascadeStep.cs` | Snapshots de uma etapa da cascata |
| `Assets/!/Scripts/Gameplay/MergeOutcome.cs` | Dados de uma combinação |
| `Assets/!/Scripts/Managers/ScoreService.cs` | Pontuação e recorde |
| `Assets/!/Scripts/Save/SaveSystem.cs` | Leitura e escrita do recorde e do save da partida |
| `Assets/!/Scripts/Save/BoardState.cs` | Formato serializado da partida em andamento |
| `Assets/!/Scripts/Audio/AudioManager.cs` | Música e volumes via AudioMixer |
| `Assets/!/Scripts/UI/GameplayScreen.cs` | Controlador da cena, input e salvamento |
| `Assets/!/Scripts/UI/BoardView.cs` | Visual do tabuleiro e animações |
| `Assets/!/Scripts/UI/BoardViewConfig.cs` | ScriptableObject com grade e durações do tabuleiro |
| `Assets/!/Data/BoardViewConfig.asset` | Asset de configuração usado pelo `BoardView` |
| `Assets/!/Scripts/UI/PieceQueueView.cs` | Fila visual de duas peças |
| `Assets/!/Scripts/UI/TileView.cs` | Visual individual de tile |
| `Assets/!/Scripts/UI/TileTheme.cs` | Sprites e cores das peças e do tabuleiro |
| `Assets/!/Scripts/UI/GameOverScreen.cs` | Tela de game over e reinício |
| `Assets/!/Scripts/UI/DebugScreen.cs` | Overlay de debug (F12) |
| `Assets/!/Scripts/UI/FullscreenBackground.cs` | Fundo em tela cheia |
| `Assets/!/Prefabs/Tile.prefab` | Prefab base do tile |
| `Assets/!/Themes/DefaultTileTheme.asset` | Tema padrão |
| `Assets/!/Scenes/Gameplay.unity` | Cena jogável atual |
| `Assets/!/Scenes/MainMenu.unity` | Placeholder de menu |
| `Assets/LeanTween` | Biblioteca de animações |

## 16. Mudanças desde a versão anterior

Esta seção resume o que mudou em relação ao documento de 2026-10-23:

- **Configuração do tabuleiro em ScriptableObject (2026-10-29):** `ColumnCount`, `VisibleRowCount`, `Spacing`, `CellSizeOverride` e as quatro durações de animação saíram do `BoardView` e foram para o `BoardViewConfig` (`Assets/!/Data/BoardViewConfig.asset`). O `BoardView` carrega esses valores em `Start` (também no primeiro acesso, antes de `Start`). O asset foi criado com os valores que a cena usava, incluindo `Spacing` = 3. Detalhes na seção 9.7.

- **Geração de peças com pressão do tabuleiro (2026-10-28):** a escolha final passou a depender da pressão do tabuleiro. A pressão combina altura, densidade e desequilíbrio. Cada candidato é pontuado em frequência, flexibilidade de combinação, congestionamento e progressão, com pesos que mudam com a pressão. A seleção usa softmax com temperatura 0,8. A elegibilidade e a probabilidade base continuam vindo de `SpawnTable`, sem alteração de regra. Detalhes na seção 6 e na seção 9.3.
- **Balanceamento por asset (2026-10-28):** os parâmetros da geração de peças estão no ScriptableObject `PieceGenerationConfig` (`Assets/!/Settings/PieceGenerationConfig.asset`). O `GameplayScreen` o usa para criar o `PieceGenerator`.
- **Peças fora do tabuleiro (2026-10-28):** potências de 2 abaixo do limite agora podem nascer mesmo sem estar no tabuleiro, com peso `AbsentValueWeight`. Antes só nascia o que já estava no tabuleiro, mais o `2`.
- **Teto de geração (2026-10-28):** o teto passou de "menor que a segunda maior peça" para "até a segunda maior peça, inclusive". Medição com jogadas aleatórias nas 25 primeiras peças de 300 partidas: peças com uma única opção caíram de 63% para 30%, e o `2` de 83% para 59%.
- **Teto configurável (2026-10-28):** o teto de geração deixou de ser a segunda maior peça e passou a ser `maior peça × 2^SpawnCeilingLevelOffset` (padrão -1), exposto no asset `PieceGenerationConfig`. Positivos permitem valores acima da maior peça; negativos, valores abaixo dela.
- **Teto fixo de geração (2026-10-28):** `SpawnHardCeiling` limita o maior valor que pode nascer, independentemente do tabuleiro. Não afeta as combinações. Padrão `0` (sem teto).
- **Testes automatizados:** 72 testes EditMode para a geração de peças.
- **Geração de peças:** deixou de ser fixa (90% `2`, 10% `4`). Passou a ser adaptativa (`SpawnTable`), com o `2` com no mínimo 20%.
- **Fórmula de combinação corrigida na documentação:** o resultado é `valor × 2^(participantes − 1)`. A versão anterior descrevia `valor × (participantes − 1) × 2`, que dá resultados diferentes para 4 e 5 participantes (por exemplo, 16 em vez de 12 para 4 participantes com `2`).
- **Recorde persistido:** deixou de ser apenas em memória. Salvo em `PlayerPrefs`.
- **Partida retomável:** o tabuleiro, a fila e a pontuação são salvos após cada jogada e restaurados ao abrir o jogo.
- **Game over:** agora tem tela própria com pontos, recorde e reinício.
- **Overlay de debug (F12):** adicionado.
- **Áudio:** `AudioManager` com AudioMixer e música de fundo. Efeitos sonoros ainda não estão ligados.
- **Textos:** a UI usa TextMeshPro, não mais `UnityEngine.UI.Text`.
- **Tema:** as cores e sprites das peças vêm de `TileTheme` (ScriptableObject).
- **Tamanho das células:** calculado a partir do `RectTransform` do tabuleiro, não mais fixo em `84 px`.
- **Build:** `EditorBuildSettings.asset` agora aponta para `Gameplay.unity` (antes apontava para `SampleScene.unity`).
- **Histórico de commits:** `6d8951b Game Over and Save system`, `2a61669 icon` e `9d541ef bug fix` foram incluídos nesta atualização.
