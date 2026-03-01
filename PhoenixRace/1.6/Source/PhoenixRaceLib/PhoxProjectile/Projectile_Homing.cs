using PhoenixRaceLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PhoxProjectile
{
    [StaticConstructorOnStartup]
    public class Projectile_HomingWithLaser : Projectile
    {
        #region 静态资源

        private static readonly Material LaserMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.Transparent,
            new Color(1f, 0f, 0f, 0.8f)
        );

        #endregion

        #region 字段

        protected Thing homingTarget;
        protected Vector3 targetLastPos;
        private Vector3 exactPos;
        private Vector3 velocity;
        private float currentSpeed;
        private float maxSpeed;
        private float acceleration;
        private float turnRatePerTick = 10f;
        private float hitRadius = 0.4f;
        private int ticksAlive = 0;
        private bool initialized = false;

        private const float INITIAL_SPEED_RATIO = 0.01f;
        private const float ACCELERATION_TICKS = 180f;

        #endregion

        #region 属性重写

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

        #endregion

        #region 生命周期方法

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags,
            bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags,
                preventFriendlyFire, equipment, targetCoverDef);

            exactPos = origin;
            exactPos.y = def.Altitude;

            homingTarget = intendedTarget.Thing;
            if (homingTarget != null && homingTarget.Spawned)
            {
                targetLastPos = homingTarget.DrawPos;
            }
            else
            {
                targetLastPos = intendedTarget.Cell.ToVector3Shifted();
            }

            if (def.projectile.speed > 0)
            {
                maxSpeed = def.projectile.speed / 60f;
            }
            else
            {
                maxSpeed = 0.5f;
            }

            currentSpeed = maxSpeed * INITIAL_SPEED_RATIO;
            acceleration = (maxSpeed - currentSpeed) / ACCELERATION_TICKS;

            Vector3 toTarget = (targetLastPos - exactPos).Yto0();
            if (toTarget.sqrMagnitude > 0.001f)
            {
                velocity = toTarget.normalized * currentSpeed;
            }
            else
            {
                velocity = Vector3.forward * currentSpeed;
            }

            // 设置一个很大的 ticksToImpact 防止基类销毁
            ticksToImpact = 99999;

            initialized = true;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(ref homingTarget, "homingTarget");
            Scribe_Values.Look(ref targetLastPos, "targetLastPos");
            Scribe_Values.Look(ref exactPos, "exactPos");
            Scribe_Values.Look(ref velocity, "velocity");
            Scribe_Values.Look(ref currentSpeed, "currentSpeed");
            Scribe_Values.Look(ref maxSpeed, "maxSpeed");
            Scribe_Values.Look(ref acceleration, "acceleration");
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
            // 不调用 base.Tick()，完全自己控制

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
            UpdateSpeed();
            UpdateDirection();
            UpdatePosition();

            if (!exactPos.ToIntVec3().InBounds(Map))
            {
                Destroy();
                return;
            }

            if (CheckHit())
            {
                return;
            }

            // 超时保护（15秒）
            if (ticksAlive > 900)
            {
                Destroy();
            }
        }

        #endregion

        #region 运动逻辑

        private void UpdateTargetPosition()
        {
            if (homingTarget != null && homingTarget.Spawned)
            {
                targetLastPos = homingTarget.DrawPos;
            }
        }

        private void UpdateSpeed()
        {
            if (currentSpeed < maxSpeed)
            {
                currentSpeed += acceleration;
                if (currentSpeed > maxSpeed)
                {
                    currentSpeed = maxSpeed;
                }

                if (velocity.sqrMagnitude > 0.0001f)
                {
                    velocity = velocity.normalized * currentSpeed;
                }
            }
        }

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

            float maxRadians = turnRatePerTick * Mathf.Deg2Rad;
            Vector3 newDir = Vector3.RotateTowards(currentDir, desiredDir, maxRadians, 0f);

            velocity = newDir.normalized * currentSpeed;
        }

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

            if (!impactPos.InBounds(map))
            {
                impactPos = base.Position;
            }

            DoExplosion(impactPos, map);
            ApplyHediffInRadius(impactPos, map);
            Destroy();
        }

        private void DoExplosion(IntVec3 position, Map map)
        {
            GenExplosion.DoExplosion(
                center: position,
                map: map,
                radius: 3,
                damType: DamageDefOf.Bomb,
                instigator: launcher,
                damAmount: (int)10,
                armorPenetration: -1f,
                explosionSound: null,
                weapon: equipmentDef,
                projectile: def,
                intendedTarget: intendedTarget.Thing,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 0,
                postExplosionGasType: null,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 0,
                chanceToStartFire: 0f,
                damageFalloff: true,
                direction: null,
                ignoredThings: null,
                affectedAngle: null,
                doVisualEffects: true,
                propagationSpeed: 1f,
                excludeRadius: 0f,
                doSoundEffects: true,
                screenShakeFactor: 1f
            );
        }

        // 给半径内所有 Pawn 施加 Hediff，不分敌我
        private void ApplyHediffInRadius(IntVec3 center, Map map)
        {
            HediffDef hediffDef = PhoxDefRef.Hediff_ArmorShred;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 5, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing is Pawn pawn && !pawn.Dead)
                    {
                        Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);

                        if (existingHediff != null)
                        {
                            // 移除旧的再添加，以刷新持续时间
                            pawn.health.RemoveHediff(existingHediff);
                        }

                        Hediff newHediff = HediffMaker.MakeHediff(hediffDef, pawn);
                        newHediff.Severity = 1.0f;
                        pawn.health.AddHediff(newHediff);
                        if (pawn.Drawer?.renderer != null)
                        {
                            pawn.Drawer.renderer.SetAllGraphicsDirty();
                        }
                    }
                }
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

        #endregion

        #region 渲染

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = exactPos;
            drawPos.y = def.Altitude;

            if (def.projectile.shadowSize > 0f)
            {
                DrawShadow(drawPos);
            }

            Graphics.DrawMesh(
                MeshPool.GridPlane(def.graphicData.drawSize),
                drawPos,
                ExactRotation,
                def.graphic.MatSingleFor(this),
                0
            );

            DrawLaserToTarget();
        }

        private void DrawShadow(Vector3 drawPos)
        {
            Material shadowMat = MaterialPool.MatFrom(
                "Things/Skyfaller/SkyfallerShadowCircle",
                ShaderDatabase.Transparent
            );

            float size = def.projectile.shadowSize;
            Vector3 shadowPos = drawPos;
            shadowPos.y = AltitudeLayer.Shadows.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(shadowPos, Quaternion.identity, new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMat, 0);
        }

        private void DrawLaserToTarget()
        {
            if (homingTarget == null || !homingTarget.Spawned)
            {
                return;
            }

            Vector3 startPos = exactPos;
            if (velocity.sqrMagnitude > 0.001f)
            {
                startPos += velocity.normalized * 0.3f;
            }

            Vector3 endPos = homingTarget.DrawPos;

            startPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            endPos.y = startPos.y;

            float distance = Vector3.Distance(startPos, endPos);
            if (distance < 0.1f)
            {
                return;
            }

            float baseWidth = 0.3f;
            float pulseAmount = 0.05f;
            float pulse = baseWidth + Mathf.Sin(ticksAlive * 0.4f) * pulseAmount;

            Vector3 direction = (endPos - startPos).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);
            Vector3 center = (startPos + endPos) / 2f;
            Vector3 scale = new Vector3(pulse, 1f, distance);

            Matrix4x4 matrix = Matrix4x4.TRS(center, rotation, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, LaserMat, 0);
        }

        #endregion
    }
}