using System;
using UnityEngine;

namespace NumberMerge.UI
{
    // Fades an overlay in and out with LeanTween: alpha on the overlay's CanvasGroup,
    // scale on the panel inside it (the overlay itself is not scaled).
    // Uses unscaled time so it still runs while Time.timeScale is 0 (paused).
    public static class OverlayFade
    {
        public const float Duration = 0.2f;
        private const float StartScale = 0.9f;

        public static void FadeIn(GameObject overlay, Transform panel)
        {
            CanvasGroup group = GetOrAddCanvasGroup(overlay);

            // Cancel any fade-out in progress so its completion cannot hide the overlay again.
            LeanTween.cancel(overlay);
            LeanTween.cancel(panel.gameObject);

            overlay.SetActive(true);
            group.alpha = 0f;
            panel.localScale = Vector3.one * StartScale;

            LeanTween.alphaCanvas(group, 1f, Duration)
                .setIgnoreTimeScale(true)
                .setEaseOutQuad();
            LeanTween.scale(panel.gameObject, Vector3.one, Duration)
                .setIgnoreTimeScale(true)
                .setEaseOutQuad();
        }

        public static void FadeOut(GameObject overlay, Transform panel, Action onComplete = null)
        {
            CanvasGroup group = GetOrAddCanvasGroup(overlay);

            LeanTween.cancel(overlay);
            LeanTween.cancel(panel.gameObject);

            LeanTween.alphaCanvas(group, 0f, Duration)
                .setIgnoreTimeScale(true)
                .setEaseInQuad()
                .setOnComplete(() =>
                {
                    overlay.SetActive(false);
                    onComplete?.Invoke();
                });
            LeanTween.scale(panel.gameObject, Vector3.one * StartScale, Duration)
                .setIgnoreTimeScale(true)
                .setEaseInQuad();
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject overlay)
        {
            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = overlay.AddComponent<CanvasGroup>();
            }
            return group;
        }
    }
}
