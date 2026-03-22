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
            < Protocol18Handler.MC_1_20_6_Version => new EntityMetadataPalette1194(),  // 1.19.4 - 1.20.4
            <= Protocol18Handler.MC_1_21_4_Version => new EntityMetadataPalette1206(),  // 1.20.6 - 1.21.4
            <= Protocol18Handler.MC_1_21_7_Version => new EntityMetadataPalette1215(),  // 1.21.5 - 1.21.8
            <= Protocol18Handler.MC_1_21_9_Version => new EntityMetadataPalette1219(),  // 1.21.9 - 1.21.10
            <= Protocol18Handler.MC_1_21_11_Version => new EntityMetadataPalette12111(), // 1.21.11
            <= Protocol18Handler.MC_26_1_Version => new EntityMetadataPalette261(),    // 26.1
            _ => throw new NotImplementedException()
        };
    }
}