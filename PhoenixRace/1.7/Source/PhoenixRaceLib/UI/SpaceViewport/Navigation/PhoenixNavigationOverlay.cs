using System.Collections.Generic;
using RimUnity;

namespace PhoenixRaceLib.SpaceViewport
{
    //维护地块高亮和球面虚线网格，所有网格由界面实例独占。
    internal sealed class PhoenixNavigationOverlay
    {
        private readonly Mesh hoverMesh = new Mesh { name = "PhoenixHoveredTile" };
        private readonly Mesh targetMesh = new Mesh { name = "PhoenixSelectedTile" };
        private readonly Mesh routeMesh = new Mesh { name = "PhoenixOrbitRoute" };
        private readonly List<Vector3> corners = new List<Vector3>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> indices = new List<int>();
        private readonly List<Color> colors = new List<Color>();
        private int hover = -2, target = -2;
        private Vector3 routeStart, routeEnd;
        private bool routeVisible;

        //绑定预制体具名网格控件，不在运行时创建材质。
        internal PhoenixNavigationOverlay(Transform sphere)
        {
            sphere.Find("NavigationGeometry/HoverRegion").GetComponent<MeshFilter>().sharedMesh = hoverMesh;
            sphere.Find("NavigationGeometry/TargetRegion").GetComponent<MeshFilter>().sharedMesh = targetMesh;
            sphere.Find("NavigationGeometry/Route").GetComponent<MeshFilter>().sharedMesh = routeMesh;
        }

        //只有地块编号改变时重建高亮，并在移动后刷新剩余路线。
        internal void Update(PhoenixPlanetIndex index, int hovered, int selected, GameComponent_PhoenixNavigation state)
        {
            int destination = state.Travelling ? state.DestinationTile : selected;
            if (hover != hovered) { hover = hovered; Region(index, hoverMesh, hover, new Color(0.1f, 0.9f, 1f, 0.6f)); }
            if (target != destination) { target = destination; Region(index, targetMesh, target, new Color(1f, 0.7f, 0.1f, 0.8f)); }
            bool visible = state.Initialized && destination >= 0;
            Vector3 end = visible ? GameComponent_PhoenixNavigation.Direction(destination) : Vector3.zero;
            if (visible == routeVisible && (!visible || (routeStart == state.Position && routeEnd == end))) return;
            routeVisible = visible; routeStart = state.Position; routeEnd = end;
            routeMesh.Clear();
            if (!visible) return;
            vertices.Clear(); indices.Clear(); colors.Clear();
            int segments = Mathf.Clamp(Mathf.CeilToInt(Vector3.Angle(routeStart, routeEnd) * 1.5f), 6, 270);
            for (int i = 0; i < segments; i += 2)
            {
                Vector3 a = PhoenixOrbitMath.Sample(routeStart, routeEnd, (float)i / segments);
                Vector3 b = PhoenixOrbitMath.Sample(routeStart, routeEnd, (float)(i + 1) / segments);
                Vector3 side = Vector3.Cross(a, b).normalized * 0.0025f;
                int start = vertices.Count;
                vertices.Add(a * 1.025f - side); vertices.Add(a * 1.025f + side);
                vertices.Add(b * 1.025f + side); vertices.Add(b * 1.025f - side);
                for (int j = 0; j < 4; j++) colors.Add(new Color(1f, 0.75f, 0.2f, 0.9f));
                indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            Upload(routeMesh);
        }

        //以略高于球面的多边形显示目标和悬浮地块，背面由球面深度遮挡。
        private void Region(PhoenixPlanetIndex index, Mesh mesh, int id, Color color)
        {
            mesh.Clear();
            if (id < 0) return;
            index.GetCorners(id, corners);
            vertices.Clear(); indices.Clear(); colors.Clear();
            vertices.Add(GameComponent_PhoenixNavigation.Direction(id) * 1.012f); colors.Add(color);
            foreach (Vector3 point in corners) { vertices.Add(point * 1.012f); colors.Add(color); }
            for (int i = 0; i < corners.Count; i++) indices.AddRange(new[] { 0, i + 1, (i + 1) % corners.Count + 1 });
            Upload(mesh);
        }

        //上传复用列表中的网格数据，不生成新的材质或网格实例。
        private void Upload(Mesh mesh)
        {
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
        }

        //释放随窗口创建的动态网格。
        internal void Dispose()
        {
            Object.Destroy(hoverMesh); Object.Destroy(targetMesh); Object.Destroy(routeMesh);
        }
    }
}
