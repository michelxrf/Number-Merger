using System;
using TMPro;
using UnityEngine;

namespace NumberMerge.UI
{
    // A "+N" text that flies from its start position into the score text, fades out, then destroys itself.
    // Spawned per merge by GameplayScreen. Uses scaled time, so it pauses with the game.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScoreFloater : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        public void Play(long points, Color color, Vector3 from, Vector3 to, ScoreFloatConfig config, Action onArrive)
        {
            if (_label != null)
            {
                _label.text = "+" + ScoreFormatter.Format(points);
                _label.color = color;
            }

            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            transform.position = from;
            transform.localScale = Vector3.one * config.StartScale;

            float duration = config.Duration;

            LeanTween.move(gameObject, to, duration).setEase(config.MoveEase);
            LeanTween.scale(gameObject, Vector3.one * config.EndScale, duration).setEase(config.MoveEase);

            // Fades out while it approaches, so it visibly merges into the score.
            LeanTween.alphaCanvas(canvasGroup, 0f, duration).setEaseInQuad();

            LeanTween.delayedCall(gameObject, duration, () =>
            {
                try
                {
                    onArrive?.Invoke();
                }
                finally
                {
                    // Always removed, even if the arrival callback throws.
                    Destroy(gameObject);
                }
            });
        }
    }
}
