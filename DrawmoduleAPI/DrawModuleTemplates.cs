using System;
using UnityEngine;

namespace DrawModuleLib
{
    internal static class DrawModuleTemplates
    {
        // 绘图元素模板
        internal static DrawableLine lineTemplate;
        internal static DrawableShape shapeTemplate;
        internal static DrawableDisc discTemplate;

        // UI 模板
        internal static Transform drawingToolHubTemplate;
        internal static Transform paletteManagerTemplate;

        private static bool templatesInitialized = false;

        /// <summary>
        /// 初始化模板
        /// </summary>
        internal static void InitializeTemplates()
        {
            if (templatesInitialized)
                return;

            try
            {
                var drawHelpers = DrawHelpers.Instance;

                if (drawHelpers != null)
                {
                    lineTemplate = drawHelpers.Line;
                    shapeTemplate = drawHelpers.FilledShape;
                    discTemplate = drawHelpers.Disc;

                    DrawModuleEntry.logger.LogInfo("DrawModule element templates initialized successfully");
                }

                // 查找 UI 模板
                var drawModule = DrawModule.DrawModuleInstance;
                if (drawModule != null)
                {
                    drawingToolHubTemplate = FindDrawingToolHub(drawModule);
                    paletteManagerTemplate = FindPaletteManager(drawModule);

                    DrawModuleEntry.logger.LogInfo("DrawModule UI templates initialized successfully");
                }

                templatesInitialized = true;
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to initialize DrawModule templates: {ex.Message}");
            }
        }

        private static Transform FindDrawingToolHub(DrawModule drawModule)
        {
            var hub = DrawModuleReflection.GetDrawingToolHub(drawModule);
            return hub != null ? ((MonoBehaviour)hub).transform : null;
        }

        private static Transform FindPaletteManager(DrawModule drawModule)
        {
            var paletteManagerField = HarmonyLib.AccessTools.Field(typeof(DrawModule), "PaletteManager");
            var paletteManager = paletteManagerField?.GetValue(drawModule);
            return paletteManager != null ? ((MonoBehaviour)paletteManager).transform : null;
        }

        public static DrawableLine GetLineTemplate()
        {
            if (!templatesInitialized)
                InitializeTemplates();
            return lineTemplate;
        }

        public static DrawableShape GetShapeTemplate()
        {
            if (!templatesInitialized)
                InitializeTemplates();
            return shapeTemplate;
        }

        public static DrawableDisc GetDiscTemplate()
        {
            if (!templatesInitialized)
                InitializeTemplates();
            return discTemplate;
        }
    }
}