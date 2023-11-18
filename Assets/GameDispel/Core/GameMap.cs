using ER;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

namespace GameDispel
{
    /// <summary>
    /// 方块地图(用于管理方块数据)
    /// </summary>
    public class BMap : Map<Block>
    {
        public BMap(int width, int height) : base(width, height)
        {
            for (int i = 0; i < Size; i++)
            {
                array[i] = Block.Empty;
            }
        }

        public bool IsEmptyBlock(int x, int y)
        {
            Block block = this[x, y];
            return block == null || block.isEmpty();
        }
    }

    /// <summary>
    /// 消消乐游戏: 游戏系统主体
    /// </summary>
    public class GameMap
    {
        /**
         * combo 计算方法:
         * 在检测指定方块时, 若存在消除时, 则将其封装成一个 消除事件;
         * 由于是针对 检测点进行 combo 增加的, 会存在以下情况导致 combo 重复计算:
         *          例如 (0,0) (0,1) (0,2) 连成三消, 但是 其中(0,0) 和 (0,1) 是检测点,
         *          那么这两个检测点的结果都会是 存在消除事件 而导致 combo+2(本来应该是combo+1)
         * 为了消除这种重复计算 combo ,在结算combo时,通过比对消除事件受影响的 位点, 以及消除源 的方式种类, 来排除这种重复计算
         * 这里定义为:
         * 如果 消除源 的消除方式不同则认定为 不同的消除事件, 独立计算 combo
         * 如果 消除源 的消除方式相同, 但是受影响的 位点不完全相同, 则视作不同消除事件, 独立计算 combo
         * 如果 两个 事件被判定为同一事件, 则保留增加combo数更多的
         */

        /// <summary>
        /// 消除事件
        /// </summary>
        private struct DispelEvent
        {
            /// <summary>
            /// 事件源
            /// </summary>
            public Point origin;

            /// <summary>
            /// 消除事件类型(一般传入执行检测行为的委托名)
            /// </summary>
            public string type;

            /// <summary>
            /// 影响区域
            /// </summary>
            public Point[] effect;
            /// <summary>
            /// 该事件增加的combo数
            /// </summary>
            public int combo;

            public override bool Equals(object obj)
            {
                return obj is DispelEvent @event &&
                       EqualityComparer<Point>.Default.Equals(origin, @event.origin) &&
                       type == @event.type &&
                       EqualityComparer<Point[]>.Default.Equals(effect, @event.effect);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(origin);
            }

            public static bool operator ==(DispelEvent dispel_1, DispelEvent dispel_2)
            {
                if (dispel_1.type != dispel_2.type) return false;//事件类型不同 则不同
                if (dispel_1.effect.Length != dispel_2.effect.Length) return false;//影响区域不同 则不同
                foreach (var p in dispel_1.effect)//如果区域数量相同,则判断是否完全相同
                {
                    if (!dispel_2.effect.Contains(p))
                    {
                        return false;
                    }
                }
                return true;
            }

            public static bool operator !=(DispelEvent dispel_1, DispelEvent dispel_2)
            {
                return !(dispel_1 == dispel_2);
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder($"事件源:{origin.ToString()},type:{type},effect:\n");
                foreach(var e in effect)
                {
                    sb.Append(e.ToString()+"\n");
                }
                return sb.ToString();
            }
        }

        #region 地图属性

        /// <summary>
        /// 得分
        /// </summary>
        public float score;
        /// <summary>
        /// 连击
        /// </summary>
        public int Combo
        {
            get => combo;
            set
            {
                if (!init) return;
                int old = combo;
                combo = value;
                if(old < combo )
                    OnComboAdd?.Invoke(combo);
            }
        }

        /// <summary>
        /// 连击
        /// </summary>
        private int combo;

        private BMap map;

        public int Width => map.Width;
        public int Height => map.Height;

        /// <summary>
        /// 当前光标位置 (-1,-1)表示光标不存在
        /// </summary>
        private Point cursor;
        /// <summary>
        /// 是否已初始化,
        /// 未初始化则不开启 combo 计数
        /// </summary>
        private bool init = false;

        /// <summary>
        /// 地图中非空方块数量
        /// </summary>
        public int BlockCount
        {
            get
            {
                int sum = 0;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (map[x, y].isEmpty()) continue;
                        sum++;
                    }
                }
                return sum;
            }
        }

        /// <summary>
        /// 显示器
        /// </summary>
        public BlockDisplayer displayer;

        /// <summary>
        /// 方块生成器
        /// </summary>
        public BlockCreator blockCreator;

        #endregion 地图属性

        #region 事件接口

        /// <summary>
        /// 当发生光标操作时触发
        /// </summary>
        public event Action<CursorEventInfo> OnCursorOperate;

        /// <summary>
        /// 当发生消除时(combo增加)触发的事件
        /// </summary>
        public event Action<int> OnComboAdd;

        /// <summary>
        /// 当分数增加时触发的事件
        /// </summary>
        public event Action<float> OnScoreAdd;

        #endregion 事件接口

        #region 缓存组

        /// <summary>
        /// 消除事件缓存
        /// </summary>
        private List<DispelEvent> dispelEvents = new List<DispelEvent>();
        /// <summary>
        /// 有效的消除事件 缓存
        /// </summary>
        private List<DispelEvent> dispelEventEffect = new List<DispelEvent>();

        /// <summary>
        /// 检查方块的坐标
        /// </summary>
        private List<Point> checkBlocks = new List<Point>();

        /// <summary>
        /// 销毁方块名单
        /// </summary>
        private List<Point> destroyBlocks = new List<Point>();

        #endregion 缓存组

        public GameMap(int width, int height)
        {
            map = new BMap(width, height);
        }

        #region 对外接口

        public Point GetCursorPos()
        {
            return new Point(cursor.x,cursor.y);
        }

        /// <summary>
        /// 操作光标; 如果重复操作一个位置两次, 则视作该位置的取消动作
        /// </summary>
        /// <param name="point">操作位置</param>
        public void MoveCursor(Point point)
        {

            CursorEventInfo info = new CursorEventInfo()
            {
                aimPoint = point,
                oldPoint = cursor
            };
            //生成操作事件信息, 并改变光标位置
            if (cursor == point || !InRange(point))//两坐标相等, 表示取消操作
            {
                info.operate = CursorEventInfo.Operate.Cancel;
                cursor = Point.Error;
            }
            else
            {
                info.operate = CursorEventInfo.Operate.Confirm;
                cursor = point;
            }

            combo = 0;//连击清零
            dispelEvents.Clear();
            dispelEventEffect.Clear();

            //触发事件, 发送事件消息
            OnCursorOperate?.Invoke(info);
            BlockCursorEventResponse(info);
        }

        /// <summary>
        /// 添加需要检查的点
        /// </summary>
        /// <param name="ps"></param>
        public void AddCheck(params Point[] ps)
        {
            foreach (var p in ps)
            {
                Debug.Log($"已添加检查点:{p.ToString()}");
            }
            checkBlocks.Add(ps);
        }

        /// <summary>
        /// 添加需要检查的点
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void AddCheck(int x, int y)
        {
            Debug.Log($"已添加检查点:({x},{y})");
            checkBlocks.Add(new Point(x, y));
        }

        /// <summary>
        /// 添加需要销毁的点
        /// </summary>
        /// <param name="ps"></param>
        public void AddDestroy(params Point[] ps)
        {
            destroyBlocks.Add(ps);
        }

        /// <summary>
        /// 检查并销毁可销毁的方块
        /// </summary>
        public void CheckAndDestroy()
        {
            if (CheckBlock())
            {
                DestroyBlock();
            }
        }

        /// <summary>
        /// 检查方块
        /// </summary>
        /// <param name="record">是否更新销毁检查区</param>
        /// <returns>是否存在可销毁的方块</returns>
        public bool CheckBlock(bool record = true)
        {
            bool dst = false;
            List<Point> points = new List<Point>();
            //Debug.Log("正在检查方块消除性...");
            foreach (Point p in checkBlocks)
            {
                Block block = GetBlock(p);

                Debug.Log($"检查方块:{block != null} 坐标:({p.x},{p.y}),委托:{block.OnDetected != null}");
                BlockDetectedResponse response = block.OnDetected(block);
                if (response.combo > 0 && response.effect != null && response.effect.Length > 0)//添加需要销毁的方块坐标
                {
                    //记作一次消除, 连击增加
                    points.Add(response.effect);
                    DispelEvent dispelEvent = new DispelEvent()
                    {
                        origin = p,
                        type = block.CheckType,
                        effect = response.effect,
                        combo = response.combo,
                    };
                    AddComboEvent(dispelEvent);
                    dst = true;
                }
            }

            //结算combo数
            UpdateCombo();

            checkBlocks.Clear();
            if (record)
                destroyBlocks.Add(points);
            return dst;
        }

        /// <summary>
        /// 添加combo事件(自动剔除重复性事件)
        /// </summary>
        private void AddComboEvent(DispelEvent de)
        {
            dispelEvents.Add(de);
            bool contains = false;
            for(int i=0;i<dispelEventEffect.Count;i++)
            {
                if (dispelEventEffect[i] == de)
                {
                    contains = true;
                    break;
                }
            }
            if(!contains)
                dispelEventEffect.Add(de);
        }
        /// <summary>
        /// 更新Combo数
        /// </summary>
        private void UpdateCombo()
        {
            int combo = 0;
            foreach(var dee in dispelEventEffect)
            {
                combo+=dee.combo;
            }
            Combo = combo;
        }

        /// <summary>
        /// 销毁方块
        /// </summary>
        public void DestroyBlock()
        {
            /*
            foreach (var e in destroyBlocks)
            {
                Debug.Log($"初始等待销毁的方块: {e.ToString()}");
            }*/

            List<BlockEventInfo> infos = new List<BlockEventInfo>();
            Queue<Point> waitDestroy = new Queue<Point>();//待销毁执行的方块
            List<Point> destroyed = new List<Point>();
            foreach (Point p in destroyBlocks)
            {
                waitDestroy.Enqueue(p);
            }
            destroyBlocks.Clear();

            while (waitDestroy.Count > 0)
            {
                Point check = waitDestroy.Dequeue();
                //Debug.Log($"正在检查的点:{check.ToString()}");
                if (destroyed.Contains(check)) continue;
                destroyed.Add(check);

                Block block = GetBlock(check);
                Debug.Log($"销毁方块:{block != null} 坐标:({block.X},{block.Y}),委托:{block.OnDestroy != null}");
                BlockDestroyResponse response = block.OnDestroy(block);

                //执行摧毁指令
                switch (response.state)
                {
                    case ActionResponse.Cancel:
                        break;

                    case ActionResponse.Pass:
                        AddScore(block);
                        break;

                    case ActionResponse.Comfirm:
                        //更新摧毁队列
                        if (response.destroyPoints != null)
                        {
                            foreach (Point tp in response.destroyPoints)
                            {
                                Debug.Log($"追加更新销毁:{tp.ToString()}");
                                if (destroyed.Contains(tp))
                                {
                                    waitDestroy.Enqueue(tp);
                                }
                            }
                        }
                        //常规摧毁逻辑
                        AddScore(block);
                        map[check.x, check.y] = Block.Empty;
                        //同步显示信息
                        BlockEventInfo info = new BlockEventInfo()
                        {
                            state = BlockEventInfo.Status.Destory,
                            block = block,
                            position = block.position
                        };
                        infos.Add(info);
                        break;
                }
            }

            displayer.BlockActionDisplay(() => FillBlocks(), infos.ToArray());
        }
        /// <summary>
        /// 消除指定位点上的方块, 但是不触发其消除效果
        /// </summary>
        /// <param name="ps"></param>
        public void DestroyBlockWithoutEffect(params Point[] ps)
        {
            List<BlockEventInfo> infos = new List<BlockEventInfo>();
            if (ps == null || ps.Length == 0) return;
            for (int i = 0; i < ps.Length; i++)
            {
                Block block = GetBlock(ps[i]);
                map[ps[i].x,ps[i].y] = Block.Empty;
                //同步显示信息
                BlockEventInfo info = new BlockEventInfo()
                {
                    state = BlockEventInfo.Status.Destory,
                    block = block,
                    position = block.position
                };
                infos.Add(info);
            }
        }

        private void FillBlockUp()
        {
            List<BlockEventInfo> infos = new List<BlockEventInfo>();//方块更新信息
            for (int x = 0; x < Width; x++)
            {
                //Debug.Log($"正在检查第{x}列空穴");
                int y = 0;//索引标记
                int u = Height - 1;//越界位置标记(新生成的方块位置标记)
                bool blockChecked = false;//是否检测到空方块
                bool topEmpty = false;//顶层是否为空

                while (map.IsInRangeY(y))
                {
                    //Debug.Log($"正在检查空缺:{map[x, y].isEmpty()}");
                    //非空位置直接跳过
                    if (!map.IsEmptyBlock(x, y))
                    {
                        y++;
                        continue;
                    }

                    //在第一次检测到空方块时将空方块上方所有位点加入检测
                    if (!blockChecked)
                    {
                        blockChecked = true;
                        Debug.Log($"开始添加需要检查点,起始点:({x},{y})");
                        for (int i = y; map.IsInRangeY(i); i++)
                        {
                            AddCheck(new Point(x, i));
                        }
                    }

                    int k = y + 1;//k作为向上层搜索的索引
                    while (!topEmpty)//顶层非空时
                    {
                        if (!map.IsInRangeY(k))//k越界表明顶层全为空,跳出
                        {
                            topEmpty = true;
                            break;
                        }
                        if (map[x, k].isEmpty())//寻找第一个不为空的位置
                        {
                            k++;
                            continue;
                        }

                        //方块位置移动, 封装消息
                        map[x, y] = map[x, k];
                        map[x, y].position = new Point(x, y);
                        map[x, k] = Block.Empty;
                        BlockEventInfo info2 = new BlockEventInfo()
                        {
                            block = map[x, y],
                            position = new Point(x, k),
                            state = BlockEventInfo.Status.Move
                        };
                        infos.Add(info2);
                        break;
                    }

                    //特殊情况处理:顶部为空
                    if (topEmpty)
                    {
                        Block block = CreateBlock(x, y);//数据上直接填充到该空位置
                        map[x, y] = block;
                        //block.PrintState();
                        u++;
                        BlockEventInfo info = new BlockEventInfo
                        {
                            block = block,
                            position = new Point(x, u),//显示层面, 新创建的方块移动起始点为(x,u)
                            state = BlockEventInfo.Status.Move,
                        };
                        infos.Add(info);
                    }
                    y++;
                }
            }
            //更新显示画面, 并在刷新画面之后 检查并销毁可销毁的方块
            displayer.BlockActionDisplay(CheckAndDestroy, infos.ToArray());
        }

        private void FillBlockDown()
        {
            List<BlockEventInfo> infos = new List<BlockEventInfo>();//方块更新信息
            for (int x = 0; x < Width; x++)
            {
                //Debug.Log($"正在检查第{x}列空穴");
                int y = Height - 1;//索引标记
                int u = 0;//越界位置标记(新生成的方块位置标记)
                bool blockChecked = false;//是否检测到空方块
                bool topEmpty = false;//顶层是否为空

                while (map.IsInRangeY(y))
                {
                    //Debug.Log($"正在检查空缺:{map[x, y].isEmpty()}");
                    //非空位置直接跳过
                    if (!map.IsEmptyBlock(x, y))
                    {
                        y--;
                        continue;
                    }

                    //在第一次检测到空方块时将空方块上方所有位点加入检测
                    if (!blockChecked)
                    {
                        blockChecked = true;
                        Debug.Log($"开始添加需要检查点,起始点:({x},{y})");
                        for (int i = y; map.IsInRangeY(i); i--)
                        {
                            AddCheck(new Point(x, i));
                        }
                    }

                    int k = y - 1;//k作为向上层搜索的索引
                    while (!topEmpty)//顶层非空时
                    {
                        if (!map.IsInRangeY(k))//k越界表明顶层全为空,跳出
                        {
                            topEmpty = true;
                            break;
                        }
                        if (map[x, k].isEmpty())//寻找第一个不为空的位置
                        {
                            k--;
                            continue;
                        }

                        //方块位置移动, 封装消息
                        map[x, y] = map[x, k];
                        map[x, y].position = new Point(x, y);
                        map[x, k] = Block.Empty;
                        BlockEventInfo info2 = new BlockEventInfo()
                        {
                            block = map[x, y],
                            position = new Point(x, k),
                            state = BlockEventInfo.Status.Move
                        };
                        infos.Add(info2);
                        break;
                    }

                    //特殊情况处理:顶部为空
                    if (topEmpty)
                    {
                        Block block = CreateBlock(x, y);//数据上直接填充到该空位置
                        map[x, y] = block;
                        //block.PrintState();
                        u--;
                        BlockEventInfo info = new BlockEventInfo
                        {
                            block = block,
                            position = new Point(x, u),//显示层面, 新创建的方块移动起始点为(x,u)
                            state = BlockEventInfo.Status.Move,
                        };
                        infos.Add(info);
                    }
                    y--;
                }
            }
            //更新显示画面, 并在刷新画面之后 检查并销毁可销毁的方块
            displayer.BlockActionDisplay(CheckAndDestroy, infos.ToArray());
        }

        private void FillBlockLeft()
        {
            List<BlockEventInfo> infos = new List<BlockEventInfo>();//方块更新信息
            for (int y = 0; y < Height; y++)
            {
                //Debug.Log($"正在检查第{x}列空穴");
                int x = Width - 1;//索引标记
                int u = 0;//越界位置标记(新生成的方块位置标记)
                bool blockChecked = false;//是否检测到空方块
                bool topEmpty = false;//顶层是否为空

                while (map.IsInRangeX(x))
                {
                    //Debug.Log($"正在检查空缺:{map[x, y].isEmpty()}");
                    //非空位置直接跳过
                    if (!map.IsEmptyBlock(x, y))
                    {
                        x--;
                        continue;
                    }

                    //在第一次检测到空方块时将空方块上方所有位点加入检测
                    if (!blockChecked)
                    {
                        blockChecked = true;
                        Debug.Log($"开始添加需要检查点,起始点:({x},{y})");
                        for (int i = x; map.IsInRangeX(i); i--)
                        {
                            AddCheck(new Point(i, y));
                        }
                    }

                    int k = x - 1;//k作为向上层搜索的索引
                    while (!topEmpty)//顶层非空时
                    {
                        if (!map.IsInRangeX(k))//k越界表明顶层全为空,跳出
                        {
                            topEmpty = true;
                            break;
                        }
                        if (map[k, y].isEmpty())//寻找第一个不为空的位置
                        {
                            k--;
                            continue;
                        }

                        //方块位置移动, 封装消息
                        map[x, y] = map[k, y];
                        map[x, y].position = new Point(x, y);
                        map[k, y] = Block.Empty;
                        BlockEventInfo info2 = new BlockEventInfo()
                        {
                            block = map[x, y],
                            position = new Point(k, y),
                            state = BlockEventInfo.Status.Move
                        };
                        infos.Add(info2);
                        break;
                    }

                    //特殊情况处理:顶部为空
                    if (topEmpty)
                    {
                        Block block = CreateBlock(x, y);//数据上直接填充到该空位置
                        map[x, y] = block;
                        //block.PrintState();
                        u--;
                        BlockEventInfo info = new BlockEventInfo
                        {
                            block = block,
                            position = new Point(u, y),//显示层面, 新创建的方块移动起始点为(x,u)
                            state = BlockEventInfo.Status.Move,
                        };
                        infos.Add(info);
                    }
                    x--;
                }
            }
            //更新显示画面, 并在刷新画面之后 检查并销毁可销毁的方块
            displayer.BlockActionDisplay(CheckAndDestroy, infos.ToArray());
        }

        private void FillBlockRight()
        {
            List<BlockEventInfo> infos = new List<BlockEventInfo>();//方块更新信息
            for (int y = 0; y < Height; y++)
            {
                //Debug.Log($"正在检查第{x}列空穴");
                int x = 0;//索引标记
                int u = Width - 1;//越界位置标记(新生成的方块位置标记)
                bool blockChecked = false;//是否检测到空方块
                bool topEmpty = false;//顶层是否为空

                while (map.IsInRangeX(x))
                {
                    //Debug.Log($"正在检查空缺:{map[x, y].isEmpty()}");
                    //非空位置直接跳过
                    if (!map.IsEmptyBlock(x, y))
                    {
                        x++;
                        continue;
                    }

                    //在第一次检测到空方块时将空方块上方所有位点加入检测
                    if (!blockChecked)
                    {
                        blockChecked = true;
                        Debug.Log($"开始添加需要检查点,起始点:({x},{y})");
                        for (int i = x; map.IsInRangeX(i); i++)
                        {
                            AddCheck(new Point(i, y));
                        }
                    }

                    int k = x + 1;//k作为向上层搜索的索引
                    while (!topEmpty)//顶层非空时
                    {
                        if (!map.IsInRangeX(k))//k越界表明顶层全为空,跳出
                        {
                            topEmpty = true;
                            break;
                        }
                        if (map[k, y].isEmpty())//寻找第一个不为空的位置
                        {
                            k++;
                            continue;
                        }

                        //方块位置移动, 封装消息
                        map[x, y] = map[k, y];
                        map[x, y].position = new Point(x, y);
                        map[k, y] = Block.Empty;
                        BlockEventInfo info2 = new BlockEventInfo()
                        {
                            block = map[x, y],
                            position = new Point(k, y),
                            state = BlockEventInfo.Status.Move
                        };
                        infos.Add(info2);
                        break;
                    }

                    //特殊情况处理:顶部为空
                    if (topEmpty)
                    {
                        Block block = CreateBlock(x, y);//数据上直接填充到该空位置
                        map[x, y] = block;
                        //block.PrintState();
                        u++;
                        BlockEventInfo info = new BlockEventInfo
                        {
                            block = block,
                            position = new Point(u, y),//显示层面, 新创建的方块移动起始点为(x,u)
                            state = BlockEventInfo.Status.Move,
                        };
                        infos.Add(info);
                    }
                    x++;
                }
            }
            //更新显示画面, 并在刷新画面之后 检查并销毁可销毁的方块
            displayer.BlockActionDisplay(CheckAndDestroy, infos.ToArray());
        }

        /// <summary>
        /// 更新下落 以及 填充方块
        /// </summary>
        /// <param name="fillDir">填充方向</param>
        public void FillBlocks(Dir4 fillDir = Dir4.Up)
        {
            Debug.Log("开始更新和下落方块");
            switch (fillDir)
            {
                case Dir4.Up:
                    FillBlockUp();
                    break;

                case Dir4.Down:
                    FillBlockDown();
                    break;

                case Dir4.Left:
                    FillBlockLeft();
                    break;

                case Dir4.Right:
                    FillBlockRight();
                    break;
            }
        }

        /// <summary>
        /// 随机创建一个方块(方块生成器)
        /// </summary>
        /// <returns></returns>
        public Block CreateBlock(int x, int y)
        {
            if (blockCreator == null)
            {
                Debug.LogError("未挂载方块生成器");
                return null;
            }
            //Debug.Log("正在创建新的方块");

            Block block = blockCreator.GetNextBlock();
            block.position = new Point(x, y);
            block.ownerMap = this;
            //Debug.Log(block.id);
            return block;
            /*
            return new Block()
            {
                position = new Point(x, y),
            };*/
        }

        #endregion 对外接口

        #region 内部方法

        private void AddScore(Block block)//记入得分
        {
            score += block.score;
            OnScoreAdd?.Invoke(score);
        }

        #endregion 内部方法

        #region 方法

        /// <summary>
        /// 判断点是否在地图范围内
        /// </summary>
        /// <returns></returns>
        public bool InRange(Point p)
        {
            return map.IsPointInRange(p.x, p.y);
        }

        public bool InRange(int x, int y)
        {
            return map.IsPointInRange(x, y);
        }

        /// <summary>
        /// 生成随机地图
        /// </summary>
        public void CreateMap()
        {
        }

        /// <summary>
        /// 获取指定方块对象
        /// </summary>
        /// <param name="x">位置x</param>
        /// <param name="y">位置y</param>
        /// <returns></returns>
        public Block GetBlock(int x, int y)
        {
            if (InRange(x, y))
                return map[x, y];
            return null;
        }

        /// <summary>
        /// 获取指定方块
        /// </summary>
        /// <param name="point">方块的有效位置</param>
        /// <returns></returns>
        public Block GetBlock(Point point)
        {
            return GetBlock(point.x, point.y);
        }

        /// <summary>
        /// 设置指定位置的方块对象
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>被替换的方块对象</returns>
        public Block SetBlock(int x, int y, Block block)
        {
            if (!InRange(x, y)) return null;
            Block b = map[x, y];
            map[x, y] = block;
            block.position = new Point() { x = x, y = y };
            return b;
        }

        /// <summary>
        /// 交换两个位置上的方块
        /// </summary>
        /// <returns>是否交换成功</returns>
        public bool ChangeBlockPos(Point p1, Point p2, Action callBack = null)
        {
            if (InRange(p1) && InRange(p2))
            {
                Block tmp = map[p1.x, p1.y];
                map[p1.x, p1.y] = map[p2.x, p2.y];
                map[p1.x, p1.y].position = p1;
                map[p2.x, p2.y] = tmp;
                tmp.position = p2;

                BlockEventInfo info = new BlockEventInfo()
                {
                    state = BlockEventInfo.Status.Move,
                    block = map[p1.x, p1.y],
                    position = map[p2.x, p2.y].position
                };
                BlockEventInfo info2 = new BlockEventInfo()
                {
                    state = BlockEventInfo.Status.Move,
                    block = map[p2.x, p2.y],
                    position = map[p1.x, p1.y].position
                };
                displayer.BlockActionDisplay(callBack, info, info2);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 刷新地图(重置地图)
        /// </summary>
        public void InitMap()
        {
            Debug.Log("正在刷新地图...");
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    //Debug.Log($"正在填充[{x},{y}]");
                    while (true)
                    {
                        map[x, y] = CreateBlock(x, y);
                        AddCheck(x, y);
                        if (!CheckBlock())
                            break;
                        destroyBlocks.Clear();//这里清空销毁缓存, 是因为检查方块是否销毁时会添加临时的销毁缓存, 为了防止后面出错需要及时清除
                        //Debug.Log($"发生冲突:[{x},{y}]");
                    }
                }
            }
            init = true;
            dispelEvents.Clear();
            dispelEventEffect.Clear();
            checkBlocks.Clear();
            destroyBlocks.Clear();

            //Debug.Log("正在刷新地图...");
            displayer.UpdateDisplay(() =>
            {
                Debug.Log("地图刷新完毕");
            });
        }

        #endregion 方法

        #region 内部委托(系统规则)

        /// <summary>
        /// 方块对于 光标事件的响应 委托
        /// </summary>
        private void BlockCursorEventResponse(CursorEventInfo info)
        {
            if (info.aimPoint.isError()) return; //如果光标错误, 则不操作

            Block block = GetBlock(info.oldPoint);//优先执行前者方块的操作
            if (block != null)
            {
                block.OnOperate(block, info);
            }
            else//如果前者方块操作无效再执行后者操作
            {
                block = GetBlock(info.aimPoint);
                if (block != null)
                    block.OnOperate(block, info);
            }
        }

        #endregion 内部委托(系统规则)
    }
}