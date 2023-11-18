using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace GameDispel
{
    /// <summary>
    /// 方块生成器
    /// </summary>
    public interface BlockCreator
    {
        /// <summary>
        /// 获取下一个随机方块
        /// </summary>
        public Block GetNextBlock();

        /// <summary>
        /// 基于地图获取随机方块
        /// </summary>
        /// <param name="map"></param>
        public Block GetNextBlock(GameMap map);
    }

    public class CommonBlockCreator : BlockCreator
    {
        Random random = new Random(DateTime.Now.Millisecond);
        public class BlockCreateInfo
        {
            /// <summary>
            /// 源方块
            /// </summary>
            public Block seed;
            /// <summary>
            /// 生成权重(绝对权重)
            /// </summary>
            public float weight;
            /// <summary>
            /// 生成权重(相对权重)(非生成概率)
            /// </summary>
            public float relativeWeight;

            public BlockCreateInfo(Block seed, float weight)
            {
                this.seed = seed;
                this.weight = weight;
            }
        }


        /// <summary>
        /// 种子信息
        /// </summary>
        private List<BlockCreateInfo> seeds = new List<BlockCreateInfo>();
        /// <summary>
        /// 获取种子信息
        /// </summary>
        public BlockCreateInfo[] CreatorInfos { get => seeds.ToArray(); }

        /// <summary>
        /// 添加新的种子
        /// </summary>
        /// <param name="infos"></param>
        public void AddSeed(params BlockCreateInfo[] infos)
        {
            seeds.Add(infos);
            UpdateRelativeWeight();
        }

        /// <summary>
        /// 更新相对权重
        /// </summary>
        private void UpdateRelativeWeight()
        {
            
            float sum = 0;
            for (int i = 0; i < seeds.Count; i++)
            {
                sum += seeds[i].weight;
            }
            for (int i = 0; i < seeds.Count;i++)
            {
                if (i == seeds.Count - 1)
                {
                    seeds[i].relativeWeight = 1f;
                    continue;
                }
                seeds[i].relativeWeight = seeds[i].weight / sum;
                if (i > 0)
                    seeds[i].relativeWeight += seeds[i - 1].relativeWeight;
            }
            StringBuilder sb = new StringBuilder();
            for(int i=0;i<seeds.Count;i++)
            {
                sb.Append($"seed[{i + 1}]:{seeds[i].relativeWeight}");
            }
            Debug.Log("更新种子相对权重:"+sb.ToString());
        }

        public Block GetNextBlock()
        {
            float weight = (float)random.NextDouble();
            
            int index = 0;
            while (index < seeds.Count)
            {
                if(weight <= seeds[index].relativeWeight)
                {
                    return seeds[index].seed.Copy();
                }
                index++;
            }
            Debug.Log($"value:{weight}, max:{seeds[seeds.Count-1].relativeWeight}");
            throw new Exception("随机数生成发生错误 或者 概率出现错误");
        }

        public Block GetNextBlock(GameMap map)
        {
            return GetNextBlock();
        }
    }
}