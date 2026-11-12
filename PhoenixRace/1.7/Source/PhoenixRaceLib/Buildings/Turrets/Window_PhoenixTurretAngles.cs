using UnityEngine;
using Verse;

namespace PhoenixRaceLib.Buildings.Turrets;

//暂停后通过地图上的真实模型预览姿态，不向炮塔逻辑写入调试角度。
internal sealed class Window_PhoenixTurretAngles : Window
{
    private const string Hint = "拖动滑条观察地图上的炮塔。仅预览外观，关闭后恢复正常姿态。预览期间暂停，可移动镜头和窗口。";
    private readonly CompPhoenixTurretModel model;
    private float yaw, pitch;
    private Vector2 scrollPosition;
    public override Vector2 InitialSize => new(440, Mathf.Min(UI.screenHeight - 60, 420));

    //沿用原版暂停与禁止保存机制，镜头可移动以便观察细节。
    internal Window_PhoenixTurretAngles(CompPhoenixTurretModel model)
    {
        this.model = model;
        optionalTitle = "凤凰炮塔角度预览";
        doCloseX = doCloseButton = draggable = forcePause = preventSave = true;
        preventCameraMotion = false;
        closeOnAccept = false;
    }

    //旧窗口关闭后再读取真实朝向，避免连续打开时保留上次预览。
    public override void PreOpen()
    {
        base.PreOpen();
        ResetAngles();
    }

    //默认靠右放置，留出地图中央查看模型。
    protected override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        windowRect.x = UI.screenWidth - windowRect.width - 20;
        windowRect.y = 30;
    }

    //切换地图、拆除炮塔或退出开发者模式时结束视觉覆盖。
    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (!Prefs.DevMode || !model.parent.Spawned || model.parent.Map != Find.CurrentMap) Close();
    }

    //关闭和按 Esc 均恢复由战斗逻辑控制的正常姿态。
    public override void PostClose()
    {
        model.EndPreview();
        base.PostClose();
    }

    //测量中文说明与标签，滚动内容始终避开原版关闭按钮。
    public override void DoWindowContents(Rect inRect)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        bool oldWrap = Text.WordWrap;
        Color oldColor = GUI.color;
        try
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = Color.white;
            Rect body = new(0, 0, inRect.width, inRect.height - FooterRowHeight);
            float width = body.width - 16;
            float heading = Text.CalcHeight(model.parent.LabelCap, width);
            float hint = Text.CalcHeight(Hint, width);
            float button = Mathf.Max(36, Text.LineHeight + 12);
            float height = heading + hint + 24 + (Text.LineHeight + 40) * 2 + button;
            Widgets.BeginScrollView(body, ref scrollPosition, new Rect(0, 0, width, Mathf.Max(body.height, height)));
            try
            {
                Widgets.Label(new Rect(0, 0, width, heading), model.parent.LabelCap);
                float y = heading + 8;
                Widgets.Label(new Rect(0, y, width, hint), Hint);
                y += hint + 16;
                float oldYaw = yaw, oldPitch = pitch;
                yaw = DrawAngle(width, ref y, "水平旋转", yaw, 0, 360);
                pitch = DrawAngle(width, ref y, "炮管俯仰", pitch, -90, 90);
                if (Widgets.ButtonText(new Rect(0, y, width, button), "恢复原姿态")) ResetAngles();
                else if (yaw != oldYaw || pitch != oldPitch) model.SetPreviewAngles(yaw, pitch);
            }
            finally { Widgets.EndScrollView(); }
        }
        finally
        {
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            Text.WordWrap = oldWrap;
            GUI.color = oldColor;
        }
    }

    //连续调整角度，正俯仰表示抬高炮口。
    private static float DrawAngle(float width, ref float y, string label, float value, float min, float max)
    {
        Widgets.Label(new Rect(0, y, width, Text.LineHeight), $"{label}：{value:F1}°（{min:F0}° ～ {max:F0}°）");
        y += Text.LineHeight + 4;
        value = Widgets.HorizontalSlider(new Rect(0, y, width, 24), value, min, max,
            middleAlignment: true, roundTo: 0.1f);
        y += 36;
        return value;
    }

    //恢复逻辑朝向与零俯仰，仍可继续拖动滑条。
    private void ResetAngles()
    {
        yaw = model.CurrentYaw;
        pitch = 0;
        model.SetPreviewAngles(yaw, pitch);
    }
}
