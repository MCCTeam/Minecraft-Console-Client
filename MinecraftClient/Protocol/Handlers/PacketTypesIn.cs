namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Incomming packet types
    /// </summary>
    public enum PacketTypesIn
    {
        AcknowledgePlayerDigging,   //
        ActionBar,                  //
        Advancements,               //
        AttachEntity,               //
        BlockAction,                //
        BlockBreakAnimation,        //
        BlockChange,                //
        BlockChangedAck,            // Added in 1.19
        BlockEntityData,            //
        BossBar,                    //
        Camera,                     //
        ChangeGameState,            //
        ChatMessage,                //
        ChatPreview,                // Added in 1.19
        ChatSuggestions,            // Added in 1.19.1 (1.19.2)
        ChunkData,                  //
        ClearTiles,                 //
        CloseWindow,                //
        CollectItem,                //
        CombatEvent,                //
        CraftRecipeResponse,        //
        DeathCombatEvent,           //
        DeclareCommands,            //
        DeclareRecipes,             //
        DestroyEntities,            //
        Disconnect,                 //
        DisplayScoreboard,          //
        Effect,                     //
        EndCombatEvent,             //
        EnterCombatEvent,           //
        EntityAnimation,            //
        EntityEffect,               //
        EntityEquipment,            //
        EntityHeadLook,             //
        EntityMetadata,             //
        EntityMovement,             //
        EntityPosition,             //
        EntityPositionAndRotation,  //
        EntityProperties,           //
        EntityRotation,             //
        EntitySoundEffect,          //
        EntityStatus,               //
        EntityTeleport,             //
        EntityVelocity,             //
        Explosion,                  //
        FacePlayer,                 //
        HeldItemChange,             //
        HideMessage,                // Added in 1.19.1 (1.19.2)
        InitializeWorldBorder,      //
        JoinGame,                   //
        KeepAlive,                  //
        MapChunkBulk,               // For 1.8 or below
        MapData,                    //
        MessageHeader,              // Added in 1.19.1 (1.19.2)
        MultiBlockChange,           //
        NamedSoundEffect,           //
        NBTQueryResponse,           //
        OpenBook,                   //
        OpenHorseWindow,            //
        OpenSignEditor,             //
        OpenWindow,                 //
        Particle,                   //
        Ping,                       //
        PlayerAbilities,            //
        PlayerInfo,                 //
        PlayerListHeaderAndFooter,  //
        PlayerPositionAndLook,      //
        PluginMessage,              //
        RemoveEntityEffect,         //
        ResourcePackSend,           //
        Respawn,                    //
        ScoreboardObjective,        //
        SelectAdvancementTab,       //
        ServerData,                 // Added in 1.19
        ServerDifficulty,           //
        SetCompression,             // For 1.8 or below
        SetCooldown,                //
        SetDisplayChatPreview,      // Added in 1.19
        SetExperience,              //
        SetPassengers,              //
        SetSlot,                    //
        SetTitleSubTitle,           //
        SetTitleText,               //
        SetTitleTime,               //
        SkulkVibrationSignal,       //
        SoundEffect,                //
        SpawnEntity,                //
        SpawnExperienceOrb,         //
        SpawnLivingEntity,          //
        SpawnPainting,              //
        SpawnPlayer,                //
        SpawnPosition,              //
        SpawnWeatherEntity,         //
        Statistics,                 //
        StopSound,                  //
        SystemChat,                 // Added in 1.19
        TabComplete,                //
        Tags,                       //
        Teams,                      //
        TimeUpdate,                 //
        Title,                      //
        TradeList,                  //
        Unknown,                    // For old version packet that have been removed and not used by mcc 
        UnloadChunk,                //
        UnlockRecipes,              //
        UpdateEntityNBT,            // For 1.8 or below
        UpdateHealth,               //
        UpdateLight,                //
        UpdateScore,                //
        UpdateSign,                 // For 1.8 or below
        UpdateSimulationDistance,   //
        UpdateViewDistance,         //
        UpdateViewPosition,         //
        UseBed,                     // For 1.13.2 or below
        VehicleMove,                //
        WindowConfirmation,         //
        WindowItems,                //
        WindowProperty,             //
        WorldBorder,                //
        WorldBorderCenter,          //
        WorldBorderLerpSize,        //
        WorldBorderSize,            //
        WorldBorderWarningDelay,    //
        WorldBorderWarningReach,    //
    }
}
