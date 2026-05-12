using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrawModuleLib.MonoBehaviors
{
    /// <summary>
    /// DrawModule 扩展组件 - 用于添加自定义功能
    /// </summary>
    public class DrawModuleExtension : MonoBehaviour
    {
        private DrawModule drawModule;

        // 自定义数据
        public Dictionary<string, object> CustomData { get; private set; }

        // 绘图统计
        public int TotalLinesDrawn { get; private set; }
        public int TotalShapesDrawn { get; private set; }
        public int TotalFillsDrawn { get; private set; }
        public float TotalDrawTime { get; private set; }

        private float drawStartTime;
        private bool isDrawing;

        private void Awake()
        {
            drawModule = GetComponent<DrawModule>();
            CustomData = new Dictionary<string, object>();

            // 注册事件
            RegisterEvents();
        }

        private void RegisterEvents()
        {

        }

        private void OnLineDrawStart(DrawModule dm)
        {
            if (dm != drawModule) return;

            isDrawing = true;
            drawStartTime = Time.time;
        }

        private void OnLineFinished(DrawModule dm, DrawableLine line)
        {
            if (dm != drawModule) return;

            if (isDrawing)
            {
                TotalDrawTime += Time.time - drawStartTime;
                isDrawing = false;
            }

            TotalLinesDrawn++;
        }

        private void OnShapeCreated(DrawModule dm, DrawableShape shape)
        {
            if (dm != drawModule) return;
            TotalShapesDrawn++;
        }

        private void OnFillStart(DrawModule dm, Vector2 point)
        {
            if (dm != drawModule) return;
            TotalFillsDrawn++;
        }

        /// <summary>
        /// 获取绘图统计信息
        /// </summary>
        public string GetStatistics()
        {
            return $"Lines: {TotalLinesDrawn}, Shapes: {TotalShapesDrawn}, Fills: {TotalFillsDrawn}, Time: {TotalDrawTime:F2}s";
        }

        /// <summary>
        /// 重置统计
        /// </summary>
        public void ResetStatistics()
        {
            TotalLinesDrawn = 0;
            TotalShapesDrawn = 0;
            TotalFillsDrawn = 0;
            TotalDrawTime = 0f;
        }

        private void OnDestroy()
        {
            // 取消注册事件

        }
    }
}
