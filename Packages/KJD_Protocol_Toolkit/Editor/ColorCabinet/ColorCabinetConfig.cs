using System.Collections.Generic;
using UnityEngine;

namespace KJD.Editor.ColorCabinet
{
    [System.Serializable]
    public struct ColorRule
    {
        [Tooltip("Le mot-clé à repérer (ex: '_', 'Manager', 'UI', 'Art')")]
        public string m_keyword;

        [Tooltip("Couleur du texte")]
        public Color m_textColor;

        [Tooltip("Couleur du fond (laisser transparent si non désiré)")]
        public Color m_backgroundColor;

        [Tooltip("Est-ce que le mot-clé doit être un préfixe exact ou simplement contenu dans le nom ?")]
        public bool m_matchPrefixOnly;
    }

    [CreateAssetMenu(fileName = "NewColorCabinetConfig", menuName = "KJD/Color Cabinet/Cabinet Config")]
    public class ColorCabinetConfig : ScriptableObject
    {
        [Header("Color Cabinet System Rules")]
        [Tooltip("Liste des règles de coloration appliquées aux noms des objets et dossiers.")]
        [SerializeField] private List<ColorRule> _rules = new List<ColorRule>();

        public List<ColorRule> Rules => _rules;
    }
}