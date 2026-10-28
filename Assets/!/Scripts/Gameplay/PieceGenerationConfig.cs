using UnityEngine;

namespace NumberMerge.Gameplay
{
    // Balance asset for the piece generator. Create it with Assets > Create > Number Merge > Piece Generation Config.
    // GameplayScreen passes Settings to the PieceGenerator, so editing this asset changes the game.
    [CreateAssetMenu(fileName = "PieceGenerationConfig", menuName = "Number Merge/Piece Generation Config")]
    public sealed class PieceGenerationConfig : ScriptableObject
    {
        [SerializeField] private PieceGenerationSettings _settings = new PieceGenerationSettings();

        public PieceGenerationSettings Settings => _settings;

        private void OnValidate()
        {
            // Report invalid values as soon as they are edited, instead of at the start of a game.
            if (_settings == null)
            {
                _settings = new PieceGenerationSettings();
                return;
            }

            try
            {
                _settings.Validate();
            }
            catch (System.InvalidOperationException exception)
            {
                Debug.LogError("[PieceGenerationConfig] " + name + ": " + exception.Message, this);
            }
        }
    }
}
