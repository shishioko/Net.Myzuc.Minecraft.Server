namespace Net.Myzuc.Minecraft.Server.Resources
{
    public abstract class Configuration<T> : Resource<T> where T : class, new()
    {
        protected Configuration(string identifier) : base(identifier)
        {
            
        }
        public override async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await base.LoadAsync(cancellationToken);
            }
            catch (Exception)
            {
                if (!File.Exists(GetPath())) await ResetAsync(cancellationToken);
            }
        }
        public override sealed async Task<T?> ResetAsync(CancellationToken cancellationToken = default)
        {
            Value = new();
            await SaveAsync(cancellationToken);
            return Value;
        }
        protected override string GetPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configurations", $"{Identifier.Replace(':', '/')}");
        }
    }
}