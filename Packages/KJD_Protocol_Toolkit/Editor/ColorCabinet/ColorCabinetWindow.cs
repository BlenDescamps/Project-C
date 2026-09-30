using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace KJD.Editor.ColorCabinet
{
    public class ColorCabinetWindow : EditorWindow
    {
        #region Private Fields

        private ColorCabinetConfig _config;

        // Variables de création pour la règle actuelle
        private string _detectedName = "";
        private Color _textColor = Color.white;
        private Color _backgroundColor = new Color(0f, 0f, 0f, 0f);
        private bool _matchPrefixOnly = false;

        private Vector2 _scrollPosition;
        private GUIStyle _richTextStyle; // Style mis en cache pour le texte enrichi

        #endregion

        #region Unity Menu API

        [MenuItem("KJD/Color Cabinet 🗄️🎨")]
        public static void ShowWindow()
        {
            ColorCabinetWindow window = GetWindow<ColorCabinetWindow>("Color Cabinet");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            LoadConfig();
            Selection.selectionChanged += OnSelectionChanged;
            UpdateSelectionAnalysis();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void LoadConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:ColorCabinetConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _config = AssetDatabase.LoadAssetAtPath<ColorCabinetConfig>(path);
            }
        }

        private void OnSelectionChanged()
        {
            UpdateSelectionAnalysis();
            Repaint();
        }

        private void UpdateSelectionAnalysis()
        {
            _detectedName = "";

            if (Selection.activeObject != null)
            {
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path))
                {
                    _detectedName = System.IO.Path.GetFileNameWithoutExtension(path);
                    return;
                }
            }

            if (Selection.activeGameObject != null)
            {
                _detectedName = Selection.activeGameObject.name;
            }
        }

        #endregion

        #region Layout & UI

        private void OnGUI()
        {
            // Initialisation sécurisée du style de texte enrichi
            if (_richTextStyle == null)
            {
                _richTextStyle = new GUIStyle(EditorStyles.label);
                _richTextStyle.richText = true;
            }

            GUILayout.Space(10);
            GUILayout.Label("🗄️🎨 Color Cabinet - Panneau de Contrôle", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sélectionne un dossier ou un GameObject dans Unity. Son nom sera automatiquement détecté pour lui appliquer une règle.", MessageType.Info);

            if (_config == null)
            {
                EditorGUILayout.HelpBox("⚠️ Fichier 'ColorCabinetConfig' introuvable dans le package. Crée-le via le clic droit pour activer l'outil.", MessageType.Error);
                if (GUILayout.Button("🔄 Recharger la Configuration")) LoadConfig();
                return;
            }

            GUILayout.Space(15);
            DrawCreationZone();

            GUILayout.Space(20);
            DrawExistingRulesZone();
        }

        #endregion

        #region Sub-Renderers

        private void DrawCreationZone()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("➕ Ajouter une nouvelle règle colorée", EditorStyles.boldLabel);
                GUILayout.Space(5);

                if (string.IsNullOrEmpty(_detectedName))
                {
                    GUI.enabled = false;
                    EditorGUILayout.TextField("Élément détecté :", "Aucune sélection (Dossier ou GameObject)");
                    GUI.enabled = true;
                }
                else
                {
                    // CORRECTIF : Utilisation du style personnalisé avec richText = true
                    EditorGUILayout.LabelField("Élément détecté :", $"<b><color=#F7B801>{_detectedName}</color></b>", _richTextStyle);
                }

                GUILayout.Space(5);
                _textColor = EditorGUILayout.ColorField("Couleur du Texte :", _textColor);
                _backgroundColor = EditorGUILayout.ColorField("Couleur du Fond :", _backgroundColor);
                _matchPrefixOnly = EditorGUILayout.Toggle("Préfixe exact uniquement ?", _matchPrefixOnly);

                GUILayout.Space(10);

                if (string.IsNullOrEmpty(_detectedName)) GUI.enabled = false;

                Color defaultColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.95f, 0.45f, 0.08f);

                if (GUILayout.Button("💾 Enregistrer la règle dans le Cabinet", GUILayout.Height(30)))
                {
                    AddNewRule(_detectedName, _textColor, _backgroundColor, _matchPrefixOnly);
                }

                GUI.backgroundColor = defaultColor;
                GUI.enabled = true;
            }
        }

        private void DrawExistingRulesZone()
        {
            GUILayout.Label("🗄️ Règles actuellement enregistrées :", EditorStyles.miniBoldLabel);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox, GUILayout.Height(220));

            if (_config.Rules.Count == 0)
            {
                EditorGUILayout.HelpBox("Le Cabinet est vide pour le moment.", MessageType.None);
            }
            else
            {
                int indexToRemove = -1;

                for (int i = 0; i < _config.Rules.Count; i++)
                {
                    ColorRule rule = _config.Rules[i];

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        string prefixInfo = rule.m_matchPrefixOnly ? "[Préfixe]" : "[Contient]";

                        // CORRECTIF : Utilisation du style personnalisé avec richText = true
                        GUILayout.Label($"{prefixInfo} <b>{rule.m_keyword}</b>", _richTextStyle);

                        GUILayout.Space(10);
                        Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(40));
                        if (rule.m_backgroundColor.a > 0f) EditorGUI.DrawRect(r, rule.m_backgroundColor);

                        GUIStyle previewStyle = new GUIStyle(EditorStyles.boldLabel);
                        previewStyle.normal.textColor = rule.m_textColor;
                        EditorGUI.LabelField(r, " Texte", previewStyle);

                        Color defaultBg = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("❌", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            indexToRemove = i;
                        }
                        GUI.backgroundColor = defaultBg;
                    }
                }

                if (indexToRemove >= 0)
                {
                    Undo.RecordObject(_config, "Color Cabinet - Règle Supprimée");
                    _config.Rules.RemoveAt(indexToRemove);
                    EditorUtility.SetDirty(_config);
                }
            }

            GUILayout.EndScrollView();
        }

        #endregion

        #region Core Configuration Logic

        private void AddNewRule(string keyword, Color text, Color bg, bool prefixOnly)
        {
            foreach (var existingRule in _config.Rules)
            {
                if (existingRule.m_keyword == keyword)
                {
                    EditorUtility.DisplayDialog("Règle Existante", $"Le mot-clé '{keyword}' possède déjà une règle dans le Cabinet.", "D'accord");
                    return;
                }
            }

            ColorRule newRule = new ColorRule
            {
                m_keyword = keyword,
                m_textColor = text,
                m_backgroundColor = bg,
                m_matchPrefixOnly = prefixOnly
            };

            Undo.RecordObject(_config, "Color Cabinet - Nouvelle Règle");
            _config.Rules.Add(newRule);

            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();

            ShowNotification(new GUIContent("Règle enregistrée ! 🎨"));
        }

        #endregion
    }
}