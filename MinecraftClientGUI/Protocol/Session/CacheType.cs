namespace MinecraftClient.Protocol.Session
{
    public enum CacheType
    {
        /// <summary>
        /// Do not perform any session caching, always perform login requests from scratch.
        /// </summary>
        None,

        /// <summary>
        /// Cache session information in memory to reuse session tokens across server joins.
        /// </summary>
        Memory,

        /// <summary>
        /// Cache session information in a SessionCache file to share session tokens between different MCC instances.
        /// </summary>
        Disk
    };
}
