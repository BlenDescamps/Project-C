using UnityEngine;

[System.Serializable]
public struct TileMapping
{
    [Tooltip("La couleur exacte sur le PNG (Ex: 255, 255, 255 pour le blanc pur)")]
    public Color32 m_pixelColor;

    [Tooltip("Le préfabriqué à instancier pour cette couleur dans ce thème")]
    public GameObject m_prefab;
}

[CreateAssetMenu(fileName = "NewLevelTheme", menuName = "KJD/Database/Level Theme")]
public class LevelTheme : ScriptableObject
{
    #region Private and Protected

    [Header("Theme Configuration")]
    [Tooltip("La liste de toutes les correspondances Couleur -> Préfabriqué pour ce thème.")]
    [SerializeField] private TileMapping[] _tileMappings;

    #endregion


    #region Main API

    public GameObject GetPrefabForColor(Color32 targetColor)
    {
        for (int i = 0; i < _tileMappings.Length; i++)
        {
            Color32 databaseColor = _tileMappings[i].m_pixelColor;

            if (databaseColor.r == targetColor.r &&
                databaseColor.g == targetColor.g &&
                databaseColor.b == targetColor.b)
            {
                return _tileMappings[i].m_prefab;
            }
        }

        return null;
    }

    #endregion
}