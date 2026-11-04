using TMPro;
using UnityEngine;

namespace NumberMerge.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class PieceQueueView : MonoBehaviour
    {
        [SerializeField] private TileView _tilePrefab;
        [Tooltip("Same theme as the board, so queued pieces match the board pieces.")]
        [SerializeField] private TileTheme _theme;
        [SerializeField] private float _nextScale = 0.7f;
        [SerializeField] private float _previewDuration = 0.12f;
        [SerializeField] private float _shiftDuration = 0.3f;

        [Tooltip("\"Next\" label shown above the next piece.")]
        [SerializeField] private RectTransform _nextLabel;
        [Tooltip("Vertical gap between the top of the next piece and the label.")]
        [SerializeField] private float _nextLabelGap = 12f;
        [Tooltip("Time in seconds for the next piece to scale up from 0 to its next scale.")]
        [SerializeField] private float _nextTileScaleDuration = 0.35f;
        [SerializeField] private LeanTweenType _nextTileEase = LeanTweenType.easeInCubic;

        private TMP_Text _nextLabelText;
        private TileView _currentTile;
        private TileView _nextTile;
        private RectTransform _currentRect;
        private RectTransform _nextRect;
        private Vector2 _currentPosition;
        private Vector2 _nextPosition;
        private float _cellSize;
        private LTDescr _previewTween;
        private bool _initialized;

        public void Initialize(int currentValue, int nextValue, Vector2 currentPosition, Vector2 nextPosition, float cellSize)
        {
            _cellSize = cellSize;
            _currentPosition = currentPosition;
            _nextPosition = nextPosition;

            CreateCurrent(currentValue);
            CreateNext(nextValue);
            ShowNextLabel();
            PlayNextTileScaleUp();

            _initialized = true;
        }

        public void BeginPreview()
        {
            if (!_initialized || _currentTile == null)
            {
                return;
            }

            CancelPreviewTween();
            LeanTween.cancel(_currentRect.gameObject);
            _currentTile.ResetVisual();
            _currentRect.SetAsLastSibling();
        }

        public void MovePreviewTo(float x)
        {
            if (!_initialized || _currentRect == null)
            {
                return;
            }

            CancelPreviewTween();
            _previewTween = LeanTween.move(
                    _currentRect,
                    new Vector3(x, _currentPosition.y, 0f),
                    _previewDuration)
                .setEase(LeanTweenType.easeOutQuad);
        }

        public void CancelPreview()
        {
            if (!_initialized || _currentRect == null)
            {
                return;
            }

            CancelPreviewTween();
            _previewTween = LeanTween.move(
                    _currentRect,
                    new Vector3(_currentPosition.x, _currentPosition.y, 0f),
                    _previewDuration)
                .setEase(LeanTweenType.easeOutQuad);
        }

        public void Advance(int newNextValue)
        {
            if (!_initialized)
            {
                return;
            }

            CancelPreviewTween();

            if (_currentTile != null)
            {
                LeanTween.cancel(_currentTile.gameObject);
                _currentTile.gameObject.SetActive(false);
                Destroy(_currentTile.gameObject);
            }

            _currentTile = _nextTile;
            _currentRect = _nextRect;
            _nextTile = null;
            _nextRect = null;

            if (_currentTile != null)
            {
                LeanTween.cancel(_currentTile.gameObject);
                _currentTile.SetAlpha(1f);
                _currentTile.name = "QueueCurrent";
                _currentRect.SetAsLastSibling();

                LeanTween.move(
                        _currentRect,
                        new Vector3(_currentPosition.x, _currentPosition.y, 0f),
                        _shiftDuration)
                    .setEase(LeanTweenType.easeOutBack);

                LeanTween.scale(_currentRect, Vector3.one, _shiftDuration)
                    .setEase(LeanTweenType.easeOutBack);
            }

            _nextTile = CreateTile(newNextValue, _nextPosition, _nextScale, "QueueNext");
            if (_nextTile == null)
            {
                HideNextLabel();
                return;
            }

            _nextTile.transform.SetAsFirstSibling();
            _nextRect = _nextTile.GetComponent<RectTransform>();
            _nextTile.SetAlpha(1f);
            _nextTile.SetScale(_nextScale);

            ShowNextLabel();
            PlayNextTileScaleUp();
        }

        // Plays the ease-in scale-up on the next piece tile itself.
        private void PlayNextTileScaleUp()
        {
            if (_nextTile == null)
            {
                return;
            }

            LeanTween.cancel(_nextTile.gameObject);
            _nextTile.transform.localScale = Vector3.zero;
            LeanTween.scale(_nextTile.gameObject, Vector3.one * _nextScale, _nextTileScaleDuration)
                .setEase(_nextTileEase);
        }

        // Places the "Next" label root (text + background children) above the next piece.
        private void ShowNextLabel()
        {
            if (_nextLabel == null)
            {
                return;
            }

            float halfSize = _cellSize * _nextScale * 0.5f;
            _nextLabel.anchorMin = new Vector2(0.5f, 0.5f);
            _nextLabel.anchorMax = new Vector2(0.5f, 0.5f);
            _nextLabel.pivot = new Vector2(0.5f, 0f);
            _nextLabel.anchoredPosition = _nextPosition + new Vector2(0f, halfSize + _nextLabelGap);
            _nextLabel.SetAsLastSibling();

            if (_nextLabelText == null)
            {
                // Text lives on a child (alongside the background), so search the hierarchy.
                _nextLabelText = _nextLabel.GetComponentInChildren<TMP_Text>(true);
            }

            if (_nextLabelText != null && _theme != null)
            {
                _nextLabelText.color = _theme.TextColor;
            }

            _nextLabel.gameObject.SetActive(true);
        }

        private void HideNextLabel()
        {
            if (_nextLabel == null)
            {
                return;
            }

            _nextLabel.gameObject.SetActive(false);
        }

        private void CreateCurrent(int value)
        {
            _currentTile = CreateTile(value, _currentPosition, 1f, "QueueCurrent");
            _currentRect = _currentTile != null ? _currentTile.GetComponent<RectTransform>() : null;
            _currentTile.transform.SetAsLastSibling();
        }

        private void CreateNext(int value)
        {
            _nextTile = CreateTile(value, _nextPosition, _nextScale, "QueueNext");
            _nextRect = _nextTile != null ? _nextTile.GetComponent<RectTransform>() : null;
        }

        private TileView CreateTile(int value, Vector2 position, float scale, string tileName)
        {
            if (_tilePrefab == null)
            {
                Debug.LogError("PieceQueueView has no tile prefab assigned.", this);
                return null;
            }

            TileView tile = Instantiate(_tilePrefab, transform, false);
            tile.name = tileName;

            var tileRect = tile.GetComponent<RectTransform>();
            tileRect.anchorMin = new Vector2(0.5f, 0.5f);
            tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);
            tileRect.anchoredPosition = position;
            tileRect.sizeDelta = new Vector2(_cellSize, _cellSize);

            tile.SetValue(value, _theme);
            tile.SetScale(scale);
            tile.SetAlpha(1f);
            return tile;
        }

        private void CancelPreviewTween()
        {
            if (_previewTween != null)
            {
                LeanTween.cancel(_previewTween.id);
                _previewTween = null;
            }
        }

        private void OnDestroy()
        {
            CancelPreviewTween();

            if (_currentTile != null)
            {
                LeanTween.cancel(_currentTile.gameObject);
            }

            if (_nextTile != null)
            {
                LeanTween.cancel(_nextTile.gameObject);
            }

        }
    }
}
