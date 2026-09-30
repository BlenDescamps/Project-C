using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace KJD.Editor.NamedropConvention
{
    public class NamedropWindow : EditorWindow
    {
        public enum NamingConvention
        {
            None,
            PascalCase,     // MonAssetCool
            camelCase,      // monAssetCool
            snake_case,     // mon_asset_cool
            SCREAMING_SNAKE // MON_ASSET_COOL
        }

        #region Private Fields

        private Texture2D _headerLogo;
        private NamedropPrefixTable _prefixTable;
        private NamingConvention _selectedConvention = NamingConvention.PascalCase;

        private List<Object> _assetsToRename = new List<Object>();
        private Vector2 _scrollPosition;

        // ⏪ Mémoire du dernier lot pour le Revert temporel
        private Dictionary<string, string> _lastBatchRevertData = new Dictionary<string, string>();

        #endregion


        #region Unity Menu API

        [MenuItem("KJD/Namedrop Convention 🏷️")]
        public static void ShowWindow()
        {
            NamedropWindow window = GetWindow<NamedropWindow>("Namedrop");
            window.minSize = new Vector2(400, 550);
            window.Show();
        }

        #endregion


        #region Lifecycle

        private void OnEnable()
        {
            // 1. On lance le scanner à la recherche du nom du fichier (sans l'extension)
            string[] guids = AssetDatabase.FindAssets("logo_blt t:Texture2D");

            if (guids.Length > 0)
            {
                // 2. On a trouvé ! On convertit le GUID en chemin exact, peu importe où il est
                string realPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _headerLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(realPath);
            }
            else
            {
                // 3. Si l'image reste introuvable, la console va nous alerter pour qu'on sache pourquoi
                Debug.LogWarning("<b>[Namedrop Convention]</b> ⚠️ Alerte Matrice : Impossible de trouver 'logo_blt' dans les assets ou les packages.");
            }
        }

        #endregion


        #region Layout & UI

        private void OnGUI()
        {
            DrawBanner();

            GUILayout.Label("🏷️ KJD Namedrop Convention", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Glisse-dépose tes assets pour standardiser leurs noms. Opération 100% sécurisée (GUIDs préservés).", MessageType.Info);

            GUILayout.Space(10);

            DrawSettingsZone();
            GUILayout.Space(10);
            DrawDragAndDropZone();
            GUILayout.Space(10);
            DrawPreviewList();
            GUILayout.Space(10);

            DrawApplyButton();

            if (_lastBatchRevertData.Count > 0)
            {
                GUILayout.Space(5);
                DrawRevertButton();
            }
        }

        #endregion


        #region Sub-Renderers

        private void DrawBanner()
        {
            if (_headerLogo != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(_headerLogo, GUILayout.Height(180));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
        }

        private void DrawSettingsZone()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("⚙️ Configuration", EditorStyles.boldLabel);
                _prefixTable = (NamedropPrefixTable)EditorGUILayout.ObjectField("Table de Préfixes", _prefixTable, typeof(NamedropPrefixTable), false);
                _selectedConvention = (NamingConvention)EditorGUILayout.EnumPopup("Convention", _selectedConvention);
            }
        }

        private void DrawDragAndDropZone()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0f, 60f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "\n📥 GLISSE TES ASSETS ICI", EditorStyles.helpBox);

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            string path = AssetDatabase.GetAssetPath(draggedObject);

                            if (path.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(path))
                            {
                                if (!_assetsToRename.Contains(draggedObject))
                                {
                                    _assetsToRename.Add(draggedObject);
                                }
                            }
                        }
                    }
                    Event.current.Use();
                    break;
            }
        }

        private void DrawPreviewList()
        {
            GUILayout.Label($"👁️ Prévisualisation ({_assetsToRename.Count} objets) :", EditorStyles.boldLabel);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            for (int i = _assetsToRename.Count - 1; i >= 0; i--)
            {
                Object asset = _assetsToRename[i];
                if (asset == null)
                {
                    _assetsToRename.RemoveAt(i);
                    continue;
                }

                string oldName = asset.name;
                string newName = GenerateNewName(asset);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        _assetsToRename.RemoveAt(i);
                        continue;
                    }

                    GUILayout.Label(oldName, GUILayout.Width(150));
                    GUILayout.Label("➔", GUILayout.Width(20));

                    if (oldName == newName)
                        GUILayout.Label($"<color=grey>{newName}</color>", new GUIStyle(EditorStyles.label) { richText = true });
                    else
                        GUILayout.Label($"<color=#4CAF50><b>{newName}</b></color>", new GUIStyle(EditorStyles.label) { richText = true });
                }
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("🧹 Vider la liste"))
            {
                _assetsToRename.Clear();
            }
        }

        private void DrawApplyButton()
        {
            GUI.enabled = _assetsToRename.Count > 0;
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.18f, 0.55f, 0.34f);

            if (GUILayout.Button("⚡ Appliquer la Convention", GUILayout.Height(40)))
            {
                ExecuteRenaming();
            }

            GUI.backgroundColor = defaultColor;
            GUI.enabled = true;
        }

        private void DrawRevertButton()
        {
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.4f, 0.2f);

            if (GUILayout.Button("⏪ Revert : Annuler le dernier lot", GUILayout.Height(30)))
            {
                ExecuteRevert();
            }

            GUI.backgroundColor = defaultColor;
        }

        #endregion


        #region Logic Engines

        private string GenerateNewName(Object asset)
        {
            string baseName = asset.name;
            string prefix = _prefixTable != null ? _prefixTable.GetPrefixForObject(asset) : "";

            string cleanName = Regex.Replace(baseName, @"[^a-zA-Z0-9_]", " ");
            string formattedName = ApplyNamingConvention(cleanName, _selectedConvention);

            if (!string.IsNullOrEmpty(prefix) && !formattedName.StartsWith(prefix))
            {
                formattedName = prefix + formattedName;
            }

            return formattedName;
        }

        private string ApplyNamingConvention(string input, NamingConvention convention)
        {
            if (convention == NamingConvention.None || string.IsNullOrWhiteSpace(input)) return input;

            string[] words = Regex.Split(input.Trim(), @"(?<!^)(?=[A-Z])|[\s_-]+");
            List<string> validWords = new List<string>();

            foreach (string word in words)
            {
                if (!string.IsNullOrWhiteSpace(word)) validWords.Add(word.ToLower());
            }

            if (validWords.Count == 0) return input;

            switch (convention)
            {
                case NamingConvention.PascalCase:
                    for (int i = 0; i < validWords.Count; i++)
                        validWords[i] = char.ToUpper(validWords[i][0]) + validWords[i].Substring(1);
                    return string.Join("", validWords);

                case NamingConvention.camelCase:
                    validWords[0] = validWords[0].ToLower();
                    for (int i = 1; i < validWords.Count; i++)
                        validWords[i] = char.ToUpper(validWords[i][0]) + validWords[i].Substring(1);
                    return string.Join("", validWords);

                case NamingConvention.snake_case:
                    return string.Join("_", validWords);

                case NamingConvention.SCREAMING_SNAKE:
                    return string.Join("_", validWords).ToUpper();

                default:
                    return input;
            }
        }

        private void ExecuteRenaming()
        {
            int successCount = 0;
            _lastBatchRevertData.Clear();

            foreach (Object asset in _assetsToRename)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string newName = GenerateNewName(asset);
                string oldName = asset.name;

                if (oldName == newName) continue;

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                string result = AssetDatabase.RenameAsset(assetPath, newName);

                if (string.IsNullOrEmpty(result))
                {
                    _lastBatchRevertData[guid] = oldName;
                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"[Namedrop Convention] Échec sur {oldName} : {result}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _assetsToRename.Clear();

            Debug.Log($"<b>[Namedrop Convention]</b> Formatage terminé : {successCount} asset(s) renommé(s). 🏷️");
        }

        private void ExecuteRevert()
        {
            foreach (var kvp in _lastBatchRevertData)
            {
                string guid = kvp.Key;
                string originalName = kvp.Value;

                string currentPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(currentPath))
                {
                    AssetDatabase.RenameAsset(currentPath, originalName);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _lastBatchRevertData.Clear();

            Debug.Log("<b>[Namedrop Convention]</b> ⏪ Ligne temporelle restaurée : anciens noms appliqués.");
        }

        #endregion
    }
}