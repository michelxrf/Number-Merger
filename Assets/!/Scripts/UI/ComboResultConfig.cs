using UnityEngine;

namespace NumberMerge.UI
{
    // Timing, threshold, and pitch settings for the combo result screen.
    [CreateAssetMenu(fileName = "ComboResultConfig", menuName = "Number Merge/Combo Result Config")]
    public sealed class ComboResultConfig : ScriptableObject
    {
        [Header("Timing (seconds, unscaled)")]
        [Tooltip("Fade and scale-in duration.")]
        [SerializeField, Min(0f)] private float _fadeInDuration = 0.5f;
        [Tooltip("How long the screen stays fully visible.")]
        [SerializeField, Min(0f)] private float _holdDuration = 1f;
        [Tooltip("Fade-out duration.")]
        [SerializeField, Min(0f)] private float _fadeOutDuration = 0.5f;

        [Header("Scale")]
        [Tooltip("Scale at the start of the fade-in.")]
        [SerializeField, Min(0f)] private float _startScale = 0.5f;
        [Tooltip("Scale at the end of the fade-in.")]
        [SerializeField, Min(0f)] private float _endScale = 1f;

        [Header("Threshold")]
        [Tooltip("The screen is only shown when a move has at least this many combos.")]
        [SerializeField, Min(0)] private int _minCombosToShow = 2;

        [Header("Pitch")]
        [Tooltip("First combo count that raises the pitch. Counts below this play at 1x.")]
        [SerializeField, Min(1)] private int _pitchStartCount = 3;
        [Tooltip("Pitch added per combo from Pitch Start Count onward. 0.1 = 1.1x, 1.2x, ...")]
        [SerializeField, Min(0f)] private float _chainPitchGain = 0.1f;
        [Tooltip("Upper limit for the combo sound's pitch.")]
        [SerializeField, Min(1f)] private float _maxPitch = 2f;

        public float FadeInDuration => _fadeInDuration;
        public float HoldDuration => _holdDuration;
        public float FadeOutDuration => _fadeOutDuration;
        public float StartScale => _startScale;
        public float EndScale => _endScale;
        public int MinCombosToShow => _minCombosToShow;
        public int PitchStartCount => _pitchStartCount;
        public float ChainPitchGain => _chainPitchGain;
        public float MaxPitch => _maxPitch;

        // 1x below PitchStartCount; each combo from there on adds ChainPitchGain, capped at MaxPitch.
        public float GetComboPitch(int comboCount)
        {
            int steps = Mathf.Max(0, comboCount - _pitchStartCount + 1);
            return Mathf.Min(1f + steps * _chainPitchGain, _maxPitch);
        }
    }
}
