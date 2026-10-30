using UnityEngine;

namespace NumberMerge.UI
{
    // Plays a short scale pulse on a score text: grows to peakScale and returns to 1 within duration seconds.
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScorePulse : MonoBehaviour
    {
        [Tooltip("Scale reached at the peak of the pulse.")]
        [SerializeField] private float _peakScale = 1.1f;

        [Tooltip("Total time in seconds for the whole pulse (up and back to 1).")]
        [SerializeField] private float _duration = 0.2f;

        public void Pulse()
        {
            // Restart cleanly if a pulse is still running, so the scale never drifts away from 1.
            LeanTween.cancel(gameObject);
            transform.localScale = Vector3.one;

            // Ping-pong with one repeat = up leg + down leg, each half of the duration.
            LeanTween.scale(gameObject, Vector3.one * _peakScale, _duration * 0.5f)
                .setEaseOutQuad()
                .setLoopPingPong(1);
        }

        private void OnDisable()
        {
            LeanTween.cancel(gameObject);
            transform.localScale = Vector3.one;
        }
    }
}
