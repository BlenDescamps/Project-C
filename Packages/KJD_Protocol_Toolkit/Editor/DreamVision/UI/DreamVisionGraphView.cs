using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using KJD.DreamVision.Core;
using KJD.DreamVision.Data;
using System.Collections.Generic;
using System.Linq;

namespace KJD.DreamVision.Editor.UI
{
    public class DreamVisionGraphView : GraphView
    {
        // Un délégué (callback) pour envoyer le nœud sélectionné à l'inspecteur de la fenêtre
        public System.Action<SnapshotMetadata> OnNodeSelected;

        public DreamVisionGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            PopulateTimeline();
        }

        public void PopulateTimeline()
        {
            // Nettoyage complet (Nœuds + Liens)
            DeleteElements(graphElements.ToList());

            var allSnapshots = TimelineVault.GetAllSnapshots();
            string activeGUID = TimelineVault.GetActiveNodeGUID();

            // Dictionnaires pour stocker les ports afin de les relier plus tard
            Dictionary<string, Port> inputPorts = new Dictionary<string, Port>();
            Dictionary<string, Port> outputPorts = new Dictionary<string, Port>();
            Dictionary<string, Node> visualNodes = new Dictionary<string, Node>();

            // Génération algorithmique des positions pour éviter les superpositions
            Dictionary<string, Vector2> nodePositions = CalculateTreePositions(allSnapshots);

            foreach (var crystal in allSnapshots)
            {
                Node node = new Node();
                node.title = crystal.BranchName;

                // Si c'est le nœud actif, on le stylise avec une bordure dorée (Style Persona !)
                if (crystal.NodeGUID == activeGUID)
                {
                    node.style.borderTopColor = new StyleColor(new Color(1f, 0.8f, 0f));
                    node.style.borderBottomColor = new StyleColor(new Color(1f, 0.8f, 0f));
                    node.style.borderLeftColor = new StyleColor(new Color(1f, 0.8f, 0f));
                    node.style.borderRightColor = new StyleColor(new Color(1f, 0.8f, 0f));
                    node.style.borderTopWidth = 2;
                    node.style.borderBottomWidth = 2;
                    node.style.borderLeftWidth = 2;
                    node.style.borderRightWidth = 2;
                }

                // --- CRÉATION DES SYNAPSES (PORTS) ---
                // Port d'entrée (Haut)
                Port inputPort = node.InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
                inputPort.portName = "";
                node.inputContainer.Add(inputPort);
                inputPorts[crystal.NodeGUID] = inputPort;

                // Port de sortie (Bas)
                Port outputPort = node.InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
                outputPort.portName = "";
                node.outputContainer.Add(outputPort);
                outputPorts[crystal.NodeGUID] = outputPort;

                // Bouton de saut temporel
                Button restoreBtn = new Button(() => {
                    SnapshotEngine.RestoreSnapshot(crystal);
                    PopulateTimeline(); // Rafraîchit les bordures dorées !
                });
                restoreBtn.text = "⏪ Voyager";
                restoreBtn.style.backgroundColor = new StyleColor(new Color(0.15f, 0.35f, 0.55f));
                restoreBtn.style.color = Color.white;
                node.mainContainer.Add(restoreBtn);

                // --- ÉVÉNEMENT DE SÉLECTION (Pour l'Inspecteur) ---
                node.RegisterCallback<MouseDownEvent>(evt => {
                    OnNodeSelected?.Invoke(crystal);
                });

                // Positionnement
                Vector2 pos = nodePositions.ContainsKey(crystal.NodeGUID) ? nodePositions[crystal.NodeGUID] : new Vector2(100, 200);
                node.SetPosition(new Rect(pos.x, pos.y, 200, 150));

                visualNodes[crystal.NodeGUID] = node;
                AddElement(node);
            }

            // --- SOUDAGE DES CÂBLES VISUELS ---
            foreach (var crystal in allSnapshots)
            {
                if (!string.IsNullOrEmpty(crystal.ParentGUID) && outputPorts.ContainsKey(crystal.ParentGUID))
                {
                    Port parentOutput = outputPorts[crystal.ParentGUID];
                    Port childInput = inputPorts[crystal.NodeGUID];

                    Edge edge = parentOutput.ConnectTo(childInput);
                    AddElement(edge);
                }
            }
        }

        /// <summary>
        /// Petit estimateur de position pour étaler l'arbre proprement.
        /// </summary>
        private Dictionary<string, Vector2> CalculateTreePositions(List<SnapshotMetadata> snapshots)
        {
            var positions = new Dictionary<string, Vector2>();
            if (snapshots.Count == 0) return positions;

            // On trie du plus ancien au plus récent
            var ordered = snapshots.OrderBy(s => s.TimestampTicks).ToList();

            Dictionary<string, List<string>> childrenMap = new Dictionary<string, List<string>>();
            List<string> roots = new List<string>();

            foreach (var s in ordered)
            {
                if (string.IsNullOrEmpty(s.ParentGUID) || !ordered.Any(p => p.NodeGUID == s.ParentGUID))
                {
                    roots.Add(s.NodeGUID);
                }
                else
                {
                    if (!childrenMap.ContainsKey(s.ParentGUID)) childrenMap[s.ParentGUID] = new List<string>();
                    childrenMap[s.ParentGUID].Add(s.NodeGUID);
                }
            }

            int currentX = 50;
            foreach (var root in roots)
            {
                ComputeLayout(root, currentX, 100, childrenMap, positions, out int nextX);
                currentX = nextX + 100;
            }

            return positions;
        }

        private void ComputeLayout(string nodeGUID, int x, int y, Dictionary<string, List<string>> childrenMap, Dictionary<string, Vector2> positions, out int nextX)
        {
            positions[nodeGUID] = new Vector2(x, y);
            nextX = x;

            if (childrenMap.ContainsKey(nodeGUID) && childrenMap[nodeGUID].Count > 0)
            {
                int childX = x;
                foreach (var child in childrenMap[nodeGUID])
                {
                    ComputeLayout(child, childX, y + 220, childrenMap, positions, out int childNextX);
                    childX = childNextX + 250;
                }
                nextX = Mathf.Max(x, childX - 250);
            }
        }
    }
}