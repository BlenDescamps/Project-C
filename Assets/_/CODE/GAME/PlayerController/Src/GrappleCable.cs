using UnityEngine;

namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Gère le rendu et la simulation physique de la corde du grappin.
    /// S'affaisse naturellement par gravité quand elle est détendue et se raidit en ligne droite quand elle est tendue.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class GrappleCable : MonoBehaviour
    {
        #region Publics

        public bool IsVisible => _lineRenderer != null && _lineRenderer.enabled;
        public bool IsTaut => _isTaut;
        public float CurrentSlack => _currentSlack;

        #endregion

        #region Unity API

        private void Awake()
        {
            EnsureLineRenderer();
        }

        #endregion

        #region Main API

        public void EnableCable()
        {
            EnsureLineRenderer();
            _lineRenderer.enabled = true;
        }

        public void DisableCable()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Met à jour la forme de la corde selon la distance réelle et la longueur déployée.
        /// </summary>
        /// <param name="startPos">Position de sortie de la corde (bouche de l'arbalète).</param>
        /// <param name="endPos">Position d'ancrage du grappin.</param>
        /// <param name="ropeLength">Longueur physique déployée de la corde.</param>
        /// <param name="isReeling">Vrai si le treuil est en train de ré-enrouler la corde.</param>
        public void UpdateCable(Vector3 startPos, Vector3 endPos, float ropeLength, bool isReeling = false)
        {
            if (_lineRenderer == null || !_lineRenderer.enabled) return;

            float realDistance = Vector3.Distance(startPos, endPos);
            _currentSlack = Mathf.Max(0f, ropeLength - realDistance);
            _isTaut = realDistance >= (ropeLength - 0.05f);

            int count = _segmentCount;
            if (_lineRenderer.positionCount != count)
            {
                _lineRenderer.positionCount = count;
            }

            // 1. Corde TENDUE : ligne droite avec micro-vibration sous tension
            if (_isTaut || _currentSlack < 0.02f)
            {
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / (count - 1);
                    Vector3 pos = Vector3.Lerp(startPos, endPos, t);

                    // Micro-tremblement mécanique si en train de treuiller ou sous forte tension
                    if (isReeling && i > 0 && i < count - 1)
                    {
                        float vibration = Mathf.Sin((Time.time * 40f) + (i * 2f)) * 0.015f;
                        pos += Vector3.up * vibration;
                    }

                    _lineRenderer.SetPosition(i, pos);
                }
            }
            // 2. Corde DÉTENDUE : courbe parabolique (Bézier quadratique tombant vers le bas)
            else
            {
                float sagAmount = Mathf.Min(_currentSlack * _sagMultiplier, _maxSagDepth);
                Vector3 midPoint = ((startPos + endPos) * 0.5f) + (Vector3.down * sagAmount);

                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / (count - 1);
                    float oneMinusT = 1f - t;

                    // Formule de Bézier quadratique : B(t) = (1-t)² * P0 + 2(1-t)t * P1 + t² * P2
                    Vector3 pos = (oneMinusT * oneMinusT * startPos) + (2f * oneMinusT * t * midPoint) + (t * t * endPos);
                    _lineRenderer.SetPosition(i, pos);
                }
            }
        }

        #endregion

        #region Tools and Utilities

        private void EnsureLineRenderer()
        {
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }

            _lineRenderer.useWorldSpace = true;
            _lineRenderer.startWidth = _cableWidth;
            _lineRenderer.endWidth = _cableWidth * 0.85f;
            _lineRenderer.numCapVertices = 4;
            _lineRenderer.numCornerVertices = 4;

            if (_lineRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    Material mat = new Material(shader)
                    {
                        color = _cableColor
                    };
                    _lineRenderer.sharedMaterial = mat;
                }
            }
        }

        #endregion

        #region Private and Protected

        [Header("--- PARAMÈTRES DU CÂBLE ---")]
        [Tooltip("Nombre de segments pour la fluidité de la corde")]
        [Range(6, 32)]
        [SerializeField] private int _segmentCount = 16;

        [Tooltip("Épaisseur de la corde en mètres")]
        [Range(0.005f, 0.08f)]
        [SerializeField] private float _cableWidth = 0.022f;

        [Tooltip("Multiplicateur de flèche / affaissement vers le bas quand la corde a du mou")]
        [Range(0.2f, 2.5f)]
        [SerializeField] private float _sagMultiplier = 1.1f;

        [Tooltip("Profondeur maximale d'affaissement de la corde")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _maxSagDepth = 4.5f;

        [Tooltip("Couleur de la corde / câble")]
        [SerializeField] private Color _cableColor = new Color(0.28f, 0.22f, 0.16f, 1f);

        private LineRenderer _lineRenderer;
        private bool _isTaut;
        private float _currentSlack;

        #endregion
    }
}
