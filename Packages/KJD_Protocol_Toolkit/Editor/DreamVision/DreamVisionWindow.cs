using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using KJD.DreamVision.Data;
using KJD.DreamVision.Core;
using KJD.DreamVision.Editor.UI;

namespace KJD.DreamVision.Editor
{
    public class DreamVisionWindow : EditorWindow
    {
        private DreamVisionGraphView graphView;
        private VisualElement inspectorPanel;

        // Les champs de l'inspecteur
        private Label inspectorTitle;
        private TextField notesField;
        private Label dateLabel;
        private SnapshotMetadata selectedCrystal;

        [MenuItem("KJD/DreamVision/Open Timeline", false, 10)]
        public static void ShowWindow()
        {
            DreamVisionWindow window = GetWindow<DreamVisionWindow>();
            window.titleContent = new GUIContent("DreamVision");
            window.Show();
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            // --- ZONE SUPERIEURE (Barre de Capture) ---
            VisualElement topPanel = new VisualElement();
            topPanel.style.paddingTop = 8;
            topPanel.style.paddingBottom = 8;
            topPanel.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f));
            topPanel.style.flexDirection = FlexDirection.Row; // Alignement horizontal pour gagner de la place

            TextField branchNameField = new TextField("Nouvelle Idée :");
            branchNameField.style.flexGrow = 1;
            branchNameField.style.marginLeft = 10;
            branchNameField.style.marginRight = 10;
            topPanel.Add(branchNameField);

            Button captureButton = new Button();
            captureButton.text = "📸 Capturer l'Instant";
            captureButton.style.width = 180;
            captureButton.style.marginRight = 10;
            captureButton.style.backgroundColor = new StyleColor(new Color(0.18f, 0.55f, 0.34f));
            captureButton.style.color = Color.white;
            captureButton.style.unityFontStyleAndWeight = FontStyle.Bold;

            captureButton.clicked += () =>
            {
                if (string.IsNullOrEmpty(branchNameField.value)) return;
                SnapshotEngine.CaptureCurrentScene(branchNameField.value);
                branchNameField.value = "";
                graphView.PopulateTimeline();
            };
            topPanel.Add(captureButton);
            root.Add(topPanel);

            // --- ZONE PRINCIPALE (Séparée en 2 horizontalement) ---
            VisualElement workspaceContainer = new VisualElement();
            workspaceContainer.style.flexDirection = FlexDirection.Row;
            workspaceContainer.style.flexGrow = 1;

            // 1. La Carte (À gauche)
            graphView = new DreamVisionGraphView();
            graphView.style.flexGrow = 3; // Prend 75% de la largeur
            workspaceContainer.Add(graphView);

            // 2. L'Inspecteur (À droite)
            inspectorPanel = new VisualElement();
            inspectorPanel.style.width = 280; // Largeur fixe pour l'inspecteur
            inspectorPanel.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f));
            inspectorPanel.style.borderLeftColor = new StyleColor(Color.black);
            inspectorPanel.style.borderLeftWidth = 1;
            inspectorPanel.style.paddingLeft = 10;
            inspectorPanel.style.paddingRight = 10;
            inspectorPanel.style.paddingTop = 10;

            inspectorTitle = new Label("Aucun nœud sélectionné");
            inspectorTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            inspectorTitle.style.fontSize = 14;
            inspectorTitle.style.whiteSpace = WhiteSpace.Normal;
            inspectorPanel.Add(inspectorTitle);

            dateLabel = new Label("");
            dateLabel.style.fontSize = 11;
            dateLabel.style.color = new StyleColor(Color.gray);
            dateLabel.style.marginBottom = 15;
            inspectorPanel.Add(dateLabel);

            notesField = new TextField("Notes de Design :");
            notesField.multiline = true; // Permet d'écrire des paragraphes entier !
            notesField.style.flexGrow = 1; // Prend toute la hauteur disponible
            notesField.style.marginBottom = 10;
            notesField.style.whiteSpace = WhiteSpace.Normal;
            inspectorPanel.Add(notesField);

            Button saveNotesBtn = new Button();
            saveNotesBtn.text = "💾 Sauvegarder les Notes";
            saveNotesBtn.style.height = 30;
            saveNotesBtn.style.marginBottom = 15;
            saveNotesBtn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            saveNotesBtn.style.color = Color.white;

            saveNotesBtn.clicked += () =>
            {
                if (string.IsNullOrEmpty(selectedCrystal.NodeGUID)) return;
                selectedCrystal.UserNotes = notesField.value;
                SnapshotEngine.UpdateSnapshotMetadata(selectedCrystal);
                Debug.Log($"[DreamVision] Notes de design enregistrées pour : {selectedCrystal.BranchName}");
            };
            inspectorPanel.Add(saveNotesBtn);

            workspaceContainer.Add(inspectorPanel);
            root.Add(workspaceContainer);

            // --- CONNEXION ENTRE LA CARTE ET L'INSPECTEUR ---
            graphView.OnNodeSelected += (crystal) =>
            {
                selectedCrystal = crystal;
                inspectorTitle.text = $"📝 {crystal.BranchName}";
                notesField.value = crystal.UserNotes;

                System.DateTime date = new System.DateTime(crystal.TimestampTicks);
                dateLabel.text = $"Capturé le : {date.ToString("g")}";
            };
        }
    }
}