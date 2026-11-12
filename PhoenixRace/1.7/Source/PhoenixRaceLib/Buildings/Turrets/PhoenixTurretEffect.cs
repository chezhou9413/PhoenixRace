using ChezhouLib.Assets;
using RimUnity;
using Verse;

namespace PhoenixRaceLib.Buildings.Turrets;

//管理一次性炮口和命中粒子，沿用 RimUnity 随游戏暂停的粒子时钟。
public sealed class PhoenixTurretEffect : MonoBehaviour
{
    private Map map;
    private ParticleSystem[] particles;

    //独立世界节点让烟气留在开火位置，不跟随炮管后坐或旋转。
    internal static void Spawn(string key, Map map, Vector3 position, Quaternion rotation)
    {
        GameObject root = ClPrefab.Instantiate(ClPrefab.Load(key), position, rotation);
        var effect = root.AddComponent<PhoenixTurretEffect>();
        effect.map = map;
        effect.particles = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var system in effect.particles) system.Play(false);
    }

    //切换地图时清理短暂效果，正常播放结束后释放整个实例。
    private void LateUpdate()
    {
        if (map != Find.CurrentMap || Current.ProgramState != ProgramState.Playing)
        {
            Destroy(gameObject);
            return;
        }
        foreach (var system in particles) if (system.IsAlive(false)) return;
        Destroy(gameObject);
    }
}
