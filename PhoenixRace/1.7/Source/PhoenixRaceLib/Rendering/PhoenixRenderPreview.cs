using System;
using System.IO;
using RimWorld;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.Rendering;

//仅在显式指定预览参数的开发会话中，通过游戏真实 Pawn 渲染器导出四方向。
public sealed class PhoenixRenderPreview : GameComponent
{
    private bool captured;
    private int readyFrames;
    public PhoenixRenderPreview(Game game) { }

    //等待地图完成初始化，避免在 Def 加载或长事件中创建角色。
    public override void GameComponentUpdate()
    {
        if (captured || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || !GenCommandLine.TryGetCommandLineArg("phoenix-preview", out string directory)) return;
        if (++readyFrames < 30) return;
        captured = true;
        try
        {
            Directory.CreateDirectory(directory);
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDef.Named("Phoenix_Colonist"), Faction.OfPlayer);
            pawn.ageTracker.AgeBiologicalTicks = 25L * 3600000L;
            pawn.apparel.DestroyAll();
            pawn.equipment.DestroyAllEquipment();
            GenSpawn.Spawn(pawn, CellFinder.StandableCellNear(Find.CurrentMap.Center, Find.CurrentMap, 10), Find.CurrentMap);
            Find.TickManager.Pause();
            foreach (var entry in new[] { (Rot4.South, "south"), (Rot4.East, "east"), (Rot4.West, "west"), (Rot4.North, "north") })
            {
                pawn.Rotation = entry.Item1;
                pawn.Drawer.renderer.SetAllGraphicsDirty();
                var target = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
                target.Create();
                var previous = RenderTexture.active;
                try
                {
                    Find.PawnCacheRenderer.RenderPawn(pawn, target, Vector3.zero, 1f, 0f, entry.Item1,
                        renderHeadgear: false, renderClothes: false, portrait: false);
                    RenderTexture.active = target;
                    var image = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
                    image.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Path.Combine(directory, "Phoenix_" + entry.Item2 + ".png"), image.EncodeToPNG());
                    UnityEngine.Object.Destroy(image);
                }
                finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.Destroy(target); }
            }
            File.WriteAllText(Path.Combine(directory, "render-complete.txt"), "RimWorld " + VersionControl.CurrentVersionString + "\nPawnCacheRenderer，portrait=false；四方向无服装，成年凤凰。", new System.Text.UTF8Encoding(false));
            Log.Message("[PhoenixRace] 游戏渲染预览完成：" + directory);
        }
        catch (Exception error) { Log.Error("[PhoenixRace] 游戏渲染预览失败：" + error); }
    }
}
