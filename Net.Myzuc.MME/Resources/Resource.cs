namespace Net.Myzuc.MME.Resources
{
    public abstract class Resource<T> : IDisposable where T : class
    {
        public string Identifier { get; }
        public T Value
        {
            get
            {
                ObjectDisposedException.ThrowIf(Disposed, this);
                if (InternalValue is not null) return InternalValue;
                Logs.Debug($"Tried to read unloaded resource \"{Identifier}\"!");
                throw new InvalidOperationException("Tried to read unloaded resource.");
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
        public virtual async Task<T?> LoadAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            string path = GetPath();
            try
            {
                await Sync.WaitAsync(cancellationToken);
                Logs.Debug($"Loading resource \"{Identifier}\" at \"{path}\".");
                if (!File.Exists(path)) return null;
                byte[] data = await File.ReadAllBytesAsync(path, cancellationToken);
                return InternalValue = Deserialize(data);
            }
            catch (Exception ex)
            {
                Logs.Warning($"Error while loading resource \"{Identifier}\" at \"{path}\": {ex}");
                return null;
            }
            finally
            {
                Sync.Release();
            }
        }
        public virtual async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            string path = GetPath();
            try
            {
                await Sync.WaitAsync(cancellationToken);
                Logs.Debug($"Saving resource \"{Identifier}\" at \"{path}\".");
                byte[] data = Serialize(Value);
                if (Watcher is not null) Watcher.EnableRaisingEvents = false;
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new NullReferenceException());
                await File.WriteAllBytesAsync(path, data, cancellationToken);
                if (Watcher is not null) Watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Logs.Warning($"Error while saving resource \"{Identifier}\" at \"{path}\": {ex}");
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
                Logs.Debug($"Deleting resource \"{Identifier}\" at \"{path}\".");
                InternalValue = null;
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Logs.Warning($"Error while deleting resource \"{Identifier}\" at \"{path}\": {ex}");
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
                Logs.Debug($"Watching resource \"{Identifier}\" at \"{path}\".");
                Watcher = new(Path.GetDirectoryName(path) ?? throw new NullReferenceException())
                {
                    Filter = Path.GetFileName(path),
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size,
                    EnableRaisingEvents = true,
                };
                Watcher.Created += async (sender, args) =>
                {
                    await LoadAsync();
                };
                Watcher.Deleted += async (sender, args) =>
                {
                    await LoadAsync();
                };
                Watcher.Renamed += async (sender, args) =>
                {
                    await LoadAsync();
                };
                Watcher.Changed += async (sender, args) =>
                {
                    await LoadAsync();
                };
                return true;
            }
            catch (Exception ex)
            {
                Logs.Warning($"Error while watching resource \"{Identifier}\" at \"{path}\": {ex}");
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