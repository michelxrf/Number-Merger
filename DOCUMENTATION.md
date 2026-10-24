# Number Merge — Documentação do Estado Atual

> Última atualização: 2026-10-23  
> Projeto: `D:\Projects\Number Merger`  
> Unity: `6000.3.10f1`

## Sumário

1. [Visão geral](#1-visão-geral)
2. [Estado atual](#2-estado-atual)
3. [Tecnologias e configuração](#3-tecnologias-e-configuração)
4. [Estrutura de arquivos](#4-estrutura-de-arquivos)
5. [Arquitetura](#5-arquitetura)
6. [Regras do jogo](#6-regras-do-jogo)
7. [Fluxo de uma jogada](#7-fluxo-de-uma-jogada)
8. [Implementação por parte](#8-implementação-por-parte)
9. [Cena e layout](#9-cena-e-layout)
10. [Input e animações](#10-input-e-animações)
11. [Como executar e testar](#11-como-executar-e-testar)
12. [Verificações realizadas](#12-verificações-realizadas)
13. [Limitações e próximos passos](#13-limitações-e-próximos-passos)
14. [Referência rápida](#14-referência-rápida)

## 1. Visão geral

**Number Merge** é um protótipo de jogo de combinação de números em um tabuleiro vertical. O jogador recebe peças `2` ou `4`, escolhe uma coluna por toque/arraste e solta a peça. Peças iguais adjacentes se combinam, geram uma peça maior e podem iniciar cascatas.

O estado atual é um **protótipo jogável da cena Gameplay**, com:

- Tabuleiro 5×8.
- Geração de peças 2/4.
- Combinação por vizinhança ortogonal.
- Cascatas com gravidade.
- Pontuação e recorde em memória.
- Fila visual de duas peças.
- Posicionamento por toque/arraste, sem botões de coluna.
- Animações de inserção e de fila com LeanTween.
- Condição de game over.

A cena `MainMenu` ainda é apenas um placeholder com câmera, e não existe fluxo de menu ou reinício.

## 2. Estado atual

### Funciona

- Inserção na célula vazia mais baixa da coluna.
- Rejeição de coluna cheia.
- Combinação de 2 a 5 peças iguais adjacentes.
- Resultado `valor × (participantes − 1) × 2`.
- Limite de peça `8192`.
- Prioridade por maior número de vizinhos iguais.
- Escolha aleatória entre candidatos de mesma prioridade.
- Gravidade e múltiplas cascatas.
- Detecção de game over por overflow da coluna.
- Pontuação somando o valor resultante de cada combinação.
- Recorde em memória durante a sessão.
- Fila de duas peças sempre ativa.
- Prévia da peça atual sobre a coluna mais próxima.
- Inserção ao soltar.
- Remoção dos botões de coluna.
- Cena Gameplay salva e sem referências ausentes.
- Compilação sem erros.

### Ainda não implementado ou pendente

- Cena `MainMenu` funcional.
- Transição menu → gameplay.
- Reinício após game over.
- Persistência de recorde entre sessões.
- Áudio integrado.
- Teste automatizado do caminho real de toque no Editor.
- Build configurado com a cena Gameplay.
- Testes automatizados de lógica.
- Ajuste definitivo de responsividade/orientação.

## 3. Tecnologias e configuração

| Item | Configuração |
|---|---|
| Unity | `6000.3.10f1` |
| Render pipeline | URP 2D |
| UI | uGUI 2.0, `UnityEngine.UI.Text` |
| Fonte | `LegacyRuntime.ttf` |
| Input | Input System 1.18.0 |
| `activeInputHandler` | `1` (Input System apenas) |
| Canvas | Overlay |
| Referência do CanvasScaler | `1080×1920`, match `0.5` |
| Resolução usada na verificação | `1920×1080` |
| Animações novas | LeanTween em `Assets/LeanTween` |
| Assembly | Sem `.asmdef`; runtime em `Assembly-CSharp` |
| Testes | Nenhum teste automatizado no projeto |

Observação: a referência do CanvasScaler é retrato (`1080×1920`), mas a cena foi montada e verificada em landscape (`1920×1080`). Se o alvo final for retrato, o layout precisa ser revisto.

## 4. Estrutura de arquivos

```text
Assets/
  !/
    Prefabs/
      Tile.prefab
    Scenes/
      Gameplay.unity
      MainMenu.unity
    Scripts/
      Gameplay/
        BoardController.cs
        BoardModel.cs
        BoardPosition.cs
        BoardSnapshot.cs
        CascadeResolver.cs
        CascadeResult.cs
        CascadeStep.cs
        MergeMath.cs
        MergeOutcome.cs
        MergeResolver.cs
        MoveResult.cs
        PieceGenerator.cs
        RandomSource.cs
      Managers/
        ScoreService.cs
      UI/
        BoardView.cs
        GameplayScreen.cs
        PieceQueueView.cs
        TileView.cs
  LeanTween/
ProjectSettings/
  ProjectSettings.asset
  EditorBuildSettings.asset
```

Não existem scripts de Editor, assemblies separadas ou testes no estado atual.

## 5. Arquitetura

A implementação separa lógica de jogo e apresentação:

```text
Pointer.current
      │
      ▼
GameplayScreen ──────────────► PieceGenerator
      │                              │
      │                              ▼
      ├──────────────────────► PieceQueueView
      │                              │
      ▼                              ▼
BoardController ─────────────► BoardView
      │                              │
      ├── BoardModel                 ├── TileView
      ├── CascadeResolver            └── LeanTween
      │     └── MergeResolver
      └── ScoreService
```

### Camadas

- **Gameplay puro:** `BoardModel`, `BoardController`, `CascadeResolver`, `MergeResolver`, `MergeMath`, `ScoreService`, `PieceGenerator`.
- **Apresentação:** `GameplayScreen`, `BoardView`, `PieceQueueView`, `TileView`.
- **Dados de resultado:** `BoardSnapshot`, `MoveResult`, `CascadeResult`, `CascadeStep`, `MergeOutcome`, `BoardPosition`.

A lógica pura não depende de `MonoBehaviour` e recebe dependências por construtor quando necessário. `GameplayScreen` é o ponto de composição da cena.

## 6. Regras do jogo

### Tabuleiro

- **5 colunas** (`0..4`).
- **8 linhas totais** (`0..7`), contando de baixo para cima.
- **7 linhas visíveis** (`0..6`).
- A linha `7` é a linha de entrada/overflow.
- A peça é inserida na célula vazia mais baixa da coluna.
- Uma coluna com 8 peças está cheia e rejeita novas peças.
- Após a resolução, se qualquer coluna tiver mais de 7 peças, o jogo termina.

### Peças

- Valores gerados: `2` e `4`.
- Probabilidade: `2` = 90%, `4` = 10%.
- A fila mantém `Current` e `Next`.
- `Advance()` promove `Next` para `Current` e gera um novo `Next`.

### Combinação

- Apenas peças iguais adjacentes **ortogonalmente** (cima, baixo, esquerda, direita).
- Participantes: mínimo 2, máximo 5.
- Resultado: `valor × (participantes − 1) × 2`.
- Valor máximo: `8192`.
- Peças com valor `8192` não combinam.
- Prioridade: maior número de vizinhos iguais.
- Empate: escolha aleatória entre os candidatos da mesma prioridade.
- Após cada combinação, a gravidade é aplicada e o processo continua.

Exemplos:

| Peça | Participantes | Resultado | Pontos |
|---|---:|---:|---:|
| 2 | 2 | 4 | 4 |
| 2 | 3 | 8 | 8 |
| 2 | 4 | 12 | 12 |
| 4 | 2 | 8 | 8 |
| 4 | 3 | 16 | 16 |

### Pontuação

- Cada combinação adiciona o **valor resultante** à pontuação.
- A pontuação é acumulada por toda a cascata.
- `ScoreService` usa `long` e protege contra overflow.
- O recorde é atualizado quando a pontuação atual o supera.
- O recorde não é persistido em disco.

## 7. Fluxo de uma jogada

1. O jogador pressiona a tela.
2. `GameplayScreen.Update` detecta `Pointer.current.press.wasPressedThisFrame`.
3. `BeginPreview` ativa a prévia, reposiciona a peça atual e muda o status para “Solte para inserir”.
4. O arraste chama `UpdatePreview`, que calcula a coluna mais próxima e move a peça com LeanTween.
5. Ao soltar, `CommitPreview` chama `PlayColumn(coluna)`.
6. `BoardController.TryPlay` valida game over e overflow de pontuação, captura o snapshot inicial e insere a peça.
7. `CascadeResolver.Resolve` aplica gravidade, resolve combinações e devolve a cascata completa.
8. `BoardController` soma os pontos, atualiza o recorde e verifica overflow.
9. Em caso de sucesso:
   - `PieceGenerator.Advance()` gera a próxima peça.
   - `PieceQueueView.Advance()` promove a segunda peça e cria uma nova.
   - `BoardView.PlayMove()` anima inserção, gravidade e combinações.
10. Em caso de falha:
    - A fila volta ao centro.
    - O status mostra “Coluna cheia”.
    - A peça atual não é consumida.

## 8. Implementação por parte

### 8.1 `BoardModel`

Arquivo: `Assets/!/Scripts/Gameplay/BoardModel.cs`

Responsável pelo estado bruto do tabuleiro:

- Matriz `int[TotalRowCount, ColumnCount]`.
- `TryInsert` encontra a linha vazia mais baixa.
- `ApplyGravity` compacta as peças para baixo.
- `GetPieceCount`, `GetHeight`, `IsColumnFull`, `HasOverflow`.
- `TrySetValue` e `TryRemove` são usados pela resolução de combinações.

A linha `0` é a base; a linha `7` é o overflow.

### 8.2 Snapshots e resultados

Arquivos:

- `BoardSnapshot.cs`
- `MoveResult.cs`
- `CascadeResult.cs`
- `CascadeStep.cs`
- `MergeOutcome.cs`
- `BoardPosition.cs`

`BoardSnapshot` copia o tabuleiro para permitir animar transições sem alterar o modelo vivo.

`MoveResult` guarda:

- Tabuleiro antes da jogada.
- Tabuleiro após a inserção.
- Coluna, valor e linha inserida.
- Resultado completo da cascata.
- Tabuleiro final.

`CascadeStep` guarda os snapshots de cada etapa:

- Antes da combinação.
- Depois da combinação.
- Depois da gravidade.

`MergeOutcome` descreve uma combinação:

- Célula de origem.
- Valor original.
- Valor resultante.
- Vizinhos removidos.
- Quantidade de participantes.

### 8.3 Geração de peças

Arquivo: `Assets/!/Scripts/Gameplay/PieceGenerator.cs`

- `Current` e `Next` são gerados no construtor.
- `Advance()` promove `Next`.
- `Reset()` recria a fila.
- `IRandomSource` permite substituir o gerador aleatório.
- `SystemRandomSource` usa `System.Random`.

A chance de `4` é controlada por `FourChancePercent = 10`.

### 8.4 Combinação

Arquivos:

- `MergeMath.cs`
- `MergeResolver.cs`

`MergeMath` centraliza:

- Valor máximo `8192`.
- Participantes mínimos e máximos.
- Cálculo do resultado.

`MergeResolver.TryResolveOnce`:

1. Percorre todas as células.
2. Coleta vizinhos ortogonais iguais.
3. Calcula a prioridade pelo número de vizinhos.
4. Filtra os candidatos da maior prioridade.
5. Sorteia um candidato.
6. Aplica o resultado na origem e remove os vizinhos.

A origem mantém o resultado; os vizinhos são removidos.

### 8.5 Cascatas

Arquivo: `Assets/!/Scripts/Gameplay/CascadeResolver.cs`

O resolvedor:

1. Aplica gravidade inicial.
2. Captura o snapshot pós-gravidade.
3. Resolve uma combinação por vez.
4. Verifica progresso: a quantidade de peças deve cair em `participantes − 1`.
5. Aplica gravidade.
6. Detecta ciclos por snapshot textual.
7. Limita a execução a `10000` etapas.
8. Retorna `CascadeResult` com todas as etapas.

As proteções de progresso, ciclo e limite evitam loops infinitos em caso de bug.

### 8.6 `BoardController` e pontuação

Arquivos:

- `Assets/!/Scripts/Gameplay/BoardController.cs`
- `Assets/!/Scripts/Managers/ScoreService.cs`

`BoardController.TryPlay` é a operação principal:

- Bloqueia se o jogo acabou.
- Verifica se a pontuação suporta o pior caso possível.
- Insere a peça.
- Resolve a cascata.
- Soma os valores resultantes.
- Atualiza a pontuação.
- Define `IsGameOver`.
- Produz `MoveResult`.

`ScoreService` mantém `CurrentScore` e `Record`, ambos `long`, com `CanAdd` e `TryAdd`.

### 8.7 `BoardView`

Arquivo: `Assets/!/Scripts/UI/BoardView.cs`

Responsável por toda a representação visual do tabuleiro:

- Cria 40 células em runtime.
- Dimensão calculada: `5 × 84 + 4 × 6 = 444` de largura e `8 × 84 + 7 × 6 = 714` de altura.
- A linha de entrada usa cor diferente.
- Cria e destrói `TileView` conforme os snapshots.
- `GetNearestColumn` converte posição local em coluna.
- `GetColumnCenterX` retorna o centro da coluna.
- `GetDropStartLocalY` retorna o ponto acima da linha de entrada.
- `GetDropStartPosition` combina coluna e ponto de queda.

As células e os tiles não ficam salvos na cena: são criados em runtime por `EnsureBuilt` e `SetBoard`.

`PlayMove` executa:

1. Restaura o snapshot anterior.
2. Cria a peça inserida na posição inicial da prévia.
3. Anima a inserção.
4. Anima a gravidade até `AfterInitialGravity`.
5. Para cada `CascadeStep`, anima a combinação e a gravidade seguinte.
6. Sincroniza o grid com `FinalBoard`.

As animações de inserção usam LeanTween. As animações de combinação e gravidade continuam em coroutines com interpolação manual; isso foi mantido porque o fluxo já estava estável e o requisito de LeanTween era focado nas novas animações de peça/fila.

### 8.8 `TileView`

Arquivo: `Assets/!/Scripts/UI/TileView.cs`

- Exibe o valor no `Text`.
- Aplica cor da paleta conforme `abs(value) % 8`.
- Controla escala via `transform.localScale`.
- Controla transparência via `CanvasGroup`.
- `ResetVisual()` restaura escala 1 e alpha 1.

O prefab `Tile.prefab` contém:

- `Tile` com `Image`, `TileView` e `CanvasGroup`.
- Filho `Label` com `Text`.
- Tamanho base `84×84`.
- Raycast desativado.

### 8.9 `PieceQueueView`

Arquivo: `Assets/!/Scripts/UI/PieceQueueView.cs`

Responsável pela fila visual de duas peças:

- `Initialize` cria `QueueCurrent` e `QueueNext`.
- `QueueCurrent` fica centralizada acima do tabuleiro, em escala 1.
- `QueueNext` fica no canto superior direito, em escala `0.7`.
- `BeginPreview` cancela tweens, restaura o visual e coloca a peça atual na frente.
- `MovePreviewTo` move a peça atual com LeanTween `easeOutQuad`.
- `CancelPreview` devolve a peça ao centro.
- `Advance`:
  1. Desativa e destrói a peça atual.
  2. Promove a próxima peça para atual.
  3. Move a nova atual para o centro com `easeOutBack`.
  4. Escala de `0.7` para `1` com `easeOutBack`.
  5. Cria a nova próxima já visível em `0.7`.

A nova próxima peça entra imediatamente visível para manter sempre duas peças na tela. A peça antiga é desativada no mesmo frame e destruída pelo Unity ao final do frame.

O `QueueRoot` não tem filhos salvos na cena; as duas peças são criadas em runtime. Os antigos objetos `ButtonsRoot`, `PieceText` e `NextPiecePanel` e o script `ColumnButton` foram removidos.

### 8.10 `GameplayScreen`

Arquivo: `Assets/!/Scripts/UI/GameplayScreen.cs`

É o controlador da cena e ponto de composição:

- Cria `BoardController` e `PieceGenerator` em `Awake`.
- Inicializa `BoardView` e `PieceQueueView`.
- Lê `Pointer.current` em `Update`.
- Bloqueia input durante animação ou game over.
- Implementa prévia, arraste e commit.
- Expõe `PlayColumn(int)` para inserção direta.
- Atualiza pontuação e status.
- Executa a coroutine de animação da jogada.

Estados de status:

- “Toque e arraste para escolher a coluna”
- “Solte para inserir”
- “Resolvendo...”
- “Coluna cheia”
- “GAME OVER”

## 9. Cena e layout

### Hierarquia da cena Gameplay

```text
Main Camera
GameCanvas
  Background
  Board
  Title
  ScoreText
  StatusText
  QueueRoot
EventSystem
```

`GameCanvas` contém:

- `Canvas`
- `CanvasScaler`
- `GraphicRaycaster`
- `GameplayScreen`

`EventSystem` usa `InputSystemUIInputModule`, mas o gameplay lê o ponteiro diretamente.

### Layout atual

| Elemento | Posição/tamanho |
|---|---|
| Canvas | `1920×1080` na verificação |
| Referência do CanvasScaler | `1080×1920`, match `0.5` |
| Board | `(0, -40)`, `444×714` |
| Topo local do board | `y = 317` |
| Centro da coluna 0 | `x = -180` |
| Centro da coluna 4 | `x = +180` |
| Fila atual | `(0, 361)`, escala `1` |
| Fila próxima | `(280, 363)`, escala `0.7` |
| Ponto de queda local | `y = 401` |
| StatusText | anchor inferior, `y = 30`, `800×50` |

A fila atual fica acima do board. A próxima fica no canto superior direito, na área antes ocupada pelo painel de próxima peça. O `QueueRoot` é o último filho do Canvas, garantindo que as peças da fila fiquem na frente do board.

## 10. Input e animações

### Input

- O projeto usa o novo Input System.
- `GameplayScreen.Update` lê `Pointer.current`.
- A leitura foi implementada para mouse e toque.
- Não há botões de coluna.
- A coluna é escolhida pela posição horizontal do ponteiro, convertida para coordenadas locais do board.
- A coluna é limitada entre `0` e `4`.
- A prévia só é atualizada quando a coluna muda.
- Soltar confirma a jogada.
- Se a coluna estiver cheia, a peça volta ao centro.

O caminho de input foi implementado com a API padrão do Input System, mas a simulação automatizada de toque no Editor não foi confiável. O teste final de toque real deve ser manual.

### Animações

| Animação | Implementação |
|---|---|
| Prévia horizontal | LeanTween `move`, `easeOutQuad`, `0.12s` |
| Retorno ao centro | LeanTween `move`, `easeOutQuad`, `0.12s` |
| Promoção da fila | LeanTween `move` + `scale`, `easeOutBack`, `0.3s` |
| Inserção no board | LeanTween `move` + `scale` + `value`, `easeOutQuad`, `0.18s` |
| Combinação | Coroutine com interpolação smoothstep, `0.12s` |
| Pop do resultado | Coroutine com interpolação, `0.12s` |
| Gravidade | Coroutine com interpolação smoothstep, `0.12s` |

A escolha de manter combinação/gravidade em coroutines foi deliberada para não reescrever um fluxo já validado. As novas partes visuais usam LeanTween.

## 11. Como executar e testar

1. Abrir o projeto no Unity `6000.3.10f1`.
2. Abrir `Assets/!/Scenes/Gameplay.unity`.
3. Entrar em Play Mode.
4. Pressionar e arrastar com mouse ou toque.
5. Soltar para inserir.
6. Verificar:
   - Fila inicial com duas peças.
   - Prévia acompanhando a coluna.
   - Inserção na coluna escolhida.
   - Fila avançando após uma jogada válida.
   - Combinações e pontuação.
   - Coluna cheia mantendo a peça atual.
   - Game over quando uma coluna ultrapassar 7 peças.

Para build, a cena Gameplay ainda precisa ser adicionada ao Build Settings. Atualmente `EditorBuildSettings.asset` aponta para `Assets/Scenes/SampleScene.unity`.

## 12. Verificações realizadas

| Verificação | Resultado |
|---|---|
| Compilação | 0 erros |
| Referências ausentes na cena | 0 |
| Cena Gameplay salva | Sim |
| Fila inicial | Atual `(0, 361)` escala `1`; próxima `(280, 363)` escala `0.7` |
| Prévia nas colunas 0/4 | `x = -180` e `x = +180` |
| Commit de jogada | Peça inserida, fila avançou |
| Combinação | Peça resultante e pontuação corretas |
| Coluna cheia | Status “Coluna cheia”, fila não avançou |
| Layout final | Fila e status sem sobreposição |
| Toque real no Editor | Não automatizado de forma confiável; pendente de teste manual |

## 13. Limitações e próximos passos

### Prioridade alta

1. Adicionar `Gameplay.unity` ao Build Settings e testar um build.
2. Testar toque real em dispositivo ou no Editor com mouse.
3. Definir a orientação/resolução alvo e revisar o CanvasScaler.

### Prioridade média

4. Implementar reinício após game over.
5. Persistir o recorde.
6. Criar a cena de menu e a transição para o gameplay.
7. Adicionar testes automatizados para `BoardModel`, `MergeResolver`, `CascadeResolver` e `ScoreService`.

### Prioridade baixa

8. Integrar áudio.
9. Adicionar feedback visual de pontuação.
10. Avaliar pooling de tiles.
11. Revisar animações de combinação/gravidade para LeanTween, se necessário.
12. Remover assets não utilizados ou documentar seu propósito.

## 14. Referência rápida

| Arquivo | Responsabilidade |
|---|---|
| `Assets/!/Scripts/Gameplay/BoardModel.cs` | Estado e operações básicas do tabuleiro |
| `Assets/!/Scripts/Gameplay/BoardController.cs` | Orquestra jogada, cascata e game over |
| `Assets/!/Scripts/Gameplay/CascadeResolver.cs` | Resolve cascatas com proteções |
| `Assets/!/Scripts/Gameplay/MergeResolver.cs` | Escolhe e aplica uma combinação |
| `Assets/!/Scripts/Gameplay/MergeMath.cs` | Fórmula e limites de combinação |
| `Assets/!/Scripts/Gameplay/PieceGenerator.cs` | Geração 2/4 e fila lógica |
| `Assets/!/Scripts/Gameplay/BoardSnapshot.cs` | Cópia imutável do tabuleiro |
| `Assets/!/Scripts/Gameplay/MoveResult.cs` | Dados de uma jogada completa |
| `Assets/!/Scripts/Gameplay/CascadeResult.cs` | Resultado agregado da cascata |
| `Assets/!/Scripts/Gameplay/CascadeStep.cs` | Snapshots de uma etapa da cascata |
| `Assets/!/Scripts/Gameplay/MergeOutcome.cs` | Dados de uma combinação |
| `Assets/!/Scripts/Managers/ScoreService.cs` | Pontuação e recorde |
| `Assets/!/Scripts/UI/GameplayScreen.cs` | Controlador da cena e input |
| `Assets/!/Scripts/UI/BoardView.cs` | Visual do tabuleiro e animações |
| `Assets/!/Scripts/UI/PieceQueueView.cs` | Fila visual de duas peças |
| `Assets/!/Scripts/UI/TileView.cs` | Visual individual de tile |
| `Assets/!/Prefabs/Tile.prefab` | Prefab base do tile |
| `Assets/!/Scenes/Gameplay.unity` | Cena jogável atual |
| `Assets/!/Scenes/MainMenu.unity` | Placeholder de menu |
| `Assets/LeanTween` | Biblioteca de animações |
