using System.Collections;
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
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private GameOverScreen _gameOverScreen;
        [SerializeField] private ComboResultScreen _comboResultScreen;
        [Tooltip("Balance asset for piece generation. If empty, the default settings are used.")]
        [SerializeField] private PieceGenerationConfig _pieceGenerationConfig;
        [Tooltip("Theme whose Background Sprites are picked from at random on load.")]
        [SerializeField] private TileTheme _theme;
        [Tooltip("SpriteRenderer of the BackgroundSprite object that displays the chosen background.")]
        [SerializeField] private SpriteRenderer _backgroundRenderer;

        private BoardController _controller;
        private PieceGenerator _pieces;
        private RectTransform _boardRect;
        private bool _isAnimating;
        private bool _isPreviewing;
        private int _previewColumn = -1;

        private void Awake()
        {
            _boardRect = _boardView.GetComponent<RectTransform>();

            ApplyRandomBackground();
            ApplyTextColor();
            LoadOrStartGame();

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
                _controller = new BoardController(restored, new CascadeResolver(), new ScoreService(record, score));

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
            _controller = new BoardController(board, new CascadeResolver(), new ScoreService(record));
            _pieces = CreatePieceGenerator();
            _pieces.Reset(board);
        }

        private PieceGenerator CreatePieceGenerator()
        {
            if (_pieceGenerationConfig == null)
            {
                return new PieceGenerator();
            }

            return new PieceGenerator(new SystemRandomSource(), _pieceGenerationConfig.Settings);
        }

        private void Update()
        {
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

            // Game over is final: remove the saved board as soon as it happens, not after the animation.
            if (_controller.IsGameOver)
            {
                SaveSystem.DeleteBoardState();
            }

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

        // Called right before the player regains control. Record is always saved.
        // The board is saved in its playable state, or deleted on game over.
        private void SaveProgress()
        {
            SaveSystem.SaveRecord(_controller.Record);

            if (_controller.IsGameOver)
            {
                SaveSystem.DeleteBoardState();
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
            _scoreText.text = "Pontos: " + _controller.CurrentScore + "\nRecorde: " + _controller.Record;

            if (_controller.IsGameOver)
            {
                _statusText.gameObject.SetActive(false);
                _gameOverScreen.Show(_controller.CurrentScore, _controller.Record);
                return;
            }

            _statusText.text = "Toque e arraste para escolher a coluna";
        }
    }
}
