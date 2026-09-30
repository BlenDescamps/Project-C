using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace KJD.Editor.HierarchyFastForward
{
    public class HierarchyFastForwardWindow : EditorWindow
    {
        #region Private and Protected

        private List<ProjectStructureProfile> _availableProfiles = new List<ProjectStructureProfile>();
        private string[] _profileNames = new string[0];
        private int _selectedProfileIndex = 0;

        private Vector2 _scrollPosition;

        #endregion


        #region Unity Editor Menu API

        [MenuItem("KJD/Hierarchy Fast Forward ⚡")]
        public static void ShowWindow()
        {
            HierarchyFastForwardWindow window = GetWindow<HierarchyFastForwardWindow>("Hierarchy FF");
            window.minSize = new Vector2(420, 460);
            window.Show();
        }

        #endregion


        #region Init & Lifecycle

        private void OnEnable()
        {
            RefreshAvailableProfiles();
        }

        private void RefreshAvailableProfiles()
        {
            _availableProfiles.Clear();

            // On cherche tous les ScriptableObjects de type ProjectStructureProfile dans le projet ET dans les packages
            string[] guids = AssetDatabase.FindAssets("t:ProjectStructureProfile");

            List<string> names = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ProjectStructureProfile profile = AssetDatabase.LoadAssetAtPath<ProjectStructureProfile>(path);

                if (profile != null)
                {
                    _availableProfiles.Add(profile);
                    // On affiche le nom du profil + une indication si c'est dans le Package
                    string origin = path.StartsWith("Packages/") ? "[Package]" : "[Local]";
                    names.Add($"{origin} {profile.ProfileName}");
                }
            }

            _profileNames = names.ToArray();

            // Sécurité si l'index est hors limites après rafraîchissement
            if (_selectedProfileIndex >= _availableProfiles.Count)
            {
                _selectedProfileIndex = 0;
            }
        }

        #endregion


        #region Layout & UI

        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(10);

            // Petit bouton de secours pour rafraîchir la liste si on crée un profil à la volée
            if (GUILayout.Button("🔄 Rafraîchir les profils dispos", GUILayout.Width(180)))
            {
                RefreshAvailableProfiles();
            }

            GUILayout.Space(5);
            DrawProfileSelector();

            GUILayout.Space(15);

            if (_availableProfiles.Count == 0 || _selectedProfileIndex >= _availableProfiles.Count)
            {
                EditorGUILayout.HelpBox("⚠️ Aucun profil trouvé dans le package ou le projet. Crée un 'Project Profile' via le clic droit pour commencer.", MessageType.Warning);
                return;
            }

            ProjectStructureProfile currentProfile = _availableProfiles[_selectedProfileIndex];
            DrawProfileDetails(currentProfile);

            GUILayout.Space(20);

            DrawActionButtons(currentProfile);
        }

        #endregion


        #region Sub-Renderers

        private void DrawHeader()
        {
            GUILayout.Space(10);
            GUILayout.Label("⚡ Hierarchy Fast Forward", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sélectionne une configuration embarquée pour déployer ton architecture d'Assets instantanément.", MessageType.Info);
        }

        private void DrawProfileSelector()
        {
            if (_profileNames.Length > 0)
            {
                _selectedProfileIndex = EditorGUILayout.Popup("Profil actif :", _selectedProfileIndex, _profileNames);
            }
        }

        private void DrawProfileDetails(ProjectStructureProfile profile)
        {
            GUILayout.Label($"Configuration : {profile.ProfileName}", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(profile.Description))
            {
                EditorGUILayout.HelpBox(profile.Description, MessageType.None);
            }

            GUILayout.Space(10);
            GUILayout.Label("📂 Structure programmée :", EditorStyles.miniBoldLabel);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox, GUILayout.Height(180));

            foreach (string folderPath in profile.TargetFolders)
            {
                if (string.IsNullOrWhiteSpace(folderPath)) continue;

                bool exists = Directory.Exists(Path.Combine(Application.dataPath, folderPath));
                string statusIcon = exists ? "✅ (Prêt)" : "➕ (À créer)";

                GUILayout.Label($"  • Assets/{folderPath}  {statusIcon}", EditorStyles.label);
            }

            GUILayout.EndScrollView();
        }

        private void DrawActionButtons(ProjectStructureProfile profile)
        {
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.95f, 0.45f, 0.08f);

            if (GUILayout.Button("🚀 Fast Forward - Générer l'Arborescence", GUILayout.Height(40)))
            {
                ExecuteFastForward(profile);
            }

            GUI.backgroundColor = defaultColor;
        }

        #endregion


        #region Main Core Logic

        private void ExecuteFastForward(ProjectStructureProfile profile)
        {
            if (profile == null || profile.TargetFolders == null) return;

            int createdCount = 0;
            string basePath = Application.dataPath;

            foreach (string relativePath in profile.TargetFolders)
            {
                if (string.IsNullOrWhiteSpace(relativePath)) continue;

                string cleanRelativePath = relativePath.Trim('/', '\\');
                string fullPath = Path.Combine(basePath, cleanRelativePath);

                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    createdCount++;
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"<b>[HierarchyFastForward]</b> {createdCount} dossier(s) injecté(s) via le profil '{profile.ProfileName}'. 📁");
            ShowNotification(new GUIContent($"{createdCount} Dossiers déployés !"));
        }

        #endregion
    }
}