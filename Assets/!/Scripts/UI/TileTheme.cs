using NumberMerge.Gameplay;
using UnityEngine;

namespace NumberMerge.UI
{
    // Visual theme for the board: the sprite of each empty slot, the sprite of each piece value,
    // and the color of all game texts.
    [CreateAssetMenu(fileName = "TileTheme", menuName = "Number Merge/Tile Theme")]
    public sealed class TileTheme : ScriptableObject
    {
        [Header("Board")]
        [Tooltip("Sprite of every empty slot on the board.")]
        [SerializeField] private Sprite _slotSprite;

        [Header("Pieces")]
        [Tooltip("Color of all texts in the game: piece values, score, status, and game over screen.")]
        [SerializeField] private Color _textColor = Color.white;
        [Tooltip("Sprite used by values that are not covered by Piece Sprites.")]
        [SerializeField] private Sprite _fallbackPieceSprite;
        [Tooltip("Sprites in increasing value order, starting at 2: index 0 = 2, 1 = 4, 2 = 8, 3 = 16, and so on.")]
        [SerializeField] private Sprite[] _pieceSprites;

        [Header("Bad Tile")]
        [Tooltip("Sprite for bad tiles. Not used yet; reserved for later gameplay.")]
        [SerializeField] private Sprite _badTileSprite;

        [Header("Background")]
        [Tooltip("Candidate sprites for the gameplay background. One is picked at random when the scene loads.")]
        [SerializeField] private Sprite[] _backgroundSprites;

        public Sprite SlotSprite => _slotSprite;
        public Color TextColor => _textColor;
        public Sprite BadTileSprite => _badTileSprite;

        // Returns a random non-null background sprite, or null if none are assigned.
        public Sprite GetRandomBackgroundSprite()
        {
            if (_backgroundSprites == null || _backgroundSprites.Length == 0)
            {
                return null;
            }

            int validCount = 0;
            foreach (Sprite sprite in _backgroundSprites)
            {
                if (sprite != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            int pick = Random.Range(0, validCount);
            foreach (Sprite sprite in _backgroundSprites)
            {
                if (sprite == null)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return sprite;
                }

                pick--;
            }

            return null;
        }

        public Sprite GetPieceSprite(int value)
        {
            // Piece values are powers of two starting at MinimumValue, so the index is the number of
            // doublings from MinimumValue to value. Anything else uses the fallback.
            long current = SpawnTable.MinimumValue;
            int index = 0;

            while (current < value)
            {
                current *= 2;
                index++;
            }

            if (current != value || _pieceSprites == null || index >= _pieceSprites.Length)
            {
                return _fallbackPieceSprite;
            }

            return _pieceSprites[index] != null ? _pieceSprites[index] : _fallbackPieceSprite;
        }
    }
}
