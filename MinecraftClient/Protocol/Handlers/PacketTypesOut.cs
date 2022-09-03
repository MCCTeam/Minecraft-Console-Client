﻿using System;
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
        AdvancementTab,              //
        Animation,                   //
        ChatCommand,                 // Added in 1.19
        ChatMessage,                 //
        ChatPreview,                 // Added in 1.19
        ClickWindow,                 //
        ClickWindowButton,           //
        ClientSettings,              //
        ClientStatus,                //
        CloseWindow,                 //
        CraftRecipeRequest,          //
        CreativeInventoryAction,     //
        EditBook,                    //
        EnchantItem,                 // For 1.13.2 or below
        EntityAction,                //
        EntityNBTRequest,            //
        GenerateStructure,           // Added in 1.16
        HeldItemChange,              //
        InteractEntity,              //
        KeepAlive,                   //
        LockDifficulty,              //
        MessageAcknowledgment,       // Added in 1.19.1 (1.19.2)
        NameItem,                    //
        PickItem,                    //
        PlayerAbilities,             //
        PlayerBlockPlacement,        //
        PlayerDigging,               //
        PlayerMovement,              //
        PlayerPosition,              //
        PlayerPositionAndRotation,   //
        PlayerRotation,              //
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
        UseItem,                     //
        VehicleMove,                 //
        WindowConfirmation,          //
    }
}
