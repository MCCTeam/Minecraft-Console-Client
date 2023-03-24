using MinecraftClient.Mapping.EntityMetadataPalettes;
using MinecraftClient.Protocol.Handlers;
using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping;

public abstract class EntityMetadataPalette
{
    public abstract Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList();

    public EntityMetaDataType GetDataType(int typeId)
    {
        return GetEntityMetadataMappingsList()[typeId];
    }

    public static EntityMetadataPalette GetPalette(int protocolVersion)
    {
        if (protocolVersion < Protocol18Handler.MC_1_9_1_Version)
            return new EntityMetadataPalette19();
        else if (protocolVersion <= Protocol18Handler.MC_1_11_2_Version)
            return new EntityMetadataPalette111();
        else if (protocolVersion <= Protocol18Handler.MC_1_13_2_Version)
            return new EntityMetadataPalette113();
        else if (protocolVersion <= Protocol18Handler.MC_1_14_Version)
            return new EntityMetadataPalette114();
        else if (protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
            return new EntityMetadataPalette1191();
        else if (protocolVersion <= Protocol18Handler.MC_1_19_3_Version)
            return new EntityMetadataPalette1193();
        else if (protocolVersion <= Protocol18Handler.MC_1_19_4_Version)
            return new EntityMetadataPalette1194();
        else 
            throw new NotImplementedException();
    }
}