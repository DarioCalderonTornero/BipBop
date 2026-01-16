using UnityEngine;

public abstract class AvatarFxSO : ScriptableObject
{
    public abstract IAvatarFxRuntime CreateRuntime();
}

public interface IAvatarFxRuntime
{
    void Init(Material mat);
    void Tick(float dtUnscaled);
    void Dispose();
}
