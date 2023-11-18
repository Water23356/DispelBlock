using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDispel
{
    public struct BlockEventInfo
    {
        public enum Status
        { Move, Destory }

        /// <summary>
        /// 事件类型
        /// </summary>
        public Status state;

        /// <summary>
        /// 方块对象
        /// </summary>
        public Block block;

        /// <summary>
        /// 发生位置(移动事件时, 方块对象位置为目标位置, 这个位置为旧位置)
        /// </summary>
        public Point position;
    }
    /// <summary>
    /// 方块检测事件反馈
    /// </summary>
    public struct BlockDetectedResponse
    {
        /// <summary>
        /// 销毁位点
        /// </summary>
        public Point[] effect;
        /// <summary>
        /// 增加的combo数
        /// </summary>
        public int combo;
    }
    /// <summary>
    /// 方块销毁事件反馈
    /// </summary>
    public struct BlockDestroyResponse
    {
        /// <summary>
        /// 指令状态
        /// </summary>
        public ActionResponse state;

        /// <summary>
        /// 需要摧毁的点
        /// </summary>
        public Point[] destroyPoints;
    }

    /// <summary>
    /// 方块
    /// </summary>
    public class Block
    {
        #region 方块属性

        public int id;

        /// <summary>
        /// 方块分数
        /// </summary>
        public float score;

        /// <summary>
        /// 方块所在位置
        /// </summary>
        public Point position;

        /// <summary>
        /// 所属游戏地图
        /// </summary>
        public GameMap ownerMap;
        /// <summary>
        /// 标记(用作属性拓展接口)
        /// </summary>
        public Dictionary<string, object> marks = new Dictionary<string, object>();

        public int X
        {
            get => position.x;
            set => position.x = value;
        }

        public int Y
        {
            get => position.y;
            set => position.y = value;
        }

        /// <summary>
        /// 消除检测类型
        /// </summary>
        public string CheckType = "None";

        #endregion 方块属性

        #region 事件/委托

        /// <summary>
        /// 被操作时触发的事件(初始化必须不为null)
        /// </summary>
        public Action<Block,CursorEventInfo> OnOperate;

        /// <summary>
        /// 进行消除检测时触发的事件(初始化必须不为null)
        /// </summary>
        public Func<Block,BlockDetectedResponse> OnDetected;

        /// <summary>
        /// 当销毁时触发(初始化必须不为null)
        /// </summary>
        public Func<Block, BlockDestroyResponse> OnDestroy;

        #endregion 事件/委托

        public Block(GameMap map)
        {
            ownerMap = map;
        }

        public static Block Empty
        {
            get
            {
                Block block = new Block(null);
                block.id = 0;
                return block;
            }
        }

        public bool isEmpty()
        {
            return id == 0;
        }

        /// <summary>
        /// 获取一个自身的深拷贝
        /// </summary>
        /// <returns></returns>
        public Block Copy()
        {
            return new Block(ownerMap)
            {
                id = id,
                score = score,
                position = position,
                OnOperate = OnOperate,
                OnDetected = OnDetected,
                OnDestroy = OnDestroy
            };
        }

        /// <summary>
        /// 获取一个一般的可三消方块
        /// </summary>
        /// <param name="m_id"></param>
        /// <returns></returns>
        public static Block GetNormalBlock(int m_id)
        {
            return new Block(null)
            {
                id = m_id,
                score = 10,
                position = Point.Error,
                OnDestroy = BlockSystem.BDNormal,
                OnOperate = BlockSystem.CRChangeBlockPos,
                OnDetected = BlockSystem.CADispelMore3,
                CheckType = BlockSystem.CA_DISPEL_MORE3

            };
        }

        public void PrintState()
        {
            Debug.Log($"id:{id},score:{score},position:{position.ToString()},Operate:{OnOperate != null},Detected:{OnDetected != null},Destroy{OnDestroy != null}");
        }
    }
}