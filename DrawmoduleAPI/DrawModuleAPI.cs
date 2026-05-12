using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrawModuleLib
{
    public static class DrawModuleAPI
    {
        #region 内部委托 - 简化为构建器模式

        internal static BuilderDelegate onDrawModuleReadyDelegate;

        public delegate void BuilderDelegate(DrawModule drawModule);

        #endregion

        #region 注���方法

        /// <summary>
        /// 当 DrawModule 准备就绪时执行回调
        /// </summary>
        public static void OnDrawModuleReady(BuilderDelegate builderDelegate) =>
            onDrawModuleReadyDelegate += builderDelegate;

        #endregion

        #region 工具方法 API

        /// <summary>
        /// 获取当前 DrawModule 实例
        /// </summary>
        public static DrawModule GetCurrentDrawModule() => DrawModule.DrawModuleInstance;

        /// <summary>
        /// 检查是否可以绘图
        /// </summary>
        public static bool CanDraw()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule != null && drawModule.CanDraw();
        }

        /// <summary>
        /// 获取当前激活的工具
        /// </summary>
        public static DrawModule.DrawTool GetActiveTool()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.ActiveDrawTool ?? DrawModule.DrawTool.Brush;
        }

        /// <summary>
        /// 切换工具
        /// </summary>
        public static void SwitchTool(DrawModule.DrawTool tool, bool force = false)
        {
            var drawModule = GetCurrentDrawModule();
            drawModule?.ActivateTool(tool, force, true);
        }

        /// <summary>
        /// 获取当前颜色
        /// </summary>
        public static Color GetCurrentColor()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule != null ? DrawModuleReflection.GetRGBColor(drawModule) : Color.black;
        }

        /// <summary>
        /// 设置颜色
        /// </summary>
        public static void SetColor(Color color, bool updatePicker = true, bool withSound = true)
        {
            var drawModule = GetCurrentDrawModule();
            drawModule?.SetRGBColor(color, updatePicker, false, false, withSound);
        }

        /// <summary>
        /// 获取当前笔刷大小
        /// </summary>
        public static int GetBrushSize()
        {
            try
            {
                // 1. 获取 BrushSize 静态字段对象
                var field = AccessTools.Field(typeof(DrawModule), "BrushSize");
                if (field == null) return 10; // 默认值

                var persistentIntObj = field.GetValue(null);
                if (persistentIntObj == null) return 10;

                // 2. 获取 PersistentInt 对象当中的 Value 属性
                var valueProp = AccessTools.Property(persistentIntObj.GetType(), "Value");
                if (valueProp == null) return 10;

                return (int)valueProp.GetValue(persistentIntObj);
            }
            catch
            {
                return 10; // 出错时返回默认安全值
            }
        }


        /// <summary>
        /// 设置笔刷大小
        /// </summary>
        public static void SetBrushSize(int size)
        {
            try
            {
                // 1. 简单的范围限制 (假设是 PC 最大 35，Mobile 28)
                // 这里的 GlobalVars.IsMobile 如果访问不到，就写死一个安全值，比如 35
                int maxSize = 35;
                // 尝试通过反射获取 GlobalVars.IsMobile (可选)
                /* 
                var isMobileField = AccessTools.Field(typeof(GlobalVars), "IsMobile");
                if (isMobileField != null && (bool)isMobileField.GetValue(null)) maxSize = 28;
                */

                int clampedSize = Mathf.Clamp(size, 2, maxSize);

                // 2. 获取 BrushSize 对象
                var field = AccessTools.Field(typeof(DrawModule), "BrushSize");
                if (field == null) return;
                var persistentIntObj = field.GetValue(null);
                if (persistentIntObj == null) return;

                // 3. 设置 Value 属性
                var valueProp = AccessTools.Property(persistentIntObj.GetType(), "Value");
                if (valueProp != null)
                {
                    valueProp.SetValue(persistentIntObj, clampedSize);
                }
            }
            catch (Exception) { /* 忽略错误 */ }
        }


        /// <summary>
        /// 执行撤销
        /// </summary>
        public static void Undo()
        {
            var drawModule = GetCurrentDrawModule();
            drawModule?.Undo(DrawingToolTrigger.System);
        }

        /// <summary>
        /// 执行重做
        /// </summary>
        public static void Redo()
        {
            var drawModule = GetCurrentDrawModule();
            drawModule?.Redo(DrawingToolTrigger.System);
        }

        /// <summary>
        /// 清除画布
        /// </summary>
        public static void ClearCanvas()
        {
            var drawModule = GetCurrentDrawModule();
            drawModule?.ExecuteClear(DrawingToolTrigger.System, true, true);
        }

        /// <summary>
        /// 获取所有已绘制的元素
        /// </summary>
        public static List<DrawableElement> GetAllDrawnElements()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.GetMyDrawnElements() ?? new List<DrawableElement>();
        }

        /// <summary>
        /// 获取绘图信息（用于导出）
        /// </summary>
        public static List<LineInformation> GetDrawingInfo(bool finishActiveLines = true, bool normalize = false)
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.GetDrawingAsLineInfo(finishActiveLines, normalize) ?? new List<LineInformation>();
        }

        /// <summary>
        /// 检查是否有可撤销的操作
        /// </summary>
        public static bool CanUndo()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.UndoSystem.CanUndo ?? false;
        }

        /// <summary>
        /// 检查是否有可重做的操作
        /// </summary>
        public static bool CanRedo()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.UndoSystem.CanRedo ?? false;
        }

        /// <summary>
        /// 获取当前选中的图层
        /// </summary>
        public static int GetSelectedLayer()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.SelectedLayer ?? 1;
        }

        /// <summary>
        /// 检查是否已提交
        /// </summary>
        public static bool HasSubmitted()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.HasSubmitted ?? false;
        }

        /// <summary>
        /// 检查是否有自己绘制的线条
        /// </summary>
        public static bool HasOwnLines()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.HasOwnLines ?? false;
        }

        /// <summary>
        /// 获取特殊模式
        /// </summary>
        public static GameSpecialMode GetSpecialMode()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.SpecialMode ?? GameSpecialMode.Weight;
        }

        /// <summary>
        /// 检查是否是像素艺术模式
        /// </summary>
        public static bool IsPixelArtMode()
        {
            var drawModule = GetCurrentDrawModule();
            return drawModule?.IsPixelArt ?? false;
        }

        #endregion

        #region 工厂方法 API - 创建绘图元素

        /// <summary>
        /// 创建一条线
        /// </summary>
        public static DrawableLine CreateLine(Vector2 startPosition, Color color, int brushSize, Transform parent = null)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null)
            {
                DrawModuleEntry.logger.LogError("DrawModule instance not found!");
                return null;
            }

            if (DrawModuleTemplates.GetLineTemplate() == null)
            {
                DrawModuleEntry.logger.LogError("Line template not found!");
                return null;
            }

            var line = Object.Instantiate(DrawModuleTemplates.GetLineTemplate());
            line.name = $"Line - {DateTime.Now:HH:mm:ss}";

            line.Init(
                (byte)brushSize,
                color,
                drawModule.SortOrder.GetNextSortOrder(false),
                DrawHelpers.GetSortingLayer(
    drawModule.SelectedLayer,
    false,
    DrawHelpers.SortLayerType.Normal
),
                DrawModuleReflection.GetLocalPlayfabId(drawModule),
                drawModule
            );

            line.transform.position = startPosition;

            var lineParent = parent ?? DrawModuleReflection.GetLineParent(drawModule);
            line.transform.SetParent(lineParent);
            line.transform.localPosition = new Vector3(
                line.transform.localPosition.x,
                line.transform.localPosition.y,
                drawModule.SortOrder.NextLineZPos(false)
            );

            var createdList = DrawModuleReflection.GetCreatedDrawablesByMe(drawModule);
            createdList?.Add(line);

            return line;
        }

        /// <summary>
        /// 创建一个形状
        /// </summary>
        public static DrawableShape CreateShape(Vector2 position, Color color, int brushSize, Transform parent = null)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null)
            {
                DrawModuleEntry.logger.LogError("DrawModule instance not found!");
                return null;
            }

            if (DrawModuleTemplates.GetShapeTemplate() == null)
            {
                DrawModuleEntry.logger.LogError("Shape template not found!");
                return null;
            }

            var shape = Object.Instantiate(DrawModuleTemplates.GetShapeTemplate());
            shape.name = $"Shape - {DateTime.Now:HH:mm:ss}";

            shape.Init(
                (byte)brushSize,
                color,
                drawModule.SortOrder.GetNextSortOrder(false),
                DrawHelpers.GetSortingLayer(
    drawModule.SelectedLayer,
    false,
    DrawHelpers.SortLayerType.Normal
),
                DrawModuleReflection.GetLocalPlayfabId(drawModule),
                drawModule
            );

            shape.transform.position = position;

            var lineParent = parent ?? DrawModuleReflection.GetLineParent(drawModule);
            shape.transform.SetParent(lineParent);
            shape.transform.localPosition = new Vector3(
                shape.transform.localPosition.x,
                shape.transform.localPosition.y,
                drawModule.SortOrder.NextLineZPos(false)
            );

            var createdList = DrawModuleReflection.GetCreatedDrawablesByMe(drawModule);
            createdList?.Add(shape);

            return shape;
        }

        /// <summary>
        /// 创建一个圆点
        /// </summary>
        public static DrawableDisc CreateDisc(Vector2 position, Color color, int brushSize, Transform parent = null)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null)
            {
                DrawModuleEntry.logger.LogError("DrawModule instance not found!");
                return null;
            }

            if (DrawModuleTemplates.GetDiscTemplate() == null)
            {
                DrawModuleEntry.logger.LogError("Disc template not found!");
                return null;
            }

            var disc = Object.Instantiate(DrawModuleTemplates.GetDiscTemplate());
            disc.name = $"Disc - {DateTime.Now:HH:mm:ss}";

            disc.Init(
                (byte)brushSize,
                color,
                drawModule.SortOrder.GetNextSortOrder(false),
                DrawHelpers.GetSortingLayer(
    drawModule.SelectedLayer,
    false,
    DrawHelpers.SortLayerType.Normal
),
                DrawModuleReflection.GetLocalPlayfabId(drawModule),
                drawModule
            );

            disc.transform.position = position;

            var lineParent = parent ?? DrawModuleReflection.GetLineParent(drawModule);
            disc.transform.SetParent(lineParent);
            disc.transform.localPosition = new Vector3(
                disc.transform.localPosition.x,
                disc.transform.localPosition.y,
                drawModule.SortOrder.NextLineZPos(false)
            );

            var createdList = DrawModuleReflection.GetCreatedDrawablesByMe(drawModule);
            createdList?.Add(disc);

            return disc;
        }

        /// <summary>
        /// 创建一个填充形状
        /// </summary>
        public static DrawableShape CreateFillShape(Vector2 position, Color color, List<Vector2> points, Transform parent = null)
        {
            var shape = CreateShape(position, color, 1, parent);
            if (shape == null) return null;

            shape.SetTypeToFill();

            if (points != null && points.Count > 0)
            {
                shape.Polygon.SetPoints(points);
                shape.SetColliderPoints(points.ToArray());
            }

            return shape;
        }

        #endregion

        #region 批量操作 API

        /// <summary>
        /// 批量删除元素
        /// </summary>
        public static void DeleteElements(IEnumerable<DrawableElement> elements)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return;

            foreach (var element in elements.Where(e => e != null))
            {
                drawModule.DeletedPrecise(element);
            }
        }

        /// <summary>
        /// 修改元素颜色
        /// </summary>
        public static void ChangeElementColor(DrawableElement element, Color newColor)
        {
            if (element == null) return;

            var oldColor = element.RGBColor;
            element.SetColor(newColor);

            var drawModule = GetCurrentDrawModule();
            drawModule?.OnSendUpdatePrecise(element, oldColor, newColor);
        }

        /// <summary>
        /// 批量修改元素颜色
        /// </summary>
        public static void ChangeElementsColor(IEnumerable<DrawableElement> elements, Color newColor)
        {
            foreach (var element in elements.Where(e => e != null))
            {
                ChangeElementColor(element, newColor);
            }
        }

        #endregion

        #region 内部方法 - 由 Entry 调用

        /// <summary>
        /// 内部方法：触发 DrawModule 准备就绪事件
        /// </summary>
        internal static void TriggerDrawModuleReady(DrawModule drawModule)
        {
            onDrawModuleReadyDelegate?.Invoke(drawModule);
        }
        // 在DrawModuleAPI类中添加以下方法

        #region 内部状态管理 API

        /// <summary>
        /// 获取正在绘制的线条
        /// </summary>
        public static DrawableLine GetLineInProgress()
        {
            var drawModule = GetCurrentDrawModule();
            return DrawModuleReflection.GetLineInProgress(drawModule);
        }

        /// <summary>
        /// 设置正在绘制的线条
        /// </summary>
        public static void SetLineInProgress(DrawableLine line)
        {
            var drawModule = GetCurrentDrawModule();
            DrawModuleReflection.SetLineInProgress(drawModule, line);
        }

        /// <summary>
        /// 获取正在绘制的形状
        /// </summary>
        public static DrawableShape GetShapeInProgress()
        {
            var drawModule = GetCurrentDrawModule();
            return DrawModuleReflection.GetShapeInProgress(drawModule);
        }

        /// <summary>
        /// 设置正在绘制的形状
        /// </summary>
        public static void SetShapeInProgress(DrawableShape shape)
        {
            var drawModule = GetCurrentDrawModule();
            DrawModuleReflection.SetShapeInProgress(drawModule, shape);
        }

        /// <summary>
        /// 清空所有进行中的绘制状态
        /// </summary>
        public static void ClearInProgressStates()
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return;

            DrawModuleReflection.SetLineInProgress(drawModule, null);
            DrawModuleReflection.SetShapeInProgress(drawModule, null);
        }

        #endregion

        #region 图层管理 API

        /// <summary>
        /// 设置选中的图层
        /// </summary>
        public static void SetSelectedLayer(int layer)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return;

            // 使用反射设置图层
            var prop = AccessTools.DeclaredProperty(typeof(DrawModule), "SelectedLayer");
            prop?.SetValue(drawModule, layer);
        }

        /// <summary>
        /// 切换到指定图层
        /// </summary>
        public static void SwitchToLayer(int layer)
        {
            SetSelectedLayer(layer);
        }

        #endregion

        #region UI控制 API

        /// <summary>
        /// 获取绘图工具中心组件
        /// </summary>
        public static object GetDrawingToolHub()
        {
            var drawModule = GetCurrentDrawModule();
            return DrawModuleReflection.GetDrawingToolHub(drawModule);
        }

        /// <summary>
        /// 设置橡皮擦按钮状态
        /// </summary>
        public static void SetEraseButtonsState(bool enabled)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return;

            var hub = DrawModuleReflection.GetDrawingToolHub(drawModule);
            if (hub == null) return;

            try
            {
                var method = AccessTools.Method(typeof(DrawingToolHub), "SetEraseButtons", new[] { typeof(bool) });
                method?.Invoke(hub, new object[] { enabled });
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to set erase buttons state: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新撤销按钮状态
        /// </summary>
        public static void UpdateUndoButtons()
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return;

            var hub = DrawModuleReflection.GetDrawingToolHub(drawModule);
            if (hub == null) return;

            try
            {
                var method = AccessTools.Method(typeof(DrawingToolHub), "SetUndoButtons");
                method?.Invoke(hub, null);
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to update undo buttons: {ex.Message}");
            }
        }

        /// <summary>
        /// 高亮指定的工具按钮
        /// </summary>
        public static void HighlightToolButton(DrawModule.DrawTool tool)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return;

            var hub = DrawModuleReflection.GetDrawingToolHub(drawModule);
            if (hub == null) return;

            try
            {
                var method = AccessTools.Method(typeof(DrawingToolHub), "HighlightButton");
                method?.Invoke(hub, new object[] { tool });
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to highlight tool button: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新颜色按钮显示
        /// </summary>
        public static void UpdateColorButtons()
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return;

            var hub = DrawModuleReflection.GetDrawingToolHub(drawModule);
            if (hub == null) return;

            try
            {
                var method = AccessTools.Method(typeof(DrawingToolHub), "ColorButtons");
                method?.Invoke(hub, null);
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to update color buttons: {ex.Message}");
            }
        }

        #endregion

        #region 高级绘图操作 API

        /// <summary>
        /// 创建压感线段（专用于压感绘图）
        /// </summary>
        public static DrawableShape CreatePressureSegment(Vector2 position, Color color, float brushSize, Transform parent = null)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null)
            {
                DrawModuleEntry.logger.LogError("DrawModule instance not found!");
                return null;
            }

            brushSize = Mathf.Clamp(brushSize, 1f, 199f);

            // 创建基础形状，位置设为零点（压感线条的特殊需求）
            var shape = CreateShape(Vector2.zero, color, (int)brushSize, parent);

            if (shape != null)
            {
                // 压感线条特殊设置
                shape.SetIsPressureSegment(true);

                // 确保橡皮擦按钮被设置
                SetEraseButtonsState(true);
            }

            return shape;
        }

        /// <summary>
        /// 完成当前绘制操作
        /// </summary>
        public static void FinishCurrentDrawing(DrawableElement drawable)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null || drawable == null) return;

            // 设置图层
            drawable.Layer = GetSelectedLayer();

            // 增加笔画计数
            drawModule.SortOrder.IncreaseStrokeCount(drawable.DrawType == DrawElementT.Fill);

            // 清空进行中的状态
            ClearInProgressStates();

            // 检查是否是压感线段
            bool isPressure = drawable is DrawableShape shape && shape.GetIsPressureSegment();

            if (!isPressure)
            {
                // 添加到撤销系统
                drawModule.UndoSystem.AddEvent(drawModule.UndoSystem.CreateUndoDrawElement(drawable));
                UpdateUndoButtons();
            }
        }

        /// <summary>
        /// 检查防抖器是否允许操作
        /// </summary>
        public static bool CheckDebouncerAllowed()
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return true;

            try
            {
                var debouncerField = AccessTools.DeclaredField(typeof(DrawModule), "Debouncer");
                var debouncer = debouncerField?.GetValue(drawModule);

                if (debouncer == null) return true;

                var method = AccessTools.Method(debouncer.GetType(), "CheckAllowed");
                return (bool)(method?.Invoke(debouncer, null) ?? true);
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to check debouncer: {ex.Message}");
                return true;
            }
        }

        #endregion

        #region 扩展的元素管理 API

        /// <summary>
        /// 发送精确删除消息
        /// </summary>
        public static void SendDeletePrecise(int elementId)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return;

            try
            {
                var method = AccessTools.Method(typeof(DrawModule), "OnSendDeletePrecise");
                method?.Invoke(drawModule, new object[] { elementId });
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to send delete precise: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送精确更新消息
        /// </summary>
        public static void SendUpdatePrecise(DrawableElement element, Color oldColor, Color newColor)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null || element == null) return;

            try
            {
                var method = AccessTools.Method(typeof(DrawModule), "OnSendUpdatePrecise");
                method?.Invoke(drawModule, new object[] { element, oldColor, newColor });
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to send update precise: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建多元素撤销操作
        /// </summary>
        public static IUndo CreateMultiElementUndo(IEnumerable<DrawableElement> elements)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null) return null;

            return new UndoMultiDrawElement(elements, drawModule);
        }

        /// <summary>
        /// 添加撤销事件到撤销系统
        /// </summary>
        public static void AddUndoEvent(IUndo undoEvent)
        {
            var drawModule = GetCurrentDrawModule();
            if (drawModule == null || undoEvent == null) return;

            drawModule.UndoSystem.AddEvent(undoEvent);
            UpdateUndoButtons();
        }

        #endregion
        #region 元素高级操作 API (DrawableElement)

        /// <summary>
        /// 强制更改元素的所有者 ID
        /// </summary>
        public static void SetElementOwner(DrawableElement element, string newOwnerId)
        {
            if (element == null) return;
            // 优先使用游戏自带的公共方法
            element.ChangeOwner(newOwnerId);
        }

        /// <summary>
        /// 启用或禁用元素的重力效果 (物理)
        /// </summary>
        /// <returns>返回碰撞次数，如果不支持重力则返回 0</returns>
        public static int SetElementGravity(DrawableElement element, bool enabled)
        {
            if (element == null) return 0;
            return element.SetGravityEnabled(enabled);
        }

        /// <summary>
        /// 初始化元素的重力状态
        /// </summary>
        public static void InitElementGravity(DrawableElement element, bool enabled, bool newlyCreatedByOwner)
        {
            if (element == null) return;
            element.InitGravityEnabled(enabled, newlyCreatedByOwner);
        }

        public static LineInformation ConvertToLineInfo(DrawableElement element, bool normalize = true)
        {
            if (element == null) return null;
            // shiftByTransformDueToGravity 通常设为 false，除非你需要根据重力位移后的位置保存
            return element.ToLineInformation(false, normalize);
        }

        /// <summary>
        /// 判断元素是否被标记为删除
        /// </summary>
        public static bool IsElementDeleted(DrawableElement element)
        {
            return element != null && element.IsMarkedAsDeleted;
        }

        #endregion

        #region 大逃杀模式专用 API (BattleRoyal)

        /// <summary>
        /// 检查当前是否处于大逃杀绘图模式
        /// </summary>
        public static bool IsBattleRoyalMode()
        {
            var module = GetCurrentDrawModule();
            return module is BattleRoyalDrawModule;
        }

        /// <summary>
        /// 获取当前的大逃杀绘图模块实例
        /// </summary>
        public static BattleRoyalDrawModule GetBattleRoyalModule()
        {
            return GetCurrentDrawModule() as BattleRoyalDrawModule;
        }

        /// <summary>
        /// [大逃杀] 检查是否在大厅模式（等待中）
        /// </summary>
        public static bool IsInLobbyMode()
        {
            var brModule = GetBattleRoyalModule();
            return brModule != null && brModule.InLobbyMode;
        }

        /// <summary>
        /// [大逃杀] 强制返回大厅
        /// </summary>
        public static void BackToLobby()
        {
            var brModule = GetBattleRoyalModule();
            if (brModule == null) return;

            // 调用公共方法
            brModule.BackToLobby();
        }

        /// <summary>
        /// [大逃杀] 强制提交画作
        /// </summary>
        public static void ForceSubmitBattleRoyal()
        {
            var brModule = GetBattleRoyalModule();
            if (brModule == null) return;

            brModule.SubmitDrawing();
        }




        // 确保 DrawModuleLib.DrawModuleAPI 类的定义在这里


            #region 笔刷大小和 UI 辅助 API

            /// <summary>
            /// 获取当前平台下的最大笔刷大小。
            /// 内部逻辑与 DrawModuleAPI.SetBrushSize 中的钳制逻辑保持一致。
            /// </summary>
            public static int GetMaxAllowedBrushSize()
            {
                // 假设 DrawModuleAPI 能够访问到 GlobalVars
                // 如果 GlobalVars 不在 DrawModuleLib 的引用中，您需要通过反射访问
                // 或者在 BepInEx 插件的 Awake 中，将 GlobalVars.IsMobile 的值传递给 API。
                // 为了简化，这里假设 GlobalVars.IsMobile 可直接访问。
                return GlobalVars.IsMobile ? GlobalVars.MAX_BRUSH_SIZE_MOBILE : GlobalVars.MAX_BRUSH_SIZE;
            }

            /// <summary>
            /// 获取当前平台是否为移动设备。
            /// </summary>
            public static bool IsMobilePlatform()
            {
                // 同样，假设可以访问 GlobalVars
                return GlobalVars.IsMobile;
            }

            public static float CalculateVisualScaleFactor(float currentBrushSize, float minVisualScale, float maxVisualScale)
            {
                // 原始代码：
                // float num = MiscFunctions.Normalize(this.Slider.value, (float)(GlobalVars.IsMobile ? 28 : 35), 2f);
                // num = MiscFunctions.DeNormalize(num, this.MaxScale, this.MinScale);

                // 这里的 Normalize 和 DeNormalize 假设是线性映射。
                // MiscFunctions.Normalize(value, max, min) 相当于 Mathf.InverseLerp(min, max, value)
                // MiscFunctions.DeNormalize(normalizedValue, newMax, newMin) 相当于 Mathf.Lerp(newMin, newMax, normalizedValue)

                float minBrushSize = 2f; // 来自 LineWeightTool 和 DrawModuleAPI.SetBrushSize 的钳制值
                float maxBrushForCurrentPlatform = GetMaxAllowedBrushSize();

                // 将笔刷大小从 [minBrushSize, maxBrushForCurrentPlatform] 映射到 [0, 1]
                float normalizedValue = Mathf.InverseLerp(minBrushSize, maxBrushForCurrentPlatform, currentBrushSize);

                // 将 [0, 1] 的值映射到 [minVisualScale, maxVisualScale]
                float visualScale = Mathf.Lerp(minVisualScale, maxVisualScale, normalizedValue);

                return visualScale;
            }

            #endregion




   
    public static void SetRoundSpecialMode(GameSpecialMode mode)
        {
            var brModule = GetBattleRoyalModule();
            if (brModule == null) return;

            brModule.RoundSpecialMode = mode;
        }

        #endregion
        #endregion
        // 在DrawModuleAPI文件中添加这个类
        /// <summary>
        /// 多元素撤销操作
        /// </summary>
        public class UndoMultiDrawElement : IUndo
        {
            private readonly List<DrawableElement> _elements;
            private readonly DrawModule _drawModule;

            public UndoMultiDrawElement(IEnumerable<DrawableElement> elements, DrawModule drawModule)
            {
                _elements = new List<DrawableElement>(elements);
                _drawModule = drawModule;
            }

            public void Undo()
            {
                foreach (var element in _elements.Where(e => e != null))
                {
                    element.MarkAsDeleted(true);

                    if (element is DrawableShape shape &&
                        shape.GetIsPressureSegment() &&
                        element.Owner == DrawModuleReflection.GetLocalPlayfabId(_drawModule))
                    {
                        DrawModuleAPI.SendDeletePrecise(element.Ident);
                    }
                }
            }

            public void Redo()
            {
                foreach (var element in _elements.Where(e => e != null))
                {
                    element.MarkAsDeleted(false);
                }
            }
        }

    }

}
