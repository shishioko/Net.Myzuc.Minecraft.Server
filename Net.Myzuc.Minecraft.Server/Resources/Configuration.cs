namespace Net.Myzuc.Minecraft.Server.Resources
{
    public abstract class Configuration<T> : Resource<T> where T : class, new()
    {
        protected Configuration(string identifier) : base(identifier)
        {
            
        }
        public async Task<T> LoadOrResetAsync()
        {
            if (await LoadAsync() is null) await ResetAsync();
            return Value;
        }
        public override sealed async Task<T?> ResetAsync(CancellationToken cancellationToken = default)
        {
            Value = new();
            await SaveAsync(cancellationToken);
            return Value;
        }
        protected override string GetPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "Configurations", $"{Identifier.Replace('.', '/')}");
        }
    }
}