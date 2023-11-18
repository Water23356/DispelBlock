using ER;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace GameDispel
{
    /// <summary>
    /// 方块显示器
    /// </summary>
    public class BaseDisplayer : MonoBehaviour, BlockDisplayer
    {
        #region 组件

        public ObjectPool pool;

        [HideInInspector]
        public List<MonoBlock> blocks = new List<MonoBlock>();

        /// <summary>
        /// 显示遮罩
        /// </summary>
        public Transform mask;

        /// <summary>
        /// 背景
        /// </summary>
        public Transform background;

        public TMP_Text text_score;
        public TMP_Text text_combo;

        /// <summary>
        /// 光标方块
        /// </summary>
        public Transform cursor_trf;

        /// <summary>
        /// 被选中的方块
        /// </summary>
        public Transform selected_trf;

        #endregion 组件

        #region 显示设置

        public Color[] colors;

        /// <summary>
        /// 方块大小
        /// </summary>
        public Vector2 blockSize;

        /// <summary>
        /// 显示位置偏移(测试)
        /// </summary>
        public Vector2 offset;

        /// <summary>
        /// 边距
        /// </summary>
        public float margins;

        /// <summary>
        /// 动画速度
        /// </summary>
        public float speed = 1f;

        /// <summary>
        /// 最小动画匹配距离
        /// </summary>
        public float minDistance = 0.0001f;

        #endregion 显示设置

        /// <summary>
        /// 是否处于演出状态
        /// </summary>
        public bool showing = false;

        public Point cursor = new Point(0, 0);

        private List<AnimationInfo> animations = new List<AnimationInfo>();
        private Action callBack;

        private GameMap map;

        public GameMap Map
        {
            get => map;
            set
            {
                map = value;

                //更新背景大小
                Vector2 size = background.GetComponent<SpriteRenderer>().sprite.rect.size;
                Debug.Log($"背景大小:{blockSize.x},width:{map.Width},marg:{margins},size:{size.x}, backsize:{(blockSize.x * map.Width + margins * 2)}");
                Vector2 pixel = new Vector2(blockSize.x * map.Width + margins * 2, blockSize.y * map.Height + margins * 2);
                background.localScale = new Vector2((blockSize.x * map.Width + margins * 2) / size.x, (blockSize.y * map.Height + margins * 2) / size.y);
                //更新遮罩大小
                size = mask.GetComponent<SpriteMask>().bounds.size;
                Debug.Log("遮罩图片大小:" + size);
                mask.localScale = new Vector2((blockSize.x * map.Width + margins * 2) / size.x, (blockSize.y * map.Height + margins * 2) / size.y) * 0.01f;

                map.OnComboAdd += (combo) =>
                {
                    text_combo.text = $"combo {combo}";
                    text_combo.GetComponent<Animator>().SetTrigger("combo");
                };
                map.OnScoreAdd += (score) =>
                {
                    text_score.text = $"得分: {score}";
                };
                map.OnCursorOperate += (c) =>
                {
                    if (c.operate == CursorEventInfo.Operate.Cancel)
                    {
                        selected_trf.gameObject.SetActive(false);
                    }
                    else
                    {
                        selected_trf.gameObject.SetActive(true);
                        selected_trf.localPosition = GetPointPosition(c.aimPoint);
                    }
                };
            }
        }

        private struct AnimationInfo
        {
            public MonoBlock block;
            public Point aimPoint;
            public Vector2 dir;
            public Vector2 aimPos;
            public Vector2 oldPos;
            public float distance;
        }

        public void MoveTo(MonoBlock block, Point aimPoint)
        {
            block.GetComponent<SpriteRenderer>().sortingOrder = 9;
            Vector2 aimPos = new Vector2((aimPoint.x * blockSize.x - blockSize.x * (map.Width - 1) * 0.5f), (aimPoint.y * blockSize.y - blockSize.y * (map.Height - 1) * 0.5f)) * 0.01f + offset;

            //Debug.Log($"aim position:{aimPos} now position:{block.Position}");
            animations.Add(new AnimationInfo
            {
                block = block,
                aimPoint = aimPoint,
                aimPos = aimPos,
                dir = (aimPos - block.Position).normalized,
                distance = (aimPos - block.Position).magnitude,
                oldPos = block.Position
            });
        }

        public Vector2 cursor_aimPos;

        private void Update()
        {
            #region 输入监听

            if (Input.anyKeyDown)
            {
                if (Input.GetKeyDown(KeyCode.A))
                {
                    if(map.InRange(cursor.x-1,cursor.y))
                    {
                        cursor.x--;
                        cursor_aimPos = GetPointPosition(cursor);
                    }
                }
                else if (Input.GetKeyDown(KeyCode.D))
                {
                    if (map.InRange(cursor.x + 1, cursor.y))
                    {
                        cursor.x++;
                        cursor_aimPos = GetPointPosition(cursor);
                    }
                }
                else if (Input.GetKeyDown(KeyCode.W))
                {
                    if (map.InRange(cursor.x, cursor.y+1))
                    {
                        cursor.y++;
                        cursor_aimPos = GetPointPosition(cursor);
                    }
                }
                else if (Input.GetKeyDown(KeyCode.S))
                {
                    if (map.InRange(cursor.x, cursor.y-1))
                    {
                        cursor.y--;
                        cursor_aimPos = GetPointPosition(cursor);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter) && !showing)
                {
                    map.MoveCursor(cursor);
                }
            }

            #endregion 输入监听

            Vector2 distance = cursor_aimPos - (Vector2)cursor_trf.localPosition;
            if (distance.magnitude > 0.01f)
            {
                cursor_trf.localPosition += new Vector3(distance.x, distance.y, 0) / 5f;
            }

            if (animations.Count == 0 && showing)
            {
                showing = false;
                Debug.Log("动画播放结束");
                Debug.Log($"回调函数:{callBack != null}");
                callBack?.Invoke();
            }
            else
            {
                for (int i = 0; i < animations.Count; i++)
                {
                    AnimationInfo info = animations[i];
                    Vector2 dis = new Vector2(info.dir.x, info.dir.y) * speed * Time.deltaTime;
                    info.block.Position += dis;
                    //Debug.Log($"info.progress:{(info.block.Position - info.oldPos).magnitude}, info.distance:{info.distance}");
                    if ((info.block.Position - info.oldPos).magnitude >= info.distance)//完成动画播放
                    {
                        info.block.GetComponent<SpriteRenderer>().sortingOrder = 1;
                        info.block.Position = info.aimPos;
                        animations.Remove(info);
                        i--;
                    }
                }
            }
        }

        private MonoBlock GetBlockFromPool()
        {
            MonoBlock block = (MonoBlock)pool.GetObject();
            block.transform.SetParent(transform);
            //Debug.Log("方块实际大小:"+block.transform.lossyScale);
            blocks.Add(block);
            block.Display();
            return block;
        }

        /// <summary>
        /// 从表中获取指定位点上的MonoBlock
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private MonoBlock GetBlock(Point position)
        {
            foreach (MonoBlock block in blocks)
            {
                if (block.block.position == position)
                {
                    return block;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取和指定方块绑定的MonoBlock
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private MonoBlock GetBlock(Block b)
        {
            foreach (MonoBlock block in blocks)
            {
                if (block.block == b)
                {
                    return block;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取地图中指定位点在显示中的具体位置
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector2 GetPointPosition(int x, int y)
        {
            return new Vector2((x * blockSize.x - blockSize.x * (map.Width - 1) * 0.5f), (y * blockSize.y - blockSize.y * (map.Height - 1) * 0.5f)) * 0.01f + offset;
        }

        public Vector2 GetPointPosition(Point point)
        {
            return new Vector2((point.x * blockSize.x - blockSize.x * (map.Width - 1) * 0.5f), (point.y * blockSize.y - blockSize.y * (map.Height - 1) * 0.5f)) * 0.01f + offset;
        }

        public void UpdateDisplay(Action callBack)
        {
            //更新方块显示
            int i = 0;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Block block = map.GetBlock(x, y);
                    if (block.isEmpty()) continue;
                    MonoBlock mb = GetBlockFromPool();
                    i++;
                    mb.block = block;
                    mb.Position = GetPointPosition(x, y);
                    if (block.id < 0 || block.id >= colors.Length)
                    {
                        mb.SetColor(Color.white);
                    }
                    else
                    {
                        mb.SetColor(colors[block.id]);
                    }
                }
            }
            callBack?.Invoke();
        }

        public void BlockActionDisplay(Action callBack, params BlockEventInfo[] eventInfos)
        {
            Debug.Log($"事件信息个数:{eventInfos.Length}, 回调函数:{callBack != null}");
            StringBuilder sb = new StringBuilder();
            foreach (var info in eventInfos)
            {
                sb.Append($"方块消息: \n\t状态:{info.state}\n\t位置:{info.position.ToString()}\n\t位点:{info.block.position.ToString()}\n");
                switch (info.state)
                {
                    case BlockEventInfo.Status.Move:
                        MonoBlock mb;
                        if (map.InRange(info.position))//在范围内直接获取对应的 MonoBlock对象
                        {
                            mb = GetBlock(info.block);
                        }
                        else//否则需要新建一个对象
                        {
                            mb = GetBlockFromPool();
                            mb.block = info.block;
                            mb.Position = GetPointPosition(info.position);
                            if (mb.block.id < 0 || mb.block.id >= colors.Length)
                            {
                                mb.SetColor(Color.white);
                            }
                            else
                            {
                                mb.SetColor(colors[mb.block.id]);
                            }
                        }
                        MoveTo(mb, info.block.position);
                        showing = true;
                        break;

                    case BlockEventInfo.Status.Destory:
                        MonoBlock mb2 = GetBlock(info.block);
                        Debug.Log($"抓取销毁方块成功:{mb2 != null}");
                        if (mb2 != null)
                        {
                            mb2.Hide();
                            blocks.Remove(mb2);
                        }
                        break;
                }
            }
            Debug.Log(sb.ToString());
            if (showing)
            {
                this.callBack = callBack;
            }
            else
            {
                Debug.Log($"回调函数状态:{callBack != null}");
                callBack?.Invoke();
            }
        }
    }
}