using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using KJD.DreamVision.Data;

namespace KJD.DreamVision.Core
{
    public static class SnapshotEngine
    {
        public static void CaptureCurrentScene(string branchName)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(activeScene.path))
            {
                Debug.LogError("[DreamVision] ERREUR : Sauvegarde ta scène d'abord !");
                return;
            }

            if (activeScene.isDirty)
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            string snapshotID = Guid.NewGuid().ToString();
            string originalScenePath = Path.GetFullPath(activeScene.path);
            string nodeVaultPath = Path.Combine(Application.dataPath, "~DreamVisionVault", snapshotID);

            if (!Directory.Exists(nodeVaultPath)) Directory.CreateDirectory(nodeVaultPath);

            string clonedScenePath = Path.Combine(nodeVaultPath, activeScene.name + "_clone.unity");
            File.Copy(originalScenePath, clonedScenePath, true);

            // --- CHANGEMENT ICI : On récupère le vrai parent ! ---
            string parentGUID = TimelineVault.GetActiveNodeGUID();

            SnapshotMetadata crystal = new SnapshotMetadata
            {
                NodeGUID = snapshotID,
                ParentGUID = parentGUID, // Lié à l'histoire !
                BranchName = string.IsNullOrEmpty(branchName) ? "Idée sans nom" : branchName,
                TimestampTicks = DateTime.Now.Ticks,
                Tags = new string[] { "WIP" },
                UserNotes = "Écris tes notes ici dans l'inspecteur...",
                SceneFilePath = clonedScenePath
            };

            TimelineVault.SaveSnapshotMetadata(crystal);

            // Le nouveau nœud devient le nœud actif
            TimelineVault.SaveActiveNodeGUID(snapshotID);

            Debug.Log($"[DreamVision] Nouvelle branche générée : {crystal.BranchName}");
        }

        public static void RestoreSnapshot(SnapshotMetadata crystal)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[DreamVision] Voyage annulé.");
                return;
            }

            string workspaceDir = "Assets/DreamVision_Workspace";
            if (!AssetDatabase.IsValidFolder(workspaceDir))
            {
                AssetDatabase.CreateFolder("Assets", "DreamVision_Workspace");
            }

            string safeCloneName = $"Restored_{crystal.BranchName.Replace(" ", "_")}.unity";
            string targetPath = Path.Combine(workspaceDir, safeCloneName);

            File.Copy(crystal.SceneFilePath, targetPath, true);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(targetPath);

            // --- CHANGEMENT ICI : On met à jour le nœud actif lors du saut ---
            TimelineVault.SaveActiveNodeGUID(crystal.NodeGUID);

            Debug.Log($"[DreamVision] Réalité alignée sur : {crystal.BranchName}");
        }

        // Fonction utilitaire pour mettre à jour les métadonnées (Notes/Tags) depuis l'inspecteur
        public static void UpdateSnapshotMetadata(SnapshotMetadata crystal)
        {
            TimelineVault.SaveSnapshotMetadata(crystal);
        }
    }
}