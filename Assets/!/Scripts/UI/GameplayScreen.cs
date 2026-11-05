using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using NumberMerge.Ads;
using NumberMerge.Audio;
using NumberMerge.Gameplay;
using NumberMerge.Localization;
using NumberMerge.Managers;
using NumberMerge.Save;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NumberMerge.UI
{
    public sealed class GameplayScreen : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private PieceQueueView _queueView;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _recordText;
        [Tooltip("Pulse played on the score text every time the score goes up.")]
        [SerializeField] private ScorePulse _scorePulse;
        [Tooltip("Pulse played on the record text every time the record goes up.")]
        [SerializeField] private ScorePulse _recordPulse;
        [Tooltip("Floater prefab spawned for each merge. Flies from under the score text into it.")]
        [SerializeField] private ScoreFloater _scoreFloaterPrefab;
        [Tooltip("Timing and offset of the score floaters.")]
        [SerializeField] private ScoreFloatConfig _scoreFloatConfig;
        [Tooltip("Parent for the spawned floaters. Should be under the same Canvas as the score text, not in a layout group.")]
        [SerializeField] private RectTransform _scoreFloatParent;
        [Tooltip("Object whose horizontal center the floaters spawn at (the Score panel).")]
        [SerializeField] private RectTransform _scoreFloatSpawnAnchor;
        [SerializeField] private TMP_Text _statusText;
        [Tooltip("Localized event on the status text. Its key is changed for each status message.")]
        [SerializeField] private LocalizeStringEvent _statusLocalize;
        [Tooltip("Localized event on the score text. Its argument is the formatted score.")]
        [SerializeField] private LocalizeStringEvent _scoreLocalize;
        [Tooltip("Localized event on the record text. Its argument is the formatted record.")]
        [SerializeField] private LocalizeStringEvent _recordLocalize;
        [SerializeField] private GameOverScreen _gameOverScreen;
        [SerializeField] private ComboResultScreen _comboResultScreen;
        [Tooltip("Balance asset for piece generation and merge rules (including the merge ceiling). Must be assigned.")]
        [SerializeField] private PieceGenerationConfig _pieceGenerationConfig;
        [Tooltip("Theme whose Background Sprites are picked from at random on load.")]
        [SerializeField] private TileTheme _theme;
        [Tooltip("SpriteRenderer of the BackgroundSprite object that displays the chosen background.")]
        [SerializeField] private SpriteRenderer _backgroundRenderer;

        private PieceGenerationSettings _settings;
        private BoardController _controller;
        private PieceGenerator _pieces;
        private RectTransform _boardRect;
        private bool _isAnimating;
        private bool _isPreviewing;
        private int _previewColumn = -1;
        private bool _hasShownScore;
        private long _shownRecord;
        private readonly ScoreDisplayCounter _scoreCounter = new ScoreDisplayCounter();
        private Color _textColor = Color.white;

        [Tooltip("Revive balance: tiles removed per revive and the delay before the removal starts.")]
        [SerializeField] private ReviveConfig _reviveConfig;

        // Revives the player can still use. Saved by SaveSystem; only changed after a revive has been saved.
        private int _revives;

        // Set by the debug key so the next revive does not spend one.
        private bool _debugFreeRevive;

        [Tooltip("Undo balance: uses per game and the animation time.")]
        [SerializeField] private UndoConfig _undoConfig;
        [Tooltip("Undo button. Interactable only when an undo is available.")]
        [SerializeField] private Button _undoButton;
        [Tooltip("Small counter on the Undo button showing the uses left.")]
        [SerializeField] private TMP_Text _undoCounterText;

        // One random source for piece generation, merges and revive. Its state is saved with the board,
        // and undo rewinds it, so the pieces after an undo are the ones the player would have seen.
        private SeededRandomSource _random;

        // Playable state before the last insertion. Null when there is nothing to undo. Saved with the board.
        private BoardState _undoState;

        // Undos left. Saved by SaveSystem; only changed when an undo is used.
        private int _undoUses;

        [Tooltip("Destroy balance: uses per game. Tile counts come from ReviveConfig.")]
        [SerializeField] private DestroyConfig _destroyConfig;
        [Tooltip("Destroy button. Interactable only when Destroy can be used.")]
        [SerializeField] private Button _destroyButton;
        [Tooltip("Small counter on the Destroy button showing the uses left.")]
        [SerializeField] private TMP_Text _destroyCounterText;

        // Destroy uses left. Saved by SaveSystem; only changed when a Destroy is used.
        private int _destroyUses;

        [Tooltip("Rewarded recharge balance: how many insertions must pass before the ad icon is offered.")]
        [SerializeField] private RechargeConfig _rechargeConfig;
        [Tooltip("Ad icon shown on the Undo button when its uses are empty (a copy of the Game Over ad icon).")]
        [SerializeField] private GameObject _undoAdIcon;
        [Tooltip("Ad icon shown on the Destroy button when its uses are empty (a copy of the Game Over ad icon).")]
        [SerializeField] private GameObject _destroyAdIcon;

        // Tile insertions since the last rewarded recharge, shared by Undo and Destroy. Saved with the board.
        private int _movesSinceRecharge;

        private enum Powerup
        {
            Undo,
            Destroy
        }

        private int RechargeMoves => _rechargeConfig != null ? _rechargeConfig.MovesPerRecharge : RechargeConfig.DefaultMovesPerRecharge;

        // True while a rewarded ad is on screen (including the grace period after it closes).
        private bool IsAdShowing => AdsManager.Instance != null &&
                                    AdsManager.Instance.Rewarded != null &&
                                    AdsManager.Instance.Rewarded.IsShowing;

        private int UndoMaxUses => _undoConfig != null ? _undoConfig.MaxUses : UndoConfig.DefaultMaxUses;
        private float UndoDuration => _undoConfig != null ? _undoConfig.AnimationDuration : UndoConfig.DefaultAnimationDuration;

        // True when the Undo button can be used right now.
        public bool CanUndo =>
            _undoState != null &&
            _undoUses > 0 &&
            _controller != null &&
            !_controller.IsGameOver &&
            !_isAnimating &&
            !_isPreviewing &&
            !IsAdShowing;

        private void OnEnable()
        {
            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        private void Awake()
        {
            _boardRect = _boardView.GetComponent<RectTransform>();
            _boardView.MergeCompleted += OnMergeCompleted;

            if (_scoreFloatParent == null && _scoreText != null)
            {
                _scoreFloatParent = _scoreText.rectTransform.parent as RectTransform;
            }

            _settings = ResolveSettings();

            ApplyRandomBackground();
            ApplyTextColor();
            _revives = SaveSystem.LoadRevives();
            _undoUses = SaveSystem.LoadUndoUses(UndoMaxUses);
            _destroyUses = SaveSystem.LoadDestroyUses(DestroyMaxUses);
            LoadOrStartGame();

            if (_undoButton != null)
            {
                _undoButton.onClick.AddListener(OnUndoClicked);
            }

            if (_destroyButton != null)
            {
                _destroyButton.onClick.AddListener(OnDestroyClicked);
            }

            if (_gameOverScreen != null)
            {
                _gameOverScreen.ContinueConfirmed += OnContinueConfirmed;
            }

            _boardView.Initialize(_controller.Board);
            InitializeQueue();
            RefreshUi();

            // Plays on every gameplay scene load, including when a saved board is restored.
            AudioManager.Instance?.Play(SfxId.GameStart);

            // The player has control from the start, so save the initial state now.
            SaveProgress();
        }

        // Picks one of the theme's background sprites at random. Keeps the current sprite if none are assigned.
        private void ApplyRandomBackground()
        {
            if (_backgroundRenderer == null || _theme == null)
            {
                return;
            }

            Sprite background = _theme.GetRandomBackgroundSprite();
            if (background != null)
            {
                _backgroundRenderer.sprite = background;
            }
        }

        private void OnDestroy()
        {
            if (_boardView != null)
            {
                _boardView.MergeCompleted -= OnMergeCompleted;
            }
        }

        // Applies the theme's text color to every text in the game (score, status, and game over screen).
        private void ApplyTextColor()
        {
            if (_theme == null)
            {
                return;
            }

            Color color = _theme.TextColor;
            _textColor = color;
            _scoreText.color = color;
            _recordText.color = color;
            _statusText.color = color;

            if (_undoCounterText != null)
            {
                _undoCounterText.color = color;
            }

            if (_destroyCounterText != null)
            {
                _destroyCounterText.color = color;
            }

            if (_gameOverScreen != null)
            {
                _gameOverScreen.SetTextColor(color);
            }

            if (_comboResultScreen != null)
            {
                _comboResultScreen.SetTextColor(color);
            }
        }

        // Restores the saved board when there is one; otherwise starts a new game with the saved record.
        private void LoadOrStartGame()
        {
            long record = SaveSystem.LoadRecord();
            BoardState saved = SaveSystem.LoadBoardState();

            if (saved != null &&
                saved.TryRestore(out BoardModel restored, out long score) &&
                restored.ColumnCount == _boardView.ColumnCount &&
                restored.VisibleRowCount == _boardView.VisibleRowCount)
            {
                // A save without a random state (made before it existed) starts a fresh sequence.
                _random = new SeededRandomSource(saved.TryGetRandomState(out ulong randomState)
                    ? randomState
                    : SeededRandomSource.NewSeed());

                _pieces = CreatePieceGenerator();
                _pieces.Restore(saved.CurrentPiece, saved.NextPiece);
                _controller = new BoardController(restored, CreateCascadeResolver(), new ScoreService(record, score), _settings.MergeCeilingValue);
                _undoState = ValidUndoState(saved.GetUndoState());
                _movesSinceRecharge = Math.Max(0, saved.MovesSinceRecharge);

                if (_controller.IsGameOver)
                {
                    _undoState = null;
                    SaveSystem.DeleteBoardState();
                }

                return;
            }

            // Invalid or mismatched saved data is discarded.
            if (saved != null)
            {
                SaveSystem.DeleteBoardState();
            }

            _random = new SeededRandomSource(SeededRandomSource.NewSeed());
            var board = new BoardModel(_boardView.ColumnCount, _boardView.VisibleRowCount);
            _controller = new BoardController(board, CreateCascadeResolver(), new ScoreService(record), _settings.MergeCeilingValue);
            _pieces = CreatePieceGenerator();
            _pieces.Reset(board);
            _undoState = null;
            _movesSinceRecharge = 0;
        }

        // An undo state is only usable when it is complete and matches the board dimensions.
        private BoardState ValidUndoState(BoardState candidate)
        {
            if (candidate == null ||
                candidate.ColumnCount != _boardView.ColumnCount ||
                candidate.VisibleRowCount != _boardView.VisibleRowCount ||
                !candidate.TryRestore(out _, out _) ||
                !candidate.TryGetRandomState(out _))
            {
                return null;
            }

            return candidate;
        }

        // Piece generation and merge rules (including the merge ceiling) all come from the same balance asset.
        private PieceGenerationSettings ResolveSettings()
        {
            if (_pieceGenerationConfig == null)
            {
                Debug.LogError("[GameplayScreen] No PieceGenerationConfig assigned; using the default settings.", this);
                return new PieceGenerationSettings();
            }

            return _pieceGenerationConfig.Settings;
        }

        private PieceGenerator CreatePieceGenerator()
        {
            return new PieceGenerator(_random, _settings);
        }

        private CascadeResolver CreateCascadeResolver()
        {
            var mergeResolver = new MergeResolver(_random, _settings.MergeCeilingValue);
            return new CascadeResolver(mergeResolver);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
            {
                DebugRevive();
            }
#endif

            RefreshPowerupButtons();

            if (DebugScreen.IsOpen || PauseMenu.IsPaused || _controller == null || _controller.IsGameOver || _isAnimating || IsAdShowing)
            {
                return;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            if (!_isPreviewing && pointer.press.wasPressedThisFrame)
            {
                BeginPreview(pointer.position.ReadValue());
            }

            if (!_isPreviewing)
            {
                return;
            }

            if (pointer.press.wasReleasedThisFrame || !pointer.press.isPressed)
            {
                CommitPreview();
            }
            else
            {
                UpdatePreview(pointer.position.ReadValue());
            }
        }

        public bool PlayColumn(int column)
        {
            if (_controller.IsGameOver || _isAnimating)
            {
                return false;
            }

            int value = _pieces.Current;

            // The playable state before this insertion. Only kept if the insertion succeeds.
            BoardState beforeMove = CaptureState();

            if (!_controller.TryPlay(column, value, out MoveResult move))
            {
                return false;
            }

            // The board is already resolved here, so the new piece uses the post-move state.
            _pieces.Advance(_controller.Board);
            _queueView.Advance(_pieces.Next);

            // Only tile insertions count toward the rewarded recharge.
            _movesSinceRecharge++;

            // A move that ends in game over cannot be undone. Only Revive gets the player out of it.
            _undoState = _controller.IsGameOver ? null : beforeMove;

            _isAnimating = true;
            UiText.SetKey(_statusLocalize, "status.resolving");
            StartCoroutine(AnimateMove(move, column));
            return true;
        }

        private void BeginPreview(Vector2 screenPosition)
        {
            // Presses on a button (e.g. the pause button) or in the safe area below the board must not start a drop.
            if (IsPressBlocked(screenPosition))
            {
                return;
            }

            _isPreviewing = true;
            _previewColumn = -1;
            _queueView.BeginPreview();
            UpdatePreview(screenPosition);
            UiText.SetKey(_statusLocalize, "status.release_to_drop");
        }

        // True when the press must not start a drop: it hits a button (the topmost UI element is a Selectable),
        // or it hits a UiInputBlocker anywhere in the stack (the safe area below the board).
        private static bool IsPressBlocked(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            var eventData = new PointerEventData(eventSystem) { position = screenPosition };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(eventData, results);

            // Results are sorted topmost first. Only raycast-target graphics appear here, so non-interactive
            // images (background, board, text) are ignored automatically.
            if (results.Count > 0 && results[0].gameObject.GetComponentInParent<Selectable>() != null)
            {
                return true;
            }

            // The safe area can be covered by other raycast-target graphics (e.g. the status bar), so search all hits.
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.GetComponentInParent<UiInputBlocker>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdatePreview(Vector2 screenPosition)
        {
            int column = GetColumnAt(screenPosition);
            if (column == _previewColumn)
            {
                return;
            }

            _previewColumn = column;
            float x = _boardRect.anchoredPosition.x + _boardView.GetColumnCenterX(column);
            _queueView.MovePreviewTo(x);
            UpdateInsertionGhost(column);
        }

        // Shows the ghost on the cell where the piece would land: the lowest empty row of the column.
        private void UpdateInsertionGhost(int column)
        {
            if (_controller.Board.TryFindLowestEmptyRow(column, out int row))
            {
                _boardView.ShowInsertionPreview(column, row);
            }
            else
            {
                _boardView.HideInsertionPreview();
            }
        }

        private int GetColumnAt(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _boardRect,
                screenPosition,
                null,
                out Vector2 localPoint);

            return _boardView.GetNearestColumn(localPoint);
        }

        private void CommitPreview()
        {
            _isPreviewing = false;
            _boardView.HideInsertionPreview();

            int column = _previewColumn;
            _previewColumn = -1;

            if (column < 0)
            {
                _queueView.CancelPreview();
                return;
            }

            if (!PlayColumn(column))
            {
                _queueView.CancelPreview();
                UiText.SetKey(_statusLocalize, "status.column_full");
            }
        }

        private void InitializeQueue()
        {
            float previewY = _boardRect.anchoredPosition.y + _boardView.GetDropStartLocalY();

            _queueView.Initialize(
                _pieces.Current,
                _pieces.Next,
                new Vector2(_boardRect.anchoredPosition.x, previewY),
                new Vector2(280f, previewY + 75f),
                _boardView.CellSize);
        }

        private IEnumerator AnimateMove(MoveResult move, int column)
        {
            yield return _boardView.PlayMove(move, _boardView.GetDropStartPosition(column));
            yield return WaitForScoreFloaters();

            // Save right before the player regains control, so the saved state is the one they can act on.
            SaveProgress();

            // Combo result is shown from the count of this move, then the counter is cleared for the next move.
            if (_comboResultScreen != null)
            {
                _comboResultScreen.Show(_controller.Combos.Count);
            }

            _controller.Combos.Reset();
            _isAnimating = false;
            RefreshUi();
        }

        // Removes the given tiles at once and plays the solve that follows. Input is ignored until it ends.
        // onComplete runs after the board has been saved. Returns false, without changing anything, when the
        // tiles cannot be removed right now.
        public bool DestroyTiles(IReadOnlyList<BoardPosition> positions, Action onComplete = null)
        {
            if (_controller == null || _isAnimating || _isPreviewing)
            {
                return false;
            }

            if (!_controller.TryDestroyTiles(positions, out DestructionResult destruction))
            {
                return false;
            }

            // Undo never reaches back across a removal (revive, or Destroy later): the board it would restore is gone.
            _undoState = null;
            _isAnimating = true;
            _statusText.gameObject.SetActive(true);
            UiText.SetKey(_statusLocalize, "status.resolving");
            StartCoroutine(AnimateDestruction(destruction, onComplete));
            return true;
        }

        private IEnumerator AnimateDestruction(DestructionResult destruction, Action onComplete)
        {
            yield return _boardView.PlayDestruction(destruction);
            yield return WaitForScoreFloaters();

            // Save the playable board before anything else, so onComplete runs after it.
            SaveProgress();

            // Merges from the solve count as normal merges: they show the combo result like a move does.
            if (_comboResultScreen != null)
            {
                _comboResultScreen.Show(_controller.Combos.Count);
            }

            _controller.Combos.Reset();
            _isAnimating = false;
            RefreshUi();
            onComplete?.Invoke();
        }

        // Runs after the game over screen has faded out. Reached only once the rewarded ad has been granted
        // (or from the debug key).
        private void OnContinueConfirmed()
        {
            bool freeRevive = _debugFreeRevive;
            _debugFreeRevive = false;

            if (!freeRevive && _revives <= 0)
            {
                // No revive left: show the game over screen again instead of leaving the player with no way forward.
                _gameOverScreen.Show(_controller.CurrentScore, _controller.Record, _revives);
                return;
            }

            StartCoroutine(StartRevive(spendRevive: !freeRevive));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Debug key (F11): runs the revive from the game over screen without watching an ad or spending a revive.
        private void DebugRevive()
        {
            if (_controller == null || !_controller.IsGameOver || _isAnimating || _gameOverScreen == null)
            {
                Debug.Log("[Debug] F11 revive ignored: the game is not in game over.", this);
                return;
            }

            Debug.Log("[Debug] F11 revive (no ad, no revive spent).", this);
            _debugFreeRevive = true;
            _gameOverScreen.TriggerContinue();
        }
#endif

        private float ReviveDelay => _reviveConfig != null ? _reviveConfig.DestructionDelay : 0.5f;

        // The tiles a removal takes (Revive and Destroy alike). Same picking rules, from ReviveConfig.
        private List<BoardPosition> ChooseRemovalTiles()
        {
            return _reviveConfig != null
                ? ReviveSelection.Choose(_controller.Board, _random, _reviveConfig.MinDestroyedTiles, _reviveConfig.MaxDestroyedFraction)
                : ReviveSelection.Choose(_controller.Board, _random);
        }

        private int RemovalMinimumTiles => _reviveConfig != null ? _reviveConfig.MinDestroyedTiles : ReviveSelection.DefaultMinTiles;

        private int CountTiles()
        {
            int count = 0;
            for (int column = 0; column < _controller.Board.ColumnCount; column++)
            {
                count += _controller.Board.GetPieceCount(column);
            }

            return count;
        }

        private int DestroyMaxUses => _destroyConfig != null ? _destroyConfig.MaxUses : DestroyConfig.DefaultMaxUses;

        // True when the Destroy button can be used right now.
        public bool CanDestroy =>
            _controller != null &&
            DestroyEligibility.CanDestroy(
                _destroyUses,
                _controller.IsGameOver,
                _isAnimating || _isPreviewing || IsAdShowing,
                CountTiles(),
                RemovalMinimumTiles);

        // Removes the same tiles a revive would, outside game over. Costs one use, spent only after the removal.
        // Undo is cleared by the removal (inside DestroyTiles), as it is for revive.
        public bool TryDestroy()
        {
            if (!CanDestroy)
            {
                return false;
            }

            return DestroyTiles(ChooseRemovalTiles(), () =>
            {
                _destroyUses = Math.Max(0, _destroyUses - 1);
                SaveSystem.SaveDestroyUses(_destroyUses);
            });
        }

        private void OnDestroyClicked()
        {
            if (CanDestroy)
            {
                TryDestroy();
            }
            else
            {
                RequestRecharge(Powerup.Destroy);
            }
        }

        // Waits after the game over fade-out, then removes the tiles and solves the board.
        private IEnumerator StartRevive(bool spendRevive)
        {
            yield return new WaitForSecondsRealtime(ReviveDelay);

            if ((spendRevive && _revives <= 0) || _controller == null || !_controller.IsGameOver)
            {
                yield break;
            }

            List<BoardPosition> tiles = ChooseRemovalTiles();

            bool started = DestroyTiles(tiles, () =>
            {
                if (!spendRevive)
                {
                    return;
                }

                // The revive is spent only now: the board has been resolved and saved.
                _revives = Math.Max(0, _revives - 1);
                SaveSystem.SaveRevives(_revives);
            });

            if (!started)
            {
                // Nothing was removed, so the player is still in game over: show the screen again.
                _gameOverScreen.Show(_controller.CurrentScore, _controller.Record, _revives);
            }
        }

        // Called right before the player regains control. Record is always saved.
        // The board is saved in its playable state. A game over board is never saved, so the save stays at
        // the state before the move that caused the game over.
        private void SaveProgress()
        {
            SaveSystem.SaveRecord(_controller.Record);

            if (_controller.IsGameOver)
            {
                return;
            }

            BoardState state = CaptureState();
            state.SetUndoState(_undoState);
            SaveSystem.SaveBoardState(state);
        }

        // The playable state as it is now: board, queue, score and random state. The undo state is not included.
        private BoardState CaptureState()
        {
            BoardState state = BoardState.Capture(
                _controller.Board,
                _pieces.Current,
                _pieces.Next,
                _controller.CurrentScore);

            state.RandomState = _random.State.ToString(CultureInfo.InvariantCulture);
            state.MovesSinceRecharge = _movesSinceRecharge;
            return state;
        }

        private void OnUndoClicked()
        {
            if (CanUndo)
            {
                TryUndo();
            }
            else
            {
                RequestRecharge(Powerup.Undo);
            }
        }

        // Takes the board back to the playable state before the last insertion: board, score, queue and random state.
        // The record is not reverted. Costs one use. Only one undo is possible per insertion.
        public bool TryUndo()
        {
            if (!CanUndo)
            {
                return false;
            }

            BoardState snapshot = _undoState;
            if (!snapshot.TryRestore(out BoardModel restored, out long score) ||
                !snapshot.TryGetRandomState(out ulong randomState))
            {
                return false;
            }

            // What is on screen now, so the animation can move it back.
            BoardSnapshot shown = BoardSnapshot.Capture(_controller.Board);

            _undoUses = Math.Max(0, _undoUses - 1);
            SaveSystem.SaveUndoUses(_undoUses);

            _random.State = randomState;
            _controller = new BoardController(restored, CreateCascadeResolver(), new ScoreService(_controller.Record, score), _settings.MergeCeilingValue);
            _pieces.Restore(snapshot.CurrentPiece, snapshot.NextPiece);
            _movesSinceRecharge = Math.Max(0, snapshot.MovesSinceRecharge);
            _undoState = null;

            // Saved right away, so quitting after an undo keeps the undo spent.
            SaveProgress();

            AudioManager.Instance?.Play(SfxId.Undo);
            UiText.SetKey(_statusLocalize, "status.resolving");
            _queueView.SetPieces(_pieces.Current, _pieces.Next);

            _isAnimating = true;
            StartCoroutine(AnimateUndo(shown));
            return true;
        }

        private IEnumerator AnimateUndo(BoardSnapshot shown)
        {
            BoardSnapshot restored = BoardSnapshot.Capture(_controller.Board);
            yield return _boardView.PlayUndo(shown, restored, UndoDuration);

            _isAnimating = false;
            RefreshUi();
        }

        // Keeps both power-up buttons in step with the game: interactable state, counter, and the rewarded ad icon.
        // Cheap, so it runs every frame.
        private void RefreshPowerupButtons()
        {
            RefreshPowerupButton(_undoButton, _undoCounterText, _undoAdIcon, _undoUses, CanUndo, RechargeOffered(Powerup.Undo));
            RefreshPowerupButton(_destroyButton, _destroyCounterText, _destroyAdIcon, _destroyUses, CanDestroy, RechargeOffered(Powerup.Destroy));
        }

        // The button is clickable when it can be used or when its ad icon is offered. The icon replaces the counter.
        private static void RefreshPowerupButton(
            Button button,
            TMP_Text counter,
            GameObject adIcon,
            int uses,
            bool canUse,
            bool rechargeOffered)
        {
            if (button != null)
            {
                button.interactable = canUse || rechargeOffered;
            }

            if (adIcon != null && adIcon.activeSelf != rechargeOffered)
            {
                adIcon.SetActive(rechargeOffered);
            }

            if (counter != null)
            {
                if (counter.gameObject.activeSelf == rechargeOffered)
                {
                    counter.gameObject.SetActive(!rechargeOffered);
                }

                string count = uses.ToString(CultureInfo.InvariantCulture);
                if (counter.text != count)
                {
                    counter.text = count;
                }
            }
        }

        // The ad icon is offered only when the power-up is empty, enough insertions have passed since the last
        // recharge, an ad is loaded, and the power-up's own condition holds (an undo snapshot, or enough tiles for Destroy).
        private bool RechargeOffered(Powerup powerup)
        {
            if (_controller == null)
            {
                return false;
            }

            bool adAvailable = AdsManager.Instance != null &&
                               AdsManager.Instance.Rewarded != null &&
                               AdsManager.Instance.Rewarded.CanShow;

            bool busy = _isAnimating || _isPreviewing || IsAdShowing;

            bool buttonConditions = powerup == Powerup.Undo
                ? _undoState != null
                : CountTiles() >= RemovalMinimumTiles;

            return RechargeEligibility.ShowsAdIcon(
                powerup == Powerup.Undo ? _undoUses : _destroyUses,
                _movesSinceRecharge,
                RechargeMoves,
                adAvailable,
                _controller.IsGameOver,
                busy,
                buttonConditions);
        }

        // Shows the rewarded ad. The refill happens only from the reward callback, like the revive on game over.
        private void RequestRecharge(Powerup powerup)
        {
            if (!RechargeOffered(powerup))
            {
                return;
            }

            RewardedAdService rewarded = AdsManager.Instance.Rewarded;
            rewarded.TryShow(onRewarded: () => Recharge(powerup), onFailed: null);
        }

        // Refills the power-up to its config value and restarts the insertion count. Saved right away.
        private void Recharge(Powerup powerup)
        {
            if (powerup == Powerup.Undo)
            {
                _undoUses = UndoMaxUses;
                SaveSystem.SaveUndoUses(_undoUses);
            }
            else
            {
                _destroyUses = DestroyMaxUses;
                SaveSystem.SaveDestroyUses(_destroyUses);
            }

            _movesSinceRecharge = 0;

            // Not in game over here (the icon is hidden then), so the board state is saved with the new count.
            SaveProgress();
        }

        // Runs once the board animation has ended. Input stays locked until every floater has reached the score,
        // so the score text is always exact again before the next move starts.
        private IEnumerator WaitForScoreFloaters()
        {
            yield return new WaitUntil(() => _scoreCounter.Pending == 0);
        }

        // Spawns a "+N" floater for one merge. The points are counted as pending until the floater arrives.
        private void OnMergeCompleted(MergeOutcome merge)
        {
            long points = merge.ResultValue;
            _scoreCounter.AddPending(points);

            if (_scoreFloaterPrefab == null || _scoreFloatConfig == null || _scoreText == null)
            {
                // Without a floater the points still arrive at once, so nothing waits forever.
                ArriveScore(points);
                return;
            }

            // The floater rises to a point a quarter of the anchor's height above its bottom edge (the center of the
            // lower half), and starts SpawnOffset below that point.
            RectTransform anchor = _scoreFloatSpawnAnchor != null ? _scoreFloatSpawnAnchor : _scoreText.rectTransform;
            Rect rect = anchor.rect;
            Vector3 target = anchor.TransformPoint(new Vector3(rect.center.x, rect.yMin + rect.height * 0.25f, 0f));
            Vector3 start = target + anchor.TransformVector(0f, -_scoreFloatConfig.SpawnOffset, 0f);

            Transform parent = _scoreFloatParent != null ? _scoreFloatParent : _scoreText.rectTransform.parent;
            ScoreFloater floater = Instantiate(_scoreFloaterPrefab, parent);
            floater.Play(points, _textColor, start, target, _scoreFloatConfig, () => ArriveScore(points));
        }

        // A floater reached the score: the displayed number goes up and the score text pulses.
        private void ArriveScore(long points)
        {
            long displayed = _scoreCounter.Arrive(points);
            UpdateScoreLabels(displayed, _shownRecord);
            _scorePulse.Pulse();
        }

        private void RefreshUi()
        {
            long score = _controller.CurrentScore;
            long record = _controller.Record;

            // All floaters have landed (see WaitForScoreFloaters), so the displayed score is the real total.
            _scoreCounter.SyncTo(score);
            UpdateScoreLabels(score, record);

            // The first refresh (scene load) shows the values without pulsing. Score pulses on each arrival instead.
            if (_hasShownScore && record > _shownRecord)
            {
                _recordPulse.Pulse();
            }

            _hasShownScore = true;
            _shownRecord = record;

            if (_controller.IsGameOver)
            {
                _statusText.gameObject.SetActive(false);
                _gameOverScreen.Show(_controller.CurrentScore, _controller.Record, _revives);
                return;
            }

            UiText.SetKey(_statusLocalize, "status.drag_to_choose_column");
        }

        // Score and record are formatted with the language's digit grouping, so they are set again on a language change.
        private void UpdateScoreLabels(long score, long record)
        {
            UiText.SetKey(_recordLocalize, "score.record", ScoreFormatter.Format(record));
            UiText.SetKey(_scoreLocalize, "score.points", ScoreFormatter.Format(score));
        }

        private void OnLanguageChanged(Locale locale)
        {
            UpdateScoreLabels(_scoreCounter.Displayed, _shownRecord);
        }
    }
}
