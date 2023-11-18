using System;
using System.Collections.Generic;

namespace GameDispel
{
    /// <summary>
    /// 动作相应枚举
    /// </summary>
    public enum ActionResponse
    {
        /// <summary>
        /// 确认操作
        /// </summary>
        Comfirm,
        /// <summary>
        /// 取消操作
        /// </summary>
        Cancel,
        /// <summary>
        /// 取消操作, 但是保留携带的触发效果
        /// </summary>
        Pass
    }
    /// <summary>
    /// 光标操作事件信息
    /// </summary>
    public struct CursorEventInfo
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public enum Operate { Confirm, Cancel }
        /// <summary>
        /// 操作类型
        /// </summary>
        public Operate operate;
        /// <summary>
        /// 目标位置(操作点位置)
        /// </summary>
        public Point aimPoint;
        /// <summary>
        /// 旧光标位置
        /// </summary>
        public Point oldPoint;
    }

    [Serializable]
    /// <summary>
    /// 点
    /// </summary>
    public struct Point
    {
        public int x;
        public int y;
        /// <summary>
        /// 坐标为(-1,-1)表示一个错误坐标, 判断是否为错误坐标
        /// </summary>
        /// <returns></returns>
        public bool isError()
        {
            return x == -1 && y == -1;
        }
        /// <summary>
        /// 一个错误坐标实例
        /// </summary>
        public static Point Error
        {
            get=> new Point() { x = -1, y = -1 };
        }

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public static bool operator ==(Point p1,Point p2)
        {
            return p1.x == p2.x && p1.y == p2.y;
        }
        public static bool operator !=(Point p1, Point p2)
        {
            return p1.x != p2.x || p1.y != p2.y;
        }
        public override string ToString()
        {
            return $"({x},{y})";
        }

        public override bool Equals(object obj)
        {
            return obj is Point point &&
                   x == point.x &&
                   y == point.y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }
    }

    public static class Expand
    {
        /// <summary>
        /// 添加一组新元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="items"></param>
        /// <param name="repeatable">是否可重复(不可重复将自动剔除重复元素)</param>
        public static void Add<T>(this List<T> list, T[] items,bool repeatable = true)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
            if (!repeatable)
                list.EliminateDuplicates();
        }
        public static void Add<T>(this List<T> list, List<T> items, bool repeatable = true)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
            if (!repeatable)
                list.EliminateDuplicates();
        }
        /// <summary>
        /// 剔除列表中的重复元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void EliminateDuplicates<T>(this List<T> list)
        {
            for(int i=0;i<list.Count;i++)
            {
                for(int k=i+1;k<list.Count;k++)
                {
                    if (list[i].Equals(list[k]))
                    {
                        list.RemoveAt(k);
                        k--;
                    }
                }
            }
        }
    }

}