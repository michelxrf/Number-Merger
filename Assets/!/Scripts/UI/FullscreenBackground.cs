using UnityEngine;

namespace NumberMerge.UI
{
    // Scales a SpriteRenderer (2D object) so it covers the whole camera view ("cover" fit),
    // keeping its aspect ratio. Works for any screen format because it reads the camera every frame.
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FullscreenBackground : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            Fit();
        }

        private void Fit()
        {
            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam == null || _renderer == null || _renderer.sprite == null)
            {
                return;
            }

            Vector3 forward = cam.transform.forward;
            float distance = Mathf.Max(0.01f, Vector3.Dot(transform.position - cam.transform.position, forward));

            float viewHeight = cam.orthographic
                ? cam.orthographicSize * 2f
                : 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewWidth = viewHeight * cam.aspect;

            transform.position = cam.transform.position + forward * distance;

            Vector2 spriteSize = _renderer.sprite.bounds.size;
            float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
