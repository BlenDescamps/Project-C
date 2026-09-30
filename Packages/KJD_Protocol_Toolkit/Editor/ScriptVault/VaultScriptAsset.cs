using UnityEngine;

namespace KJD.Editor.ScriptVault
{
    public enum ScriptCategory
    {
        Gameplay,
        Architecture,
        UI,
        Utils,
        EditorTools,
        AI,
        Audio
    }

    [CreateAssetMenu(fileName = "NewVaultScript", menuName = "KJD/Script Vault/Vault Script")]
    public class VaultScriptAsset : ScriptableObject
    {
        #region Private Fields

        [Header("Identity")]
        [SerializeField] private string _scriptName;
        [SerializeField] private ScriptCategory _category = ScriptCategory.Gameplay;
        [SerializeField] private string[] _tags;

        [Header("Documentation")]
        [TextArea(3, 6)]
        [SerializeField] private string _description;
        [TextArea(3, 10)]
        [SerializeField] private string _howToUse;

        [Header("Source Code (Inerte)")]
        [Tooltip("Glisse le script sauvegardé au format .txt ici.")]
        [SerializeField] private TextAsset _sourceTextAsset; // Remplacé MonoScript par TextAsset

        [Tooltip("Nom complet du fichier cible avec son extension .cs (ex: SpaceEventArgs.cs)")]
        [SerializeField] private string _targetFileName;

        #endregion

        #region Public API

        public string ScriptName => string.IsNullOrEmpty(_scriptName) && _sourceTextAsset != null ? _sourceTextAsset.name : _scriptName;
        public ScriptCategory Category => _category;
        public string[] Tags => _tags;
        public string Description => _description;
        public string HowToUse => _howToUse;
        public TextAsset SourceTextAsset => _sourceTextAsset;
        public string TargetFileName => string.IsNullOrEmpty(_targetFileName) && _sourceTextAsset != null ? $"{_sourceTextAsset.name}.cs" : _targetFileName;

        #endregion
    }
}