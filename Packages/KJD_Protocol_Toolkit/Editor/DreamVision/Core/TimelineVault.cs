using System.IO;
using UnityEngine;

namespace KJD.DreamVision.Core
{
    /// <summary>
    /// The manager that handles saving and loading from the hidden folder.
    /// </summary>
    public static class TimelineVault
    {
        // This calculates the path to your active game project and adds our hidden folder.
        // E.g., D:/MyUnityProjects/PinballGame/Assets/~DreamVisionVault
        private static string VaultDirectory => Path.Combine(Application.dataPath, "~DreamVisionVault");

        /// <summary>
        /// Checks if the vault exists. If not, builds it.
        /// </summary>
        public static void InitializeVault()
        {
            if (!Directory.Exists(VaultDirectory))
            {
                Directory.CreateDirectory(VaultDirectory);
                Debug.Log($"[DreamVision] Secret memory vault forged at: {VaultDirectory}");
            }
        }

        /// <summary>
        /// Takes a memory crystal and physically saves it as a JSON file.
        /// </summary>
        public static void SaveSnapshotMetadata(Data.SnapshotMetadata metadata)
        {
            // Always make sure the vault exists first
            InitializeVault();

            // Create a specific folder just for this snapshot, named using its unique ID
            string nodePath = Path.Combine(VaultDirectory, metadata.NodeGUID);
            if (!Directory.Exists(nodePath))
            {
                Directory.CreateDirectory(nodePath);
            }

            // Convert the data struct into readable text (JSON)
            string jsonPath = Path.Combine(nodePath, "metadata.json");
            string jsonContent = JsonUtility.ToJson(metadata, true); // true = pretty print

            // Write the file to the hard drive
            File.WriteAllText(jsonPath, jsonContent);

            Debug.Log($"[DreamVision] Snapshot '{metadata.BranchName}' sealed securely in the vault.");
        }

        /// <summary>
        /// Scans the hidden vault and returns all saved memory crystals.
        /// </summary>
        public static System.Collections.Generic.List<Data.SnapshotMetadata> GetAllSnapshots()
        {
            var allSnapshots = new System.Collections.Generic.List<Data.SnapshotMetadata>();
            InitializeVault(); // Make sure vault exists

            // Look at every folder inside the vault
            string[] nodeFolders = Directory.GetDirectories(VaultDirectory);

            foreach (string folderPath in nodeFolders)
            {
                string jsonPath = Path.Combine(folderPath, "metadata.json");
                if (File.Exists(jsonPath))
                {
                    // Read the JSON and turn it back into a SnapshotMetadata struct
                    string jsonContent = File.ReadAllText(jsonPath);
                    var crystal = JsonUtility.FromJson<Data.SnapshotMetadata>(jsonContent);
                    allSnapshots.Add(crystal);
                }
            }

            // Sort them by time so the newest ideas are at the top
            allSnapshots.Sort((a, b) => b.TimestampTicks.CompareTo(a.TimestampTicks));

            return allSnapshots;
        }
        /// <summary>
        /// Sauvegarde l'ID du nœud sur lequel le développeur travaille actuellement.
        /// </summary>
        public static void SaveActiveNodeGUID(string guid)
        {
            InitializeVault();
            string path = Path.Combine(VaultDirectory, "state.json");
            File.WriteAllText(path, guid);
        }

        /// <summary>
        /// Récupère l'ID du nœud actif. Retourne une chaîne vide si aucun.
        /// </summary>
        public static string GetActiveNodeGUID()
        {
            string path = Path.Combine(VaultDirectory, "state.json");
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            return "";
        }
    }
}