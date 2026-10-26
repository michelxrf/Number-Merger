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
                return;
            }

            _nextTile.transform.SetAsFirstSibling();
            _nextRect = _nextTile.GetComponent<RectTransform>();
            _nextTile.SetAlpha(1f);
            _nextTile.SetScale(_nextScale);
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
