using RimWorld.Planet;
using RimUnity;
using Verse;

namespace PhoenixRaceLib.SpaceViewport
{
    //保存凤凰舰船的独立轨道位置，按游戏刻推进航行，不创建地图舰船或搬迁地图。
    public sealed class GameComponent_PhoenixNavigation : GameComponent
    {
        private bool initialized;
        private bool travelling;
        private Vector3 position;
        private Vector3 start;
        private Vector3 destination;
        private Vector3 heading;
        private Vector3 startHeading;
        private int destinationTile = -1;
        private int elapsedTicks;
        private int durationTicks;
        private int turnTicks;
        private float turnAngle;

        public static GameComponent_PhoenixNavigation Instance => Current.Game?.GetComponent<GameComponent_PhoenixNavigation>();
        public bool Initialized => initialized;
        public bool Travelling => travelling;
        public Vector3 Position => position;
        public Vector3 Heading => heading;
        public Vector3 Destination => destination;
        public int DestinationTile => destinationTile;
        public float Progress => durationTicks > 0 ? Mathf.Clamp01((float)elapsedTicks / durationTicks) : 0f;
        public int RemainingTicks => travelling ? durationTicks - elapsedTicks : 0;
        //标明起航前的原地转向阶段。
        public bool Turning => travelling && elapsedTicks < turnTicks;
        //转向期间侧倾，离开转向阶段时自然回正。
        public float BankDegrees => Turning && turnTicks > 0 ? Mathf.Clamp(turnAngle * 0.12f, -14f, 14f) * Mathf.Sin(Mathf.PI * elapsedTicks / turnTicks) : 0f;

        //接收游戏创建组件时的实例，运行状态与界面生命周期分离。
        public GameComponent_PhoenixNavigation(Game game) { }

        //首次打开界面时以有效当前地图为起点；没有地图时保留未定位状态。
        public void InitializeFromMap()
        {
            if (initialized || Find.CurrentMap == null || !Find.CurrentMap.Tile.Valid) return;
            PlanetTile tile = Find.CurrentMap.Tile;
            Initialize((tile.Layer.GetTileCenter(tile) - tile.Layer.Origin).normalized.ToRim());
        }

        //确认人工选择的初始地块，不覆盖已有位置。
        public void SetInitialTile(int tile)
        {
            if (initialized) return;
            Initialize(Direction(tile));
        }

        //建立独立位置与默认沿纬线航向。
        private void Initialize(Vector3 radial)
        {
            position = radial;
            heading = PhoenixOrbitMath.Tangent(radial);
            initialized = true;
        }

        //读取真实地表地块中心的径向方向。
        internal static Vector3 Direction(int tile)
        {
            PlanetLayer surface = Find.WorldGrid.Surface;
            return (surface.GetTileCenter(tile) - surface.Origin).normalized.ToRim();
        }

        //确认目的地并建立航线，同位置或正在航行时拒绝重复命令。
        public bool Depart(int tile)
        {
            if (!initialized || travelling) return false;
            Vector3 target = Direction(tile);
            if (Vector3.Angle(position, target) < 0.01f) return false;
            start = position;
            startHeading = heading;
            destination = target;
            destinationTile = tile;
            elapsedTicks = 0;
            Vector3 tangent = Vector3.Cross(PhoenixOrbitMath.Axis(start, target), start).normalized;
            turnAngle = UnityEngine.Vector3.SignedAngle(startHeading.ToHost(), tangent.ToHost(), start.ToHost());
            turnTicks = TurnDuration(Mathf.Abs(turnAngle));
            durationTicks = PhoenixOrbitMath.Duration(start, target) + turnTicks;
            travelling = true;
            return true;
        }

        //停止航行并保留插值后的实际位置与朝向。
        public void Stop()
        {
            travelling = false;
            destinationTile = -1;
            durationTicks = 0;
            elapsedTicks = 0;
        }

        //预计时间包含起航前调整航向所需的游戏刻。
        public int EstimateTicks(int tile)
        {
            Vector3 target = Direction(tile);
            Vector3 tangent = Vector3.Cross(PhoenixOrbitMath.Axis(position, target), position).normalized;
            float angle = Vector3.Angle(heading, tangent);
            return PhoenixOrbitMath.Duration(position, target) + TurnDuration(angle);
        }

        //按每秒十二度缓慢调整舰艏，小角度也保留三秒的可见转向过程。
        private static int TurnDuration(float angle)
        {
            return angle < 1f ? 0 : Mathf.Clamp(Mathf.CeilToInt(angle / 0.2f), 180, 900);
        }

        //游戏暂停时不会调用刻更新，关闭界面也不影响航行和抵达。
        public override void GameComponentTick()
        {
            if (!travelling) return;
            elapsedTicks = Mathf.Min(elapsedTicks + 1, durationTicks);
            if (elapsedTicks <= turnTicks && turnTicks > 0)
            {
                float turn = Mathf.SmoothStep(0f, 1f, (float)elapsedTicks / turnTicks);
                heading = (Quaternion.AngleAxis(turnAngle * turn, start) * startHeading).normalized;
                return;
            }
            float t = Mathf.Clamp01((float)(elapsedTicks - turnTicks) / Mathf.Max(1, durationTicks - turnTicks));
            float eased = t * t * (3f - 2f * t);
            position = PhoenixOrbitMath.Sample(start, destination, eased);
            Vector3 axis = PhoenixOrbitMath.Axis(start, destination);
            Vector3 tangent = Vector3.Cross(axis, position).normalized;
            heading = tangent;
            if (elapsedTicks == durationTicks)
            {
                position = destination;
                travelling = false;
            }
        }

        //保存纯数据，在读档后继续同一条路线，不保存任何 Unity 对象。
        public override void ExposeData()
        {
            Scribe_Values.Look(ref initialized, "phoenixOrbitInitialized");
            Scribe_Values.Look(ref travelling, "phoenixOrbitTravelling");
            PhoenixUnityBoundary.Look(ref position, "phoenixOrbitPosition");
            PhoenixUnityBoundary.Look(ref start, "phoenixOrbitStart");
            PhoenixUnityBoundary.Look(ref destination, "phoenixOrbitDestination");
            PhoenixUnityBoundary.Look(ref heading, "phoenixOrbitHeading");
            PhoenixUnityBoundary.Look(ref startHeading, "phoenixOrbitStartHeading");
            Scribe_Values.Look(ref destinationTile, "phoenixOrbitDestinationTile", -1);
            Scribe_Values.Look(ref elapsedTicks, "phoenixOrbitElapsed");
            Scribe_Values.Look(ref durationTicks, "phoenixOrbitDuration");
            Scribe_Values.Look(ref turnTicks, "phoenixOrbitTurnTicks");
            Scribe_Values.Look(ref turnAngle, "phoenixOrbitTurnAngle");
        }
    }
}
