using System;
using Brigadier.NET.Context;
using MinecraftClient.CommandHandler.ArgumentType;

namespace MinecraftClient.CommandHandler
{
    public static class MccArguments
    {
        public static LocationArgumentType Location()
        {
            return new LocationArgumentType();
        }

        public static Mapping.Location GetLocation<TSource>(CommandContext<TSource> context, string name)
        {
            return context.GetArgument<Mapping.Location>(name);
        }

        public static TupleArgumentType Tuple()
        {
            return new TupleArgumentType();
        }

        public static Tuple<int, int> GetTuple<TSource>(CommandContext<TSource> context, string name)
        {
            return context.GetArgument<Tuple<int, int>>(name);
        }

        public static EntityTypeArgumentType EntityType()
        {
            return new EntityTypeArgumentType();
        }

        public static Mapping.EntityType GetEntityType<TSource>(CommandContext<TSource> context, string name)
        {
            return context.GetArgument<Mapping.EntityType>(name);
        }

        public static ItemTypeArgumentType ItemType()
        {
            return new ItemTypeArgumentType();
        }

        public static Inventory.ItemType GetItemType<TSource>(CommandContext<TSource> context, string name)
        {
            return context.GetArgument<Inventory.ItemType>(name);
        }

        public static BotNameArgumentType BotName()
        {
            return new BotNameArgumentType();
        }

        public static ServerNickArgumentType ServerNick()
        {
            return new ServerNickArgumentType();
        }

        public static AccountNickArgumentType AccountNick()
        {
            return new AccountNickArgumentType();
        }

        public static InventoryIdArgumentType InventoryId()
        {
            return new InventoryIdArgumentType();
        }

        public static InventoryActionArgumentType InventoryAction()
        {
            return new InventoryActionArgumentType();
        }

        public static InventorySlotArgumentType InventorySlot()
        {
            return new InventorySlotArgumentType();
        }

        public static Inventory.WindowActionType GetInventoryAction<TSource>(CommandContext<TSource> context, string name)
        {
            return context.GetArgument<Inventory.WindowActionType>(name);
        }

        public static AutoCraftRecipeNameArgumentType AutoCraftRecipeName()
        {
            return new AutoCraftRecipeNameArgumentType();
        }

        public static FarmerCropTypeArgumentType FarmerCropType()
        {
            return new FarmerCropTypeArgumentType();
        }

        public static ChatBots.Farmer.CropType GetFarmerCropType<TSource>(CommandContext<TSource> context, string name)
        {
            return context.GetArgument<ChatBots.Farmer.CropType>(name);
        }

        public static PlayerNameArgumentType PlayerName()
        {
            return new PlayerNameArgumentType();
        }

        public static MapBotMapIdArgumentType MapBotMapId()
        {
            return new MapBotMapIdArgumentType();
        }

        public static HotbarSlotArgumentType HotbarSlot()
        {
            return new HotbarSlotArgumentType();
        }

        public static ScriptNameArgumentType ScriptName()
        {
            return new ScriptNameArgumentType();
        }
    }
}
