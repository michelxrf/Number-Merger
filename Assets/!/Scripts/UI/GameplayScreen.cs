using System.Collections;
using NumberMerge.Gameplay;
using NumberMerge.Managers;
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

        private BoardController _controller;
        private PieceGenerator _pieces;
        private RectTransform _boardRect;
        private bool _isAnimating;
        private bool _isPreviewing;
        private int _previewColumn = -1;

        private void Awake()
        {
            var board = new BoardModel(_boardView.ColumnCount, _boardView.VisibleRowCount);
            _controller = new BoardController(board, new CascadeResolver(), new ScoreService());
            _pieces = new PieceGenerator();
            _boardRect = _boardView.GetComponent<RectTransform>();

            _pieces.Reset(_controller.Board);
            _boardView.Initialize(_controller.Board);
            InitializeQueue();
            RefreshUi();
        }

        private void Update()
        {
            if (_controller == null || _controller.IsGameOver || _isAnimating)
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
                new Vector2(280f, previewY + 2f),
                _boardView.CellSize);
        }

        private IEnumerator AnimateMove(MoveResult move, int column)
        {
            yield return _boardView.PlayMove(move, _boardView.GetDropStartPosition(column));

            _isAnimating = false;
            RefreshUi();
        }

        private void RefreshUi()
        {
            _scoreText.text = "Pontos: " + _controller.CurrentScore + "    Recorde: " + _controller.Record;
            _statusText.text = _controller.IsGameOver
                ? "GAME OVER"
                : "Toque e arraste para escolher a coluna";
        }
    }
}
