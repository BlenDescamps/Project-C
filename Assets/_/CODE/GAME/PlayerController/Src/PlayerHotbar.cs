using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Gestionnaire de la barre d'items (Hotbar façon Minecraft).
    /// Gère les emplacements d'équipement, le slot actif et la liaison des objets pour le rappel.
    /// </summary>
    [RequireComponent(typeof(PlayerInteraction))]
    public class PlayerHotbar : MonoBehaviour
    {
        #region Publics

        public int ActiveSlotIndex => _activeSlotIndex;
        public int SlotCount => _slots != null ? _slots.Length : 0;
        public HotbarSlot ActiveSlot => (_slots != null && _activeSlotIndex >= 0 && _activeSlotIndex < _slots.Length) ? _slots[_activeSlotIndex] : null;
        public bool HasItemInActiveSlot => ActiveSlot != null && !ActiveSlot.IsEmpty;

        public event Action<int> OnActiveSlotChanged;
        public event Action<int> OnSlotUpdated;

        public HotbarSlot GetSlot(int index)
        {
            if (_slots == null || index < 0 || index >= _slots.Length) return null;
            return _slots[index];
        }

        #endregion

        #region Unity API

        private void Awake()
        {
            _playerInteraction = GetComponent<PlayerInteraction>();
            InitializeSlots();
        }

        private void Update()
        {
            HandleInputs();
        }

        private void OnGUI()
        {
            if (!_showHotbarUI) return;
            DrawHotbarGUI();
        }

        #endregion

        #region Main API

        /// <summary>
        /// Assigne un objet ramassé au slot actif (ou au premier slot vide, typiquement Slot 1).
        /// </summary>
        public bool AssignItemToActiveOrFirstSlot(IHoldable item, string itemName = "")
        {
            if (item == null) return false;

            // 1. Vérifie si l'objet est déjà lié à un slot
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Item == item)
                {
                    _slots[i].IsInHand = true;
                    _slots[i].IsStashed = false;
                    _activeSlotIndex = i;
                    OnSlotUpdated?.Invoke(i);
                    return true;
                }
            }

            // 2. Si le slot actif est vide, on l'équipe ici
            if (_slots[_activeSlotIndex].IsEmpty)
            {
                _slots[_activeSlotIndex].BindItem(item, itemName);
                _slots[_activeSlotIndex].IsInHand = true;
                OnSlotUpdated?.Invoke(_activeSlotIndex);
                return true;
            }

            // 3. Sinon on cherche le premier slot vide
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    _slots[i].BindItem(item, itemName);
                    _slots[i].IsInHand = true;
                    SetActiveSlot(i);
                    OnSlotUpdated?.Invoke(i);
                    return true;
                }
            }

            // Aucun slot libre : remplace le slot actif
            _slots[_activeSlotIndex].BindItem(item, itemName);
            _slots[_activeSlotIndex].IsInHand = true;
            OnSlotUpdated?.Invoke(_activeSlotIndex);
            return true;
        }

        /// <summary>
        /// Notifie la hotbar que l'objet a été lancé dans le monde.
        /// Il reste équipé dans son slot pour permettre son rappel.
        /// </summary>
        public void NotifyItemThrown(IHoldable item)
        {
            if (item == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Item == item)
                {
                    _slots[i].IsInHand = false;
                    _slots[i].IsStashed = false;
                    OnSlotUpdated?.Invoke(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Notifie la hotbar que l'objet a été déposé au sol.
        /// </summary>
        public void NotifyItemDropped(IHoldable item)
        {
            NotifyItemThrown(item);
        }

        /// <summary>
        /// Notifie la hotbar que l'objet a été rappelé avec succès dans les mains.
        /// </summary>
        public void NotifyItemRecalled(IHoldable item)
        {
            if (item == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Item == item)
                {
                    _slots[i].IsInHand = true;
                    _slots[i].IsStashed = false;
                    _activeSlotIndex = i;
                    OnSlotUpdated?.Invoke(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Récupère l'objet à rappeler lié au slot actif (ou au premier slot ayant un objet dans le monde).
        /// </summary>
        public IHoldable GetRecallableItemForActiveSlot()
        {
            // En priorité : l'objet du slot actif
            if (ActiveSlot != null && ActiveSlot.IsRecallable)
            {
                return ActiveSlot.Item;
            }

            // Fallback : tout slot contenant un objet au loin
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsRecallable)
                {
                    return _slots[i].Item;
                }
            }

            return null;
        }

        /// <summary>
        /// Sélectionne un slot précis (0 à SlotCount - 1).
        /// Gère le rangement (sheathe) ou la sortie d'objet en main.
        /// </summary>
        public void SetActiveSlot(int newIndex)
        {
            if (_slots == null || newIndex < 0 || newIndex >= _slots.Length || newIndex == _activeSlotIndex) return;

            if (_playerInteraction != null && _playerInteraction.IsRecalling) return;

            HotbarSlot previousSlot = ActiveSlot;
            _activeSlotIndex = newIndex;
            HotbarSlot nextSlot = ActiveSlot;

            // 1. Rangement de l'objet précédent s'il était en main
            if (previousSlot != null && previousSlot.Item != null && previousSlot.IsInHand)
            {
                previousSlot.IsInHand = false;
                previousSlot.IsStashed = true;
                if (_playerInteraction != null)
                {
                    _playerInteraction.OnItemStashedByHotbar(previousSlot.Item);
                }
            }

            // 2. Sortie de l'objet du nouveau slot s'il était rangé
            if (nextSlot != null && nextSlot.Item != null)
            {
                if (nextSlot.IsStashed)
                {
                    nextSlot.IsStashed = false;
                    nextSlot.IsInHand = true;
                    if (_playerInteraction != null)
                    {
                        _playerInteraction.OnItemUnstashedByHotbar(nextSlot.Item);
                    }
                }
            }

            OnActiveSlotChanged?.Invoke(_activeSlotIndex);
        }

        public void NextSlot()
        {
            int next = (_activeSlotIndex + 1) % _slots.Length;
            SetActiveSlot(next);
        }

        public void PreviousSlot()
        {
            int prev = (_activeSlotIndex - 1 + _slots.Length) % _slots.Length;
            SetActiveSlot(prev);
        }

        public void ClearSlot(int index)
        {
            if (_slots == null || index < 0 || index >= _slots.Length) return;
            _slots[index].Clear();
            OnSlotUpdated?.Invoke(index);
        }

        #endregion

        #region Tools and Utilities

        private void InitializeSlots()
        {
            _slots = new HotbarSlot[_slotCount];
            for (int i = 0; i < _slotCount; i++)
            {
                _slots[i] = new HotbarSlot(i);
            }
            _activeSlotIndex = 0;
        }

        private void HandleInputs()
        {
            // Molette de souris
            if (_allowMouseScrollWheel && Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (scroll > 0.1f) PreviousSlot();
                else if (scroll < -0.1f) NextSlot();
            }

            // Touches numériques 1 à 9
            if (_allowNumberKeys && Keyboard.current != null)
            {
                var kb = Keyboard.current;
                if (kb[Key.Digit1].wasPressedThisFrame || kb[Key.Numpad1].wasPressedThisFrame) SetActiveSlot(0);
                else if (kb[Key.Digit2].wasPressedThisFrame || kb[Key.Numpad2].wasPressedThisFrame) SetActiveSlot(1);
                else if (kb[Key.Digit3].wasPressedThisFrame || kb[Key.Numpad3].wasPressedThisFrame) SetActiveSlot(2);
                else if (kb[Key.Digit4].wasPressedThisFrame || kb[Key.Numpad4].wasPressedThisFrame) SetActiveSlot(3);
                else if (kb[Key.Digit5].wasPressedThisFrame || kb[Key.Numpad5].wasPressedThisFrame) SetActiveSlot(4);
                else if (kb[Key.Digit6].wasPressedThisFrame || kb[Key.Numpad6].wasPressedThisFrame) SetActiveSlot(5);
                else if (kb[Key.Digit7].wasPressedThisFrame || kb[Key.Numpad7].wasPressedThisFrame) SetActiveSlot(6);
                else if (kb[Key.Digit8].wasPressedThisFrame || kb[Key.Numpad8].wasPressedThisFrame) SetActiveSlot(7);
                else if (kb[Key.Digit9].wasPressedThisFrame || kb[Key.Numpad9].wasPressedThisFrame) SetActiveSlot(8);
            }

            // D-Pad Manette
            if (Gamepad.current != null)
            {
                var gp = Gamepad.current;
                if (gp.dpad.left.wasPressedThisFrame) PreviousSlot();
                else if (gp.dpad.right.wasPressedThisFrame) NextSlot();
            }
        }

        private void DrawHotbarGUI()
        {
            if (_slots == null || _slots.Length == 0) return;

            float slotSize = 56f;
            float padding = 6f;
            float totalWidth = (_slots.Length * slotSize) + ((_slots.Length - 1) * padding);
            float startX = (Screen.width - totalWidth) * 0.5f;
            float startY = Screen.height - slotSize - 16f;

            Color oldColor = GUI.color;

            // 1. Tooltip au-dessus de la barre pour le slot actif
            if (ActiveSlot != null && !ActiveSlot.IsEmpty)
            {
                GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold
                };

                string statusText = ActiveSlot.IsInHand ? "<color=#73ff73>● En main</color>" : 
                                   (ActiveSlot.IsRecallable ? "<color=#5ce1e6>⚡ Au loin [Q pour Rappeler]</color>" : "<color=#aaaaaa>📦 Rangé</color>");

                string tooltip = $"<b>{ActiveSlot.ItemName}</b>  •  {statusText}";
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.Box(new Rect(Screen.width * 0.5f - 180, startY - 28f, 360, 24), GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(Screen.width * 0.5f - 180, startY - 28f, 360, 24), tooltip, titleStyle);
            }

            // 2. Rendu des slots façon Minecraft
            for (int i = 0; i < _slots.Length; i++)
            {
                float x = startX + i * (slotSize + padding);
                Rect slotRect = new Rect(x, startY, slotSize, slotSize);
                bool isActive = (i == _activeSlotIndex);

                DrawSlot(slotRect, _slots[i], i, isActive);
            }

            GUI.color = oldColor;
        }

        private void DrawSlot(Rect rect, HotbarSlot slot, int index, bool isActive)
        {
            // Cadre d'activation (Surbrillance dorée)
            if (isActive)
            {
                GUI.color = _activeSlotBorderColor;
                GUI.Box(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), GUIContent.none);
            }

            // Fond du slot
            GUI.color = isActive ? new Color(0.18f, 0.18f, 0.22f, 0.9f) : _slotBackgroundColor;
            GUI.Box(rect, GUIContent.none);

            // Numéro du slot (en haut à gauche : 1, 2, 3...)
            GUIStyle numStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = isActive ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.7f) }
            };
            GUI.Label(new Rect(rect.x + 3, rect.y + 2, 20, 16), (index + 1).ToString(), numStyle);

            // Contenu du slot
            if (slot != null && !slot.IsEmpty)
            {
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                };

                string displayName = slot.ItemName;
                if (displayName.Length > 8) displayName = displayName.Substring(0, 7) + "…";

                // Nom de l'objet
                nameStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(rect.x + 2, rect.y + 14, rect.width - 4, 18), displayName, nameStyle);

                // Badge d'état
                GUIStyle badgeStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 9,
                    fontStyle = FontStyle.Bold
                };

                if (slot.IsInHand)
                {
                    badgeStyle.normal.textColor = new Color(0.4f, 1f, 0.4f, 0.95f);
                    GUI.Label(new Rect(rect.x + 2, rect.y + 32, rect.width - 4, 16), "● Main", badgeStyle);
                }
                else if (slot.IsRecallable)
                {
                    badgeStyle.normal.textColor = _recallableColor;
                    GUI.Label(new Rect(rect.x + 2, rect.y + 32, rect.width - 4, 16), "⚡ [Q]", badgeStyle);
                }
                else if (slot.IsStashed)
                {
                    badgeStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                    GUI.Label(new Rect(rect.x + 2, rect.y + 32, rect.width - 4, 16), "Rangé", badgeStyle);
                }
            }
        }

        #endregion

        #region Private and Protected

        [Header("--- PARAMÈTRES HOTBAR (Minecraft Style) ---")]
        [Tooltip("Nombre d'emplacements dans la barre d'items")]
        [Range(3, 9)]
        [SerializeField] private int _slotCount = 5;

        [Tooltip("Afficher la barre d'items à l'écran")]
        [SerializeField] private bool _showHotbarUI = true;

        [Tooltip("Permettre le changement de slot avec la molette de souris")]
        [SerializeField] private bool _allowMouseScrollWheel = true;

        [Tooltip("Permettre la sélection directe avec les touches 1 à 9")]
        [SerializeField] private bool _allowNumberKeys = true;

        [Header("--- COULEURS DE L'INTERFACE ---")]
        [SerializeField] private Color _slotBackgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.85f);
        [SerializeField] private Color _activeSlotBorderColor = new Color(1f, 0.82f, 0.2f, 0.95f);
        [SerializeField] private Color _recallableColor = new Color(0.35f, 0.9f, 1f, 1f);

        private PlayerInteraction _playerInteraction;
        private HotbarSlot[] _slots;
        private int _activeSlotIndex = 0;

        #endregion
    }
}
