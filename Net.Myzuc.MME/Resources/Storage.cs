namespace Net.Myzuc.MME.Resources
{
    public abstract class Storage<T> : Resource<T> where T : class
    {
        protected Storage(string identifier) : base(identifier)
        {
            
        }
        public override sealed Task<bool> StartWatchingAsync()
        {
            throw new NotSupportedException();
        }
        protected override string GetPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "Data", $"{Identifier.Replace('.', '/')}");
        }
    }
}