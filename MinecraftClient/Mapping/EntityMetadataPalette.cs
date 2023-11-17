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
        return protocolVersion switch
        {
            <= Protocol18Handler.MC_1_8_Version => new EntityMetadataPalette18(),       // 1.8
            <= Protocol18Handler.MC_1_12_2_Version => new EntityMetadataPalette1122(),  // 1.9 - 1.12.2
            <= Protocol18Handler.MC_1_19_2_Version => new EntityMetadataPalette1191(),  // 1.13 - 1.19.2
            <= Protocol18Handler.MC_1_19_3_Version => new EntityMetadataPalette1193(),  // 1.19.3
            <= Protocol18Handler.MC_1_20_2_Version => new EntityMetadataPalette1194(),  // 1.19.4 - 1.20.2 +
            _ => throw new NotImplementedException()
        };
    }
}