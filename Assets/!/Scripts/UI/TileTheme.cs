using NumberMerge.Gameplay;
using UnityEngine;

namespace NumberMerge.UI
{
    // Visual theme for the board: the sprite of each empty slot, the sprite of each piece value,
    // and the color of the piece value text.
    [CreateAssetMenu(fileName = "TileTheme", menuName = "Number Merge/Tile Theme")]
    public sealed class TileTheme : ScriptableObject
    {
        [Header("Board")]
        [Tooltip("Sprite of every empty slot on the board.")]
        [SerializeField] private Sprite _slotSprite;

        [Header("Pieces")]
        [Tooltip("Color of the value text on every piece.")]
        [SerializeField] private Color _textColor = Color.white;
        [Tooltip("Sprite used by values that are not covered by Piece Sprites.")]
        [SerializeField] private Sprite _fallbackPieceSprite;
        [Tooltip("Sprites in increasing value order, starting at 2: index 0 = 2, 1 = 4, 2 = 8, 3 = 16, and so on.")]
        [SerializeField] private Sprite[] _pieceSprites;

        [Header("Bad Tile")]
        [Tooltip("Sprite for bad tiles. Not used yet; reserved for later gameplay.")]
        [SerializeField] private Sprite _badTileSprite;

        public Sprite SlotSprite => _slotSprite;
        public Color TextColor => _textColor;
        public Sprite BadTileSprite => _badTileSprite;

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
