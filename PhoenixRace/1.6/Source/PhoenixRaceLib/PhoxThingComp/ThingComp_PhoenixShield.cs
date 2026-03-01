using ChezhouLib.ALLmap;
using PhoenixRaceLib.PhoxGizmo;
using PhoenixRaceLib.Tool;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
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

        // ── 破盾复原倒计时（tick） ────────────────────────────────────
        public int ticksToReset = -1;

        // ── 破盾变红标记 ─────────────────────────────────────────────
        public bool isBreaking = false;

        // ── 预制体相关 ────────────────────────────────────────────────
        public static GameObject shildPab = abDatabase.prefabDataBase["Phoenix_PhoenixShid"];
        private GameObject shieldInstance = null;
        private Renderer shieldRenderer = null;
        private MaterialPropertyBlock mpb = null;

        // ── dissolveValue (0~2) → shader _Dissolve (0~1.2) 的映射 ──
        private static readonly float DISSOLVE_MAX_CODE = 2f;
        private static readonly float DISSOLVE_MAX_SHADER = 1.2f;

        private float DissolveToShader(float codeValue)
        {
            return Mathf.Clamp01(codeValue / DISSOLVE_MAX_CODE) * DISSOLVE_MAX_SHADER;
        }

        // ── 属性修正 ─────────────────────────────────────────────────
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

        // ── Initialize ──────────────────────────────────────────────
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
        }

        // ── 预制体管理 ──────────────────────────────────────────────

        private void EnsureShieldInstance(Pawn wearer)
        {
            if (shieldInstance == null && shildPab != null && wearer != null && wearer.Spawned)
            {
                shieldInstance = Object.Instantiate(shildPab);
                shieldRenderer = shieldInstance.GetComponentInChildren<Renderer>();
                mpb = new MaterialPropertyBlock();
                ApplyShaderProperties();
                shieldInstance.SetActive(true);
            }
        }

        private void ApplyShaderProperties()
        {
            if (shieldRenderer == null || mpb == null) return;
            shieldRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_Dissolve", DissolveToShader(dissolveValue));
            mpb.SetColor("_BreakTint", isBreaking ? new Color(3f, 0.2f, 0.1f, 1f) : Color.white);
            shieldRenderer.SetPropertyBlock(mpb);
        }

        private void UpdateShieldInstance(Pawn wearer)
        {
            if (shieldInstance == null) return;

            // ── 位置跟随 ──
            if (wearer != null && wearer.Spawned)
            {
                shieldInstance.transform.position = wearer.DrawPos;
            }

            // ── 显隐判定 ──
            bool shouldShow = wearer != null
                && wearer.Spawned
                && !wearer.Dead
                && !wearer.Downed
                && currentState != ShieldState.Inactive;

            if (shieldInstance.activeSelf != shouldShow)
            {
                shieldInstance.SetActive(shouldShow);
            }

            // ── 每 Tick 同步属性到 shader ──
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

        // ── CompTick ─────────────────────────────────────────────────
        public override void CompTick()
        {
            base.CompTick();

            Pawn wearer = (parent as Apparel)?.Wearer;

            EnsureShieldInstance(wearer);

            // 状态机
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

            // 同步预制体
            UpdateShieldInstance(wearer);

            // 破盾复原倒计时
            if (ticksToReset > 0)
            {
                ticksToReset--;
                if (ticksToReset <= 0)
                    ResetShield();
                return;
            }

            // 每秒回复
            if (this.parent.IsHashIntervalTick(60))
            {
                if (curRegCombatTime <= 0f)
                    updataShield(FinalRegenRate);
                if (curRegCombatTime > 0f)
                    curRegCombatTime -= 1f;
            }
        }

        // ── 清理 ────────────────────────────────────────────────────

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

        // ── 供 Harmony 补丁调用 ──────────────────────────────────────

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

        // ── PostExposeData ───────────────────────────────────────────
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

        // ── CompGetWornGizmosExtra ───────────────────────────────────
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
                    icon = ContentFinder<Texture2D>.Get("Eff/shid", true),
                    isActive = () => isOpen,
                    toggleAction = delegate
                    {
                        isOpen = !isOpen;
                        if (isOpen) SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        else SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    }
                };
            }
        }
    }
}