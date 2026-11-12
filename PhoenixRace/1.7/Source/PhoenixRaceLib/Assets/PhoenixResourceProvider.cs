using ChezhouLib.ALLmap;
using ChezhouLib.LibDef;
using RimUnity.Assets;

namespace PhoenixRaceLib.Assets;

//将 RimUnity 导出的三种特效 Shader 接入现有 CL 材质定义。
public sealed class PhoenixResourceProvider : ClNativeResourceProvider
{
    //CL 在创建材质前调用此入口，原版绘制通过宿主资源包装使用同一 Shader。
    public override void InitializeResources()
    {
        string package = modContentPack.PackageId.ToLowerInvariant();
        foreach (string name in new[] { "SingleImageGlitch", "ShieldDissolve", "ShieldEnergyFlow" })
            abDatabase.shaderDataBase[package + "=>" + name] = HostAssets.ToHost(
                AssetLoader.Load<RimUnity.Shader>(modContentPack, "Shaders/" + name));
        abDatabase.Texture2dDataBase[package + "=>Noise"] = HostAssets.ToHost(
            AssetLoader.Load<RimUnity.Texture2D>(modContentPack, "Textures/Noise"));
    }
}
