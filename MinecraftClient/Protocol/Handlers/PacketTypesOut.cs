namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Outgoing packet types
    /// </summary>
    public enum PacketTypesOut
    {
        AcknowledgeConfiguration,    // Added in 1.20.2
        AdvancementTab,              //
        Animation,                   //
        BundleItemSelected,          // Added in 1.21.2
        ChangeContainerSlotState,    // Added in 1.20.3
        ChatCommand,                 // Added in 1.19
        ChatMessage,                 //
        ChatPreview,                 // Added in 1.19
        ChunkBatchReceived,          // Added in 1.20.2
        ClickWindow,                 //
        ClickWindowButton,           //
        ClientSettings,              //
        ClientStatus,                //
        ClientTickEnd,               // Added in 1.21.2
        CloseWindow,                 //
        CraftRecipeRequest,          //
        CreativeInventoryAction,     //
        CookieResponse,              // Added in 1.20.6
        DebugSampleSubscription,     // Added in 1.20.6
        EditBook,                    //
        EnchantItem,                 // For 1.13.2 or below
        EntityAction,                //
        EntityNBTRequest,            //
        GenerateStructure,           // Added in 1.16
        HeldItemChange,              //
        InteractEntity,              //
        KeepAlive,                   //
        KnownDataPacks,              // Added in 1.20.6
        LockDifficulty,              //
        MessageAcknowledgment,       // Added in 1.19.1 (1.19.2)
        NameItem,                    //
        PickItem,                    //
        PickItemFromEntity,          // Added in 1.21.4 (split from PickItem)
        PingRequest,                 // Added in 1.20.2
        PlayerAbilities,             //
        PlayerBlockPlacement,        //
        PlayerDigging,               //
        PlayerLoaded,                // Added in 1.21.4
        PlayerMovement,              //
        PlayerPosition,              //
        PlayerPositionAndRotation,   //
        PlayerRotation,              //
        PlayerSession,               // Added in 1.19.3
        PluginMessage,               //
        Pong,                        //
        PrepareCraftingGrid,         // For 1.12 - 1.12.1 only
        QueryBlockNBT,               //
        RecipeBookData,              //
        ResourcePackStatus,          //
        SelectTrade,                 //
        SetBeaconEffect,             //
        SetDifficulty,               //
        SetDisplayedRecipe,          // Added in 1.16.2
        SetRecipeBookState,          // Added in 1.16.2
        SignedChatCommand,           // Added in 1.20.6
        Spectate,                    //
        SteerBoat,                   //
        SteerVehicle,                //
        TabComplete,                 //
        TeleportConfirm,             //
        Unknown,                     // For old version packet that have been removed and not used by mcc
        UpdateCommandBlock,          //
        UpdateCommandBlockMinecart,  //
        UpdateJigsawBlock,           //
        UpdateSign,                  //
        UpdateStructureBlock,        //
        SetTestBlock,                // Added in 1.21.5
        TestInstanceBlockAction,     // Added in 1.21.5
        UseItem,                     //
        VehicleMove,                 //
        WindowConfirmation,          //
        ChangeGameMode,              // Added in 1.21.6
        CustomClickAction,           // Added in 1.21.6
    }
}
