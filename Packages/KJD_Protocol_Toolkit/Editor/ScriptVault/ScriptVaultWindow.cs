using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace KJD.Editor.ScriptVault
{
    public class ScriptVaultWindow : EditorWindow
    {
        #region Private Fields

        private List<VaultScriptAsset> _allScripts = new List<VaultScriptAsset>();
        private List<VaultScriptAsset> _filteredScripts = new List<VaultScriptAsset>();

        private string _searchQuery = "";
        private int _selectedCategoryIndex = 0;
        private Vector2 _scrollPosition;

        #endregion

        #region Unity Menu API

        [MenuItem("KJD/Script Vault 🗄️")]
        public static void ShowWindow()
        {
            ScriptVaultWindow window = GetWindow<ScriptVaultWindow>("Script Vault");
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            RefreshVault();
        }

        #endregion

        #region Core Filtering Logic

        private void RefreshVault()
        {
            _allScripts.Clear();
            string[] guids = AssetDatabase.FindAssets("t:VaultScriptAsset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                VaultScriptAsset scriptAsset = AssetDatabase.LoadAssetAtPath<VaultScriptAsset>(path);
                if (scriptAsset != null) _allScripts.Add(scriptAsset);
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            _filteredScripts.Clear();

            foreach (var script in _allScripts)
            {
                if (_selectedCategoryIndex > 0)
                {
                    ScriptCategory targetCat = (ScriptCategory)(_selectedCategoryIndex - 1);
                    if (script.Category != targetCat) continue;
                }

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    bool matchesName = script.ScriptName.ToLower().Contains(_searchQuery.ToLower());
                    bool matchesTags = false;

                    if (script.Tags != null)
                    {
                        foreach (var tag in script.Tags)
                        {
                            if (tag.ToLower().Contains(_searchQuery.ToLower()))
                            {
                                matchesTags = true;
                                break;
                            }
                        }
                    }

                    if (!matchesName && !matchesTags) continue;
                }

                _filteredScripts.Add(script);
            }
        }

        #endregion

        #region Layout & UI

        private void OnGUI()
        {
            DrawSearchHeader();

            if (GUILayout.Button("🔄 Rafraîchir le Coffre", GUILayout.Width(150)))
            {
                RefreshVault();
            }

            GUILayout.Space(10);
            DrawHorizontalLine();
            GUILayout.Space(5);

            DrawScriptList();
        }

        #endregion

        #region Sub-Renderers

        private void DrawSearchHeader()
        {
            GUILayout.Space(10);
            GUILayout.Label("🗄️ KJD Script Vault", EditorStyles.boldLabel);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                _searchQuery = EditorGUILayout.TextField("🔍 Rechercher (Nom/Tag) :", _searchQuery);

                string[] categoriesOptions = new string[System.Enum.GetValues(typeof(ScriptCategory)).Length + 1];
                categoriesOptions[0] = "📦 Tout afficher";
                for (int i = 0; i < categoriesOptions.Length - 1; i++)
                {
                    categoriesOptions[i + 1] = ((ScriptCategory)i).ToString();
                }
                _selectedCategoryIndex = EditorGUILayout.Popup("📁 Filtrer par Type :", _selectedCategoryIndex, categoriesOptions);

                if (check.changed) ApplyFilters();
            }
        }

        private void DrawScriptList()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            if (_filteredScripts.Count == 0)
            {
                EditorGUILayout.HelpBox("Aucun script ne correspond à tes filtres actuels. 🏜️", MessageType.Info);
            }
            else
            {
                foreach (var script in _filteredScripts)
                {
                    DrawScriptRow(script);
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawScriptRow(VaultScriptAsset script)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"<b>{script.ScriptName}</b> [{script.Category}]", EditorStyles.label);

                    Color defaultColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);

                    if (GUILayout.Button("📥 Sortir du coffre", GUILayout.Width(130), GUILayout.Height(22)))
                    {
                        ExtractScriptToSelectedFolder(script);
                    }
                    GUI.backgroundColor = defaultColor;
                }

                if (script.Tags != null && script.Tags.Length > 0)
                {
                    GUILayout.Label($"<color=#7678ED><i>Tags: {string.Join(", ", script.Tags)}</i></color>", EditorStyles.label);
                }

                if (!string.IsNullOrEmpty(script.Description))
                {
                    GUILayout.Label(script.Description, EditorStyles.wordWrappedLabel);
                }
            }
            GUILayout.Space(5);
        }

        private void DrawHorizontalLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.4f, 0.4f, 0.4f, 0.5f));
        }

        #endregion

        #region File Extraction Logic

        private void ExtractScriptToSelectedFolder(VaultScriptAsset vaultAsset)
        {
            if (vaultAsset.SourceTextAsset == null)
            {
                EditorUtility.DisplayDialog("Erreur", "Ce fichier de coffre n'est lié à aucun fichier texte (.txt) !", "Oups");
                return;
            }

            string targetFolderPath = "Assets";
            foreach (var obj in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (Directory.Exists(path))
                {
                    targetFolderPath = path;
                    break;
                }
                else if (File.Exists(path))
                {
                    targetFolderPath = Path.GetDirectoryName(path);
                    break;
                }
            }

            string fileName = vaultAsset.TargetFileName;
            if (!fileName.EndsWith(".cs")) fileName += ".cs";

            string destinationPath = Path.Combine(targetFolderPath, fileName);

            if (File.Exists(destinationPath))
            {
                if (!EditorUtility.DisplayDialog("Fichier Existant", $"Le script '{fileName}' existe déjà dans ce dossier. Écraser ?", "Oui", "Annuler"))
                {
                    return;
                }
            }

            File.WriteAllText(destinationPath, vaultAsset.SourceTextAsset.text);
            AssetDatabase.Refresh();

            Debug.Log($"<b>[ScriptVault]</b> Script '{fileName}' matérialisé avec succès dans {targetFolderPath} ! 💾");
            ShowNotification(new GUIContent("Script Extrait !"));
        }

        #endregion
    }
}