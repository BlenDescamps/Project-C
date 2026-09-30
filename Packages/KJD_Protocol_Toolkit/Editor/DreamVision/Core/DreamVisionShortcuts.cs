using UnityEditor;
using UnityEngine;
using KJD.DreamVision.Editor;

namespace KJD.DreamVision.Core
{
    /// <summary>
    /// Intercepte les raccourcis clavier pour rendre DreamVision aussi rapide qu'un Ctrl+Z.
    /// </summary>
    public static class DreamVisionShortcuts
    {
        // Raccourci : Ctrl + Alt + S
        // Action : Fait un snapshot "Rapide" sans même ouvrir la fenêtre
        [MenuItem("Tools/DreamVision/⚡ Quick Capture %&s")]
        public static void QuickCapture()
        {
            // On génère un nom automatique basé sur l'heure
            string autoName = $"QuickSave_{System.DateTime.Now.ToString("HH'h'mm")}";

            SnapshotEngine.CaptureCurrentScene(autoName);
            Debug.Log($"[DreamVision] ⚡ Sauvegarde rapide exécutée : {autoName}");

            // Si la fenêtre est ouverte, on la rafraîchit en arrière-plan
            if (EditorWindow.HasOpenInstances<DreamVisionWindow>())
            {
                var window = EditorWindow.GetWindow<DreamVisionWindow>();
                window.Repaint(); // Force le rafraîchissement visuel
            }
        }

        // Raccourci : Ctrl + Alt + Z
        // Action : Ouvre immédiatement le tableau de bord pour naviguer dans le temps
        [MenuItem("Tools/DreamVision/⏱️ Ouvrir le Portail %&z")]
        public static void OpenTimelineShortcut()
        {
            DreamVisionWindow.ShowWindow();
        }
    }
}