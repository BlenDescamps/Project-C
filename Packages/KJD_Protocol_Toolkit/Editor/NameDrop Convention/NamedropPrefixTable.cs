using System.Collections.Generic;
using UnityEngine;

namespace KJD.Editor.NamedropConvention
{
    [System.Serializable]
    public struct PrefixRule
    {
        [Tooltip("Le type exact de l'asset (ex: Texture2D, GameObject, AudioClip)")]
        public string m_assetType;
        [Tooltip("Le préfixe à appliquer (ex: T_, PF_, SFX_)")]
        public string m_prefix;
    }

    [CreateAssetMenu(fileName = "NewNamedropTable", menuName = "KJD/Namedrop Convention/Prefix Table")]
    public class NamedropPrefixTable : ScriptableObject
    {
        [Header("Règles de Préfixes par Type")]
        public List<PrefixRule> Rules = new List<PrefixRule>();

        /// <summary>
        /// Cherche le préfixe correspondant au type de l'objet.
        /// </summary>
        public string GetPrefixForObject(Object obj)
        {
            if (obj == null) return "";

            string typeName = obj.GetType().Name;

            foreach (var rule in Rules)
            {
                if (rule.m_assetType == typeName)
                {
                    return rule.m_prefix;
                }
            }
            return ""; // Aucun préfixe trouvé
        }
    }
}