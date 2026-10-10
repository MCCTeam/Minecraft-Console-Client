using Umpk;
using Umpk.Game.Blocks;
using Umpk.Game.Registries;

namespace Mcc.Cli.Tests.Fakes;

/// <summary>
/// A minimal, self-contained <see cref="IBlockDataSource"/> for unit tests that need a real <see cref="BlockState"/> (for example a <see cref="Umpk.Client.Snapshots.SurfaceColumn"/> fixture) but have no live session to read one from.
/// UMPK's only <see cref="IBlockDataSource"/> implementation (<c>RegistryBlockDataSource</c>) is internal to <c>Umpk.Client</c> and built from a version's generated data, so a pure unit test outside that assembly cannot construct a version-accurate state.
/// This fixes five block ids with fabricated but internally-consistent flags instead: exactly what <c>PortedBotLogicTests</c>' surface-sample fixtures need (a standable block, water, lava, air, and a second standable block for height-delta cases).
/// </summary>
public sealed class FakeBlockDataSource : IBlockDataSource
{
    /// <summary>The shared instance; stateless and immutable, so one instance serves every test.</summary>
    public static readonly FakeBlockDataSource Instance = new();

    public const int Air = 0;
    public const int GrassBlock = 1;
    public const int Water = 2;
    public const int Lava = 3;
    public const int Stone = 4;

    private readonly Registry<BlockDefinition> _blocks;
    private readonly BlockFlags[] _flags;

    private FakeBlockDataSource()
    {
        var builder = new RegistryBuilder<BlockDefinition>(RegistryIds.Block, 5);
        builder.Add(Air, Identifier.Minecraft("air"), new BlockDefinition(Air, Air, Air));
        builder.Add(GrassBlock, Identifier.Minecraft("grass_block"), new BlockDefinition(GrassBlock, GrassBlock, GrassBlock));
        builder.Add(Water, Identifier.Minecraft("water"), new BlockDefinition(Water, Water, Water));
        builder.Add(Lava, Identifier.Minecraft("lava"), new BlockDefinition(Lava, Lava, Lava));
        builder.Add(Stone, Identifier.Minecraft("stone"), new BlockDefinition(Stone, Stone, Stone));
        _blocks = builder.Build();

        _flags = new BlockFlags[5];
        _flags[Air] = BlockFlags.Air | BlockFlags.Replaceable;
        _flags[GrassBlock] = BlockFlags.Solid | BlockFlags.BlocksMotion;
        _flags[Water] = BlockFlags.Fluid | BlockFlags.Replaceable;
        _flags[Lava] = BlockFlags.Fluid | BlockFlags.Replaceable;
        _flags[Stone] = BlockFlags.Solid | BlockFlags.BlocksMotion;
    }

    /// <summary>Builds a <see cref="BlockState"/> for one of this source's fixed block ids (also its state id).</summary>
    public BlockState StateFor(int blockId) => new(this, blockId);

    /// <inheritdoc/>
    public Registry<BlockDefinition> Blocks => _blocks;

    /// <inheritdoc/>
    public int UnknownStateId => Air;

    /// <inheritdoc/>
    public bool IsLegacy => false;

    /// <inheritdoc/>
    public int StateCount => _flags.Length;

    /// <inheritdoc/>
    public bool IsValidState(int stateId) => stateId >= 0 && stateId < _flags.Length;

    /// <inheritdoc/>
    public int GetBlockNetworkId(int stateId) => IsValidState(stateId) ? stateId : Air;

    /// <inheritdoc/>
    public int GetDefaultStateId(int blockNetworkId) => blockNetworkId;

    /// <inheritdoc/>
    public BlockFlags GetFlags(int stateId) => IsValidState(stateId) ? _flags[stateId] : BlockFlags.Air;

    /// <inheritdoc/>
    public float GetFriction(int stateId) => 0.6f;

    /// <inheritdoc/>
    public float GetSpeedFactor(int stateId) => 1.0f;

    /// <inheritdoc/>
    public float GetJumpFactor(int stateId) => 1.0f;

    /// <inheritdoc/>
    public IReadOnlyList<string> GetPropertyNames(int stateId) => [];

    /// <inheritdoc/>
    public bool TryGetPropertyValue(int stateId, string propertyName, out string value)
    {
        value = string.Empty;
        return false;
    }

    /// <inheritdoc/>
    public bool TryDecodeLegacy(int stateId, out int blockId, out int meta)
    {
        blockId = 0;
        meta = 0;
        return false;
    }

    /// <inheritdoc/>
    public int EncodeLegacy(int blockId, int meta) => (blockId << 4) | meta;
}
