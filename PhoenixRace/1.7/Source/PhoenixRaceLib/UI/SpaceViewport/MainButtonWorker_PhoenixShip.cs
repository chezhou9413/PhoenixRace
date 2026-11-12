using PhoenixRaceLib.PhoxGameComp;
using RimWorld;
using Verse;

namespace PhoenixRaceLib.SpaceViewport
{
    //通过原版底部主按钮栏切换舰船舷窗，并同步按钮的开启状态。
    public sealed class MainButtonWorker_PhoenixShip : MainButtonWorker
    {
        //在没有当前游戏组件时禁用按钮，沿用原版主按钮的其他限制。
        public override bool Disabled => base.Disabled || GameComponent_PhoenixSpaceViewport.Instance == null;

        //界面打开时填满原版按钮状态条，让玩家能识别当前入口。
        public override float ButtonBarPercent => GameComponent_PhoenixSpaceViewport.Instance?.IsVisible == true ? 1f : 0f;

        //交由舷窗组件统一处理选择状态、原版面板和界面开关。
        public override void Activate()
        {
            GameComponent_PhoenixSpaceViewport component = GameComponent_PhoenixSpaceViewport.Instance;
            if (component == null)
            {
                return;
            }
            component.ToggleViewport();
        }
    }
}
