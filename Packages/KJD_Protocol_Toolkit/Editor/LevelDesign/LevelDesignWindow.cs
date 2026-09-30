using UnityEngine;
using UnityEditor;

namespace KJD.LevelDesign.Editor
{
    public class LevelDesignWindow : EditorWindow
    {
        #region Private Fields

        private Texture2D _levelMap;
        private Transform _environmentRoot;
        private global::LevelTheme _currentTheme;

        private Vector2 _scrollPosition;

        private enum GenerationMode
        {
            Flat2D,         // X/Y, Z=0
            Flat3D,         // X/Z, Y=0
            Vertical3D      // X/Y, Z en profondeur
        }

        private GenerationMode _generationMode = GenerationMode.Flat2D;

        #endregion


        #region Window Lifecycle

        [MenuItem("KJD/LevelDesign/🗺️ Ouvrir le Level Designer", false, 20)]
        public static void ShowWindow()
        {
            LevelDesignWindow window = GetWindow<LevelDesignWindow>();
            window.titleContent = new GUIContent("🗺️ Level Design");
            window.minSize = new Vector2(320f, 550f);
            window.Show();
        }

        #endregion


        #region GUI

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            GUILayout.Space(10);
            DrawDependencies();
            GUILayout.Space(10);
            DrawGenerationMode();
            GUILayout.Space(10);
            DrawControls();
            GUILayout.Space(10);
            DrawPreview();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.Space(8);
            GUILayout.Label("⚙️ KJD — Level Designer", headerStyle);
            EditorGUILayout.Space(4);

            Rect line = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(line, new Color(0.4f, 0.4f, 0.4f));
        }

        private void DrawDependencies()
        {
            GUILayout.Label("🔗 Références", EditorStyles.boldLabel);

            _levelMap = (Texture2D)EditorGUILayout.ObjectField(
                "Level Map",
                _levelMap,
                typeof(Texture2D),
                false
            );

            _environmentRoot = (Transform)EditorGUILayout.ObjectField(
                "Environment Root",
                _environmentRoot,
                typeof(Transform),
                true
            );

            _currentTheme = (global::LevelTheme)EditorGUILayout.ObjectField(
                "Theme",
                _currentTheme,
                typeof(global::LevelTheme),
                false
            );
        }

        private void DrawGenerationMode()
        {
            GUILayout.Label("📐 Mode de Génération", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            DrawModeButton("2D\nX/Y", GenerationMode.Flat2D);
            DrawModeButton("3D Plat\nX/Z", GenerationMode.Flat3D);
            DrawModeButton("3D Vertical\nX/Y+Z", GenerationMode.Vertical3D);

            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            string description = _generationMode switch
            {
                GenerationMode.Flat2D => "Les blocs sont placés sur le plan X/Y. Z = 0. Idéal pour les jeux 2D.",
                GenerationMode.Flat3D => "Les blocs sont placés sur le plan X/Z. Y = 0. Idéal pour les jeux 3D vue de dessus.",
                GenerationMode.Vertical3D => "Les blocs sont placés sur le plan X/Y avec une profondeur en Z. Idéal pour les décors 3D latéraux.",
                _ => ""
            };

            EditorGUILayout.HelpBox(description, MessageType.None);
        }

        private void DrawModeButton(string label, GenerationMode mode)
        {
            bool isActive = _generationMode == mode;

            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal,
                fixedHeight = 40
            };

            Color defaultColor = GUI.backgroundColor;
            if (isActive) GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);

            if (GUILayout.Button(label, style))
                _generationMode = mode;

            GUI.backgroundColor = defaultColor;
        }

        private void DrawControls()
        {
            GUILayout.Label("🛠️ Panneau de Contrôle", EditorStyles.boldLabel);

            bool missingRefs = _levelMap == null || _currentTheme == null || _environmentRoot == null;

            if (missingRefs)
            {
                EditorGUILayout.HelpBox(
                    "Assigne la Level Map, l'Environment Root et le Theme pour activer les contrôles.",
                    MessageType.Warning
                );
            }

            EditorGUI.BeginDisabledGroup(missingRefs);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("🏗️ Construire", GUILayout.Height(35)))
                GenerateLevel();

            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);

            if (GUILayout.Button("🧹 Nettoyer", GUILayout.Height(35)))
                ClearLevel();

            GUI.backgroundColor = defaultColor;
            GUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }

        private void DrawPreview()
        {
            GUILayout.Label("👁️ Blueprint du Niveau", EditorStyles.boldLabel);

            if (_levelMap != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box(_levelMap, GUILayout.Width(200), GUILayout.Height(200));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Assigne une Level Map pour voir la prévisualisation.",
                    MessageType.Info
                );
            }
        }

        #endregion


        #region Level Generation

        private void GenerateLevel()
        {
            if (_levelMap == null || _currentTheme == null || _environmentRoot == null)
            {
                Debug.LogError("[LevelDesign] Paramètres manquants. Abandon de la construction.");
                return;
            }

            if (!_levelMap.isReadable)
            {
                Debug.LogError("[LevelDesign] La texture n'est pas lisible. Active 'Read/Write Enabled' dans les Import Settings.");
                return;
            }

            ClearLevel();

            Color32[] pixels = _levelMap.GetPixels32();
            Vector2Int dimensions = new Vector2Int(_levelMap.width, _levelMap.height);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 currentPixelColor = pixels[i];
                if (currentPixelColor.a == 0) continue;

                GameObject prefabToSpawn = _currentTheme.GetPrefabForColor(currentPixelColor);
                if (prefabToSpawn != null)
                {
                    Vector3 worldPosition = GetWorldPosition(i, dimensions);
                    SpawnBlock(prefabToSpawn, worldPosition, prefabToSpawn.name);
                }
            }

            Debug.Log($"<b>[LevelDesign]</b> Génération terminée en mode <b>{_generationMode}</b>. N'oublie pas de sauvegarder la scène ! 🏗️");
        }

        private void ClearLevel()
        {
            if (_environmentRoot == null) return;

            string rootName = _environmentRoot.name;
            Transform rootParent = _environmentRoot.parent;

            DestroyImmediate(_environmentRoot.gameObject);

            GameObject newRoot = new GameObject(rootName);
            newRoot.transform.SetParent(rootParent);
            newRoot.transform.localPosition = Vector3.zero;
            _environmentRoot = newRoot.transform;
        }

        #endregion


        #region Tools and Utilities

        private void SpawnBlock(GameObject prefabToSpawn, Vector3 position, string groupName)
        {
            Transform groupParent = _environmentRoot.Find(groupName);
            if (groupParent == null)
            {
                GameObject groupGO = new GameObject(groupName);
                groupGO.transform.SetParent(_environmentRoot);
                groupGO.transform.localPosition = Vector3.zero;
                groupParent = groupGO.transform;
            }

            GameObject newBlock = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);
            newBlock.transform.position = position;
            newBlock.transform.rotation = Quaternion.identity;
            newBlock.transform.SetParent(groupParent);
        }

        private Vector3 GetWorldPosition(int index, Vector2Int dimensions)
        {
            int x = index % dimensions.x;
            int y = index / dimensions.x;

            return _generationMode switch
            {
                GenerationMode.Flat2D => new Vector3(x, y, 0),
                GenerationMode.Flat3D => new Vector3(x, 0, y),
                GenerationMode.Vertical3D => new Vector3(x, y, y * 0.1f),
                _ => new Vector3(x, y, 0)
            };
        }

        #endregion
    }
}