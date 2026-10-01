using System;
using UnityEngine;

namespace KJD.Game.PlayerController
{
    /// <summary>
    /// Représente un emplacement individuel de la barre d'items (Hotbar façon Minecraft).
    /// </summary>
    [Serializable]
    public class HotbarSlot
    {
        public int SlotIndex;
        public IHoldable Item;
        public string ItemName = string.Empty;
        public bool IsInHand;
        public bool IsStashed;

        public bool IsEmpty => Item == null;
        public bool IsRecallable => Item != null && !IsInHand && !IsStashed;

        public HotbarSlot(int slotIndex)
        {
            SlotIndex = slotIndex;
            Item = null;
            ItemName = string.Empty;
            IsInHand = false;
            IsStashed = false;
        }

        public void BindItem(IHoldable item, string name)
        {
            Item = item;
            ItemName = !string.IsNullOrEmpty(name) ? name : (item != null ? item.Transform.name : string.Empty);
            IsInHand = true;
            IsStashed = false;
        }

        public void Clear()
        {
            Item = null;
            ItemName = string.Empty;
            IsInHand = false;
            IsStashed = false;
        }
    }
}
