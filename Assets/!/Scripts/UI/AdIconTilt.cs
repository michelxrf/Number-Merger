using System.Collections;
using UnityEngine;

namespace NumberMerge.UI
{
    // Periodically tilts and slightly scales the object up and down using LeanTween.
    // Each pulse goes from the resting transform to the peak and back, then waits for the interval.
    public sealed class AdIconTilt : MonoBehaviour
    {
        [Tooltip("Maximum tilt in degrees away from the resting rotation.")]
        [SerializeField] private float _angle = 10f;

        [Tooltip("Scale multiplier reached at the peak of the pulse (1 = no scaling).")]
        [SerializeField] private float _peakScale = 1.08f;

        [Tooltip("LeanTween easing type applied to the tilt and scale.")]
        [SerializeField] private LeanTweenType _easeType = LeanTweenType.easeInOutSine;

        [Tooltip("Total time in seconds for one full tilt/scale pulse (out and back).")]
        [SerializeField] private float _duration = 0.6f;

        [Tooltip("Seconds to wait between pulses.")]
        [SerializeField] private float _interval = 3f;

        private float _restZ;
        private Vector3 _restScale;
        private Coroutine _loop;

        private void OnEnable()
        {
            _restZ = transform.localEulerAngles.z;
            _restScale = transform.localScale;
            _loop = StartCoroutine(TiltLoop());
        }

        private void OnDisable()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }

            LeanTween.cancel(gameObject);
            SetRestTransform();
        }

        private IEnumerator TiltLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_interval);
                Pulse();
                yield return new WaitForSeconds(_duration);
            }
        }

        private void Pulse()
        {
            // Make sure no previous pulse is still running and start from rest.
            LeanTween.cancel(gameObject);
            SetRestTransform();

            // Ping-pong with one repeat = out leg + back leg, each half of the duration.
            LeanTween.rotateZ(gameObject, _restZ + _angle, _duration * 0.5f)
                .setEase(_easeType)
                .setLoopPingPong(1);

            LeanTween.scale(gameObject, _restScale * _peakScale, _duration * 0.5f)
                .setEase(_easeType)
                .setLoopPingPong(1);
        }

        private void SetRestTransform()
        {
            Vector3 euler = transform.localEulerAngles;
            euler.z = _restZ;
            transform.localEulerAngles = euler;
            transform.localScale = _restScale;
        }
    }
}
