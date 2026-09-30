using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelDesign : MonoBehaviour
{
    #region Publics
    #endregion


    #region Unity API
    #endregion


    #region Main API

    public void GenerateLevel()
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
                Vector3Int gridPosition = GetCoordinateFrom(i, dimensions);
                SpawnBlock(prefabToSpawn, gridPosition, prefabToSpawn.name);
            }
        }

        Debug.Log("<b>[LevelDesign]</b> Génération terminée. N'oublie pas de sauvegarder la scène ! 🏗️");
    }

    public void ClearLevel()
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

    private void SpawnBlock(GameObject prefabToSpawn, Vector3Int position, string groupName)
    {
        Transform groupParent = _environmentRoot.Find(groupName);
        if (groupParent == null)
        {
            GameObject groupGO = new GameObject(groupName);
            groupGO.transform.SetParent(_environmentRoot);
            groupGO.transform.localPosition = Vector3.zero;
            groupParent = groupGO.transform;
        }

        GameObject newBlock;

#if UNITY_EDITOR
        newBlock = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);
        newBlock.transform.position = position;
        newBlock.transform.rotation = Quaternion.identity;
#else
        newBlock = Instantiate(prefabToSpawn, position, Quaternion.identity);
#endif

        newBlock.transform.SetParent(groupParent);
    }

    private Vector3Int GetCoordinateFrom(int index, Vector2Int dimensions)
    {
        Vector3Int position = new Vector3Int();
        position.x = index % dimensions.x;
        position.y = index / dimensions.x;
        position.z = 0;
        return position;
    }

    #endregion


    #region Private and Protected

    [Header("Dependencies")]
    [SerializeField] private Texture2D _levelMap;
    [SerializeField] private Transform _environmentRoot;

    [Header("Theming System")]
    [SerializeField] private LevelTheme _currentTheme;

    #endregion
}


#if UNITY_EDITOR
[CustomEditor(typeof(LevelDesign))]
public class LevelDesignEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelDesign script = (LevelDesign)target;

        GUILayout.Space(20);
        GUILayout.Label("⚙️ Panneau de Contrôle KJD", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🏗️ Construire", GUILayout.Height(35)))
        {
            script.GenerateLevel();
            serializedObject.Update();
        }

        Color defaultColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);

        if (GUILayout.Button("🧹 Nettoyer", GUILayout.Height(35)))
        {
            script.ClearLevel();
            serializedObject.Update();
        }

        GUI.backgroundColor = defaultColor;
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.Label("👁️ Blueprint du Niveau", EditorStyles.boldLabel);

        SerializedProperty mapProp = serializedObject.FindProperty("_levelMap");
        if (mapProp == null) return;
        Texture2D mapTex = (Texture2D)mapProp.objectReferenceValue;

        if (mapTex != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box(mapTex, GUILayout.Width(200), GUILayout.Height(200));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Assigne une Level Map pour voir la prévisualisation.", MessageType.Warning);
        }
    }
}
#endif