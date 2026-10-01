using System;
using System.Collections;
using UnityEngine;

namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Gère les animations de dissolution et de matérialisation (apparition / reverse dissolve) sur le shader de l'objet.
    /// </summary>
    public class ApparitionEffect : MonoBehaviour
    {
        #region Publics

        public bool IsAppearing => _isAppearing;
        public bool IsDissolving => _isDissolving;

        #endregion

        #region Unity API

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            _dissolvePropId = Shader.PropertyToID(_dissolvePropertyName);

            // Par défaut l'objet est totalement visible (dissolve = 0)
            SetDissolveAmount(0f);
        }

        #endregion

        #region Main API

        /// <summary>
        /// Déclenche la dissolution de l'objet au loin (de visible à invisible avec liseré incandescent).
        /// </summary>
        /// <param name="duration">Durée de l'effet en secondes.</param>
        /// <param name="onComplete">Callback optionnel appelé une fois la dissolution terminée.</param>
        public void PlayDissolve(float duration = -1f, Action onComplete = null)
        {
            if (duration <= 0f) duration = _defaultDissolveDuration;

            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null) return;

            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }

            _currentCoroutine = StartCoroutine(AnimateDissolveRoutine(duration, onComplete));
        }

        /// <summary>
        /// Déclenche l'animation d'apparition magique (l'objet se reforme dans les mains du joueur).
        /// </summary>
        /// <param name="duration">Durée de l'effet en secondes.</param>
        /// <param name="onComplete">Callback optionnel appelé une fois la matérialisation terminée.</param>
        public void PlayApparition(float duration = -1f, Action onComplete = null)
        {
            if (duration <= 0f) duration = _defaultApparitionDuration;

            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null) return;

            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }

            _currentCoroutine = StartCoroutine(AnimateApparitionRoutine(duration, onComplete));
        }

        private IEnumerator AnimateDissolveRoutine(float duration, Action onComplete)
        {
            _isDissolving = true;
            _isAppearing = false;
            float elapsed = 0f;

            // Démarre à visible (0.0)
            SetDissolveAmount(0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Progression douce et incandescente
                float curveValue = Mathf.SmoothStep(0f, 1f, progress);
                SetDissolveAmount(curveValue);

                yield return null;
            }

            // Garanti totalement dissout / invisible
            SetDissolveAmount(1f);
            _isDissolving = false;
            _currentCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateApparitionRoutine(float duration, Action onComplete)
        {
            _isAppearing = true;
            _isDissolving = false;
            float elapsed = 0f;

            // Démarre totalement dissout / invisible (1.0)
            SetDissolveAmount(1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Progression majestueuse et perceptible (SmoothStep inversé)
                float curveValue = Mathf.SmoothStep(1f, 0f, progress);
                SetDissolveAmount(curveValue);

                yield return null;
            }

            // Garanti pleinement matérialisé
            SetDissolveAmount(0f);
            _isAppearing = false;
            _currentCoroutine = null;
            onComplete?.Invoke();
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

        [Tooltip("Durée par défaut de l'animation de dissolution (disparition)")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _defaultDissolveDuration = 0.5f;

        [Tooltip("Durée par défaut de l'animation d'apparition (matérialisation)")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _defaultApparitionDuration = 0.85f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private int _dissolvePropId;
        private Coroutine _currentCoroutine;
        private bool _isAppearing;
        private bool _isDissolving;

        #endregion
    }
}
