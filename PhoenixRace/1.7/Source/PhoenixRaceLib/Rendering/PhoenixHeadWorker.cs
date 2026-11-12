using AlienRace;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.Rendering;

//以 PSD 的共同像素坐标定位头部，年龄和肖像缩放沿用 HAR 身体倍率。
public sealed class PhoenixHeadWorker : PawnRenderNodeWorker_Head
{
    //四方向头部中心来自拆图清单，向上对应游戏绘制平面的正 Z。
    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        Vector3 offset = base.OffsetFor(node, parms, out pivot);
        offset -= parms.pawn.Drawer.renderer.BaseHeadOffsetAt(parms.facing);
        Vector2 center = parms.facing.AsInt switch
        {
            0 => new Vector2(513.75f, 350.25f),
            1 => new Vector2(644.75f, 363.75f),
            2 => new Vector2(513.5f, 349f),
            _ => new Vector2(379.75f, 363.75f)
        };
        var alien = parms.pawn.GetComp<AlienPartGenerator.AlienComp>();
        Vector2 size = parms.Portrait ? alien.customPortraitDrawSize : alien.customDrawSize;
        float width = HumanlikeMeshPoolUtility.HumanlikeBodyWidthForPawn(parms.pawn);
        offset.x += (center.x - 512f) / 1024f * width * size.x;
        offset.z += (512f - center.y) / 1024f * width * size.y;
        return offset;
    }
}
