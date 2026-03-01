using PhoenixRaceLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PhoxProjectile
{
    /// <summary>
    /// 干扰矩阵抛射体 - 匀速追踪型
    /// </summary>
    [StaticConstructorOnStartup]
    public class Projectile_DisruptionMatrix : Projectile
    {                   
        // 电磁干扰效果的蓝紫色激光
        private static readonly Material LaserMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.Transparent,
            new Color(0.4f, 0.6f, 1f, 0.7f) // 蓝紫色
        );

        protected Thing homingTarget;           // 追踪目标
        protected Vector3 targetLastPos;        // 目标最后位置
        private Vector3 exactPos;               // 精确位置
        private Vector3 velocity;               // 速度向量
        private float constantSpeed;            // 恒定速度
        private float turnRatePerTick = 8f;     // 每tick转向速度（度）
        private float hitRadius = 0.5f;         // 命中半径
        private int ticksAlive = 0;             // 存活时间
        private bool initialized = false;       // 是否已初始化

        public override Vector3 ExactPosition => exactPos;

        public override Quaternion ExactRotation
        {
            get
            {
                if (velocity.sqrMagnitude > 0.0001f)
                {
                    return Quaternion.LookRotation(velocity.normalized);
                }
                return Quaternion.identity;
            }
        }
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags,
            bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags,
                preventFriendlyFire, equipment, targetCoverDef);

            exactPos = origin;
            exactPos.y = def.Altitude;

            // 设置追踪目标
            homingTarget = intendedTarget.Thing;
            if (homingTarget != null && homingTarget.Spawned)
            {
                targetLastPos = homingTarget.DrawPos;
            }
            else
            {
                targetLastPos = intendedTarget.Cell.ToVector3Shifted();
            }

            // 设置恒定速度
            if (def.projectile.speed > 0)
            {
                constantSpeed = def.projectile.speed / 60f; // 转换为每tick的速度
            }
            else
            {
                constantSpeed = 0.6f; // 默认速度
            }

            // 初始化速度方向
            Vector3 toTarget = (targetLastPos - exactPos).Yto0();
            if (toTarget.sqrMagnitude > 0.001f)
            {
                velocity = toTarget.normalized * constantSpeed;
            }
            else
            {
                velocity = Vector3.forward * constantSpeed;
            }

            // 设置一个很大的 ticksToImpact 防止基类销毁
            ticksToImpact = 99999;

            initialized = true;

            Log.Message($"[DisruptionMatrix] 抛射体已发射，速度: {constantSpeed}, 目标: {homingTarget}");
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(ref homingTarget, "homingTarget");
            Scribe_Values.Look(ref targetLastPos, "targetLastPos");
            Scribe_Values.Look(ref exactPos, "exactPos");
            Scribe_Values.Look(ref velocity, "velocity");
            Scribe_Values.Look(ref constantSpeed, "constantSpeed");
            Scribe_Values.Look(ref ticksAlive, "ticksAlive", 0);
            Scribe_Values.Look(ref initialized, "initialized", false);

            // 加载后重新设置，防止旧存档问题
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ticksToImpact = 99999;
            }
        }

        protected override void Tick()
        {
            if (!initialized)
            {
                Destroy();
                return;
            }

            if (landed)
            {
                return;
            }

            ticksAlive++;

            // 保持 ticksToImpact 不变，防止其他系统干扰
            ticksToImpact = 99999;

            UpdateTargetPosition();
            UpdateDirection();
            UpdatePosition();

            // 检测出界
            if (!exactPos.ToIntVec3().InBounds(Map))
            {
                Destroy();
                return;
            }

            // 命中检测
            if (CheckHit())
            {
                return;
            }

            // 超时保护（12秒）
            if (ticksAlive > 720)
            {
                Destroy();
            }
        }
        /// <summary>
        /// 更新目标位置
        /// </summary>
        private void UpdateTargetPosition()
        {
            if (homingTarget != null && homingTarget.Spawned)
            {
                targetLastPos = homingTarget.DrawPos;
            }
        }

        /// <summary>
        /// 更新飞行方向（匀速追踪）
        /// </summary>
        private void UpdateDirection()
        {
            Vector3 toTarget = (targetLastPos - exactPos).Yto0();
            float distToTarget = toTarget.magnitude;

            if (distToTarget < 0.1f)
            {
                return;
            }

            Vector3 desiredDir = toTarget.normalized;
            Vector3 currentDir = velocity.Yto0().normalized;

            if (currentDir.sqrMagnitude < 0.001f)
            {
                currentDir = desiredDir;
            }

            // 计算转向
            float maxRadians = turnRatePerTick * Mathf.Deg2Rad;
            Vector3 newDir = Vector3.RotateTowards(currentDir, desiredDir, maxRadians, 0f);

            // 保持恒定速度
            velocity = newDir.normalized * constantSpeed;
        }

        /// <summary>
        /// 更新位置
        /// </summary>
        private void UpdatePosition()
        {
            exactPos += velocity;
            exactPos.y = def.Altitude;

            IntVec3 newCell = exactPos.ToIntVec3();
            if (newCell != base.Position && newCell.InBounds(Map))
            {
                base.Position = newCell;
            }
        }

        /// <summary>
        /// 检测命中
        /// </summary>
        private bool CheckHit()
        {
            if (homingTarget != null && homingTarget.Spawned)
            {
                float dist = Vector3.Distance(exactPos.Yto0(), homingTarget.DrawPos.Yto0());

                if (dist < hitRadius)
                {
                    Impact(homingTarget);
                    return true;
                }
            }
            else
            {
                float dist = Vector3.Distance(exactPos.Yto0(), targetLastPos.Yto0());

                if (dist < hitRadius)
                {
                    ImpactSomething();
                    return true;
                }
            }

            return false;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 impactPos = exactPos.ToIntVec3();

            // 确保位置有效
            if (!impactPos.InBounds(map))
            {
                impactPos = base.Position;
            }

            // 给命中的 Pawn 施加干扰矩阵 Hediff
            if (hitThing is Pawn targetPawn && !targetPawn.Dead)
            {
                ApplyDisruptionMatrix(targetPawn);
            }

            // 播放命中特效
            SpawnImpactEffects(impactPos, map);

            // 销毁抛射体
            Destroy();
        }

        /// <summary>
        /// 施加干扰矩阵 Hediff
        /// </summary>
        private void ApplyDisruptionMatrix(Pawn targetPawn)
        {
            // 获取 Hediff 定义（需要在 XML 中定义）
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Hediff_DisruptionMatrix");

            if (hediffDef == null)
            {
                Log.Error("[DisruptionMatrix] 未找到 Hediff_DisruptionMatrix 定义!");
                return;
            }

            // 检查是否已有此 Hediff
            Hediff existingHediff = targetPawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);

            if (existingHediff != null)
            {
                // 已有则刷新（重置持续时间）
                targetPawn.health.RemoveHediff(existingHediff);
            }

            // 添加新的 Hediff
            Hediff newHediff = HediffMaker.MakeHediff(hediffDef, targetPawn);
            newHediff.Severity = 1.0f;
            targetPawn.health.AddHediff(newHediff);

            // 刷新图形
            if (targetPawn.Drawer?.renderer != null)
            {
                targetPawn.Drawer.renderer.SetAllGraphicsDirty();
            }

            Log.Message($"[DisruptionMatrix] 对 {targetPawn.Name} 施加干扰矩阵效果");
        }

        /// <summary>
        /// 生成命中特效
        /// </summary>
        private void SpawnImpactEffects(IntVec3 position, Map map)
        {
            // 电磁冲击波特效
            FleckMaker.Static(position.ToVector3Shifted(), map, FleckDefOf.ExplosionFlash, 2.5f);

            // 额外的电弧特效
            for (int i = 0; i < 3; i++)
            {
                FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), map, 1.5f);
            }
        }

        protected override void ImpactSomething()
        {
            if (homingTarget != null && homingTarget.Spawned && CanHit(homingTarget))
            {
                Impact(homingTarget);
                return;
            }

            base.ImpactSomething();
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = exactPos;
            drawPos.y = def.Altitude;
            Graphics.DrawMesh(
                MeshPool.GridPlane(def.graphicData.drawSize),
                drawPos,
                ExactRotation,
                def.graphic.MatSingleFor(this),
                0
            );

        }
    }
}