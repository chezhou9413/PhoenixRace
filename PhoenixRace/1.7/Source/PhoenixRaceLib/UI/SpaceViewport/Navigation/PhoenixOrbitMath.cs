using RimUnity;

namespace PhoenixRaceLib.SpaceViewport
{
    //提供确定性的球面路线和局部航向计算，供存档状态与显示共用。
    internal static class PhoenixOrbitMath
    {
        //计算最短弧旋转轴，对重合和对跖方向使用稳定的正交轴。
        internal static Vector3 Axis(Vector3 from, Vector3 to)
        {
            Vector3 axis = Vector3.Cross(from, to);
            return axis.sqrMagnitude > 0.00000001f ? axis.normalized : Tangent(from);
        }

        //返回与径向垂直的确定方向，极点处避免叉乘退化。
        internal static Vector3 Tangent(Vector3 radial)
        {
            Vector3 axis = Mathf.Abs(radial.y) < 0.95f ? Vector3.up : Vector3.right;
            return Vector3.Cross(axis, radial).normalized;
        }

        //沿固定旋转轴插值球面最短弧，到达时使用精确终点。
        internal static Vector3 Sample(Vector3 from, Vector3 to, float progress)
        {
            if (progress >= 1f) return to;
            return (Quaternion.AngleAxis(Vector3.Angle(from, to) * progress, Axis(from, to)) * from).normalized;
        }

        //把半圈距离换算为三千六百游戏刻。
        internal static int Duration(Vector3 from, Vector3 to)
        {
            return Mathf.Max(1, Mathf.CeilToInt(Vector3.Angle(from, to) * 20f));
        }
    }
}
