using ER;
using System;
using UnityEngine;

namespace GameDispel
{
    /// <summary>
    /// 方块组件, 用于关联游戏物体 和 Block 对象, 主要控制视觉效果
    /// </summary>
    public class MonoBlock : Water
    {
        private SpriteRenderer sprite;
        private Animator animator;
        /// <summary>
        /// 所关联的方块对象
        /// </summary>
        public Block block;
        /// <summary>
        /// 方块的位置
        /// </summary>
        public Vector2 Position
        {
            get=>transform.localPosition;
            set=>transform.localPosition = value;
        }
        private Action callBack;

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        /// <summary>
        /// 设置贴图
        /// </summary>
        /// <param name="sprite"></param>
        public void SetSprite(Sprite sprite)
        {
            this.sprite.sprite = sprite;
        }
        /// <summary>
        /// 设置贴图颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Color color)
        {
            sprite.color = color;
        }

        /// <summary>
        /// 显现动画
        /// </summary>
        public virtual void Display(Action callBack = null)
        {
            this.callBack = callBack;
            animator.SetTrigger("display");
        }
        /// <summary>
        /// 隐藏动画
        /// </summary>
        public virtual void Hide(Action callBack = null)
        {
            this.callBack = callBack;
            animator.SetTrigger("destroy");
        }

        public override void ResetState()
        {
            transform.localScale = Vector2.one;
        }

        protected override void OnHide()
        {
            Debug.Log("已返回对象池");
        }
        /// <summary>
        /// 动画机销毁动画结束调用: 延时返回对象池
        /// </summary>
        public void DestroyBlock()
        {
            Invoke("Destroy",0.5f);
        }

    }
}