using System;
using System.Collections;
using System.Collections.Generic;
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
