using RimWorld;
using Verse;
using Verse.AI;

namespace PhoenixRaceLib.Comp
{
    public class HediffComp_DisableAttackAndAbilities : HediffComp
    {
        public HediffCompProperties_DisableAttackAndAbilities Props =>
            (HediffCompProperties_DisableAttackAndAbilities)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Dead)
            {
                return;
            }

            // 每60 tick检查一次（1秒）
            if (pawn.IsHashIntervalTick(60))
            {
                CancelHostileActions(pawn);
            }
        }

        /// <summary>
        /// 取消所有攻击性行为
        /// </summary>
        private void CancelHostileActions(Pawn pawn)
        {
            // 取消当前的攻击Job
            if (pawn.CurJob != null && IsHostileJob(pawn.CurJob))
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }

            // 清除攻击目标
            if (pawn.mindState?.enemyTarget != null)
            {
                pawn.mindState.enemyTarget = null;
            }

            // 如果正在使用能力，中断它
            if (pawn.jobs?.curDriver != null)
            {
                var jobDriver = pawn.jobs.curDriver;
                if (jobDriver.job?.verbToUse != null)
                {
                    // 取消正在进行的技能释放
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }
            }
        }

        /// <summary>
        /// 判断是否为攻击性Job
        /// </summary>
        private bool IsHostileJob(Job job)
        {
            if (job == null) return false;

            // 攻击类Job
            if (job.def == JobDefOf.AttackMelee ||
                job.def == JobDefOf.AttackStatic ||
                job.def == JobDefOf.Wait_Combat ||
                job.def == JobDefOf.UseVerbOnThing)
            {
                return true;
            }

            // 技能释放Job
            if (job.ability != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 在Pawn状态说明中显示
        /// </summary>
        public override string CompTipStringExtra
        {
            get
            {
                return "无法攻击或使用技能";
            }
        }
    }

    /// <summary>
    /// HediffComp属性定义
    /// </summary>
    public class HediffCompProperties_DisableAttackAndAbilities : HediffCompProperties
    {
        public HediffCompProperties_DisableAttackAndAbilities()
        {
            compClass = typeof(HediffComp_DisableAttackAndAbilities);
        }
    }
}
