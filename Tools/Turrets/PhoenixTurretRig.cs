using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

//按实体连通部分拆出三根完整炮管，为每根炮管建立独立刚性后坐节点。
public static class PhoenixTurretRig
{
    //焊接同位置顶点只用于识别连通关系，导出的法线与 UV 分界保持原样。
    public static void Split(Transform yaw, Mesh source, Material material, string directory)
    {
        Vector3[] vertices = source.vertices;
        int[] triangles = source.triangles;
        int[] parents = Enumerable.Range(0, vertices.Length).ToArray();
        int Root(int index) => parents[index] == index ? index : parents[index] = Root(parents[index]);
        void Join(int a, int b) => parents[Root(a)] = Root(b);
        var weld = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3Int key = Vector3Int.RoundToInt(vertices[i] * 100000);
            if (weld.TryGetValue(key, out int previous)) Join(i, previous);
            else weld.Add(key, i);
        }
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Join(triangles[i], triangles[i + 1]);
            Join(triangles[i], triangles[i + 2]);
        }
        var groups = new Dictionary<int, List<int>>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int key = Root(triangles[i]);
            if (!groups.TryGetValue(key, out var group)) groups.Add(key, group = new List<int>());
            group.AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
        }
        Bounds BoundsOf(List<int> indices)
        {
            Bounds bounds = new(vertices[indices[0]], Vector3.zero);
            foreach (int index in indices) bounds.Encapsulate(vertices[index]);
            return bounds;
        }
        var barrels = groups.Values.Where(g => BoundsOf(g).max.z > source.bounds.max.z - .01f)
            .OrderBy(g => BoundsOf(g).center.x).ToArray();
        if (barrels.Length != 3) throw new InvalidDataException("炮管连通部分应为三个，实际为：" + barrels.Length);
        var housing = groups.Values.Except(barrels).SelectMany(g => g).ToList();
        MakePart(yaw, "Housing", source, housing, material, directory);
        for (int i = 0; i < barrels.Length; i++)
        {
            Transform bone = PhoenixTurretExport.Child(yaw, "barrel_" + i);
            MakePart(bone, "Barrel" + i, source, barrels[i], material, directory);
            Vector3[] tips = barrels[i].Select(index => vertices[index]).Where(v => v.z > source.bounds.max.z - .015f).ToArray();
            Bounds tip = new(tips[0], Vector3.zero);
            foreach (Vector3 point in tips) tip.Encapsulate(point);
            Transform muzzle = PhoenixTurretExport.Child(bone, "Muzzle");
            muzzle.localPosition = tip.center;
            Debug.Log($"TURRET_BARREL {i} triangles={barrels[i].Count / 3} muzzle={muzzle.position:F5}");
        }
    }

    //每个刚性部分仍共用同一个烘焙材质。
    private static void MakePart(Transform parent, string name, Mesh source, List<int> indices, Material material, string directory)
    {
        Vector3[] sourceVertices = source.vertices, sourceNormals = source.normals;
        Vector2[] sourceUv = source.uv;
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        var remap = new Dictionary<int, int>();
        foreach (int index in indices)
        {
            if (!remap.TryGetValue(index, out int target))
            {
                target = vertices.Count;
                remap.Add(index, target);
                vertices.Add(sourceVertices[index]); normals.Add(sourceNormals[index]); uv.Add(sourceUv[index]);
            }
            triangles.Add(target);
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh = PhoenixTurretExport.SaveAsset(mesh, directory + "/" + name + ".asset");
        GameObject node = PhoenixTurretExport.Child(parent, name).gameObject;
        node.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = node.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }
}
