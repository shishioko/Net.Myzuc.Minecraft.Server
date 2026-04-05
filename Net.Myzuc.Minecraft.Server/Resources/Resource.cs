namespace Net.Myzuc.Minecraft.Server.Resources
{
    public abstract class Resource<T> : IDisposable where T : class
    {
        public string Identifier { get; }
        public T Value
        {
            get
            {
                ObjectDisposedException.ThrowIf(Disposed, this);
                if (InternalValue is null) throw new NullReferenceException();
                return InternalValue;
            }
            set
            {
                ObjectDisposedException.ThrowIf(Disposed, this);
                try
                {
                    Sync.Wait();
                    InternalValue = value;
                }
                finally
                {
                    Sync.Release();
                }
            }
        }
        public bool Loaded => InternalValue is not null;
        private T? InternalValue { get; set; } = null;
        private SemaphoreSlim Sync { get; } = new(1, 1);
        private FileSystemWatcher? Watcher { get; set; } = null;
        private bool Disposed = false;
        protected Resource(string identifier)
        {
            if (identifier.Any(c => Path.GetInvalidFileNameChars().Contains(c))) throw new ArgumentException("Malformed identifier!");
            Identifier = identifier;
        }
        public virtual async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            string path = GetPath();
            try
            {
                await Sync.WaitAsync(cancellationToken);
                byte[] data = await File.ReadAllBytesAsync(path, cancellationToken);
                InternalValue = Deserialize(data);
            }
            finally
            {
                Sync.Release();
            }
        }
        public async Task<T?> LoadOrResetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await LoadAsync(cancellationToken);
            }
            catch (Exception)
            {
                await ResetAsync(cancellationToken);
            }
            return Value;
        }
        public virtual async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            string path = GetPath();
            try
            {
                await Sync.WaitAsync(cancellationToken);
                byte[] data = Serialize(Value);
                if (Watcher is not null) Watcher.EnableRaisingEvents = false;
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
                await File.WriteAllBytesAsync(path, data, cancellationToken);
                if (Watcher is not null) Watcher.EnableRaisingEvents = true;
            }
            finally
            {
                Sync.Release();
            }
        }
        public virtual async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            string path = GetPath();
            try
            {
                await Sync.WaitAsync(cancellationToken);
                InternalValue = null;
                File.Delete(path);
            }
            finally
            {
                Sync.Release();
            }
        }
        public virtual async Task<bool> StartWatchingAsync()
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            string path = GetPath();
            try
            {
                await Sync.WaitAsync();
                if (Watcher is not null) return true;
                Watcher = new(Path.GetDirectoryName(path) ?? throw new NullReferenceException())
                {
                    Filter = Path.GetFileName(path),
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size,
                    EnableRaisingEvents = true,
                };
                Watcher.Created += (sender, args) =>
                {
                    LoadAsync().Wait();
                };
                Watcher.Deleted += (sender, args) =>
                {
                    LoadAsync().Wait();
                };
                Watcher.Renamed += (sender, args) =>
                {
                    LoadAsync().Wait();
                };
                Watcher.Changed += (sender, args) =>
                {
                    LoadAsync().Wait();
                };
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                Sync.Release();
            }
        }
        public abstract T Deserialize(byte[] data);
        public abstract byte[] Serialize(T data);
        protected abstract string GetPath();
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Sync.Dispose();
            Watcher?.Dispose();
        }
    }
}