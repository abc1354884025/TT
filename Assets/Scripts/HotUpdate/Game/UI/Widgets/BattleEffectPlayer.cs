using UnityEngine;

/// <summary>仅负责表现层的临时特效播放；资源不存在时静默降级，不影响战斗数值与回放。</summary>
public static class BattleEffectPlayer
{
    public static void Play(string vfxPath, Transform target)
    {
        if (string.IsNullOrEmpty(vfxPath) || target == null) return;

        ResourceManager.InstantiateAsync(vfxPath, target, instance =>
        {
            if (instance != null)
                Object.Destroy(instance, 2f);
        });
    }
}
