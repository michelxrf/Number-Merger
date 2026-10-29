using UnityEngine;

namespace NumberMerge.Audio
{
    // Every moment that plays a sound effect. Add a value here and a matching field in SfxLibrary.
    public enum SfxId
    {
        ButtonPress,
        TileInsert,
        Combination,
        GameStart,
        GameOver
    }

    // Holds the clips for each sound moment. Each moment can have several clips; one is picked at random
    // on every play. Empty entries play nothing, so a moment can be left unassigned.
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "Number Merge/Sfx Library")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [Header("UI")]
        [Tooltip("Played on every button press. Routed to the UI channel.")]
        [SerializeField] private AudioClip[] _buttonPress;

        [Header("Gameplay")]
        [Tooltip("Played when a tile is inserted into the board, at the moment it lands.")]
        [SerializeField] private AudioClip[] _tileInsert;
        [Tooltip("Played for each merge. In a chain, each next merge is pitched up by Chain Pitch Step.")]
        [SerializeField] private AudioClip[] _combination;
        [Tooltip("Pitch added per combination in the same chain. 0.1 = 1x, 1.1x, 1.2x, ...")]
        [SerializeField] private float _chainPitchStep = 0.1f;

        public float ChainPitchStep => _chainPitchStep;

        [Header("Game State")]
        [Tooltip("Played every time the gameplay scene loads (new game, restart, or restored save).")]
        [SerializeField] private AudioClip[] _gameStart;
        [Tooltip("Played when the game over screen appears.")]
        [SerializeField] private AudioClip[] _gameOver;

        // Returns one random non-null clip for the moment, or null if none are assigned.
        public AudioClip GetRandomClip(SfxId id)
        {
            AudioClip[] clips = GetClips(id);
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int validCount = 0;
            foreach (AudioClip clip in clips)
            {
                if (clip != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            int pick = Random.Range(0, validCount);
            foreach (AudioClip clip in clips)
            {
                if (clip == null)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return clip;
                }

                pick--;
            }

            return null;
        }

        private AudioClip[] GetClips(SfxId id)
        {
            switch (id)
            {
                case SfxId.ButtonPress:
                    return _buttonPress;
                case SfxId.TileInsert:
                    return _tileInsert;
                case SfxId.Combination:
                    return _combination;
                case SfxId.GameStart:
                    return _gameStart;
                case SfxId.GameOver:
                    return _gameOver;
                default:
                    return null;
            }
        }
    }
}
