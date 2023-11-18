using System.Collections.Generic;
using UnityEngine;

namespace GameDispel
{
    /// <summary>
    /// 方块系统, 提供一些基础的静态方法作为响应委托
    /// </summary>
    public static class BlockSystem
    {
        /*
        * CR: CursorEventResponse, 意为光标事件响应
        * CA: CheckAction, 意为检查响应动作
        * BD: BlockDestroy, 意为方块销毁时的响应
        */

        #region 光标响应

        /// <summary>
        /// 相邻交换位置
        /// </summary>
        public static void CRChangeBlockPos(Block block, CursorEventInfo info)
        {
            if (info.operate == CursorEventInfo.Operate.Cancel) return;
            if (info.oldPoint.isError()) return;
            //判断是否相邻
            if ((Mathf.Abs(info.aimPoint.x - info.oldPoint.x) == 1 && info.aimPoint.y == info.oldPoint.y) ||
                (Mathf.Abs(info.aimPoint.y - info.oldPoint.y) == 1 && info.aimPoint.x == info.oldPoint.x))
            {
                GameMap map = block.ownerMap;
                //先执行交换操作
                map.ChangeBlockPos(info.aimPoint, info.oldPoint, () =>
                {
                    //交换成功后, 演出结束后自动调用此委托
                    //先添加检查
                    map.MoveCursor(Point.Error);
                    map.AddCheck(info.aimPoint, info.oldPoint);
                    //Debug.Log("交换成功");
                    if (map.CheckBlock())//如果检查有可消除则允许交换,否则再换回来
                    {
                        map.DestroyBlock();//消除方块
                    }
                    else
                    {
                        map.ChangeBlockPos(info.aimPoint, info.oldPoint);
                    }
                });
            }
        }
        /// <summary>
        /// 相邻交换位置, 未消除不还原位置
        /// </summary>
        public static void CRChangeBlockPosNoBack(Block block, CursorEventInfo info)
        {
            if (info.operate == CursorEventInfo.Operate.Cancel) return;
            if (info.oldPoint.isError()) return;
            //判断是否相邻
            if ((Mathf.Abs(info.aimPoint.x - info.oldPoint.x) == 1 && info.aimPoint.y == info.oldPoint.y) ||
                (Mathf.Abs(info.aimPoint.y - info.oldPoint.y) == 1 && info.aimPoint.x == info.oldPoint.x))
            {
                GameMap map = block.ownerMap;
                //先执行交换操作
                map.ChangeBlockPos(info.aimPoint, info.oldPoint, () =>
                {
                    //交换成功后, 演出结束后自动调用此委托
                    //先添加检查
                    map.MoveCursor(Point.Error);
                    map.AddCheck(info.aimPoint, info.oldPoint);
                    //Debug.Log("交换成功");
                    map.CheckAndDestroy();
                });
            }
        }

        #endregion 光标响应

        #region 消除检测
        public const string CA_DISPEL_MORE3 = "CADispelMore3";

        /// <summary>
        /// 一般的三消算法
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static BlockDetectedResponse CADispelMore3(Block block)
        {
            BlockDetectedResponse response = new BlockDetectedResponse();
            //Debug.Log("三消检测");
            GameMap map = block.ownerMap;
            if (map == null)
                return new BlockDetectedResponse()
                {
                    effect = null,
                    combo = 0
                };
            //Debug.Log("所属地图对象确定");
            int dir_w = 0;
            int dir_a = 0;
            int dir_s = 0;
            int dir_d = 0;
            for (int x = block.X - 1; x >= 0; x--)//向左寻找
            {
                Block bk = map.GetBlock(x, block.Y);
                if (bk == null) break;
                if (bk.id == block.id)//ID相同视作同一种类的方块
                {
                    dir_a++;
                }
                else//必须是连续的,否则直接中断循环
                {
                    break;
                }
            }
            for (int x = block.X + 1; x < map.Width; x++)//向右寻找
            {
                Block bk = map.GetBlock(x, block.Y);
                if (bk == null) break;
                if (bk.id == block.id)//ID相同视作同一种类的方块
                {
                    dir_d++;
                }
                else//必须是连续的,否则直接中断循环
                {
                    break;
                }
            }
            for (int y = block.Y - 1; y >= 0; y--)//向上寻找
            {
                Block bk = map.GetBlock(block.X, y);
                if (bk == null) break;
                if (bk.id == block.id)//ID相同视作同一种类的方块
                {
                    dir_w++;
                }
                else//必须是连续的,否则直接中断循环
                {
                    break;
                }
            }
            for (int y = block.Y + 1; y < map.Height; y++)//向下寻找
            {
                Block bk = map.GetBlock(block.X, y);
                if (bk == null) break;
                if (bk.id == block.id)//ID相同视作同一种类的方块
                {
                    dir_s++;
                }
                else//必须是连续的,否则直接中断循环
                {
                    break;
                }
            }
            List<Point> ps = new List<Point>();
            int combo = 0;
            bool destroy = false;
            if (dir_a + dir_d + 1 >= 3)//水平方向
            {
                for (int i = 1; i <= dir_a; i++)
                {
                    ps.Add(new Point(block.X - i, block.Y));
                }
                for (int i = 1; i <= dir_d; i++)
                {
                    ps.Add(new Point(block.X + i, block.Y));
                }
                destroy = true;
                combo++;
            }
            if (dir_w + dir_s + 1 >= 3)//竖直方向
            {
                for (int i = 1; i <= dir_w; i++)
                {
                    ps.Add(new Point(block.X, block.Y - i));
                }
                for (int i = 1; i <= dir_s; i++)
                {
                    ps.Add(new Point(block.X, block.Y + i));
                }
                destroy = true;
                combo++;
            }
            if (destroy) ps.Add(block.position);

            //Debug.Log($"三消原始点:{block.position}");
            for (int i = 0; i < ps.Count; i++)
            {
                //Debug.Log($"三消确定销毁点:{ps[i].ToString()}");
            }

            //Debug.Log($"dir_w:{dir_w},dir_s:{dir_s},dir_a:{dir_a},dir_d:{dir_d}");
            response.effect = ps.ToArray();
            response.combo = combo;
            return response;
        }




        #endregion 消除检测

        #region 销毁响应

        /// <summary>
        /// 普通方块消失处理: 仅销毁自身
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static BlockDestroyResponse BDNormal(Block block)
        {
            return new BlockDestroyResponse()
            {
                destroyPoints = null,
                state = ActionResponse.Comfirm
            };
        }
        /// <summary>
        /// 销毁响应: 消除同一行
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static BlockDestroyResponse BDDestroyRow(Block block)
        {
            Point[] ps = new Point[block.ownerMap.Width];
            for(int i=0;i<block.ownerMap.Width;i++)
            {
                ps[i] = new Point(i,block.Y);
            }
            return new BlockDestroyResponse()
            {
                destroyPoints = ps,
                state = ActionResponse.Comfirm
            };
        }

        #endregion 销毁响应
    }
}