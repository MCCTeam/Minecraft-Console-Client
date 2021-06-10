using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Outgoing packet types
    /// </summary>
    public enum PacketTypesOut
    {
        TeleportConfirm,
        QueryBlockNBT,
        SetDifficulty,
        ChatMessage,
        ClientStatus,
        ClientSettings,
        TabComplete,
        WindowConfirmation,
        ClickWindowButton,
        ClickWindow,
        CloseWindow,
        PluginMessage,
        EditBook,
        EntityNBTRequest,
        InteractEntity,
        KeepAlive,
        LockDifficulty,
        PlayerPosition,
        PlayerPositionAndRotation,
        PlayerRotation,
        PlayerMovement,
        VehicleMove,
        SteerBoat,
        PickItem,
        CraftRecipeRequest,
        PlayerAbilities,
        PlayerDigging,
        EntityAction,
        SteerVehicle,
        RecipeBookData,
        NameItem,
        ResourcePackStatus,
        AdvancementTab,
        SelectTrade,
        SetBeaconEffect,
        HeldItemChange,
        UpdateCommandBlock,
        UpdateCommandBlockMinecart,
        CreativeInventoryAction,
        UpdateJigsawBlock,
        UpdateStructureBlock,
        UpdateSign,
        Animation,
        Spectate,
        PlayerBlockPlacement,
        UseItem,
        Pong,
        PrepareCraftingGrid, // For 1.12 - 1.12.1 only
        EnchantItem, // For 1.13.2 or below
        GenerateStructure, // Added in 1.16
        SetDisplayedRecipe, // Added in 1.16.2
        SetRecipeBookState, // Added in 1.16.2
        Unknown //  For old version packet that have been removed and not used by mcc 
    }
}
