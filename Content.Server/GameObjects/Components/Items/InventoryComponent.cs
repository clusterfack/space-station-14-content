using Content.Server.Interfaces.GameObjects;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.Container;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Utility;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects
{
    public class InventoryComponent : Component
    {
        public override string Name => "Inventory";

        private Dictionary<string, InventorySlot> slots = new Dictionary<string, InventorySlot>();
        private TransformComponent transform;
        // TODO: Make this container unique per-slot.
        private IContainer container;

        public override void Initialize()
        {
            transform = Owner.GetComponent<TransformComponent>();
            container = Container.Create("inventory", Owner);
            base.Initialize();
        }

        public override void OnRemove()
        {
            foreach (var slot in slots.Keys)
            {
                RemoveSlot(slot);
            }
            transform = null;
            container = null;
            base.OnRemove();
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode<YamlSequenceNode>("slots", out var slotsNode))
            {
                foreach (var node in slotsNode)
                {
                    AddSlot(node.AsString());
                }
            }
            base.LoadParameters(mapping);
        }

        /// <summary>
        ///     Gets the item in the specified slot.
        /// </summary>
        /// <param name="slot">The slot to get the item for.</param>
        /// <returns>Null if the slot is empty, otherwise the item.</returns>
        public IItemComponent Get(string slot)
        {
            return _GetSlot(slot).Item;
        }

        /// <summary>
        ///     Gets the slot with specified name.
        ///     This gets the slot, NOT the item contained therein.
        /// </summary>
        /// <param name="slot">The name of the slot to get.</param>
        public InventorySlot GetSlot(string slot)
        {
            return slots[slot];
        }

        // Private version that returns our concrete implementation.
        private InventorySlot _GetSlot(string slot)
        {
            return slots[slot];
        }

        /// <summary>
        ///     Puts an item in a slot.
        /// </summary>
        /// <remarks>
        ///     This will fail if there is already an item in the specified slot.
        /// </remarks>
        /// <param name="slot">The slot to put the item in.</param>
        /// <param name="item">The item to insert into the slot.</param>
        /// <returns>True if the item was successfully inserted, false otherwise.</returns>
        public bool Insert(string slot, IItemComponent item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "An item must be passed. To remove an item from a slot, use Drop()");
            }

            var inventorySlot = _GetSlot(slot);
            if (!CanInsert(slot, item) || !container.Insert(item.Owner))
            {
                return false;
            }

            inventorySlot.Item = item;
            item.EquippedToSlot(inventorySlot);
            return true;
        }

        /// <summary>
        ///     Checks whether an item can be put in the specified slot.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the item can be inserted into the specified slot.</returns>
        public bool CanInsert(string slot, IItemComponent item)
        {
            var inventorySlot = _GetSlot(slot);
            return inventorySlot.Item == null && container.CanInsert(item.Owner);
        }

        /// <summary>
        ///     Drops the item in a slot.
        /// </summary>
        /// <param name="slot">The slot to drop the item from.</param>
        /// <returns>True if an item was dropped, false otherwise.</returns>
        public bool Drop(string slot)
        {
            if (!CanDrop(slot))
            {
                return false;
            }

            var inventorySlot = _GetSlot(slot);
            var item = inventorySlot.Item;
            if (!container.Remove(item.Owner))
            {
                return false;
            }

            item.RemovedFromSlot();
            inventorySlot.Item = null;

            // TODO: The item should be dropped to the container our owner is in, if any.
            var itemTransform = item.Owner.GetComponent<TransformComponent>();
            itemTransform.LocalPosition = transform.LocalPosition;
            return true;
        }

        /// <summary>
        ///     Checks whether an item can be dropped from the specified slot.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <returns>
        ///     True if there is an item in the slot and it can be dropped, false otherwise.
        /// </returns>
        public bool CanDrop(string slot)
        {
            var inventorySlot = _GetSlot(slot);
            var item = inventorySlot.Item;
            return item != null && container.CanRemove(item.Owner);
        }

        /// <summary>
        ///     Adds a new slot to this inventory component.
        /// </summary>
        /// <param name="slot">The name of the slot to add.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the slot with specified name already exists.
        /// </exception>
        public InventorySlot AddSlot(string slot)
        {
            if (HasSlot(slot))
            {
                throw new InvalidOperationException($"Slot '{slot}' already exists.");
            }

            return slots[slot] = new InventorySlot(slot, this);
        }

        /// <summary>
        ///     Removes a slot from this inventory component.
        /// </summary>
        /// <remarks>
        ///     If the slot contains an item, the item is dropped.
        /// </remarks>
        /// <param name="slot">The name of the slot to remove.</param>
        public void RemoveSlot(string slot)
        {
            if (!HasSlot(slot))
            {
                throw new InvalidOperationException($"Slow '{slot}' does not exist.");
            }

            if (Get(slot) != null && !Drop(slot))
            {
                // TODO: Handle this potential failiure better.
                throw new InvalidOperationException("Unable to remove slot as the contained item could not be dropped");
            }

            slots.Remove(slot);
        }

        /// <summary>
        ///     Checks whether a slot with the specified name exists.
        /// </summary>
        /// <param name="slot">The slot name to check.</param>
        /// <returns>True if the slot exists, false otherwise.</returns>
        public bool HasSlot(string slot)
        {
            return slots.ContainsKey(slot);
        }
    }

    public class InventorySlot
    {
        /// <summary>
        /// The name of the slot.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The item contained in the slot, can be null.
        /// </summary>
        public IItemComponent Item { get; set; }

        /// <summary>
        /// The component owning us.
        /// </summary>
        public InventoryComponent Owner { get; }

        public InventorySlot(string name, InventoryComponent owner)
        {
            Name = name;
            Owner = owner;
        }
    }
}
