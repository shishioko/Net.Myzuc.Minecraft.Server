namespace Net.Myzuc.Minecraft.Server
{
    public sealed record ServerConfiguration
    {
        public int Timeout { get; init; } = 30000;
    }
}