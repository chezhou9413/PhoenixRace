using System;
using System.Collections.Generic;
using RimWorld.Planet;
using RimUnity;
using Verse;

namespace PhoenixRaceLib.SpaceViewport
{
    //为当前世界缓存地块中心的空间索引，并以球面多边形边界确认拾取结果。
    internal sealed class PhoenixPlanetIndex
    {
        private readonly Vector3[] centers;
        private readonly int[] order;
        private readonly PlanetLayer surface;
        private readonly List<PlanetTile> neighbors = new List<PlanetTile>();
        private readonly List<Vector3> corners = new List<Vector3>();

        //仅在打开导航界面时构建平衡空间索引，不在游戏刻扫描星球。
        internal PhoenixPlanetIndex()
        {
            surface = Find.WorldGrid.Surface;
            centers = new Vector3[surface.TilesCount];
            order = new int[centers.Length];
            for (int i = 0; i < centers.Length; i++)
            {
                order[i] = i;
                centers[i] = (surface.GetTileCenter(i) - surface.Origin).normalized.ToRim();
            }
            Partition(0, order.Length, 0);
        }

        //沿三个坐标轴轮换中位划分，数组中点即对应树节点。
        private void Partition(int start, int count, int depth)
        {
            if (count < 2) return;
            int axis = depth % 3;
            Array.Sort(order, start, count, Comparer<int>.Create((a, b) => centers[a][axis].CompareTo(centers[b][axis])));
            int half = count / 2;
            Partition(start, half, depth + 1);
            Partition(start + half + 1, count - half - 1, depth + 1);
        }

        //取得当前位置或悬浮点附近的地块编号。
        internal int Nearest(Vector3 direction)
        {
            int best = -1;
            float distance = float.MaxValue;
            Search(0, order.Length, 0, direction, ref best, ref distance);
            return best;
        }

        //裁剪不可能更近的空间分支，避免逐帧遍历全球地块。
        private void Search(int start, int count, int depth, Vector3 point, ref int best, ref float distance)
        {
            if (count == 0) return;
            int half = count / 2, middle = start + half, id = order[middle];
            float squared = (centers[id] - point).sqrMagnitude;
            if (squared < distance) { distance = squared; best = id; }
            float delta = point[depth % 3] - centers[id][depth % 3];
            int leftCount = half, rightCount = count - half - 1;
            if (delta < 0f)
            {
                Search(start, leftCount, depth + 1, point, ref best, ref distance);
                if (delta * delta <= distance) Search(middle + 1, rightCount, depth + 1, point, ref best, ref distance);
            }
            else
            {
                Search(middle + 1, rightCount, depth + 1, point, ref best, ref distance);
                if (delta * delta <= distance) Search(start, leftCount, depth + 1, point, ref best, ref distance);
            }
        }

        //确认最近地块或其邻居的球面边界，不把世界生成区域外的空白误选为地块。
        internal int Pick(Vector3 direction)
        {
            int id = Nearest(direction);
            if (id < 0) return -1;
            if (Contains(id, direction)) return id;
            neighbors.Clear();
            surface.GetTileNeighbors(id, neighbors);
            foreach (PlanetTile tile in neighbors) if (Contains(tile.tileId, direction)) return tile.tileId;
            return -1;
        }

        //以边所在大圆的同侧判定检查凸球面地块。
        private bool Contains(int id, Vector3 direction)
        {
            GetCorners(id, corners);
            for (int i = 0; i < corners.Count; i++)
            {
                Vector3 edge = Vector3.Cross(corners[i], corners[(i + 1) % corners.Count]).normalized;
                if (Vector3.Dot(edge, direction) * Mathf.Sign(Vector3.Dot(edge, centers[id])) < -0.000001f) return false;
            }
            return corners.Count >= 3;
        }

        //输出统一单位球面的多边形顶点，供拾取和高亮共用。
        internal void GetCorners(int id, List<Vector3> output)
        {
            output.Clear();
            var native = new List<UnityEngine.Vector3>();
            surface.GetTileVertices(surface.PlanetTileForID(id), native);
            foreach (var vertex in native) output.Add((vertex - surface.Origin).normalized.ToRim());
        }

        //组织地块的中文位置说明，保留地块编号和经纬度供导航识别。
        internal string Describe(int id)
        {
            if (id < 0) return "未选择区域";
            var coordinates = surface.LongLatOf(id);
            return "区域 " + id + " · " + surface[id].PrimaryBiome.LabelCap + "\n经度 " + coordinates.x.ToString("F1") + "° · 纬度 " + coordinates.y.ToString("F1") + "°";
        }
    }
}
