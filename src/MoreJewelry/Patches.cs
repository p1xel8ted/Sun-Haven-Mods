namespace MoreJewelry;

/// <summary>
/// Contains Harmony patches for modifying and extending the functionality of various game methods.
/// </summary>
[Harmony]
public static class Patches
{

    /// <summary>
    /// A list of custom gear slots added by the mod.
    /// </summary>
    public static List<Slot> GearSlots = [];

    /// <summary>
    /// After the game loads inventory data, check if saved items exist for custom slots
    /// and restore them. The game's LoadInventory skips keys >= Items.Count, so we
    /// extend Items and manually load the custom slot data from the save.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.LoadPlayerInventory))]
    private static void PlayerInventory_LoadPlayerInventory(PlayerInventory __instance)
    {
        var savedItems = SingletonBehaviour<GameSave>.Instance?.CurrentSave?.characterData?.Items;
        if (savedItems == null) return;

        // Extend Items list if needed
        while (__instance.Items.Count <= Const.NewAmuletSlotTwo)
        {
            __instance.Items.Add(new SlotItemData(new NormalItem(0), 0, __instance.Items.Count, null));
        }

        // Manually restore items the game skipped due to bounds check
        foreach (var kvp in savedItems)
        {
            if (kvp.Key < Const.NewRingSlotOne || kvp.Key > Const.NewAmuletSlotTwo) continue;
            if (kvp.Value?.Item == null || kvp.Value.Item.ID() == 0) continue;
            if (!PSS.Database.ValidID(kvp.Value.Item.ID())) continue;

            __instance.Items[kvp.Key].item = kvp.Value.Item;
            __instance.Items[kvp.Key].id = kvp.Value.Item.ID();
            __instance.Items[kvp.Key].amount = kvp.Value.Amount;
        }
    }

    // Save marker: which slot base the custom jewelry is currently stored at, so existing
    // saves can be migrated when a game update shifts the native slot count.
    private const string SlotBaseProgressKey = "MoreJewelrySlotBase";

    // Capture the game's native slot count the moment PlayerInventory finishes native setup,
    // before we extend Items. Our custom slots start right after it, so a game update that
    // appends a native slot (the stable slot added at index 66) can't collide with ours.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.SetUpInventoryData))]
    private static void PlayerInventory_SetUpInventoryData(PlayerInventory __instance)
    {
        if (__instance.Items != null && __instance.Items.Count > 0)
        {
            Const.BaseSlot = __instance.Items.Count;
        }
    }

    // Before the game loads the saved inventory, move jewelry stored at the old slot base to
    // the current one. Without this, after the game added its stable slot at 66, saved rings/
    // keepsakes/amulets land in the wrong slots or get read as the game's stable item.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.LoadPlayerInventory))]
    private static void PlayerInventory_LoadPlayerInventory_Migrate(PlayerInventory __instance)
    {
        try
        {
            // Make sure native setup has run so Const.BaseSlot is known before touching the save.
            if (!__instance._initialized)
            {
                __instance.SetUpInventoryData();
            }

            MigrateJewelrySlots(Const.BaseSlot);
        }
        catch (Exception e)
        {
            Plugin.LOG.LogError($"Jewelry slot migration failed: {e}");
        }
    }

    private static void MigrateJewelrySlots(int newBase)
    {
        var gameSave = SingletonBehaviour<GameSave>.Instance;
        var items = gameSave?.CurrentSave?.characterData?.Items;
        if (items == null) return;

        var oldBase = gameSave.TryGetProgressIntCharacter(SlotBaseProgressKey, out var stored) && stored > 0
            ? stored
            : 66; // pre-update layout, before the base became dynamic

        if (oldBase == newBase) return;

        // Collect the six jewelry entries at the old base. Only move actual jewelry so a
        // stable-slot item sitting at old index 66 is left where the game expects it.
        var moved = new Dictionary<short, InventoryItemData>();
        for (var offset = 0; offset < 6; offset++)
        {
            var oldKey = (short)(oldBase + offset);
            if (items.TryGetValue(oldKey, out var data) && data?.Item is ArmorItem && data.Item.ID() != 0)
            {
                moved[(short)(newBase + offset)] = data;
            }
        }

        if (moved.Count > 0)
        {
            // Remove all old jewelry keys first, then place at the new keys — safe even when
            // the old and new ranges overlap.
            for (var offset = 0; offset < 6; offset++)
            {
                var oldKey = (short)(oldBase + offset);
                if (items.TryGetValue(oldKey, out var data) && data?.Item is ArmorItem)
                {
                    items.Remove(oldKey);
                }
            }

            foreach (var kvp in moved)
            {
                items[kvp.Key] = kvp.Value;
            }

            // Logged unconditionally (not via Utils.Log, which is gated behind the Debug
            // setting) - a one-time save migration is worth seeing in every log.
            Plugin.LOG.LogInfo($"Migrated {moved.Count} jewelry item(s) from slot base {oldBase} to {newBase}.");
        }

        gameSave.SetProgressIntCharacter(SlotBaseProgressKey, newBase);
    }

    // Items is extended to cover the custom slots above so stat patches (GetStat/
    // EquipNonVisualArmor) can see equipped jewelry before the player ever opens their
    // inventory. _slots is only extended to match inside OpenMajorPanel below. If a chest is
    // closed before then, Chest.SaveInventory → LoadPlayerInventory → LoadInventory iterates
    // Items.Count keys and calls SetupItemIcon for the custom slots, which don't exist in
    // _slots yet. The original IndexOutOfRangeException
    // aborted SaveInventory before data.inUse = false, leaving the chest permanently locked
    // and the UIHandler in a broken state. Skip out-of-range slots; OpenMajorPanel will
    // re-call SetupItemIcon for them once the UI exists.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.SetupItemIcon))]
    private static bool Inventory_SetupItemIcon(Inventory __instance, int slot, ref ItemIcon __result)
    {
        if (__instance._slots == null || slot < 0 || slot >= __instance._slots.Length || __instance._slots[slot] == null)
        {
            __result = null;
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.OpenMajorPanel))]
    private static void PlayerInventory_Initialize(PlayerInventory __instance, int panelIndex)
    {
        if (panelIndex != 0) return;

        if (UI.SlotsCreated && UI.GearPanel != null)
        {
            Utils.Log("Slots already created. Skipping slot creation etc.");
            return;
        }

        // Cache inventory root transform for dynamic child lookups (no hardcoded paths).
        // PlayerInventory may be mounted at any level, so search from the hierarchy root.
        var playerRoot = __instance.transform.root;
        var uiInventory = playerRoot.FindFirstChildByName("UI_Inventory");
        UI.InventoryTransform = uiInventory?.Find("Inventory");
        if (UI.InventoryTransform == null || !UI.InventoryTransform.gameObject.activeInHierarchy)
        {
            // Hierarchy isn't fully active yet — bail out and let the next OpenMajorPanel call retry.
            return;
        }

        // Cache original keepsake/amulet slot references before creating custom slots.
        // Find by ArmorType instead of hardcoded paths to survive game hierarchy changes.
        var originalKeepsake = __instance._slots.FirstOrDefault(s =>
            s.acceptableArmorType == ArmorType.Keepsake);
        var originalAmulet = __instance._slots.FirstOrDefault(s =>
            s.acceptableArmorType == ArmorType.Amulet);

        if (originalKeepsake != null && originalAmulet != null)
        {
            UI.OriginalKeepsakeSlot = originalKeepsake.gameObject;
            UI.OriginalAmuletSlot = originalAmulet.gameObject;
        }
        else
        {
            Plugin.LOG.LogWarning("Could not find original keepsake/amulet slots by ArmorType. Controller navigation may not work.");
        }

        UI.InitializeGearPanel();

        UI.CreateSlots(__instance, ArmorType.Ring, 2);
        UI.CreateSlots(__instance, ArmorType.Keepsake, 2);
        UI.CreateSlots(__instance, ArmorType.Amulet, 2);

        // Register custom slots in Items list and _slots array
        foreach (var gearSlot in GearSlots)
        {
            while (__instance.Items.Count <= gearSlot.slotNumber)
            {
                __instance.Items.Add(new SlotItemData(new NormalItem(0), 0, __instance.Items.Count, null));
            }

            __instance.Items[gearSlot.slotNumber] = new SlotItemData(new NormalItem(0), 0, gearSlot.slotNumber, gearSlot);
            gearSlot.inventory = __instance;
        }

        // Add to _slots array so SetupItemIcon can find them. Do NOT bump maxSlots — vanilla uses it as the stashable range (0-49); widening it breaks the chest "Same"/"All" stash buttons.
        __instance._slots = __instance._slots.Concat(GearSlots.Where(s => !__instance._slots.Contains(s))).ToArray();

        // Restore saved items into custom slots and create their icons
        var savedItems = SingletonBehaviour<GameSave>.Instance?.CurrentSave?.characterData?.Items;
        if (savedItems != null)
        {
            foreach (var kvp in savedItems)
            {
                if (kvp.Key < Const.NewRingSlotOne || kvp.Key > Const.NewAmuletSlotTwo) continue;
                if (kvp.Value?.Item == null || kvp.Value.Item.ID() == 0) continue;
                if (!PSS.Database.ValidID(kvp.Value.Item.ID())) continue;

                __instance.Items[kvp.Key].item = kvp.Value.Item;
                __instance.Items[kvp.Key].id = kvp.Value.Item.ID();
                __instance.Items[kvp.Key].amount = kvp.Value.Amount;
                __instance.SetupItemIcon(kvp.Key);
            }
        }

        // Parent GearPanel to CharacterPanel/Slots — derived from original slot's parent.
        var characterPanelSlots = UI.OriginalKeepsakeSlot != null ? UI.OriginalKeepsakeSlot.transform.parent : null;
        if (characterPanelSlots != null)
        {
            UI.GearPanel.transform.SetParent(characterPanelSlots, true);
            UI.GearPanel.transform.SetAsLastSibling();
            UI.GearPanel.transform.localPosition = Const.ShowPosition;
        }
        else
        {
            Plugin.LOG.LogError("Character Panel Slots not found. Please report this.");
        }

        UI.UpdatePanelVisibility();
        UI.UpdateNavigationElements();
        UI.SlotsCreated = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MouseAndControllerInputModule), nameof(MouseAndControllerInputModule.GetMousePointerEventData))]
    private static void MouseAndControllerInputModule_GetMousePointerEventData(ref PointerInputModule.MouseState __result)
    {
        if (!MouseVisualManager.UsingController) return;
        if (EventSystem.current == null) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (!ShouldUseSelectedObjectForControllerPress(selected)) return;

        var leftButtonState = __result.GetButtonState(PointerEventData.InputButton.Left);
        var buttonData = leftButtonState?.eventData?.buttonData;
        if (buttonData == null) return;

        var raycast = buttonData.pointerCurrentRaycast;
        raycast.gameObject = selected;
        buttonData.pointerCurrentRaycast = raycast;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MouseAndControllerInputModule), nameof(MouseAndControllerInputModule.ProcessMousePress))]
    private static void MouseAndControllerInputModule_ProcessMousePress(PointerInputModule.MouseButtonEventData data)
    {
        if (!MouseVisualManager.UsingController) return;
        if (EventSystem.current == null) return;
        if (data?.buttonData == null || !data.PressedThisFrame()) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (!ShouldUseSelectedObjectForControllerPress(selected)) return;

        var buttonData = data.buttonData;
        var raycast = buttonData.pointerCurrentRaycast;
        raycast.gameObject = selected;
        buttonData.pointerCurrentRaycast = raycast;
    }

    // Forward controller Submit from a Slot to its ItemIcon when the ItemIcon was bypassed
    // by navigation. Without this, pressing Submit on a slot containing an item does nothing
    // because the game expects the ItemIcon to be the focused selectable.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Slot), nameof(Slot.OnSubmit), new[] { typeof(BaseEventData) })]
    private static void Slot_OnSubmit(Slot __instance, BaseEventData eventData)
    {
        if (!MouseVisualManager.UsingController) return;
        if (Inventory.CurrentItemIcon != null) return;

        var itemIcon = __instance.GetComponentInChildren<ItemIcon>();
        if (itemIcon == null || itemIcon.item == null || itemIcon.item.ID() == 0) return;

        itemIcon.ClickIcon(UIMoveType.Submit);
    }

    private static bool ShouldUseSelectedObjectForControllerPress(GameObject selected)
    {
        if (selected == null || !selected.activeInHierarchy) return false;

        if (UI.ToggleArrowInstance != null && (selected == UI.ToggleArrowInstance || selected.transform.IsChildOf(UI.ToggleArrowInstance.transform)))
        {
            return true;
        }

        if (Patches.GearSlots.Count == 0) return false;

        var slot = selected.GetComponentInParent<Slot>();
        return slot != null && Patches.GearSlots.Contains(slot);
    }
    
    /// <summary>
    /// Harmony prefix patch for the SwapItems method of the Inventory class.
    /// </summary>
    /// <param name="__instance">The instance of the Inventory being patched.</param>
    /// <param name="slot1">The first slot involved in the swap operation.</param>
    /// <remarks>
    /// This method modifies the behavior of the item swapping process in the inventory.
    /// It includes custom logic for handling the swapping of items in keepsake, amulet, and ring slots.
    /// <para>The method checks if the main slot for each item type is filled and attempts to swap with an alternate empty slot if available.</para>
    /// <para>Uses <see cref="Utils.SlotFilled"/> to check if a slot is filled and <see cref="Utils.GetSlotName"/> for logging purposes.</para>
    /// </remarks>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.SwapItems))]
    private static void Inventory_SwapItems(ref Inventory __instance, ref int slot1)
    {
        if (!Plugin.UseAdjustedEquipping.Value)
        {
            Utils.Log("Adjusted equipping disabled. Skipping slot swapping logic.");
            return;
        }
        
        // Helper method to handle slot swapping
        var inv = __instance;

        // Keepsake slot handling
        TrySwapSlot(ref slot1, Const.MainKeepsakeSlot, Const.NewKeepsakeSlotOne, Const.NewKeepsakeSlotTwo);

        // Amulet slot handling
        TrySwapSlot(ref slot1, Const.MainAmuletSlot, Const.NewAmuletSlotOne, Const.NewAmuletSlotTwo);

        // Ring slot handling
        TrySwapSlot(ref slot1, Const.MainRingSlot, Const.SecondaryRingSlot, Const.NewRingSlotOne, Const.NewRingSlotTwo);
        return;

        void TrySwapSlot(ref int slot, int mainSlot, params int[] alternateSlots)
        {
            if (slot != mainSlot || !Utils.SlotFilled(inv, mainSlot)) return;
            foreach (var altSlot in alternateSlots)
            {
                if (Utils.SlotFilled(inv, altSlot)) continue;
                slot = altSlot;
                Utils.Log($"Redirecting to empty slot: {Utils.GetSlotName(altSlot)}");
                return;
            }
            Utils.Log($"No empty slots found for {Utils.GetSlotName(mainSlot)}. Redirect aborted.");
        }
    }
    
    /// <summary>
    /// Harmony postfix patch for PlayerInventory's GetStat method.
    /// </summary>
    /// <param name="__instance">The instance of <see cref="PlayerInventory"/> being patched.</param>
    /// <param name="stat">The type of stat being retrieved.</param>
    /// <param name="__result">The result value of the original GetStat method.</param>
    /// <remarks>
    /// Modifies the stat calculation to include custom gear slots. This method is called every frame.
    /// </remarks>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.GetStat))]
    public static void PlayerInventory_GetStat(ref PlayerInventory __instance, StatType stat, ref float __result)
    {
        if (Plugin.MakeSlotsStorageOnly.Value) return;
        if (__instance.Items.Count <= Const.NewAmuletSlotTwo) return;
        __result += __instance.GetStatValueFromSlot(ArmorType.Ring, Const.NewRingSlotOne, 2, stat);
        __result += __instance.GetStatValueFromSlot(ArmorType.Ring, Const.NewRingSlotTwo, 3, stat);
        __result += __instance.GetStatValueFromSlot(ArmorType.Keepsake, Const.NewKeepsakeSlotOne, 1, stat);
        __result += __instance.GetStatValueFromSlot(ArmorType.Keepsake, Const.NewKeepsakeSlotTwo, 2, stat);
        __result += __instance.GetStatValueFromSlot(ArmorType.Amulet, Const.NewAmuletSlotOne, 1, stat);
        __result += __instance.GetStatValueFromSlot(ArmorType.Amulet, Const.NewAmuletSlotTwo, 2, stat);
    }

    /// <summary>
    /// Harmony postfix patch for PlayerInventory's Awake method.
    /// </summary>
    /// <param name="__instance">The instance of <see cref="PlayerInventory"/> being patched.</param>
    /// <remarks>
    /// Attaches additional actions to the OnInventoryUpdated event for cleaning up armor dictionaries. This event is triggered when
    /// the player opens or closes their inventory.
    /// </remarks>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.Awake))]
    public static void PlayerInventory_Awake(ref PlayerInventory __instance)
    {
        if (UI.ActionAttached) return;
        UI.ActionAttached = true;

        Utils.Log("Attaching RemoveNullValuesAndLogFromDictionary action to OnInventoryUpdated event.");
        var instance = __instance;
        __instance.OnInventoryUpdated += () =>
        {
            if (instance == null || instance.currentRealArmor == null || instance.currentArmor == null)
            {
                Plugin.LOG.LogError("OnInventoryUpdated: PlayerInventory instance is null. It is advised to restart your game.");
                return;
            }
            Utils.Log("OnInventoryUpdated: Cleaning armor dictionaries.");
            Utils.RemoveNullValuesAndLogFromDictionary(instance.currentRealArmor, "CurrentRealArmor");
            Utils.RemoveNullValuesAndLogFromDictionary(instance.currentArmor, "CurrentArmor");
        };
    }


    /// <summary>
    /// Harmony postfix patch for PlayerInventory's LateUpdate method.
    /// </summary>
    /// <param name="__instance">The instance of <see cref="PlayerInventory"/> being patched.</param>
    /// <remarks>
    /// Ensures custom gear slots are equipped with non-visual armor items. This is where the game begins to calculate stats the gear provides.
    /// </remarks>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.LateUpdate))]
    private static void PlayerInventory_EquipNonVisualArmor(ref PlayerInventory __instance)
    {
        if (__instance.Items.Count <= Const.NewAmuletSlotTwo) return;
        __instance.EquipNonVisualArmor(Const.NewRingSlotOne, ArmorType.Ring, 2);
        __instance.EquipNonVisualArmor(Const.NewRingSlotTwo, ArmorType.Ring, 3);
        __instance.EquipNonVisualArmor(Const.NewKeepsakeSlotOne, ArmorType.Keepsake, 1);
        __instance.EquipNonVisualArmor(Const.NewKeepsakeSlotTwo, ArmorType.Keepsake, 2);
        __instance.EquipNonVisualArmor(Const.NewAmuletSlotOne, ArmorType.Amulet, 1);
        __instance.EquipNonVisualArmor(Const.NewAmuletSlotTwo, ArmorType.Amulet, 2);
    }

    /// <summary>
    /// Harmony postfix patch for MainMenuController's EnableMenu method.
    /// </summary>
    /// <param name="__instance">The instance of <see cref="MainMenuController"/> being patched.</param>
    /// <param name="menu">The menu GameObject being enabled.</param>
    /// <remarks>
    /// Resets the mod to its initial state when certain menus are enabled.
    /// </remarks>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MainMenuController), nameof(MainMenuController.EnableMenu))]
    private static void MainMenuController_EnableMenu(ref MainMenuController __instance, ref GameObject menu)
    {
        if (menu == __instance.homeMenu || menu == __instance.newCharacterMenu || menu == __instance.loadCharacterMenu || menu == __instance.singlePlayerMenu || menu == __instance.multiplayerMenu)
        {
            Utils.ResetMod();
        }
    }


}
