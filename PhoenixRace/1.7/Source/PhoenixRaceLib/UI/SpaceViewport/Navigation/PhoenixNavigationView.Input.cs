using Input = UnityEngine.Input;
using RimUnity;

namespace PhoenixRaceLib.SpaceViewport
{
    //处理球面命中、旋转、缩放和选区，输入坐标始终来自当前显示的渲染矩形。
    internal sealed partial class PhoenixNavigationView
    {
        private bool onGlobe, dragging, moved;
        private Vector2 pressed, lastMouse;

        //每帧区分点击与拖动，只在过渡结束后接受导航手势。
        private void UpdateInput()
        {
            Vector2 mouse = Input.mousePosition.ToRim();
            onGlobe = Hit(mouse, out Vector3 direction);
            if (!navigating)
            {
                if (transition == 0f && onGlobe && Input.GetMouseButtonDown(0)) Enter();
                return;
            }
            if (transition < 1f) return;
            if (Input.GetMouseButtonDown(0) && onGlobe)
            {
                dragging = true; moved = false; pressed = lastMouse = mouse;
            }
            if (dragging && Input.GetMouseButton(0))
            {
                if ((mouse - pressed).sqrMagnitude > 36f) moved = true;
                if (moved)
                {
                    Vector2 delta = mouse - lastMouse;
                    yaw += delta.x * 0.3f; pitch = Mathf.Clamp(pitch - delta.y * 0.3f, -90f, 90f);
                }
                lastMouse = mouse;
            }
            if (dragging && Input.GetMouseButtonUp(0))
            {
                dragging = false;
                if (!moved && onGlobe && !state.Travelling)
                {
                    selected = index.Pick(direction);
                    selectedDescription = selected >= 0 ? index.Describe(selected) : null;
                    nextTextUpdate = 0f;
                }
            }
            if (onGlobe && Mathf.Abs(Input.mouseScrollDelta.y) > 0f)
                zoom = Mathf.Clamp(zoom * Mathf.Pow(0.94f, Input.mouseScrollDelta.y), 180f, 260f);
            int next = onGlobe && !dragging ? index.Pick(direction) : -1;
            if (next != hovered)
            {
                hovered = next;
                hoverDescription = next >= 0 ? index.Describe(next) : onGlobe ? "该区域没有生成地块，无法选择" : null;
                nextTextUpdate = 0f;
            }
        }

        //将 RawImage 内的鼠标点反投影，解析单位球交点并转换到未旋转的地块坐标。
        private bool Hit(Vector2 screen, out Vector3 direction)
        {
            direction = Vector3.zero;
            RectTransform rect = image.rectTransform;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null)) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out Vector2 local);
            Vector2 uv = new Vector2((local.x - rect.rect.xMin) / rect.rect.width, (local.y - rect.rect.yMin) / rect.rect.height);
            Ray ray = camera.ScreenPointToRay(new Vector3(uv.x * camera.pixelRect.width, uv.y * camera.pixelRect.height, 0f));
            Vector3 origin = sphere.InverseTransformPoint(ray.origin);
            Vector3 vector = sphere.InverseTransformVector(ray.direction).normalized;
            float b = Vector3.Dot(origin, vector);
            float discriminant = b * b - origin.sqrMagnitude + 1f;
            if (discriminant < 0f) return false;
            float distance = -b - Mathf.Sqrt(discriminant);
            if (distance < 0f) return false;
            direction = (origin + vector * distance).normalized;
            return true;
        }
    }
}
