using UnityEngine;

namespace DrawModuleLib.MonoBehaviors
{
    /// <summary>
    /// 自定义绘图工具基类
    /// </summary>
    public abstract class CustomDrawTool : MonoBehaviour
    {
        protected DrawModule drawModule;

        public abstract string ToolName { get; }
        public abstract string ToolDescription { get; }

        protected virtual void Awake()
        {
            drawModule = DrawModuleAPI.GetCurrentDrawModule();
        }

        /// <summary>
        /// 工具激活时调用
        /// </summary>
        public virtual void OnToolActivated()
        {
        }

        /// <summary>
        /// 工具停用时调用
        /// </summary>
        public virtual void OnToolDeactivated()
        {
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public virtual void OnToolUpdate()
        {
        }

        /// <summary>
        /// 鼠标按下
        /// </summary>
        public virtual void OnMouseDown(Vector2 worldPosition)
        {
        }

        /// <summary>
        /// 鼠标拖动
        /// </summary>
        public virtual void OnMouseDrag(Vector2 worldPosition)
        {
        }

        /// <summary>
        /// 鼠标抬起
        /// </summary>
        public virtual void OnMouseUp(Vector2 worldPosition)
        {
        }
    }
}
