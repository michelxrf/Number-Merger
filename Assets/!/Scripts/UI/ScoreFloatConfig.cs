using UnityEngine;

namespace NumberMerge.UI
{
    // Timing and look of the "+N" floater that flies from under the score text into it.
    [CreateAssetMenu(fileName = "ScoreFloatConfig", menuName = "Number Merge/Score Float Config")]
    public sealed class ScoreFloatConfig : ScriptableObject
    {
        [Tooltip("Distance below the center of the spawn anchor where the floater starts, in canvas units. The floater rises from there to the anchor's center.")]
        [SerializeField, Min(0f)] private float _spawnOffset = 40f;

        [Tooltip("Time in seconds for the floater to reach the score text (scaled time, so it pauses with the game).")]
        [SerializeField, Min(0.01f)] private float _duration = 0.45f;

        [Tooltip("LeanTween ease of the movement into the score text.")]
        [SerializeField] private LeanTweenType _moveEase = LeanTweenType.easeInQuad;

        [Tooltip("Scale at the start of the flight.")]
        [SerializeField, Min(0f)] private float _startScale = 1f;

        [Tooltip("Scale when the floater reaches the score text. The floater fades out during the flight.")]
        [SerializeField, Min(0f)] private float _endScale = 0.6f;

        public float SpawnOffset => _spawnOffset;
        public float Duration => _duration;
        public LeanTweenType MoveEase => _moveEase;
        public float StartScale => _startScale;
        public float EndScale => _endScale;
    }
}
