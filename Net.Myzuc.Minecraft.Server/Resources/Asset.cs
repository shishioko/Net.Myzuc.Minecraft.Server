namespace Net.Myzuc.Minecraft.Server.Resources
{
    public abstract class Asset<T> : Resource<T> where T : class
    {
        protected Asset(string identifier) : base(identifier)
        {
            
        }
        public override sealed Task SaveAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
        public override sealed Task ResetAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
        public override sealed Task<bool> StartWatchingAsync()
        {
            throw new NotSupportedException();
        }
        public override sealed byte[] Serialize(T data)
        {
            throw new NotSupportedException();
        }
        protected override string GetPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "Assets", $"{Identifier.Replace('.', '/')}");
        }
    }
}