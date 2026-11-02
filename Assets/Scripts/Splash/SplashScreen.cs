using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class SplashScreen : MonoBehaviour
{
    [Header("Logo")]
    [Tooltip("CanvasGroup on the parent of the logo and text. Its alpha is animated for the fades.")]
    [SerializeField] private CanvasGroup logo;

    [Header("Timing (seconds)")]
    [SerializeField, Min(0f)] private float delayBeforeFadeIn = 0.5f;
    [SerializeField, Min(0f)] private float fadeInDuration = 1f;
    [SerializeField, Min(0f)] private float stayDuration = 2f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 1f;
    [SerializeField, Min(0f)] private float delayAfterFadeOut = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfx;

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (logo != null)
        {
            logo.alpha = 0f;
        }
    }

    private IEnumerator Start()
    {
        if (sfx != null)
        {
            audioSource.PlayOneShot(sfx);
        }

        yield return new WaitForSecondsRealtime(delayBeforeFadeIn);
        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSecondsRealtime(stayDuration);
        yield return Fade(1f, 0f, fadeOutDuration);
        yield return new WaitForSecondsRealtime(delayAfterFadeOut);

        SceneManager.LoadScene(gameplaySceneName);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (logo == null)
        {
            yield return new WaitForSecondsRealtime(duration);
            yield break;
        }

        float elapsed = 0f;
        logo.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            logo.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        logo.alpha = to;
    }
}
