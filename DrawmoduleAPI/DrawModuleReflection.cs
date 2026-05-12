using HarmonyLib;
using System.Reflection;
using System;
using UnityEngine;

namespace DrawModuleLib
{
    public static class DrawModuleReflection
    {
        #region DrawModule 私有字段

        public static readonly FieldInfo fld_LineInProgress = AccessTools.Field(typeof(DrawModule), "LineInProgress");
        public static readonly FieldInfo fld_ShapeInProgress = AccessTools.Field(typeof(DrawModule), "ShapeInProgress");
        public static readonly FieldInfo fld_CreatedDrawablesByMe = AccessTools.Field(typeof(DrawModule), "CreatedDrawablesByMe");
        public static readonly FieldInfo fld_RGBColor = AccessTools.Field(typeof(DrawModule), "RGBColor");
        public static readonly FieldInfo fld_ActiveDrawTool = AccessTools.Field(typeof(DrawModule), "activeDrawTool");
        public static readonly FieldInfo fld_LastPos = AccessTools.Field(typeof(DrawModule), "lastPos");
        public static readonly FieldInfo fld_ColorBeforeBGColor = AccessTools.Field(typeof(DrawModule), "colorBeforeBGColor");
        public static readonly FieldInfo fld_Debouncer = AccessTools.Field(typeof(DrawModule), "Debouncer");
        public static readonly FieldInfo fld_SortOrder = AccessTools.Field(typeof(DrawModule), "sortOrder");
        public static readonly FieldInfo fld_LineParent = AccessTools.Field(typeof(DrawModule), "LineParent");
        public static readonly FieldInfo fld_DrawingToolHub = AccessTools.Field(typeof(DrawModule), "DrawingToolHub");
        public static readonly FieldInfo fld_DrawableSurface = AccessTools.Field(typeof(DrawModule), "DrawableSurface");
        public static readonly FieldInfo fld_PaletteManager = AccessTools.Field(typeof(DrawModule), "PaletteManager");

        #endregion

        #region DrawModule 属性

        public static readonly PropertyInfo prop_LocalPlayfabId = AccessTools.Property(typeof(DrawModule), "LocalPlayfabId");
        public static readonly PropertyInfo prop_HasSubmitted = AccessTools.Property(typeof(DrawModule), "HasSubmitted");
        public static readonly PropertyInfo prop_SelectedLayer = AccessTools.Property(typeof(DrawModule), "SelectedLayer");
        public static readonly PropertyInfo prop_ActiveDrawTool = AccessTools.Property(typeof(DrawModule), "ActiveDrawTool");
        public static readonly PropertyInfo prop_SpecialMode = AccessTools.Property(typeof(DrawModule), "SpecialMode");
        public static readonly PropertyInfo prop_ColorMode = AccessTools.Property(typeof(DrawModule), "ColorMode");
        public static readonly PropertyInfo prop_UndoSystem = AccessTools.Property(typeof(DrawModule), "UndoSystem");
        public static readonly PropertyInfo prop_SortOrder = AccessTools.Property(typeof(DrawModule), "SortOrder");
        public static readonly PropertyInfo prop_DrawSurface = AccessTools.Property(typeof(DrawModule), "DrawSurface");

        #endregion

        #region DrawModule 方法

        public static readonly MethodInfo meth_CreateNewLine = AccessTools.Method(typeof(DrawModule), "CreateNewLine");
        public static readonly MethodInfo meth_CreateNewShape = AccessTools.Method(typeof(DrawModule), "CreateNewShape", new[] { typeof(Vector2) });
        public static readonly MethodInfo meth_CreateNewFill = AccessTools.Method(typeof(DrawModule), "CreateNewFill", new[] { typeof(Vector2) });
        public static readonly MethodInfo meth_FinishLine = AccessTools.Method(typeof(DrawModule), "FinishLine", new[] { typeof(bool) });
        public static readonly MethodInfo meth_TryAddNewPointToCurrentLine = AccessTools.Method(typeof(DrawModule), "TryAddNewPointToCurrentLine", new[] { typeof(Vector2), typeof(bool) });
        public static readonly MethodInfo meth_CheckSpecialDrawTools = AccessTools.Method(typeof(DrawModule), "CheckSpecialDrawTools", new[] { typeof(Vector2) });
        public static readonly MethodInfo meth_OnShapeFillDone = AccessTools.Method(typeof(DrawModule), "OnShapeFillDone");
        public static readonly MethodInfo meth_SetParentAndZPos = AccessTools.Method(typeof(DrawModule), "SetParentAndZPos");
        public static readonly MethodInfo meth_RestoreColorBeforeBGEraser = AccessTools.Method(typeof(DrawModule), "RestoreColorBeforeBGEraser");
        public static readonly MethodInfo meth_ActivateAcureusPhysics2DRaycasterIfNeeded = AccessTools.Method(typeof(DrawModule), "ActivateAcureusPhysics2DRaycasterIfNeeded");

        #endregion

        #region DrawingToolHub 相关

        public static readonly MethodInfo meth_Hub_SetEraseButtons = AccessTools.Method(typeof(DrawingToolHub), "SetEraseButtons", new[] { typeof(bool) });
        public static readonly MethodInfo meth_Hub_SetUndoButtons = AccessTools.Method(typeof(DrawingToolHub), "SetUndoButtons");
        public static readonly MethodInfo meth_Hub_HighlightButton = AccessTools.Method(typeof(DrawingToolHub), "HighlightButton");
        public static readonly MethodInfo meth_Hub_ColorButtons = AccessTools.Method(typeof(DrawingToolHub), "ColorButtons");

        #endregion
        #region DrawableElement 相关反射

        // Ident 只有 private setter，需要反射设置
        public static readonly MethodInfo setter_Element_Ident = AccessTools.PropertySetter(typeof(DrawableElement), "Ident");

        // Owner 只有 private setter，虽然有 ChangeOwner 方法，但反射更直接
        public static readonly MethodInfo setter_Element_Owner = AccessTools.PropertySetter(typeof(DrawableElement), "Owner");

        public static readonly FieldInfo fld_Element_MyGravity = AccessTools.Field(typeof(DrawableElement), "MyGravity");

        #endregion
        

        /// 强制设置绘图元素的 ID (绕过 private set)
        public static void SetElementIdent(DrawableElement element, int ident)
        {
            try
            {
                setter_Element_Ident?.Invoke(element, new object[] { ident });
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to set element Ident: {ex.Message}");
            }
        }
        #region 辅助方法

        /// 获取正在绘制的线条
        public static DrawableLine GetLineInProgress(DrawModule drawModule)
        {
            return (DrawableLine)fld_LineInProgress?.GetValue(drawModule);
        }

        /// 设置正在绘制的线条
        public static void SetLineInProgress(DrawModule drawModule, DrawableLine line)
        {
            fld_LineInProgress?.SetValue(drawModule, line);
        }

        /// 获取正在绘制的形状
        public static DrawableShape GetShapeInProgress(DrawModule drawModule)
        {
            return (DrawableShape)fld_ShapeInProgress?.GetValue(drawModule);
        }

        /// 设置正在绘制的形状
        public static void SetShapeInProgress(DrawModule drawModule, DrawableShape shape)
        {
            fld_ShapeInProgress?.SetValue(drawModule, shape);
        }

        /// 获取已创建的绘图元素列表
        public static System.Collections.ObjectModel.ObservableCollection<DrawableElement> GetCreatedDrawablesByMe(DrawModule drawModule)
        {
            return (System.Collections.ObjectModel.ObservableCollection<DrawableElement>)fld_CreatedDrawablesByMe?.GetValue(drawModule);
        }

        /// 获取当前 RGB 颜色
        public static Color GetRGBColor(DrawModule drawModule)
        {
            return (Color)(fld_RGBColor?.GetValue(drawModule) ?? Color.black);
        }

        /// 设置 RGB 颜色
        public static void SetRGBColor(DrawModule drawModule, Color color)
        {
            fld_RGBColor?.SetValue(drawModule, color);
        }

        /// 获取上一个点的位置
        
        public static Vector2 GetLastPos(DrawModule drawModule)
        {
            return (Vector2)(fld_LastPos?.GetValue(drawModule) ?? Vector2.zero);
        }

        /// 获取本地 Playfab ID
        public static string GetLocalPlayfabId(DrawModule drawModule)
        {
            return (string)prop_LocalPlayfabId?.GetValue(drawModule);
        }

        /// 获取 LineParent Transform
        public static Transform GetLineParent(DrawModule drawModule)
        {
            return (Transform)fld_LineParent?.GetValue(drawModule);
        }

        /// 获取 DrawingToolHub
        public static object GetDrawingToolHub(DrawModule drawModule)
        {
            return fld_DrawingToolHub?.GetValue(drawModule);
        }

        /// 获取 DrawableSurface
        public static DrawableSurface GetDrawableSurface(DrawModule drawModule)
        {
            return (DrawableSurface)fld_DrawableSurface?.GetValue(drawModule);
        }

        /// 调用 SetEraseButtons
        public static void InvokeSetEraseButtons(object hub, bool value)
        {
            meth_Hub_SetEraseButtons?.Invoke(hub, new object[] { value });
        }

        /// 调用 SetUndoButtons
        public static void InvokeSetUndoButtons(object hub)
        {
            meth_Hub_SetUndoButtons?.Invoke(hub, null);
        }

        /// 获取 SortOrder
        public static SortOrder GetSortOrder(DrawModule drawModule)
        {
            return drawModule?.SortOrder;  // 使用公共属性
        }

        /// 安全获取字段值
        public static T GetFieldValue<T>(object instance, FieldInfo field, T defaultValue = default(T))
        {
            try
            {
                return (T)(field?.GetValue(instance) ?? defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// 安全设置字段值
        public static void SetFieldValue(object instance, FieldInfo field, object value)
        {
            try
            {
                field?.SetValue(instance, value);
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to set field value: {ex.Message}");
            }
        }

        /// 安全调用方法
        public static T InvokeMethod<T>(object instance, MethodInfo method, params object[] parameters)
        {
            try
            {
                return (T)method?.Invoke(instance, parameters);
            }
            catch (Exception ex)
            {
                DrawModuleEntry.logger.LogError($"Failed to invoke method: {ex.Message}");
                return default(T);
            }
        }

        #endregion
    }
}
