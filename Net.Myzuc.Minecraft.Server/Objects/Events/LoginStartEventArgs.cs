namespace Net.Myzuc.Minecraft.Server.Objects.Events
{
    public class LoginStartEventArgs : EventArgs
    {
        public readonly string Name;
        public readonly Guid Guid;
        public LoginStartEventArgs(string name, Guid guid)
        {
            Name = name;
            Guid = guid;
        }
    }
}