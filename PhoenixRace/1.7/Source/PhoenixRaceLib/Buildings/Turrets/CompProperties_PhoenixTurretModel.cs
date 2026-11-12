using System.Collections.Generic;
using Verse;

namespace PhoenixRaceLib.Buildings.Turrets;

//声明炮塔预制体、刚性转台、后坐节点和实际炮口路径。
public sealed class CompProperties_PhoenixTurretModel : CompProperties
{
    public string prefabKey;
    public string yawPath;
    public List<string> barrelPaths = new();
    public List<string> muzzlePaths = new();
    public float rotationSpeed = 180f;

    //绑定对应的建筑模型组件。
    public CompProperties_PhoenixTurretModel() : base(typeof(CompPhoenixTurretModel)) { }

    //缺失的节点配置在 Def 加载时明确报告。
    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (parentDef.thingClass != typeof(Building_PhoenixTurret))
            yield return "炮塔模型组件需要 Building_PhoenixTurret：" + parentDef.defName;
        if (string.IsNullOrWhiteSpace(prefabKey) || string.IsNullOrWhiteSpace(yawPath)
            || muzzlePaths.Count == 0 || barrelPaths.Count != muzzlePaths.Count || rotationSpeed <= 0)
            yield return "炮塔模型的预制体、转台、后坐与炮口路径必须完整：" + parentDef.defName;
    }
}
