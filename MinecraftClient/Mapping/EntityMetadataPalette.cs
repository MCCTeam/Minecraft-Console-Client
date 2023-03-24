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
        if (protocolVersion < Protocol18Handler.MC_1_9_Version)
            throw new NotImplementedException();
        else if (protocolVersion <= Protocol18Handler.MC_1_12_2_Version)
            return new EntityMetadataPalette1122(); // 1.9 - 1.12.2
        else if (protocolVersion <= Protocol18Handler.MC_1_19_2_Version)
            return new EntityMetadataPalette1191(); // 1.13 - 1.19.2
        else if (protocolVersion <= Protocol18Handler.MC_1_19_3_Version)
            return new EntityMetadataPalette1193(); // 1.19.3
        else if (protocolVersion <= Protocol18Handler.MC_1_19_4_Version)
            return new EntityMetadataPalette1194(); // 1.19.4
        else 
            throw new NotImplementedException();
    }
}