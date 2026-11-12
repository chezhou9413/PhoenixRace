using ChezhouLib.ALLmap;
using PhoenixRaceLib.PhoxAbility;
using RimWorld;
using System.Collections.Generic;
using RimUnity;
using PhoenixRaceLib.Rendering.Models;
using Verse;

namespace PhoenixRaceLib.PhoxGameComp
{
    public class GameComponent_AirStrikeBombingManager : GameComponent
    {
        private class AirStrikeMission : IExposable
        {
            public int mapId = -1;
            public IntVec3 targetCell = IntVec3.Invalid;
            public Vector3 spawnPos;
            public Vector3 approachPos;
            public Vector3 diveControlPos;
            public Vector3 impactPos;
            public Vector3 pullUpControlPos;
            public Vector3 exitPos;
            public int launchTick = -1;
            public int approachTicks = 36;
            public int diveTicks = 60;
            public int pullUpTicks = 48;
            public int barrelRollDelayTicks = 12;
            public int barrelRollDurationTicks = 36;
            public int damageAmount = 50;
            public float damageRadius = 2.9f;
            public int explosionVisualLifeTicks = 5 * 60;
            public DamageDef damageDef;
            public Pawn instigator;
            public bool impactTriggered;
            public float minVisualScale = 0.8f;
            public float maxVisualScale = 1.35f;

            // === 新增：用于弧长参数化的预计算数据 ===
            // 每段的近似弧长
            public float approachArcLength;
            public float diveArcLength;
            public float pullUpArcLength;
            public float totalArcLength;
            // 总飞行 tick 数
            public int totalFlightTicks;

            public AirStrikeMission()
            {
            }

            /// <summary>
            /// 在所有路径点设置完毕后调用，预计算弧长和重新分配各段 tick 数。
            /// </summary>
            public void PrecomputeFlightData()
            {
                totalFlightTicks = approachTicks + diveTicks + pullUpTicks;

                // approach 是线性段，弧长就是两点距离
                approachArcLength = Vector3.Distance(spawnPos, approachPos);

                // dive 和 pullUp 是二次贝塞尔，用采样近似弧长
                diveArcLength = ApproximateBezierArcLength(approachPos, diveControlPos, impactPos, 16);
                pullUpArcLength = ApproximateBezierArcLength(impactPos, pullUpControlPos, exitPos, 16);

                totalArcLength = approachArcLength + diveArcLength + pullUpArcLength;

                // 按弧长比例重新分配各段的 tick 数，保证速度均匀
                if (totalArcLength > 0.01f)
                {
                    float approachRatio = approachArcLength / totalArcLength;
                    float diveRatio = diveArcLength / totalArcLength;
                    // pullUp 取剩余，避免四舍五入丢失

                    int newApproach = Mathf.Max(1, Mathf.RoundToInt(totalFlightTicks * approachRatio));
                    int newDive = Mathf.Max(1, Mathf.RoundToInt(totalFlightTicks * diveRatio));
                    int newPullUp = Mathf.Max(1, totalFlightTicks - newApproach - newDive);

                    approachTicks = newApproach;
                    diveTicks = newDive;
                    pullUpTicks = newPullUp;
                }
            }

            private static float ApproximateBezierArcLength(Vector3 p0, Vector3 p1, Vector3 p2, int segments)
            {
                float length = 0f;
                Vector3 prev = p0;
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float u = 1f - t;
                    Vector3 curr = (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
                    length += Vector3.Distance(prev, curr);
                    prev = curr;
                }
                return length;
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref mapId, "mapId", -1);
                Scribe_Values.Look(ref targetCell, "targetCell", IntVec3.Invalid);
                Scribe_Values.Look(ref spawnPos, "spawnPos");
                Scribe_Values.Look(ref approachPos, "approachPos");
                Scribe_Values.Look(ref diveControlPos, "diveControlPos");
                Scribe_Values.Look(ref impactPos, "impactPos");
                Scribe_Values.Look(ref pullUpControlPos, "pullUpControlPos");
                Scribe_Values.Look(ref exitPos, "exitPos");
                Scribe_Values.Look(ref launchTick, "launchTick", -1);
                Scribe_Values.Look(ref approachTicks, "approachTicks", 36);
                Scribe_Values.Look(ref diveTicks, "diveTicks", 60);
                Scribe_Values.Look(ref pullUpTicks, "pullUpTicks", 48);
                Scribe_Values.Look(ref barrelRollDelayTicks, "barrelRollDelayTicks", 12);
                Scribe_Values.Look(ref barrelRollDurationTicks, "barrelRollDurationTicks", 36);
                Scribe_Values.Look(ref damageAmount, "damageAmount", 50);
                Scribe_Values.Look(ref damageRadius, "damageRadius", 2.9f);
                Scribe_Values.Look(ref explosionVisualLifeTicks, "explosionVisualLifeTicks", 5 * 60);
                Scribe_Values.Look(ref minVisualScale, "minVisualScale", 0.8f);
                Scribe_Values.Look(ref maxVisualScale, "maxVisualScale", 1.35f);
                Scribe_Defs.Look(ref damageDef, "damageDef");
                Scribe_References.Look(ref instigator, "instigator");
                Scribe_Values.Look(ref impactTriggered, "impactTriggered", false);
            }
        }

        private class ActiveExplosion
        {
            public GameObject instance;
            public int releaseTick = -1;
        }

        private class MissionVisual
        {
            public GameObject plane;
            public GameObject shadow;
        }

        private const float PlaneScaleFactor = 4f;
        private const int PlanePoolCap = 8;
        private const int ExplosionPoolCap = 24;
        private const int ShadowPoolCap = 8;
        private const float ImpactAltitude = 3.2f;
        private const float ShadowAltitude = 0.12f;
        private const float ShadowForwardOffset = 4.8f;
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.38f);

        // === pullUp 冲向摄像机效果参数 ===
        // 抬升阶段的额外缩放倍数：飞机在最高点时会达到 maxVisualScale * PullUpScaleBoost
        private const float PullUpScaleBoost = 1.6f;
        // pullUp 水平距离系数（原距离的比例，0.5 ≈ 50% 角度的自然弧线）
        private const float PullUpHorizontalShrink = 0.5f;
        // pullUp 高度放大系数
        private const float PullUpHeightBoost = 1.8f;
        // 淡出开始的 pullUp 进度（0.0~1.0，越大越晚开始淡出，给玩家更多时间看飞机）
        private const float FadeOutStartT = 0.6f;
        // 整体飞行时间延长系数（乘在每段 tick 上）
        private const float FlightDurationScale = 1.6f;

        private List<AirStrikeMission> missions = new List<AirStrikeMission>(8);
        private readonly List<ActiveExplosion> activeExplosions = new List<ActiveExplosion>(16);
        private readonly Stack<GameObject> planePool = new Stack<GameObject>(8);
        private readonly Stack<GameObject> explosionPool = new Stack<GameObject>(16);
        private readonly Stack<GameObject> shadowPool = new Stack<GameObject>(8);
        private readonly Dictionary<GameObject, Vector3> planeBaseScales = new Dictionary<GameObject, Vector3>(16);
        private readonly Dictionary<GameObject, Vector3> explosionBaseScales = new Dictionary<GameObject, Vector3>(32);
        private readonly Dictionary<GameObject, Vector3> shadowBaseScales = new Dictionary<GameObject, Vector3>(16);
        private readonly Dictionary<AirStrikeMission, MissionVisual> missionVisuals = new Dictionary<AirStrikeMission, MissionVisual>();
        private Material shadowMaterial;
        private Texture2D shadowTexture;

        public static GameComponent_AirStrikeBombingManager Instance =>
            Current.Game?.GetComponent<GameComponent_AirStrikeBombingManager>();

        private GameObject planePrefab;
        private GameObject explosionPrefab;

        public GameComponent_AirStrikeBombingManager(Game game)
        {
        }

        public override void FinalizeInit()
        {
            EnsurePrefabsLoaded();
            base.FinalizeInit();
        }

        public override void StartedNewGame()
        {
            EnsurePrefabsLoaded();
            CleanupRuntimeVisuals();
            base.StartedNewGame();
        }

        public override void LoadedGame()
        {
            EnsurePrefabsLoaded();
            CleanupRuntimeVisuals();
            base.LoadedGame();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref missions, "missions", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                CleanupRuntimeVisuals();
                EnsurePrefabsLoaded();
                // 加载后重新计算弧长数据
                if (missions != null)
                {
                    for (int i = 0; i < missions.Count; i++)
                    {
                        missions[i].PrecomputeFlightData();
                    }
                }
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            EnsurePrefabsLoaded();

            int nowTick = Find.TickManager.TicksGame;
            TickMissions(nowTick);
            TickExplosions(nowTick);
        }

        public void QueueAirStrike(Map map, IntVec3 targetCell, Pawn instigator, CompProperties_AbilityAirStrikeBombing props)
        {
            if (map == null || props == null || !targetCell.IsValid || !targetCell.InBounds(map))
            {
                return;
            }

            Vector3 impactPos = targetCell.ToVector3Shifted().ToRim();
            impactPos.y = ImpactAltitude;

            Vector3 flightDir = ResolveFlightDirection(instigator, impactPos);
            int nowTick = Find.TickManager.TicksGame;
            Vector3 spawnPos = impactPos - flightDir * Mathf.Max(1f, props.spawnDistance) + Vector3.up * Mathf.Max(0f, props.spawnHeight);
            Vector3 approachPos = impactPos - flightDir * Mathf.Max(1f, props.approachDistance) + Vector3.up * Mathf.Max(0f, props.spawnHeight * 0.92f);

            // === 修复：让 dive 控制点的切线方向与 approach 方向一致 ===
            // approach 方向
            Vector3 approachDir = (approachPos - spawnPos).normalized;
            // diveControlPos 沿 approach 方向延伸，同时加上弧高，保证 C1 连续
            Vector3 diveControlPos = approachPos + approachDir * (Vector3.Distance(approachPos, impactPos) * 0.35f)
                + Vector3.up * Mathf.Max(0f, props.diveArcHeight);

            // === 修复：pullUp 控制点方向与 dive 终点切线一致 ===
            // dive 在 t=1 的切线 = 2*(impactPos - diveControlPos)，方向即 impactPos - diveControlPos
            Vector3 diveEndTangent = (impactPos - diveControlPos).normalized;

            // === 冲向摄像机的拉起效果 ===
            // 水平距离大幅缩短，垂直高度大幅增加 → 飞机几乎垂直拉起
            float pullUpDist = Mathf.Max(1f, props.pullUpDistance);
            float pullUpHeight = Mathf.Max(0f, props.pullUpHeight);
            float shortenedHorizontal = pullUpDist * PullUpHorizontalShrink;
            float boostedHeight = pullUpHeight * PullUpHeightBoost;

            // 控制点：沿 dive 尾部切线方向稍微前移，然后大幅上拉
            // 这保证 C1 连续且弧线快速转向天空
            Vector3 pullUpControlPos = impactPos
                + diveEndTangent * (shortenedHorizontal * 0.35f)
                + Vector3.up * (boostedHeight * 0.6f);

            // 出口点：只在水平方向稍微前移，但高度极高 → 几乎正上方消失
            Vector3 exitPos = impactPos
                + flightDir * shortenedHorizontal
                + Vector3.up * boostedHeight;

            AirStrikeMission mission = new AirStrikeMission
            {
                mapId = map.uniqueID,
                targetCell = targetCell,
                spawnPos = spawnPos,
                approachPos = approachPos,
                diveControlPos = diveControlPos,
                impactPos = impactPos,
                pullUpControlPos = pullUpControlPos,
                exitPos = exitPos,
                launchTick = nowTick,
                // 各段 tick 乘以 FlightDurationScale，让整体飞行更慢，玩家能看清飞机细节
                approachTicks = Mathf.Max(1, Mathf.RoundToInt(props.approachTicks * FlightDurationScale)),
                diveTicks = Mathf.Max(1, Mathf.RoundToInt(props.diveTicks * FlightDurationScale)),
                pullUpTicks = Mathf.Max(1, Mathf.RoundToInt(props.pullUpTicks * FlightDurationScale)),
                barrelRollDelayTicks = Mathf.Max(0, Mathf.RoundToInt(props.barrelRollDelayTicks * FlightDurationScale)),
                barrelRollDurationTicks = Mathf.Max(1, Mathf.RoundToInt(props.barrelRollDurationTicks * FlightDurationScale)),
                damageAmount = Mathf.Max(1, props.damageAmount),
                damageRadius = Mathf.Max(0.1f, props.damageRadius),
                explosionVisualLifeTicks = Mathf.Max(1, props.explosionVisualLifeTicks),
                damageDef = props.damageDef ?? DamageDefOf.Bomb,
                instigator = instigator,
                impactTriggered = false,
                minVisualScale = 0.82f,
                maxVisualScale = 1.42f
            };

            // 预计算弧长并按比例重新分配各段 tick，保证匀速
            mission.PrecomputeFlightData();

            missions.Add(mission);
        }

        private void EnsurePrefabsLoaded()
        {
            if (ChezhouLib.Assets.ClPrefab.Templates == null)
            {
                return;
            }

            if (planePrefab == null && ChezhouLib.Assets.ClPrefab.Templates.TryGetValue("PHFEIJI1", out GameObject loadedPlane))
            {
                planePrefab = loadedPlane;
            }

            if (explosionPrefab == null && ChezhouLib.Assets.ClPrefab.Templates.TryGetValue("hongzhaExplosion", out GameObject loadedExplosion))
            {
                explosionPrefab = loadedExplosion;
            }
        }

        private void TickMissions(int nowTick)
        {
            for (int i = missions.Count - 1; i >= 0; i--)
            {
                AirStrikeMission mission = missions[i];
                Map map = FindMapById(mission.mapId);
                if (map == null)
                {
                    ReleaseMissionVisual(mission);
                    missions.RemoveAt(i);
                    continue;
                }

                MissionVisual visual = EnsureMissionVisual(mission);
                GameObject plane = visual?.plane;
                int approachEndTick = mission.launchTick + mission.approachTicks;
                int diveEndTick = approachEndTick + mission.diveTicks;
                int missionEndTick = diveEndTick + mission.pullUpTicks;

                if (!mission.impactTriggered && nowTick >= diveEndTick)
                {
                    mission.impactTriggered = true;
                    DoImpact(map, mission, nowTick);
                }

                if (nowTick >= missionEndTick)
                {
                    ReleaseMissionVisual(mission);
                    missions.RemoveAt(i);
                    continue;
                }

                if (plane == null)
                {
                    continue;
                }

                Vector3 position;
                Vector3 tangent;
                float rollDegrees = 0f;

                if (nowTick < approachEndTick)
                {
                    float rawT = Mathf.Clamp01((nowTick - mission.launchTick) / (float)mission.approachTicks);
                    // approach 是线性段，rawT 本身就是匀速的，用平滑缓动让起飞更自然
                    float t = SmoothStartStop(rawT);
                    position = Vector3.Lerp(mission.spawnPos, mission.approachPos, t);
                    tangent = (mission.approachPos - mission.spawnPos);
                }
                else if (nowTick < diveEndTick)
                {
                    float rawT = Mathf.Clamp01((nowTick - approachEndTick) / (float)mission.diveTicks);
                    // === 修复：对贝塞尔做弧长参数化近似，让速度更均匀 ===
                    float t = ApproximateArcLengthT(
                        mission.approachPos, mission.diveControlPos, mission.impactPos, rawT);
                    position = EvaluateQuadraticBezier(mission.approachPos, mission.diveControlPos, mission.impactPos, t);
                    tangent = EvaluateQuadraticBezierDerivative(mission.approachPos, mission.diveControlPos, mission.impactPos, t);
                }
                else
                {
                    float rawT = Mathf.Clamp01((nowTick - diveEndTick) / (float)mission.pullUpTicks);

                    // === 冲向摄像机效果 ===
                    // 用缓入加速曲线：初期从容飞行（让玩家看清飞机），后期逐渐加速拉起
                    // easeIn: t^1.8 温和的加速感
                    float acceleratedT = Mathf.Pow(rawT, 1.8f);

                    float t = ApproximateArcLengthT(
                        mission.impactPos, mission.pullUpControlPos, mission.exitPos, acceleratedT);
                    position = EvaluateQuadraticBezier(mission.impactPos, mission.pullUpControlPos, mission.exitPos, t);
                    tangent = EvaluateQuadraticBezierDerivative(mission.impactPos, mission.pullUpControlPos, mission.exitPos, t);
                    rollDegrees = EvaluateBarrelRollDegrees(mission, nowTick, diveEndTick);
                }

                if (tangent.sqrMagnitude < 0.001f)
                {
                    tangent = (mission.exitPos - mission.impactPos).normalized;
                }

                plane.transform.position = position;
                plane.transform.rotation = ComposePlaneRotation(tangent.normalized, rollDegrees);

                // === 判断是否在 pullUp 阶段，应用特殊缩放和淡出 ===
                float pullUpRawT = -1f;
                if (nowTick >= diveEndTick && nowTick < missionEndTick)
                {
                    pullUpRawT = Mathf.Clamp01((nowTick - diveEndTick) / (float)mission.pullUpTicks);
                }

                if (pullUpRawT >= 0f)
                {
                    // pullUp 阶段：在基础高度缩放之上，额外叠加 boost
                    // 随 pullUpRawT 递增，飞机越来越大，营造冲向摄像机的感觉
                    ApplyPlaneScalePullUp(plane, mission, position, pullUpRawT);

                    // 淡出：后半段渐隐
                    ApplyPlaneFadeOut(plane, pullUpRawT);

                    // 阴影在 pullUp 阶段也逐渐淡出消失
                    if (visual?.shadow != null)
                    {
                        float shadowFade = 1f - Mathf.Clamp01(pullUpRawT / 0.5f);
                        ApplyShadowFade(visual.shadow, shadowFade);
                    }
                }
                else
                {
                    ApplyPlaneScale(plane, mission, position);
                    // 确保非 pullUp 阶段飞机完全不透明
                    SetPlaneAlpha(plane, 1f);
                }

                UpdateShadowVisual(visual?.shadow, mission, position, tangent.normalized);
            }
        }

        private void TickExplosions(int nowTick)
        {
            for (int i = activeExplosions.Count - 1; i >= 0; i--)
            {
                ActiveExplosion explosion = activeExplosions[i];
                if (nowTick < explosion.releaseTick)
                {
                    continue;
                }

                ReleaseToPool(explosion.instance, explosionPool, ExplosionPoolCap);
                activeExplosions.RemoveAt(i);
            }
        }

        //只为当前地图创建视觉对象，其他地图仍照常推进飞行与投弹。
        private MissionVisual EnsureMissionVisual(AirStrikeMission mission)
        {
            if (Find.CurrentMap == null || mission.mapId != Find.CurrentMap.uniqueID)
            {
                ReleaseMissionVisual(mission);
                return null;
            }
            if (missionVisuals.TryGetValue(mission, out MissionVisual existingVisual))
            {
                if (existingVisual.plane != null && !existingVisual.plane.activeSelf)
                {
                    existingVisual.plane.SetActive(true);
                }
                if (existingVisual.shadow != null && !existingVisual.shadow.activeSelf)
                {
                    existingVisual.shadow.SetActive(true);
                }
                return existingVisual;
            }

            GameObject plane = AcquireFromPool(planePool, planePrefab);
            if (plane == null)
            {
                return null;
            }

            PhoenixModelComponent.Attach(plane, Find.CurrentMap);
            GameObject shadow = AcquireShadowFromPool();
            plane.transform.position = mission.spawnPos;
            plane.transform.rotation = ComposePlaneRotation((mission.approachPos - mission.spawnPos).normalized, 0f);
            ApplyPlaneScale(plane, mission, mission.spawnPos);
            UpdateShadowVisual(shadow, mission, mission.spawnPos, (mission.impactPos - mission.spawnPos).normalized);

            MissionVisual visual = new MissionVisual
            {
                plane = plane,
                shadow = shadow
            };
            missionVisuals[mission] = visual;
            return visual;
        }

        private void ReleaseMissionVisual(AirStrikeMission mission)
        {
            if (!missionVisuals.TryGetValue(mission, out MissionVisual visual))
            {
                return;
            }

            ReleaseToPool(visual.plane, planePool, PlanePoolCap);
            ReleaseToPool(visual.shadow, shadowPool, ShadowPoolCap);
            missionVisuals.Remove(mission);
        }

        private void DoImpact(Map map, AirStrikeMission mission, int nowTick)
        {
            GenExplosion.DoExplosion(
                center: mission.targetCell,
                map: map,
                radius: mission.damageRadius,
                damType: mission.damageDef ?? DamageDefOf.Bomb,
                instigator: mission.instigator,
                damAmount: mission.damageAmount,
                armorPenetration: -1f,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 0,
                postExplosionGasType: null,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 0,
                chanceToStartFire: 0f,
                damageFalloff: false,
                direction: null,
                ignoredThings: null,
                affectedAngle: null,
                doVisualEffects: false,
                propagationSpeed: 1f,
                excludeRadius: 0f,
                doSoundEffects: false,
                screenShakeFactor: 0f
            );

            GameObject explosionObj = AcquireFromPool(explosionPool, explosionPrefab);
            if (explosionObj == null)
            {
                return;
            }

            Vector3 explosionPos = mission.targetCell.ToVector3Shifted().ToRim();
            explosionPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            explosionObj.transform.position = explosionPos;
            explosionObj.transform.rotation = Quaternion.Euler(90f, Rand.Range(0f, 360f), 0f);
            ApplyExplosionScale(explosionObj);

            activeExplosions.Add(new ActiveExplosion
            {
                instance = explosionObj,
                releaseTick = nowTick + mission.explosionVisualLifeTicks
            });
        }

        // =====================================================================
        // 数学工具方法
        // =====================================================================

        private static Vector3 ResolveFlightDirection(Pawn instigator, Vector3 impactPos)
        {
            if (instigator != null)
            {
                Vector3 fromCaster = impactPos - instigator.DrawPos.ToRim();
                fromCaster.y = 0f;
                if (fromCaster.sqrMagnitude > 0.001f)
                {
                    return fromCaster.normalized;
                }
            }

            float angle = Rand.Range(0f, 360f);
            Vector3 randomDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            randomDir.y = 0f;
            return randomDir.normalized;
        }

        private static Quaternion ComposePlaneRotation(Vector3 moveDir, float rollDegrees)
        {
            if (moveDir.sqrMagnitude < 0.001f)
            {
                moveDir = Vector3.forward;
            }

            Quaternion facing = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            Quaternion correction = Quaternion.Euler(90f, 0f, 0f);
            Quaternion roll = Quaternion.AngleAxis(rollDegrees, Vector3.up);
            return facing * correction * roll;
        }

        private static float EvaluateBarrelRollDegrees(AirStrikeMission mission, int nowTick, int pullUpStartTick)
        {
            int rollStartTick = pullUpStartTick + mission.barrelRollDelayTicks;
            if (nowTick <= rollStartTick)
            {
                return 0f;
            }

            float t = Mathf.Clamp01((nowTick - rollStartTick) / (float)mission.barrelRollDurationTicks);
            float eased = 0.5f - (0.5f * Mathf.Cos(Mathf.PI * t));
            return eased * 360f;
        }

        private static Vector3 EvaluateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
        }

        private static Vector3 EvaluateQuadraticBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return (2f * (1f - t) * (p1 - p0)) + (2f * t * (p2 - p1));
        }

        /// <summary>
        /// 弧长参数化近似：给定均匀 rawT (0~1)，返回在贝塞尔曲线上对应弧长均匀分布的参数 t。
        /// 使用查找表 + 线性插值，16 段采样足够流畅且性能开销极低。
        /// </summary>
        private static float ApproximateArcLengthT(Vector3 p0, Vector3 p1, Vector3 p2, float rawT)
        {
            const int samples = 16;

            // 构建弧长查找表 (在栈上，无 GC)
            // arcLengths[i] = 从 0 到 i/samples 的累积弧长
            float totalLength = 0f;
            Vector3 prev = p0;

            // 我们需要一个小数组，用 stackalloc 风格的手动展开避免 heap 分配
            // 但 C# 在 Unity 里 stackalloc 有限制，这里用固定大小数组
            float s0 = 0f, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15, s16;
            float[] lengths = new float[samples + 1];
            lengths[0] = 0f;

            for (int i = 1; i <= samples; i++)
            {
                float st = i / (float)samples;
                float u = 1f - st;
                Vector3 curr = (u * u * p0) + (2f * u * st * p1) + (st * st * p2);
                totalLength += Vector3.Distance(prev, curr);
                lengths[i] = totalLength;
                prev = curr;
            }

            if (totalLength < 0.001f)
            {
                return rawT;
            }

            // rawT 对应的目标弧长
            float targetLength = rawT * totalLength;

            // 在查找表中二分查找
            int lo = 0, hi = samples;
            while (lo < hi - 1)
            {
                int mid = (lo + hi) / 2;
                if (lengths[mid] < targetLength)
                    lo = mid;
                else
                    hi = mid;
            }

            // 在 lo 和 hi 之间线性插值
            float segLength = lengths[hi] - lengths[lo];
            float frac = (segLength > 0.0001f) ? (targetLength - lengths[lo]) / segLength : 0f;
            return (lo + frac) / samples;
        }

        /// <summary>
        /// 平滑缓入缓出函数，用于 approach 线性段的起始/结束过渡。
        /// Hermite 插值: 3t² - 2t³
        /// </summary>
        private static float SmoothStartStop(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static Map FindMapById(int mapId)
        {
            if (Find.Maps == null)
            {
                return null;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map.uniqueID == mapId)
                {
                    return map;
                }
            }

            return null;
        }

        // =====================================================================
        // 缩放 — RimWorld 俯视正交视角下的透视模拟
        // =====================================================================

        /// <summary>
        /// RimWorld 使用俯视正交摄像机，从上往下看地图。
        /// 飞机高度越高 → 离摄像机越近 → 应该显示越大（maxVisualScale）
        /// 飞机高度越低（贴近地面）→ 离摄像机越远 → 应该显示越小（minVisualScale）
        /// 
        /// 这样飞机从高空进场时很大，俯冲到地面时缩小，
        /// 拉起升高后又变大，符合俯视视角的直觉。
        /// </summary>
        private void ApplyPlaneScale(GameObject planeObj, AirStrikeMission mission, Vector3 currentPos)
        {
            if (planeObj == null)
            {
                return;
            }

            if (!planeBaseScales.TryGetValue(planeObj, out Vector3 baseScale))
            {
                baseScale = planeObj.transform.localScale;
                planeBaseScales[planeObj] = baseScale;
            }

            // 整条飞行路径中的最高点和最低点
            float maxHeight = Mathf.Max(
                mission.spawnPos.y,
                Mathf.Max(mission.approachPos.y,
                Mathf.Max(mission.exitPos.y,
                Mathf.Max(mission.diveControlPos.y, mission.pullUpControlPos.y))));
            float minHeight = mission.impactPos.y; // 俯冲最低点

            // 当前高度在 [minHeight, maxHeight] 范围中的位置
            // 0 = 最低点（贴近地面，缩放最小），1 = 最高点（高空，缩放最大）
            float heightT = Mathf.Clamp01(
                Mathf.InverseLerp(minHeight, maxHeight, currentPos.y));

            // 平滑缓动，避免线性变化的机械感
            float smoothT = SmoothStartStop(heightT);

            // 高空 → maxVisualScale（大），低空 → minVisualScale（小）
            float perspectiveScale = Mathf.Lerp(mission.minVisualScale, mission.maxVisualScale, smoothT);
            planeObj.transform.localScale = baseScale * PlaneScaleFactor * perspectiveScale;
        }

        private void ApplyExplosionScale(GameObject explosionObj)
        {
            if (explosionObj == null)
            {
                return;
            }

            if (!explosionBaseScales.TryGetValue(explosionObj, out Vector3 baseScale))
            {
                baseScale = explosionObj.transform.localScale;
                explosionBaseScales[explosionObj] = baseScale;
            }

            explosionObj.transform.localScale = baseScale;
        }

        /// <summary>
        /// pullUp 阶段专用缩放：在基础高度缩放之上叠加额外 boost。
        /// 随着 pullUpT 从 0→1，缩放从正常的高度缩放逐渐膨胀到 PullUpScaleBoost 倍，
        /// 营造飞机冲向摄像机、越来越大的视觉冲击。
        /// </summary>
        private void ApplyPlaneScalePullUp(GameObject planeObj, AirStrikeMission mission, Vector3 currentPos, float pullUpT)
        {
            if (planeObj == null)
            {
                return;
            }

            if (!planeBaseScales.TryGetValue(planeObj, out Vector3 baseScale))
            {
                baseScale = planeObj.transform.localScale;
                planeBaseScales[planeObj] = baseScale;
            }

            // 基础高度缩放（和 ApplyPlaneScale 一样）
            float maxHeight = Mathf.Max(
                mission.spawnPos.y,
                Mathf.Max(mission.approachPos.y,
                Mathf.Max(mission.exitPos.y,
                Mathf.Max(mission.diveControlPos.y, mission.pullUpControlPos.y))));
            float minHeight = mission.impactPos.y;

            float heightT = Mathf.Clamp01(
                Mathf.InverseLerp(minHeight, maxHeight, currentPos.y));
            float smoothT = SmoothStartStop(heightT);
            float basePersp = Mathf.Lerp(mission.minVisualScale, mission.maxVisualScale, smoothT);

            // 额外 boost：pullUpT 越大飞机越大
            // 用 easeIn 曲线让后半段膨胀更剧烈
            float boostT = Mathf.Pow(pullUpT, 1.5f);
            float boost = Mathf.Lerp(1f, PullUpScaleBoost, boostT);

            planeObj.transform.localScale = baseScale * PlaneScaleFactor * basePersp * boost;
        }

        /// <summary>
        /// 飞机在 pullUp 后半段逐渐淡出，不是突然消失。
        /// 通过修改所有 Renderer 的材质 alpha 来实现。
        /// </summary>
        private static void ApplyPlaneFadeOut(GameObject planeObj, float pullUpT)
        {
            if (planeObj == null)
            {
                return;
            }

            float alpha;
            if (pullUpT < FadeOutStartT)
            {
                alpha = 1f;
            }
            else
            {
                // FadeOutStartT ~ 1.0 映射到 1.0 ~ 0.0
                float fadeT = Mathf.Clamp01((pullUpT - FadeOutStartT) / (1f - FadeOutStartT));
                // 用平滑曲线让淡出更自然（先慢后快）
                alpha = 1f - (fadeT * fadeT);
            }

            SetPlaneAlpha(planeObj, alpha);
        }

        //在最终模型画面上统一淡出，使机身与后处理描边保持相同透明度。
        private static void SetPlaneAlpha(GameObject planeObj, float alpha)
        {
            if (planeObj == null)
            {
                return;
            }

            planeObj.GetComponent<PhoenixModelComponent>().Opacity = Mathf.Clamp01(alpha);
        }

        /// <summary>
        /// 让阴影在 pullUp 阶段逐渐消失。
        /// </summary>
        private static void ApplyShadowFade(GameObject shadow, float fadeMultiplier)
        {
            if (shadow == null)
            {
                return;
            }

            Renderer renderer = shadow.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Color c = renderer.sharedMaterial.color;
                c.a *= Mathf.Clamp01(fadeMultiplier);
                renderer.sharedMaterial.color = c;
            }
        }

        // =====================================================================
        // 对象池
        // =====================================================================

        private static GameObject AcquireFromPool(Stack<GameObject> pool, GameObject prefab)
        {
            GameObject obj = null;
            while (pool.Count > 0 && obj == null)
            {
                obj = pool.Pop();
            }

            if (obj == null && prefab != null)
            {
                obj = Object.Instantiate(prefab);
            }

            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
            }

            return obj;
        }

        private static void ReleaseToPool(GameObject obj, Stack<GameObject> pool, int maxPoolSize)
        {
            if (obj == null)
            {
                return;
            }

            if (pool.Count >= maxPoolSize)
            {
                Object.Destroy(obj);
                return;
            }

            if (obj.activeSelf)
            {
                obj.SetActive(false);
            }

            pool.Push(obj);
        }

        // =====================================================================
        // 阴影
        // =====================================================================

        private GameObject AcquireShadowFromPool()
        {
            GameObject shadow = null;
            while (shadowPool.Count > 0 && shadow == null)
            {
                shadow = shadowPool.Pop();
            }

            if (shadow == null)
            {
                shadow = PhoenixUnityBoundary.CreateShadowQuad();
                shadow.name = "PhoenixAirStrikeShadow";

                Renderer renderer = shadow.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = GetOrCreateShadowMaterial();
                    if (material != null)
                    {
                        renderer.sharedMaterial = material;
                    }
                    renderer.shadowCastingMode = RimUnity.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            if (shadow != null && !shadow.activeSelf)
            {
                shadow.SetActive(true);
            }

            return shadow;
        }

        private void UpdateShadowVisual(GameObject shadow, AirStrikeMission mission, Vector3 planePos, Vector3 moveDir)
        {
            if (shadow == null)
            {
                return;
            }

            float height = Mathf.Max(0f, planePos.y - ShadowAltitude);
            float squash = Mathf.Clamp01(1f - (height / Mathf.Max(1f, mission.spawnPos.y + 8f)));
            float scaleFactor = Mathf.Lerp(3.8f, 6.6f, squash);
            Vector3 horizontalDir = moveDir;
            horizontalDir.y = 0f;
            if (horizontalDir.sqrMagnitude < 0.001f)
            {
                horizontalDir = (mission.exitPos - mission.impactPos);
                horizontalDir.y = 0f;
            }
            horizontalDir = horizontalDir.sqrMagnitude > 0.001f ? horizontalDir.normalized : Vector3.forward;
            Vector3 shadowPos = planePos + horizontalDir * ShadowForwardOffset;

            shadow.transform.position = new Vector3(shadowPos.x, ShadowAltitude, shadowPos.z);
            shadow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (!shadowBaseScales.TryGetValue(shadow, out Vector3 baseScale))
            {
                baseScale = shadow.transform.localScale;
                shadowBaseScales[shadow] = baseScale;
            }

            shadow.transform.localScale = new Vector3(baseScale.x * scaleFactor, baseScale.y * scaleFactor, baseScale.z);

            Renderer renderer = shadow.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Color color = ShadowColor;
                color.a = Mathf.Lerp(0.12f, ShadowColor.a, squash);
                renderer.sharedMaterial.color = color;
            }
        }

        private Material GetOrCreateShadowMaterial()
        {
            if (shadowMaterial != null)
            {
                return shadowMaterial;
            }

            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                Log.Warning("[PhoenixRace] Air strike shadow shader was not found. Shadow visual will be skipped.");
                return null;
            }

            shadowMaterial = new Material(shader)
            {
                color = ShadowColor
            };
            shadowMaterial.mainTexture = GetOrCreateShadowTexture();
            return shadowMaterial;
        }

        private Texture2D GetOrCreateShadowTexture()
        {
            if (shadowTexture != null)
            {
                return shadowTexture;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float radius = size * 0.5f;
            Vector2 center = new Vector2(radius - 0.5f, radius - 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (distance / radius));
                    alpha *= alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            shadowTexture = texture;
            return shadowTexture;
        }

        private void CleanupRuntimeVisuals()
        {
            foreach (KeyValuePair<AirStrikeMission, MissionVisual> pair in missionVisuals)
            {
                ReleaseToPool(pair.Value.plane, planePool, PlanePoolCap);
                ReleaseToPool(pair.Value.shadow, shadowPool, ShadowPoolCap);
            }
            missionVisuals.Clear();

            for (int i = 0; i < activeExplosions.Count; i++)
            {
                ReleaseToPool(activeExplosions[i].instance, explosionPool, ExplosionPoolCap);
            }
            activeExplosions.Clear();
        }
    }
}
