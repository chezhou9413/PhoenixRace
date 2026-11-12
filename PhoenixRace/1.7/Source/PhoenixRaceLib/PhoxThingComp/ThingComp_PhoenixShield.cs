using ChezhouLib.ALLmap;
using PhoenixRaceLib.Patch;
using PhoenixRaceLib.PhoxGizmo;
using PhoenixRaceLib.Tool;
using RimWorld;
using System.Collections.Generic;
using RimUnity;
using Verse;
using Verse.Sound;

namespace PhoenixRaceLib.PhoxThingComp
{
    public enum ShieldState { Inactive, Opening, Active, Closing, Breaking }

    public class ThingCompProperties_PhoenixShield : CompProperties
    {
        public float maxShield = 100f;
        public float minShield = 0f;
        public float shieldRegenRate = 1f;
        public float shieldRateCombatTime = 5f;
        public List<DamageDef> Skip = new List<DamageDef>();
        public ThingCompProperties_PhoenixShield()
        {
            this.compClass = typeof(ThingComp_PhoenixShield);
        }
    }

    public class ThingComp_PhoenixShield : ThingComp
    {
        // ── 原有字段 ─────────────────────────────────────────────────
        public float maxShield = 150f;
        public float minShield = 0f;
        public float curShield = 150f;
        public float shieldRegenRate = 1f;
        public float shieldRateCombatTime = 5f;
        public float curRegCombatTime = 0f;
        public bool isOpen = true;
        public List<DamageDef> Skip = new List<DamageDef>();
        public ThingCompProperties_PhoenixShield Props => (ThingCompProperties_PhoenixShield)props;
        public float transitionProgress = 0f;
        public float transitionSpeed = 0.05f;
        public float TransitionSpeed => transitionSpeed;
        public float dissolveValue = 2f;
        public ShieldState currentState = ShieldState.Inactive;

        public int ticksToReset = -1;
        public bool isBreaking = false;

        // ── 预制体相关 ────────────────────────────────────────────────
        public static GameObject shildPab = ChezhouLib.Assets.ClPrefab.Templates["Phoenix_PhoenixShid"];
        private GameObject shieldInstance = null;
        private Renderer shieldRenderer = null;
        private Material mpb = null;

        // ── 受击点管理 (8 槽循环数组) ───────────────────────────────
        private const int MAX_IMPACTS = 8;
        private Vector4[] impactPoints = new Vector4[MAX_IMPACTS];
        // 缓存 Shader 属性 ID
        private static readonly int[] ImpactPointIDs = new int[]
        {
            Shader.PropertyToID("_ImpactPoint0"),
            Shader.PropertyToID("_ImpactPoint1"),
            Shader.PropertyToID("_ImpactPoint2"),
            Shader.PropertyToID("_ImpactPoint3"),
            Shader.PropertyToID("_ImpactPoint4"),
            Shader.PropertyToID("_ImpactPoint5"),
            Shader.PropertyToID("_ImpactPoint6"),
            Shader.PropertyToID("_ImpactPoint7"),
        };

        private static readonly float DISSOLVE_MAX_CODE = 2f;
        private static readonly float DISSOLVE_MAX_SHADER = 1.2f;

        private float DissolveToShader(float codeValue)
        {
            return Mathf.Clamp01(codeValue / DISSOLVE_MAX_CODE) * DISSOLVE_MAX_SHADER;
        }

        // ── 属性修正 (保持原样) ──
        public float FinalMaxShield
        {
            get
            {
                Pawn wearer = (parent as Apparel)?.Wearer;
                if (wearer == null) return maxShield;
                return (maxShield * wearer.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldMultiplier))
                       + wearer.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldOffset);
            }
        }

        public float FinalRegenRate
        {
            get
            {
                Pawn wearer = (parent as Apparel)?.Wearer;
                if (wearer == null) return shieldRegenRate;
                return (shieldRegenRate * wearer.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldRegenMultiplier))
                       + wearer.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldRegenOffset);
            }
        }

        public int FinalBreakRecoveryTicks
        {
            get
            {
                Pawn wearer = (parent as Apparel)?.Wearer;
                float baseSec = shieldRateCombatTime;
                if (wearer == null) return Mathf.RoundToInt(baseSec * 60f);
                float finalSec = (baseSec + wearer.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldBreakRecoveryTime))
                                 * wearer.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldBreakRecoveryTimeMultiplier);
                return Mathf.Max(0, Mathf.RoundToInt(finalSec * 60f));
            }
        }

        public bool ShieldActive
        {
            get
            {
                Pawn wearer = (parent as Apparel)?.Wearer;
                return isOpen
                    && currentState == ShieldState.Active
                    && ticksToReset <= 0
                    && wearer != null
                    && !wearer.Dead
                    && !wearer.Downed;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.maxShield = Props.maxShield;
            this.minShield = Props.minShield;
            this.shieldRegenRate = Props.shieldRegenRate;
            this.shieldRateCombatTime = Props.shieldRateCombatTime;
            this.Skip = Props.Skip;

            if (isOpen) { currentState = ShieldState.Active; dissolveValue = 0f; }
            else { currentState = ShieldState.Inactive; dissolveValue = 2f; }

            for (int i = 0; i < MAX_IMPACTS; i++)
                impactPoints[i] = Vector4.zero;
        }

        // ── 预制体管理 ──────────────────────────────────────────────
        private void EnsureShieldInstance(Pawn wearer)
        {
            if (shieldInstance == null && shildPab != null && wearer != null && wearer.Spawned)
            {
                shieldInstance = Object.Instantiate(shildPab);
                shieldRenderer = shieldInstance.GetComponentInChildren<Renderer>();
                mpb = shieldRenderer.material;
                ApplyShaderProperties();
                shieldInstance.SetActive(true);
            }
        }

        private void ApplyShaderProperties()
        {
            if (shieldRenderer == null || mpb == null) return;
            

            mpb.SetFloat("_Dissolve", DissolveToShader(dissolveValue));
            mpb.SetColor("_BreakTint", isBreaking ? new Color(3f, 0.2f, 0.1f, 1f) : Color.white);

            // ★ 关键: 把 8 个受击点逐个写入 MPB
            for (int i = 0; i < MAX_IMPACTS; i++)
            {
                mpb.SetVector(ImpactPointIDs[i], impactPoints[i]);
            }

            
        }

        private void UpdateShieldInstance(Pawn wearer)
        {
            if (shieldInstance == null) return;

            if (wearer != null && wearer.Spawned)
            {
                shieldInstance.transform.position = wearer.DrawPos.ToRim();
            }

            bool shouldShow = wearer != null
                && wearer.Spawned
                && !wearer.Dead
                && !wearer.Downed
                && currentState != ShieldState.Inactive;

            if (shieldInstance.activeSelf != shouldShow)
            {
                shieldInstance.SetActive(shouldShow);
            }

            if (shouldShow)
            {
                ApplyShaderProperties();
            }
        }

        private void DestroyShieldInstance()
        {
            if (shieldInstance != null)
            {
                Object.Destroy(shieldInstance);
                shieldInstance = null;
                shieldRenderer = null;
                mpb = null;
            }
        }

        /// <summary>
        /// 触发一次受击波纹
        /// angleDeg: RimWorld 的 dinfo.Angle (抛射体飞行方向, 0°=北, 顺时针)
        /// intensity: 强度 0.8~2.5
        /// radius: 受击点距护盾中心的 UV 距离 (0~0.5), 默认 0.45 贴近边缘
        /// </summary>
        public void AddImpact(float angleDeg, float intensity = 1.5f, float radius = 0.45f)
        {
            // ★ 关键修复: dinfo.Angle 是抛射体飞向的方向,
            // 命中位置在反方向, 所以 +180°
            float impactAngle = angleDeg + 180f;

            // RimWorld: 0°=北, 顺时针 → viewUV: +x 右, +y 上
            float rad = impactAngle * Mathf.Deg2Rad;
            float uvX = 0.5f + Mathf.Sin(rad) * radius;
            float uvY = 0.5f + Mathf.Cos(rad) * radius;

            // 找空槽或最旧
            int slot = 0;
            float oldestTime = float.MaxValue;
            for (int i = 0; i < MAX_IMPACTS; i++)
            {
                if (impactPoints[i].w <= 0.001f)
                {
                    slot = i;
                    break;
                }
                if (impactPoints[i].z < oldestTime)
                {
                    oldestTime = impactPoints[i].z;
                    slot = i;
                }
            }

            impactPoints[slot] = new Vector4(uvX, uvY, Time.time, intensity);
            ApplyShaderProperties();
        }

        // 同时需要更新 CleanupExpiredImpacts 的生命周期计算,
        // 因为现在红色染色可能比波纹持续更久:
        private void CleanupExpiredImpacts()
        {
            if (shieldRenderer == null) return;

            Material mat = shieldRenderer.sharedMaterial;
            if (mat == null) return;

            float speed = mat.HasProperty("_ImpactSpeed") ? mat.GetFloat("_ImpactSpeed") : 2.0f;
            float maxR = mat.HasProperty("_ImpactMaxRadius") ? mat.GetFloat("_ImpactMaxRadius") : 1.8f;
            float redDur = mat.HasProperty("_ImpactRedDuration") ? mat.GetFloat("_ImpactRedDuration") : 1.2f;
            if (speed <= 0.001f) return;

            float waveLife = maxR / speed;
            // 取波纹和红色染色中较长的那个 + 缓冲
            float maxLife = Mathf.Max(waveLife, redDur) + 0.5f;
            float now = Time.time;

            for (int i = 0; i < MAX_IMPACTS; i++)
            {
                if (impactPoints[i].w > 0.001f && (now - impactPoints[i].z) > maxLife)
                {
                    impactPoints[i] = Vector4.zero;
                }
            }
        }

        // ── CompTick ─────────────────────────────────────────────────
        public override void CompTick()
        {
            base.CompTick();

            Pawn wearer = (parent as Apparel)?.Wearer;

            EnsureShieldInstance(wearer);

            switch (currentState)
            {
                case ShieldState.Inactive:
                    if (isOpen && ticksToReset <= 0) currentState = ShieldState.Opening;
                    break;
                case ShieldState.Opening:
                    dissolveValue -= TransitionSpeed;
                    if (dissolveValue <= 0f) { dissolveValue = 0f; currentState = ShieldState.Active; }
                    if (!isOpen) currentState = ShieldState.Closing;
                    break;
                case ShieldState.Active:
                    if (!isOpen) currentState = ShieldState.Closing;
                    break;
                case ShieldState.Closing:
                    dissolveValue += TransitionSpeed;
                    if (dissolveValue >= 2f) { dissolveValue = 2f; currentState = ShieldState.Inactive; }
                    if (isOpen) currentState = ShieldState.Opening;
                    break;
                case ShieldState.Breaking:
                    dissolveValue += TransitionSpeed * 2f;
                    if (dissolveValue >= 2f)
                    {
                        dissolveValue = 2f;
                        currentState = ShieldState.Inactive;
                        ticksToReset = FinalBreakRecoveryTicks;
                        isBreaking = false;
                    }
                    break;
            }

            // 清理过期受击点
            CleanupExpiredImpacts();

            // 同步预制体 (内部会调用 ApplyShaderProperties)
            UpdateShieldInstance(wearer);

            if (ticksToReset > 0)
            {
                ticksToReset--;
                if (ticksToReset <= 0) ResetShield();
                return;
            }

            if (this.parent.IsHashIntervalTick(60))
            {
                if (curRegCombatTime <= 0f) updataShield(FinalRegenRate);
                if (curRegCombatTime > 0f) curRegCombatTime -= 1f;
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            DestroyShieldInstance();
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            DestroyShieldInstance();
        }

        public void TriggerCombatCooldown()
        {
            curRegCombatTime = shieldRateCombatTime;
        }

        public void BreakShield(Pawn pawn)
        {
            if (pawn != null && pawn.Spawned)
            {
                SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.ExplosionFlash, 12f);
                for (int i = 0; i < 6; i++)
                {
                    FleckMaker.ThrowDustPuff(
                        pawn.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f),
                        pawn.Map, Rand.Range(0.8f, 1.2f));
                }
            }

            curShield = minShield;
            curRegCombatTime = 0f;
            dissolveValue = 0f;
            currentState = ShieldState.Breaking;
            isBreaking = true;
        }

        private void ResetShield()
        {
            Pawn pawn = (parent as Apparel)?.Wearer;
            if (pawn != null && pawn.Spawned)
            {
                SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                FleckMaker.ThrowLightningGlow(pawn.TrueCenter(), pawn.Map, 3f);
            }
            ticksToReset = -1;
            isBreaking = false;
            if (isOpen)
            {
                dissolveValue = 2f;
                currentState = ShieldState.Opening;
            }
        }

        public float updataShield(float Shield)
        {
            curShield += Shield;
            if (curShield < minShield)
            {
                float value = minShield - curShield;
                curShield = minShield;
                return value;
            }
            if (curShield > FinalMaxShield)
            {
                curShield = FinalMaxShield;
                return 0;
            }
            return 0;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref isOpen, "isOpen", true);
            Scribe_Values.Look(ref maxShield, "maxShield", 100f);
            Scribe_Values.Look(ref minShield, "minShield", 0f);
            Scribe_Values.Look(ref shieldRegenRate, "shieldRegenRate", 0f);
            Scribe_Values.Look(ref shieldRateCombatTime, "shieldRateCombatTime", 5f);
            Scribe_Values.Look(ref curShield, "curShield", 100f);
            Scribe_Values.Look(ref transitionProgress, "transitionProgress", 0f);
            Scribe_Values.Look(ref transitionSpeed, "transitionSpeed", 0.05f);
            Scribe_Values.Look(ref dissolveValue, "dissolveValue", 2f);
            Scribe_Values.Look(ref currentState, "currentState", ShieldState.Inactive);
            Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
            Scribe_Values.Look(ref isBreaking, "isBreaking", false);
            Scribe_Collections.Look(ref Skip, "Skip", LookMode.Def);
            Scribe_Values.Look(ref curRegCombatTime, "curRegCombatTime", 0f);
            base.PostExposeData();
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            Apparel apparel = parent as Apparel;
            Pawn wearer = apparel?.Wearer;
            if (wearer != null && wearer.Faction == Faction.OfPlayer)
            {
                yield return new Gizmo_PhoenixShield { comp = this };
                yield return new Command_Toggle
                {
                    defaultLabel = "护盾开关",
                    defaultDesc = "手动开启或关闭凤凰护盾。",
                    icon = ContentFinder<UnityEngine.Texture2D>.Get("Eff/shid", true),
                    isActive = () => isOpen,
                    toggleAction = delegate
                    {
                        isOpen = !isOpen;
                        if (isOpen) SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        else SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    }
                };

                // ★ Dev 模式下的测试按钮
                if (Prefs.DevMode)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: 随机受击",
                        defaultDesc = "测试受击波纹",
                        icon = ContentFinder<UnityEngine.Texture2D>.Get("Eff/shid", true),
                        action = () => AddImpact(Rand.Range(0f, 360f), 1.5f)
                    };
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: 三点同时",
                        defaultDesc = "测试多点同时受击",
                        icon = ContentFinder<UnityEngine.Texture2D>.Get("Eff/shid", true),
                        action = () =>
                        {
                            AddImpact(0f, 1.2f);
                            AddImpact(120f, 1.0f);
                            AddImpact(240f, 1.5f);
                        }
                    };
                }
            }
        }
    }
}
