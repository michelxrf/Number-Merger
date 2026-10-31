using System;
using System.Collections;
using System.Collections.Generic;
using NumberMerge.Audio;
using NumberMerge.Gameplay;
using NumberMerge.Managers;
using NumberMerge.Save;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

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
        [SerializeField] private TMP_Text _statusText;
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
        private long _shownScore;
        private long _shownRecord;

        [Tooltip("Revive balance: tiles removed per revive and the delay before the removal starts.")]
        [SerializeField] private ReviveConfig _reviveConfig;

        // Revives the player can still use. Saved by SaveSystem; only changed after a revive has been saved.
        private int _revives;

        // Set by the debug key so the next revive does not spend one.
        private bool _debugFreeRevive;

        private void Awake()
        {
            _boardRect = _boardView.GetComponent<RectTransform>();

            _settings = ResolveSettings();

            ApplyRandomBackground();
            ApplyTextColor();
            _revives = SaveSystem.LoadRevives();
            LoadOrStartGame();

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

        // Applies the theme's text color to every text in the game (score, status, and game over screen).
        private void ApplyTextColor()
        {
            if (_theme == null)
            {
                return;
            }

            Color color = _theme.TextColor;
            _scoreText.color = color;
            _recordText.color = color;
            _statusText.color = color;

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
                _pieces = CreatePieceGenerator();
                _pieces.Restore(saved.CurrentPiece, saved.NextPiece);
                _controller = new BoardController(restored, CreateCascadeResolver(), new ScoreService(record, score), _settings.MergeCeilingValue);

                if (_controller.IsGameOver)
                {
                    SaveSystem.DeleteBoardState();
                }

                return;
            }

            // Invalid or mismatched saved data is discarded.
            if (saved != null)
            {
                SaveSystem.DeleteBoardState();
            }

            var board = new BoardModel(_boardView.ColumnCount, _boardView.VisibleRowCount);
            _controller = new BoardController(board, CreateCascadeResolver(), new ScoreService(record), _settings.MergeCeilingValue);
            _pieces = CreatePieceGenerator();
            _pieces.Reset(board);
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
            return new PieceGenerator(new SystemRandomSource(), _settings);
        }

        private CascadeResolver CreateCascadeResolver()
        {
            var mergeResolver = new MergeResolver(new SystemRandomSource(), _settings.MergeCeilingValue);
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

            if (DebugScreen.IsOpen || PauseMenu.IsPaused || _controller == null || _controller.IsGameOver || _isAnimating)
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

            if (!_controller.TryPlay(column, value, out MoveResult move))
            {
                return false;
            }

            // The board is already resolved here, so the new piece uses the post-move state.
            _pieces.Advance(_controller.Board);
            _queueView.Advance(_pieces.Next);

            _isAnimating = true;
            _statusText.text = "Resolvendo...";
            StartCoroutine(AnimateMove(move, column));
            return true;
        }

        private void BeginPreview(Vector2 screenPosition)
        {
            // Presses outside the board (e.g. the pause button) must not start a drop.
            if (!RectTransformUtility.RectangleContainsScreenPoint(_boardRect, screenPosition, null))
            {
                return;
            }

            _isPreviewing = true;
            _previewColumn = -1;
            _queueView.BeginPreview();
            UpdatePreview(screenPosition);
            _statusText.text = "Solte para inserir";
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
                _statusText.text = "Coluna cheia";
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

            _isAnimating = true;
            _statusText.gameObject.SetActive(true);
            _statusText.text = "Resolvendo...";
            StartCoroutine(AnimateDestruction(destruction, onComplete));
            return true;
        }

        private IEnumerator AnimateDestruction(DestructionResult destruction, Action onComplete)
        {
            yield return _boardView.PlayDestruction(destruction);

            // Save the playable board before anything else, so onComplete (which spends a revive) runs after it.
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

        // The Continue button on the game over screen. The screen has already faded out when this runs.
        private void OnContinueConfirmed()
        {
            bool freeRevive = _debugFreeRevive;
            _debugFreeRevive = false;

            if (!freeRevive && _revives <= 0)
            {
                return;
            }

            StartCoroutine(StartRevive(spendRevive: !freeRevive));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Debug key (F11): runs the revive from the game over screen without spending a revive.
        private void DebugRevive()
        {
            if (_controller == null || !_controller.IsGameOver || _isAnimating || _gameOverScreen == null)
            {
                Debug.Log("[Debug] F11 revive ignored: the game is not in game over.", this);
                return;
            }

            Debug.Log("[Debug] F11 revive (no revive spent).", this);
            _debugFreeRevive = true;
            _gameOverScreen.TriggerContinue();
        }
#endif

        private float ReviveDelay => _reviveConfig != null ? _reviveConfig.DestructionDelay : 0.5f;

        // Waits after the game over fade-out, then removes the tiles and solves the board.
        private IEnumerator StartRevive(bool spendRevive)
        {
            yield return new WaitForSecondsRealtime(ReviveDelay);

            if ((spendRevive && _revives <= 0) || _controller == null || !_controller.IsGameOver)
            {
                yield break;
            }

            List<BoardPosition> tiles = _reviveConfig != null
                ? ReviveSelection.Choose(_controller.Board, new SystemRandomSource(), _reviveConfig.MinDestroyedTiles, _reviveConfig.MaxDestroyedFraction)
                : ReviveSelection.Choose(_controller.Board, new SystemRandomSource());

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

            SaveSystem.SaveBoardState(BoardState.Capture(
                _controller.Board,
                _pieces.Current,
                _pieces.Next,
                _controller.CurrentScore));
        }

        private void RefreshUi()
        {
            long score = _controller.CurrentScore;
            long record = _controller.Record;

            _recordText.text = "Recorde: " + ScoreFormatter.Format(record);
            _scoreText.text = "Pontos: " + ScoreFormatter.Format(score);

            // The first refresh (scene load) shows the values without pulsing; later increases pulse their own text.
            if (_hasShownScore)
            {
                if (score > _shownScore)
                {
                    _scorePulse.Pulse();
                }

                if (record > _shownRecord)
                {
                    _recordPulse.Pulse();
                }
            }

            _hasShownScore = true;
            _shownScore = score;
            _shownRecord = record;

            if (_controller.IsGameOver)
            {
                _statusText.gameObject.SetActive(false);
                _gameOverScreen.Show(_controller.CurrentScore, _controller.Record, _revives);
                return;
            }

            _statusText.text = "Toque e arraste para escolher a coluna";
        }
    }
}
