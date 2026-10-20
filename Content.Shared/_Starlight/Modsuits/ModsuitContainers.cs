namespace Content.Shared._Starlight.Modsuits;

public static class ModsuitContainers
{
    private static readonly Dictionary<ModsuitPartType, string> PartContainers = new()
    {
        {ModsuitPartType.Helmet, "helmet"},
        {ModsuitPartType.Chestplate, "chestplate"},
        {ModsuitPartType.Gauntlets, "gauntlets"},
        {ModsuitPartType.Boots, "boots"},
    };
    private static readonly Dictionary<ModsuitPartType, string> StorageContainers = new()
    {
        {ModsuitPartType.Gauntlets, "storedGloves"},
        {ModsuitPartType.Boots, "storedBoots"},
    };
    private static readonly Dictionary<ModsuitPartType, string> InventorySlots = new()
    {
        {ModsuitPartType.Helmet, "head"},
        {ModsuitPartType.Chestplate, "outerClothing"},
        {ModsuitPartType.Gauntlets, "gloves"},
        {ModsuitPartType.Boots, "shoes"},
    };

    private static readonly Dictionary<ModsuitPartType, string> PartNames = new()
    {
        { ModsuitPartType.Helmet, "helmet" },
        { ModsuitPartType.Chestplate, "chestplate" },
        { ModsuitPartType.Gauntlets, "gauntlets" },
        { ModsuitPartType.Boots, "boots" },
    };

    public static readonly Dictionary<ModsuitPartType, string> ProtectionSlots = new()
    {
        { ModsuitPartType.Helmet, "helmet" },
        { ModsuitPartType.Chestplate, "chestplate" },
    };
    public static string GetPartContainer(ModsuitPartType part)
        => PartContainers[part];
    public static string GetInventorySlot(ModsuitPartType part)
        => InventorySlots[part];
    public static bool TryGetStorageContainer(ModsuitPartType part, out string container)
        => StorageContainers.TryGetValue(part, out container!);
    public static string GetPartName(ModsuitPartType part)
        => PartNames[part];
}
