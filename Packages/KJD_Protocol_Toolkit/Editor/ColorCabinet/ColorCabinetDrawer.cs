using System.IO;
using UnityEngine;
using UnityEditor;

namespace KJD.Editor.ColorCabinet
{
    [InitializeOnLoad]
    public static class ColorCabinetDrawer
    {
        #region Private Fields

        private static ColorCabinetConfig _activeConfig;

        #endregion

        #region Initialization

        static ColorCabinetDrawer()
        {
            LoadCabinetConfig();

            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectItemGUI;
        }

        private static void LoadCabinetConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:ColorCabinetConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _activeConfig = AssetDatabase.LoadAssetAtPath<ColorCabinetConfig>(path);
            }
        }

        #endregion

        #region Unity Editor GUI Hooks

        private static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
        {
            if (_activeConfig == null || _activeConfig.Rules.Count == 0) return;

            GameObject go = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (go == null) return;

            ApplyStyleRules(go.name, selectionRect, false);
        }

        private static void OnProjectItemGUI(string guid, Rect selectionRect)
        {
            if (_activeConfig == null || _activeConfig.Rules.Count == 0) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;

            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name)) return;

            if (selectionRect.height <= 24)
            {
                ApplyStyleRules(name, selectionRect, true);
            }
        }

        #endregion

        #region Styling Core Logic

        private static void ApplyStyleRules(string name, Rect rect, bool isProjectWindow)
        {
            foreach (ColorRule rule in _activeConfig.Rules)
            {
                if (string.IsNullOrEmpty(rule.m_keyword)) continue;

                bool isMatch = false;
                if (rule.m_matchPrefixOnly)
                {
                    isMatch = name.StartsWith(rule.m_keyword);
                }
                else
                {
                    isMatch = name.Contains(rule.m_keyword);
                }

                if (isMatch)
                {
                    DrawCustomRow(name, rect, rule, isProjectWindow);
                    break;
                }
            }
        }

        private static void DrawCustomRow(string name, Rect rect, ColorRule rule, bool isProjectWindow)
        {
            // Décalage standard pour s'aligner après l'icône d'Unity
            float xOffset = 18f;

            // 1. Préparer le style de texte (Dossiers/Objets en Gras)
            GUIStyle customStyle = new GUIStyle(EditorStyles.label);
            customStyle.normal.textColor = rule.m_textColor;
            customStyle.fontStyle = FontStyle.Bold;

            // 2. CALCUL MAGIQUE : Mesurer la taille exacte du texte en pixels
            Vector2 textSize = customStyle.CalcSize(new GUIContent(name));

            // On crée un rectangle ajusté à la largeur du mot (+ 4px de marge pour respirer)
            float textWidth = textSize.x + 4f;

            // 3. Dessiner le fond (uniquement derrière le texte)
            if (rule.m_backgroundColor.a > 0f)
            {
                Rect bgRect = new Rect(rect.x + xOffset, rect.y, textWidth, rect.height);
                EditorGUI.DrawRect(bgRect, rule.m_backgroundColor);
            }

            // 4. Dessiner le texte coloré
            Rect textRect = new Rect(rect.x + xOffset, rect.y, textWidth, rect.height);
            EditorGUI.LabelField(textRect, name, customStyle);
        }

        #endregion
    }
}