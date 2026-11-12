using ChezhouLib.ALLmap;
using System.Collections.Generic;
using RimWorld;
using RimUnity;
using Verse;

namespace PhoenixRaceLib.PhoxGameComp
{
    public class GameComponent_SolarBombardmentManager : GameComponent
    {
        private class BombardmentJob : IExposable
        {
            public int mapId = -1;
            public IntVec3 center = IntVec3.Invalid;
            public float radius = 6.9f;
            public int startTick = -1;
            public int endTick = -1;
            public int spawnedShots = 0;
            public float shotProgress = 0f;
            public Pawn instigator;

            public BombardmentJob()
            {
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref mapId, "mapId", -1);
                Scribe_Values.Look(ref center, "center", IntVec3.Invalid);
                Scribe_Values.Look(ref radius, "radius", 6.9f);
                Scribe_Values.Look(ref startTick, "startTick", -1);
                Scribe_Values.Look(ref endTick, "endTick", -1);
                Scribe_Values.Look(ref spawnedShots, "spawnedShots", 0);
                Scribe_Values.Look(ref shotProgress, "shotProgress", 0f);
                Scribe_References.Look(ref instigator, "instigator");
            }
        }

        private class ActiveShell
        {
            public GameObject instance;
            public int mapId = -1;
            public IntVec3 impactCell = IntVec3.Invalid;
            public Vector3 startPos;
            public Vector3 endPos;
            public int spawnTick = -1;
            public int flightTicks = 36;
            public Pawn instigator;
        }

        private class ActiveExplosion
        {
            public GameObject instance;
            public int releaseTick = -1;
        }

        private const int DurationTicks = 20 * 60;
        private const int TotalShots = 300;
        private const float DamageAmount = 50f;
        private const float DamageRadius = 2f;
        private const float DefaultRadius = 13.8f;
        private const int ShellFlightTicks = 40;
        private const float ShellSpawnOffsetZ = 82.5f;
        private const float ShellScaleFactor = 0.5f;
        private const int ExplosionVisualLifeTicks = 7 * 60;
        private const int PoolCap = 96;

        private List<BombardmentJob> jobs = new List<BombardmentJob>(8);
        private readonly List<ActiveShell> activeShells = new List<ActiveShell>(64);
        private readonly List<ActiveExplosion> activeExplosions = new List<ActiveExplosion>(64);
        private readonly Stack<GameObject> shellPool = new Stack<GameObject>(64);
        private readonly Stack<GameObject> explosionPool = new Stack<GameObject>(64);
        private readonly Dictionary<GameObject, Vector3> shellBaseScales = new Dictionary<GameObject, Vector3>(128);

        public static GameComponent_SolarBombardmentManager Instance =>
            Current.Game?.GetComponent<GameComponent_SolarBombardmentManager>();

        public GameObject taiyanghongzha;
        public GameObject hongzhaExplosion;

        public GameComponent_SolarBombardmentManager(Game game)
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
            Scribe_Collections.Look(ref jobs, "jobs", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                CleanupRuntimeVisuals();
                EnsurePrefabsLoaded();
            }
        }

        public void QueueBombardment(Map map, IntVec3 center, float radius, Pawn instigator)
        {
            if (map == null || !center.IsValid || !center.InBounds(map))
            {
                return;
            }

            int nowTick = Find.TickManager.TicksGame;
            BombardmentJob job = new BombardmentJob
            {
                mapId = map.uniqueID,
                center = center,
                radius = Mathf.Max(0.1f, radius > 0f ? radius : DefaultRadius),
                startTick = nowTick,
                endTick = nowTick + DurationTicks,
                spawnedShots = 0,
                shotProgress = 0f,
                instigator = instigator
            };
            jobs.Add(job);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            EnsurePrefabsLoaded();

            int nowTick = Find.TickManager.TicksGame;
            TickJobs(nowTick);
            TickShells(nowTick);
            TickExplosions(nowTick);
        }

        private void EnsurePrefabsLoaded()
        {
            if (taiyanghongzha == null && ChezhouLib.Assets.ClPrefab.Templates != null)
            {
                if (ChezhouLib.Assets.ClPrefab.Templates.TryGetValue("taiyanghongzha", out GameObject shellPrefab))
                {
                    taiyanghongzha = shellPrefab;
                }
            }

            if (hongzhaExplosion == null && ChezhouLib.Assets.ClPrefab.Templates != null)
            {
                if (ChezhouLib.Assets.ClPrefab.Templates.TryGetValue("hongzhaExplosion", out GameObject explosionPrefab))
                {
                    hongzhaExplosion = explosionPrefab;
                }
            }
        }

        private void TickJobs(int nowTick)
        {
            float shotsPerTick = (float)TotalShots / DurationTicks;

            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                BombardmentJob job = jobs[i];
                Map map = FindMapById(job.mapId);
                if (map == null)
                {
                    jobs.RemoveAt(i);
                    continue;
                }

                if (nowTick > job.endTick || job.spawnedShots >= TotalShots)
                {
                    jobs.RemoveAt(i);
                    continue;
                }

                job.shotProgress += shotsPerTick;
                int spawnCount = Mathf.FloorToInt(job.shotProgress);
                if (spawnCount <= 0)
                {
                    continue;
                }

                job.shotProgress -= spawnCount;
                int remaining = TotalShots - job.spawnedShots;
                if (spawnCount > remaining)
                {
                    spawnCount = remaining;
                }

                for (int shot = 0; shot < spawnCount; shot++)
                {
                    if (TryGetRandomImpactCell(map, job.center, job.radius, out IntVec3 impactCell))
                    {
                        SpawnShell(map, impactCell, nowTick, job.instigator);
                        job.spawnedShots++;
                    }
                }
            }
        }

        private void TickShells(int nowTick)
        {
            for (int i = activeShells.Count - 1; i >= 0; i--)
            {
                ActiveShell shell = activeShells[i];
                int elapsed = nowTick - shell.spawnTick;
                float t = Mathf.Clamp01(elapsed / (float)shell.flightTicks);

                if (shell.instance != null)
                {
                    Vector3 pos = Vector3.Lerp(shell.startPos, shell.endPos, t);
                    shell.instance.transform.position = pos;
                }

                if (elapsed < shell.flightTicks)
                {
                    continue;
                }

                Map map = FindMapById(shell.mapId);
                if (map != null && shell.impactCell.InBounds(map))
                {
                    DoImpact(map, shell.impactCell, shell.instigator, nowTick);
                }

                ReleaseToPool(shell.instance, shellPool);
                activeShells.RemoveAt(i);
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

                ReleaseToPool(explosion.instance, explosionPool);
                activeExplosions.RemoveAt(i);
            }
        }

        private void SpawnShell(Map map, IntVec3 impactCell, int nowTick, Pawn instigator)
        {
            Vector3 endPos = impactCell.ToVector3Shifted().ToRim();
            endPos.y = AltitudeLayer.Projectile.AltitudeFor();

            ActiveShell shell = new ActiveShell
            {
                instance = AcquireFromPool(shellPool, taiyanghongzha),
                mapId = map.uniqueID,
                impactCell = impactCell,
                endPos = endPos,
                spawnTick = nowTick,
                flightTicks = ShellFlightTicks,
                instigator = instigator
            };

            Vector3 startPos = endPos;
            startPos.z = endPos.z + ShellSpawnOffsetZ;
            shell.startPos = startPos;

            if (shell.instance != null)
            {
                shell.instance.transform.position = startPos;
                shell.instance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                ApplyShellScale(shell.instance);
            }

            activeShells.Add(shell);
        }

        private void DoImpact(Map map, IntVec3 impactCell, Pawn instigator, int nowTick)
        {
            GenExplosion.DoExplosion(
                center: impactCell,
                map: map,
                radius: DamageRadius,
                damType: DamageDefOf.Bomb,
                instigator: instigator,
                damAmount: Mathf.RoundToInt(DamageAmount),
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

            GameObject explosionObj = AcquireFromPool(explosionPool, hongzhaExplosion);
            if (explosionObj != null)
            {
                Vector3 explosionPos = impactCell.ToVector3Shifted().ToRim();
                explosionPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                explosionObj.transform.position = explosionPos;
                explosionObj.transform.rotation = Quaternion.Euler(90f, Rand.Range(0f, 360f), 0f);
                activeExplosions.Add(new ActiveExplosion
                {
                    instance = explosionObj,
                    releaseTick = nowTick + ExplosionVisualLifeTicks
                });
            }
        }

        private static bool TryGetRandomImpactCell(Map map, IntVec3 center, float radius, out IntVec3 result)
        {
            int radiusInt = Mathf.CeilToInt(radius);
            float radiusSq = radius * radius;

            for (int i = 0; i < 10; i++)
            {
                int dx = Rand.RangeInclusive(-radiusInt, radiusInt);
                int dz = Rand.RangeInclusive(-radiusInt, radiusInt);
                if ((dx * dx) + (dz * dz) > radiusSq)
                {
                    continue;
                }

                IntVec3 cell = new IntVec3(center.x + dx, 0, center.z + dz);
                if (cell.InBounds(map))
                {
                    result = cell;
                    return true;
                }
            }

            if (center.InBounds(map))
            {
                result = center;
                return true;
            }

            result = IntVec3.Invalid;
            return false;
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

        private void ApplyShellScale(GameObject shellObj)
        {
            if (shellObj == null)
            {
                return;
            }

            if (!shellBaseScales.TryGetValue(shellObj, out Vector3 baseScale))
            {
                baseScale = shellObj.transform.localScale;
                shellBaseScales[shellObj] = baseScale;
            }

            shellObj.transform.localScale = baseScale * ShellScaleFactor;
        }

        private static void ReleaseToPool(GameObject obj, Stack<GameObject> pool)
        {
            if (obj == null)
            {
                return;
            }

            if (pool.Count >= PoolCap)
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

        private void CleanupRuntimeVisuals()
        {
            for (int i = 0; i < activeShells.Count; i++)
            {
                ReleaseToPool(activeShells[i].instance, shellPool);
            }
            activeShells.Clear();

            for (int i = 0; i < activeExplosions.Count; i++)
            {
                ReleaseToPool(activeExplosions[i].instance, explosionPool);
            }
            activeExplosions.Clear();
        }
    }
}
