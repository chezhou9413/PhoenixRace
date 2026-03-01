using RimWorld;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.Comp
{
    [StaticConstructorOnStartup]
    public class HediffComp_DisruptionMatrixOverlay : HediffComp
    {
        private static readonly Material OverlayMat = MaterialPool.MatFrom(
            "Projectile/DisruptionMatrix",
            ShaderDatabase.TransparentPostLight,Color.white
        );

        private int ticksExisted = 0;
        private const float BREATHE_SPEED = 0.05f; // 呼吸速度
        private const float BASE_SIZE = 3f;      // 基础大小（相对于Pawn）
        private const float SIZE_VARIATION = 0.1f; // 大小变化幅度
        public HediffCompProperties_DisruptionMatrixOverlay Props =>
            (HediffCompProperties_DisruptionMatrixOverlay)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            ticksExisted++;
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            // Hediff移除时的清理（如果需要）
        }

        /// <summary>
        /// 绘制贴图覆盖层
        /// </summary>
        public void DrawOverlay(Vector3 drawPos, Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead)
            {
                return;
            }

            // 计算呼吸效果
            float breathePhase = ticksExisted * BREATHE_SPEED;
            float breatheWave = Mathf.Sin(breathePhase);

            // 大小呼吸（1.05 ~ 1.35）
            float size = BASE_SIZE + (breatheWave * SIZE_VARIATION);

            // 创建动态材质
            Material dynamicMat = MaterialPool.MatFrom(OverlayMat.mainTexture as Texture2D, ShaderDatabase.TransparentPostLight, Color.white);

            // 计算绘制位置（在Pawn上方稍微偏移）
            Vector3 overlayPos = drawPos;
            overlayPos.y = AltitudeLayer.MetaOverlays.AltitudeFor(); // 绘制在最上层

            // 计算旋转（始终面向摄像机）
            Quaternion rotation = Quaternion.identity;

            // 计算缩放（根据Pawn大小和呼吸效果）
            Vector3 scale = new Vector3(size, 1f, size);

            // 绘制贴图
            Matrix4x4 matrix = Matrix4x4.TRS(overlayPos, rotation, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, dynamicMat, 0);

            // 可选：添加额外的粒子特效
            if (ticksExisted % 60 == 0) // 每秒一次
            {
                SpawnBreathEffect(pawn);
            }
        }

        private void SpawnBreathEffect(Pawn pawn)
        {
            // 在Pawn周围生成闪烁的电弧效果
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.8f);
        }
    }

    public class HediffCompProperties_DisruptionMatrixOverlay : HediffCompProperties
    {
        public HediffCompProperties_DisruptionMatrixOverlay()
        {
            compClass = typeof(HediffComp_DisruptionMatrixOverlay);
        }
    }
}