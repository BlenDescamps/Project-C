using System.Collections;
using UnityEngine;

namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Gère l'animation de matérialisation (apparition / reverse dissolve) sur le shader de l'objet.
    /// </summary>
    public class ApparitionEffect : MonoBehaviour
    {
        #region Publics

        public bool IsAppearing => _isAppearing;

        #endregion

        #region Unity API

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            _dissolvePropId = Shader.PropertyToID(_dissolvePropertyName);

            // Par défaut l'objet est visible (dissolve = 0)
            SetDissolveAmount(0f);
        }

        #endregion

        #region Main API

        /// <summary>
        /// Déclenche l'animation d'apparition magique (l'objet se reforme dans les mains du joueur).
        /// </summary>
        /// <param name="duration">Durée de l'effet en secondes (si <= 0, utilise _defaultApparitionDuration).</param>
        public void PlayApparition(float duration = -1f)
        {
            if (duration <= 0f) duration = _defaultApparitionDuration;

            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null) return;

            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }

            _currentCoroutine = StartCoroutine(AnimateApparitionRoutine(duration));
        }

        private IEnumerator AnimateApparitionRoutine(float duration)
        {
            _isAppearing = true;
            float elapsed = 0f;

            // Commence totalement invisible/dissout (1.0)
            SetDissolveAmount(1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Courbe d'apparition douce (Ease Out Quad pour un pop incisif)
                float curveValue = 1f - (progress * (2f - progress)); 
                SetDissolveAmount(curveValue);

                yield return null;
            }

            // Garanti pleinement matérialisé
            SetDissolveAmount(0f);
            _isAppearing = false;
            _currentCoroutine = null;
        }

        public void SetDissolveAmount(float amount)
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_dissolvePropId, amount);
            _renderer.SetPropertyBlock(_propBlock);
        }

        #endregion

        #region Private and Protected

        [Header("--- PARAMÈTRES DU SHADER ---")]
        [Tooltip("Nom de la propriété de dissolution dans le shader")]
        [SerializeField] private string _dissolvePropertyName = "_DissolveAmount";

        [Tooltip("Durée par défaut de l'animation d'apparition")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _defaultApparitionDuration = 0.45f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private int _dissolvePropId;
        private Coroutine _currentCoroutine;
        private bool _isAppearing;

        #endregion
    }
}
