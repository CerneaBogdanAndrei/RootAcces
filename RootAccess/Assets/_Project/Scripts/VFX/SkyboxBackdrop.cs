using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>
    /// Skybox for 2D-renderer scenes. A sky camera (Universal renderer, skybox clear, culls nothing) renders the
    /// scene's Skybox material into a small RenderTexture; this component stretches that texture over a quad
    /// behind every sorting layer and pans the sky camera with the gameplay camera for a far-parallax feel.
    /// </summary>
    public sealed class SkyboxBackdrop : MonoBehaviour
    {
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Camera skyCamera;
        [SerializeField] private Renderer backdrop;
        [SerializeField] private Vector2Int resolution = new(640, 360);
        [SerializeField] private float degreesPerUnitX = 0.12f;
        [SerializeField] private float degreesPerUnitY = 0.25f;
        [SerializeField] private float basePitch = -4f;
        [SerializeField] private float distance = 40f;

        private RenderTexture _target;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            if (gameplayCamera == null || skyCamera == null || backdrop == null)
            {
                Debug.LogError("[SkyboxBackdrop] Gameplay camera, sky camera and backdrop renderer are required.", this);
                enabled = false;
                return;
            }

            _target = new RenderTexture(resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32) { name = "SkyboxBackdrop" };
            skyCamera.targetTexture = _target;
            _block = new MaterialPropertyBlock();
            backdrop.GetPropertyBlock(_block);
            _block.SetTexture(MainTexId, _target);
            backdrop.SetPropertyBlock(_block);
        }

        private void OnDestroy()
        {
            if (skyCamera != null) skyCamera.targetTexture = null;
            if (_target != null) _target.Release();
        }

        private void LateUpdate()
        {
            Transform cam = gameplayCamera.transform;
            float height = gameplayCamera.orthographicSize * 2f;
            float width = height * gameplayCamera.aspect;
            Transform quad = backdrop.transform;
            quad.position = cam.position + cam.forward * distance;
            quad.rotation = cam.rotation;
            quad.localScale = new Vector3(width, height, 1f);

            Vector3 p = cam.position;
            skyCamera.transform.rotation = Quaternion.Euler(basePitch - p.y * degreesPerUnitY, p.x * degreesPerUnitX, 0f);
        }
    }
}
