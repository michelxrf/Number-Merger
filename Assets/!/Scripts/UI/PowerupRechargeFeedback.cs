using NumberMerge.Audio;
using UnityEngine;

namespace NumberMerge.UI
{
    // Feedback on one power-up button when its uses are recharged: a petal burst, a quick scale punch and a sound.
    // Only the button this component is on reacts, so each power-up needs its own instance.
    [RequireComponent(typeof(RectTransform))]
    public sealed class PowerupRechargeFeedback : MonoBehaviour
    {
        [Tooltip("Same petal burst prefab the board uses for destroyed tiles (UIParticle root with a ParticleSystem child).")]
        [SerializeField] private GameObject _petalBurstPrefab;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        // Plays the petals, the sound, and punches the button's scale up to punchScale and back to 1.
        // emitterScale is the emitter's size as a fraction of the button's smaller side, like the tiles' PetalEmitterScale.
        public void Play(float emitterScale, float punchScale, float punchDuration)
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            SpawnPetalBurst(emitterScale);
            AudioManager.Instance?.Play(SfxId.PowerupRecharge);
            Punch(punchScale, punchDuration);
        }

        // Mirrors BoardView.SpawnPetalBurst, except the emitter is a filled circle instead of a box.
        private void SpawnPetalBurst(float emitterScale)
        {
            if (_petalBurstPrefab == null)
            {
                return;
            }

            // Spawned as a sibling placed just before the button, so the petals draw behind its image.
            // Centered on the button's rect center, whatever the button's pivot is.
            Transform parent = transform.parent;
            GameObject instance = Instantiate(_petalBurstPrefab, parent, false);
            instance.transform.SetSiblingIndex(transform.GetSiblingIndex());

            var instanceRect = instance.GetComponent<RectTransform>();
            if (instanceRect != null)
            {
                instanceRect.anchorMin = new Vector2(0.5f, 0.5f);
                instanceRect.anchorMax = new Vector2(0.5f, 0.5f);
                instanceRect.pivot = new Vector2(0.5f, 0.5f);
                instanceRect.position = _rectTransform.TransformPoint(_rectTransform.rect.center);
            }

            ParticleSystem burst = instance.GetComponentInChildren<ParticleSystem>(true);
            if (burst == null)
            {
                Destroy(instance);
                return;
            }

            // Filled circle covering the emitter area, so petals start from the whole disc.
            float diameter = Mathf.Min(_rectTransform.rect.width, _rectTransform.rect.height) * emitterScale;
            ParticleSystem.ShapeModule shape = burst.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = diameter * 0.5f;
            shape.radiusThickness = 1f;

            instance.SetActive(true);
            burst.Play(true);

            ParticleSystem.MainModule main = burst.main;
            float lifetime = main.duration + main.startLifetime.constantMax;
            Destroy(instance, lifetime + 0.1f);
        }

        // Scales the button up to punchScale and back to 1 over duration (half up, half back), unscaled time.
        private void Punch(float punchScale, float duration)
        {
            if (duration <= 0f || punchScale <= 0f)
            {
                return;
            }

            GameObject go = gameObject;
            float half = duration * 0.5f;

            LeanTween.cancel(go);
            _rectTransform.localScale = Vector3.one;
            LeanTween.scale(go, Vector3.one * punchScale, half)
                .setEase(LeanTweenType.easeOutQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    if (go != null)
                    {
                        LeanTween.scale(go, Vector3.one, half)
                            .setEase(LeanTweenType.easeInQuad)
                            .setIgnoreTimeScale(true);
                    }
                });
        }
    }
}
