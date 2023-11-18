using System;

namespace GameDispel
{
    /// <summary>
    /// 游戏显示接口
    /// </summary>
    public interface BlockDisplayer
    {
        public GameMap Map { get; set; }
        /// <summary>
        /// 刷新地图方块显示
        /// </summary>
        /// <param name="callBack">绘制完成调用的回调函数</param>
        public void UpdateDisplay(Action callBack);
        /// <summary>
        /// 方块状态更新
        /// </summary>
        /// <param name="map"></param>
        /// <param name="callBack">完成绘制后执行的回调函数</param>
        /// <param name="eventInfos">方块更新事件信息</param>
        public void BlockActionDisplay(Action callBack, params BlockEventInfo[] eventInfos);
    }
}