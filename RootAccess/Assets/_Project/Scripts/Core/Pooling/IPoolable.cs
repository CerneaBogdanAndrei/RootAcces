namespace RootAccess.Core
{
    /// <summary>Optional callbacks for components on pooled prefabs (reset state on reuse).</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
