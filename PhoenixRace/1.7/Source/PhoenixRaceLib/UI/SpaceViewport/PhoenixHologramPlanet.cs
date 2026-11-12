using System.Collections.Generic;
using RimWorld.Planet;
using RimUnity;
using Verse;

namespace PhoenixRaceLib.SpaceViewport
{
    //负责当前存档的全息地貌网格与舰船地块定位，不承担界面或相机的生命周期。
    internal sealed class PhoenixHologramPlanet
    {
        private readonly Transform sphere;
        private readonly Transform marker;
        private readonly Transform headingRoot;
        private readonly Mesh mesh;

        //以实际世界地貌替换占位球，并取得资源包中的舰船定位节点。
        internal PhoenixHologramPlanet(Transform sphere)
        {
            this.sphere = sphere;
            marker = sphere.Find("HologramDetails/ShipLocation");
            if (marker == null) throw new System.InvalidOperationException("[PhoenixRace] 全息预制体缺少舰船位置模型，请更新 RimUnity 资源。");
            headingRoot = marker.Find("ShipHeading");
            if (headingRoot == null) throw new System.InvalidOperationException("[PhoenixRace] 舰船航向节点缺失，请更新导航 RimUnity 资源。");
            mesh = BuildSurface();
            sphere.GetComponent<MeshFilter>().sharedMesh = mesh;
            UpdateLocation();
        }

        //读取凤凰独立航行状态，模型与球面坐标共用同一位置和切线航向。
        internal void UpdateLocation()
        {
            GameComponent_PhoenixNavigation state = GameComponent_PhoenixNavigation.Instance;
            marker.gameObject.SetActive(state.Initialized);
            if (!state.Initialized) return;
            marker.localPosition = state.Position * 1.035f;
            marker.localRotation = Quaternion.FromToRotation(Vector3.up, state.Position);
            Vector3 localHeading = Quaternion.Inverse(marker.localRotation) * state.Heading;
            //定位环保持贴合球面，舰体与舰艏箭头在独立节点上同步转向。
            headingRoot.localRotation = Quaternion.LookRotation(localHeading, Vector3.up) * Quaternion.Euler(0f, 0f, -state.BankDegrees);
        }

        //从地表多边形生成一次性球面网格，以海陆颜色保留真实存档的大陆轮廓。
        private static Mesh BuildSurface()
        {
            PlanetLayer surface = Find.WorldGrid.Surface;
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> triangles = new List<int>();
            List<UnityEngine.Vector3> corners = new List<UnityEngine.Vector3>(6);
            for (int id = 0; id < surface.TilesCount; id++)
            {
                PlanetTile tile = surface.PlanetTileForID(id);
                corners.Clear();
                surface.GetTileVertices(tile, corners);
                Vector3 center = (surface.GetTileCenter(tile) - surface.Origin).normalized.ToRim();
                bool water = tile.Tile.WaterCovered;
                Color color = water ? new Color(0.018f, 0.12f, 0.21f) : new Color(0.07f, 0.52f, 0.57f);
                float height = water ? 1f : 1.003f;
                int first = vertices.Count;
                vertices.Add(center * height);
                normals.Add(center);
                colors.Add(color);
                foreach (UnityEngine.Vector3 corner in corners)
                {
                    Vector3 normal = (corner - surface.Origin).normalized.ToRim();
                    vertices.Add(normal * height);
                    normals.Add(normal);
                    colors.Add(color);
                }
                for (int i = 0; i < corners.Count; i++)
                {
                    int a = first + 1 + i;
                    int b = first + 1 + (i + 1) % corners.Count;
                    bool outward = Vector3.Dot(Vector3.Cross(vertices[a] - vertices[first], vertices[b] - vertices[first]), center) > 0f;
                    triangles.Add(first);
                    triangles.Add(outward ? a : b);
                    triangles.Add(outward ? b : a);
                }
            }
            Mesh result = new Mesh { name = "PhoenixHologram_WorldSurface", indexFormat = RimUnity.IndexFormat.UInt32 };
            result.SetVertices(vertices);
            result.SetNormals(normals);
            result.SetColors(colors);
            result.SetTriangles(triangles, 0);
            result.RecalculateBounds();
            result.UploadMeshData(true);
            return result;
        }

        //释放当前存档生成的网格，保留资源包提供的共享材质和模型。
        internal void Dispose()
        {
            Object.Destroy(mesh);
        }
    }
}
