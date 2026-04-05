namespace Net.Myzuc.Minecraft.Server.Resources
{
    public abstract class Storage<T> : Resource<T> where T : class, new()
    {
        protected Storage(string identifier) : base(identifier)
        {
            
        }
        public override sealed async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            await base.ResetAsync(cancellationToken);
            Value = new();
        }
        public override sealed Task<bool> StartWatchingAsync()
        {
            throw new NotSupportedException();
        }
        protected override string GetPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"{Identifier.Replace('.', '/')}");
        }
    }
}