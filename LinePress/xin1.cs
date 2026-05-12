using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using UnityEngine;
using UnityEngine.InputSystem;
using Shapes;
using DrawModuleLib;
using BepInEx;
using BepInEx.Logging;
using System.Collections.ObjectModel;
using UnityEngine.UI;

#region Module State Management

public class ModuleState
{
    public bool LayersVisible { get; set; } = true;
    public GUIStyle LayerStyle { get; set; }
    public GUIStyle ToggleStyle { get; set; }
    public Rect ToggleButtonRect { get; set; }
    public bool IsLineBeingDrawn { get; set; }
    public PressureLineCreator PressureLineCreator { get; set; }
}

#endregion

#region Layer UI

internal static class LayerUIConstants
{
    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;
    public const float PanelX = 1680f;
    public const float PanelLayerY_Start = 20f;
    public const float PanelLayerY_Spacing = 70f;
    public const float PanelLayerWidth = 70f;
    public const float PanelLayerHeight = 80f;
    public const float ToggleButtonX_Offset = 35f;
    public const float ToggleButtonY = 10f;
    public const float ToggleButtonWidth = 40f;
    public const float ToggleButtonHeight = 40f;
    public const float IndicatorX_Offset = 10f;
    public const float IndicatorWidth = 80f;
    public const int LayerLabelFontSize = 30;
    public const int LayerLabelSelectedFontSize = 40;
    public const int ToggleButtonFontSize = 50;
    public const int IndicatorFontSize = 30;
}

public class IngameDrawModuleLayerUIExt
{
    public bool LayersVisible = true;
    public bool IsInputProcessed;
    public GUIStyle LayerStyle;
    public GUIStyle ToggleStyle;
    public Rect ToggleButtonRect;
    public bool UIWasVisible;
}

public static class IngameDrawModuleLayerUIExtensions
{
    // 在 IngameDrawModuleLayerUIExtensions 中
    private static int ReadSelectedLayer(this IngameDrawModule self)
    {
        
        return DrawModuleAPI.GetSelectedLayer();
    }

    private static void WriteSelectedLayer(this IngameDrawModule self, int val)
    {
        
        DrawModuleAPI.SetSelectedLayer(val);
    }
    private static readonly ConditionalWeakTable<IngameDrawModule, IngameDrawModuleLayerUIExt> data =
        new ConditionalWeakTable<IngameDrawModule, IngameDrawModuleLayerUIExt>();

    public static IngameDrawModuleLayerUIExt Ext(this IngameDrawModule obj) => data.GetOrCreateValue(obj);

    public static int GetCurrentLayerIndex(this IngameDrawModule self) => 2 - self.ReadSelectedLayer();
    public static void SwitchLayerReflect(this IngameDrawModule self, int uiLayerIndex) => self.WriteSelectedLayer(2 - uiLayerIndex);

    private static float GetScale() => Mathf.Min(Screen.width / LayerUIConstants.ReferenceWidth, Screen.height / LayerUIConstants.ReferenceHeight);

    public static void InitializeStyles(this IngameDrawModule self)
    {
        var ext = self.Ext();
        float scale = GetScale();

        ext.LayerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(LayerUIConstants.LayerLabelFontSize * scale),
            normal = { textColor = Color.gray },
            clipping = TextClipping.Overflow,
            wordWrap = false
        };
        ext.ToggleStyle = new GUIStyle(ext.LayerStyle)
        {
            fontSize = Mathf.RoundToInt(LayerUIConstants.ToggleButtonFontSize * scale)
        };
    }

    public static void HandleToggleShortcut(this IngameDrawModule self)
    {
        var ext = self.Ext();
        if (ext.IsInputProcessed) return;

        var kb = Keyboard.current;
        if (kb != null && kb.backquoteKey.wasPressedThisFrame)
        {
            ext.IsInputProcessed = true;
            ext.LayersVisible = !ext.LayersVisible;
        }
    }

    public static void DrawToggleButton(this IngameDrawModule self)
    {
        var ext = self.Ext();
        float scale = GetScale();
        ext.ToggleButtonRect = new Rect(
            scale * LayerUIConstants.PanelX + LayerUIConstants.ToggleButtonX_Offset * scale,
            scale * LayerUIConstants.ToggleButtonY,
            LayerUIConstants.ToggleButtonWidth * scale,
            LayerUIConstants.ToggleButtonHeight * scale
        );

        string text = (!ext.LayersVisible ? "∨" : "∧");
        if (GUI.Button(ext.ToggleButtonRect, text, ext.ToggleStyle))
        {
            ext.LayersVisible = !ext.LayersVisible;
        }

        if (!ext.LayersVisible)
            self.DrawLayerIndicator(scale);
    }

    public static void DrawLayerIndicator(this IngameDrawModule self, float scale)
    {
        var ext = self.Ext();
        int currentLayerIndex = self.GetCurrentLayerIndex();

        Rect rect = new Rect(
            ext.ToggleButtonRect.x + ext.ToggleButtonRect.width + LayerUIConstants.IndicatorX_Offset * scale,
            ext.ToggleButtonRect.y,
            LayerUIConstants.IndicatorWidth * scale,
            ext.ToggleButtonRect.height
        );

        GUIStyle guistyle = new GUIStyle(ext.ToggleStyle)
        {
            fontSize = Mathf.RoundToInt(LayerUIConstants.IndicatorFontSize * scale),
            normal = { textColor = Color.gray }
        };

        string text = "";
        for (int i = 0; i < 3; i++)
            text += (i == currentLayerIndex) ? "●" : "○";

        GUI.Label(rect, text, guistyle);
    }

    public static void HandleKeyboardShortcuts(this IngameDrawModule self)
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) self.SwitchLayerReflect(0);
            else if (kb.digit2Key.wasPressedThisFrame) self.SwitchLayerReflect(1);
            else if (kb.digit3Key.wasPressedThisFrame) self.SwitchLayerReflect(2);
        }
    }

    public static void HandlePointerInput(this IngameDrawModule self)
    {
        var ext = self.Ext();
        if (ext.IsInputProcessed) return;

        var pen = Pen.current;
        bool penDown = pen != null && pen.tip.wasPressedThisFrame;
        var mouse = Mouse.current;
        bool mouseDown = mouse != null && mouse.leftButton.wasPressedThisFrame;

        if (!penDown && !mouseDown) return;

        ext.IsInputProcessed = true;
        Vector2 pos = penDown ? pen.position.ReadValue() : mouse.position.ReadValue();
        pos.y = Screen.height - pos.y;

        if (ext.ToggleButtonRect.Contains(pos))
        {
            ext.LayersVisible = !ext.LayersVisible;
            GUI.changed = true;
            return;
        }

        if (ext.LayersVisible)
        {
            float scale = GetScale();
            for (int i = 0; i < 3; i++)
            {
                Rect rect = new Rect(
                    scale * LayerUIConstants.PanelX,
                    scale * (LayerUIConstants.PanelLayerY_Start + LayerUIConstants.PanelLayerY_Spacing * i),
                    LayerUIConstants.PanelLayerWidth * scale,
                    LayerUIConstants.PanelLayerHeight * scale
                );
                if (rect.Contains(pos))
                {
                    self.SwitchLayerReflect(i);
                    return;
                }
            }
        }
    }

    public static void DrawLayerUI(this IngameDrawModule self)
    {
        var ext = self.Ext();
        float scale = GetScale();

        for (int i = 0; i < 3; i++)
        {
            Rect rect = new Rect(
                scale * LayerUIConstants.PanelX,
                scale * (LayerUIConstants.PanelLayerY_Start + LayerUIConstants.PanelLayerY_Spacing * i),
                LayerUIConstants.PanelLayerWidth * scale,
                LayerUIConstants.PanelLayerHeight * scale
            );

            if (self.GetCurrentLayerIndex() == i)
            {
                ext.LayerStyle.fontSize = Mathf.RoundToInt(LayerUIConstants.LayerLabelSelectedFontSize * scale);
                ext.LayerStyle.normal.textColor = Color.black;
            }

            GUI.Label(rect, "图层" + (i + 1).ToString(), ext.LayerStyle);

            ext.LayerStyle.fontSize = Mathf.RoundToInt(LayerUIConstants.LayerLabelFontSize * scale);
            ext.LayerStyle.normal.textColor = Color.gray;
        }
    }
}

#endregion

#region Pressure Line System

public static class DrawModulePressurePatch
{
    public class PressureModuleState
    {
        public bool isLineInDrawing;
        public PressureLineCreator pressureLineCreator;
    }

    private static readonly ConditionalWeakTable<DrawModule, PressureModuleState> States =
        new ConditionalWeakTable<DrawModule, PressureModuleState>();

    public static PressureModuleState GetState(DrawModule inst) => States.GetOrCreateValue(inst);

   
    /// 创建新的压感线条
    
    public static void CreateNewPressureLine(DrawModule module)
    {
        var state = GetState(module);
        if (!module.GetWorldCoordsIfOverDrawSurface(out var start)) return;

        int brushSize = DrawModuleAPI.GetBrushSize();
        float minWidth = brushSize <= 41 ? 1f : (brushSize > 50 ? 10f : brushSize - 40f);
        float maxWidth = brushSize <= 41 ? brushSize : brushSize;
        float pressure = Pen.current?.pressure.ReadValue() ?? 1f;
        float currentWidth = Mathf.Lerp(minWidth, maxWidth, pressure);

        var shape = module.CreateNewPressureSegment(start, currentWidth);
        state.pressureLineCreator = shape.gameObject.AddComponent<PressureLineCreator>();
        state.pressureLineCreator.SetDrawModule(module);
        state.pressureLineCreator.SetBrushSizeRange(minWidth, maxWidth);
        state.pressureLineCreator.StartNewLine(start, shape);
        state.isLineInDrawing = true;
    }

    
    /// 创建压感线段 - 使用 DrawModuleAPI
    
    public static DrawableShape CreateNewPressureSegment(this DrawModule drawModule, Vector2 startPoint, float brushSize)
    {
        brushSize = Mathf.Clamp(brushSize, 1f, 99f);

        // 使用 DrawModuleAPI 的专用压感线段创建方法
        var shape = DrawModuleAPI.CreatePressureSegment(Vector2.zero, DrawModuleAPI.GetCurrentColor(), brushSize);

        return shape;
    }


    public static void SetParentAndZPos(Component elem, Transform parent, float zVal = 0f)
    {
        var t = elem.transform;
        t.SetParent(parent);
        var p = t.localPosition;
        t.localPosition = new Vector3(p.x, p.y, zVal);
    }

    public static void ProcessPressureLineForFill(LineInformation lineInfo)
    {
        List<SerializableV2> points = lineInfo.Points.ToList();
        int count = points.Count;

        if (count < 2) return;

        List<SerializableV2> newPoints = new List<SerializableV2>();

        if (count % 2 == 0)
        {
            int mid1 = count / 2 - 1;
            int mid2 = count / 2;

            for (int k = 0; mid1 - k >= 0 && mid2 + k < count; k++)
            {
                Vector2 v1 = new Vector2(points[mid1 - k].x, points[mid1 - k].y);
                Vector2 v2 = new Vector2(points[mid2 + k].x, points[mid2 + k].y);
                Vector2 midPoint = (v1 + v2) * 0.5f;
                newPoints.Add(new SerializableV2(midPoint.x, midPoint.y));
            }
        }
        else
        {
            int mid = (count - 1) / 2;
            newPoints.Add(points[mid]);

            for (int k = 1; mid - k >= 0 && mid + k < count; k++)
            {
                Vector2 v1 = new Vector2(points[mid - k].x, points[mid - k].y);
                Vector2 v2 = new Vector2(points[mid + k].x, points[mid + k].y);
                Vector2 midPoint = (v1 + v2) * 0.5f;
                newPoints.Add(new SerializableV2(midPoint.x, midPoint.y));
            }
        }

        lineInfo.Points = newPoints.ToArray();
        lineInfo.DrawElementType = 0;
        lineInfo.BrushSize = 3;
    }

    public static List<LineInformation> MergeAdjacentLines(List<LineInformation> lines)
    {
        List<LineInformation> output = new List<LineInformation>();
        const float mergeThreshold = 0.3f;

        for (int i = 0; i < lines.Count; i++)
        {
            if (output.Count == 0)
            {
                output.Add(lines[i]);
                continue;
            }

            LineInformation prev = output.Last();
            LineInformation current = lines[i];

            bool canMerge = prev.DrawElementType == 0 && current.DrawElementType == 0 &&
                           prev.BrushSize == 3 && current.BrushSize == 3 &&
                           prev.Points.Length > 0 && current.Points.Length > 0;

            if (canMerge)
            {
                SerializableV2 lastPoint = prev.Points.Last();
                SerializableV2 firstPoint = current.Points.First();

                if (Math.Abs(lastPoint.x - firstPoint.x) < mergeThreshold &&
                    Math.Abs(lastPoint.y - firstPoint.y) < mergeThreshold)
                {
                    List<SerializableV2> merged = prev.Points.ToList();
                    merged.AddRange(current.Points.Skip(1));
                    prev.Points = merged.ToArray();
                    continue;
                }
            }

            output.Add(current);
        }

        return output;
    }
}

#endregion

#region Reflection Cache

internal static class ReflectionCache
{
    #region 反射成员

    // UI相关的方法调用
    public static readonly MethodInfo meth_Hub_SetEraseButtons = AccessTools.Method(typeof(DrawingToolHub), "SetEraseButtons", new[] { typeof(bool) });
    public static readonly MethodInfo meth_Hub_SetUndoButtons = AccessTools.Method(typeof(DrawingToolHub), "SetUndoButtons");

    // 内部状态管理
    public static readonly FieldInfo fld_ShapeInProgress = AccessTools.DeclaredField(typeof(DrawModule), "ShapeInProgress");
    public static readonly FieldInfo fld_LineInProgress = AccessTools.DeclaredField(typeof(DrawModule), "LineInProgress");

    // 图层设置（如果API没有提供的话）
    public static readonly PropertyInfo prop_SelectedLayer = AccessTools.DeclaredProperty(typeof(DrawModule), "SelectedLayer");

    // 委托缓存（只保留必要的）
    private static Action<DrawModule, int> _setSelectedLayerDelegate;

    #endregion

    #region 静态构造函数

    static ReflectionCache()
    {
        InitializeDelegates();
    }

    private static void InitializeDelegates()
    {
        try
        {
            
            if (prop_SelectedLayer != null)
            {
                var setter = prop_SelectedLayer.GetSetMethod(true);
                if (setter != null)
                    _setSelectedLayerDelegate = (Action<DrawModule, int>)Delegate.CreateDelegate(
                        typeof(Action<DrawModule, int>), setter);
            }
        }
        catch (Exception ex)
        {
            MyMod.ModLogger?.LogError($"Failed to initialize delegates: {ex.Message}");
        }
    }

    #endregion

    #region 简化的访问方法

    
    /// 设置选中的图层（如果API没有提供）
    
    public static void SetSelectedLayer(DrawModule dm, int layer)
    {
        if (dm == null) return;

        try
        {
            if (_setSelectedLayerDelegate != null)
                _setSelectedLayerDelegate(dm, layer);
            else
                prop_SelectedLayer?.SetValue(dm, layer);
        }
        catch (Exception ex)
        {
            MyMod.ModLogger?.LogError($"Failed to set selected layer: {ex.Message}");
        }
    }

    
    /// 调用设置橡皮擦按钮方法
    
    public static void InvokeSetEraseButtons(object hub, bool value)
    {
        if (hub == null || meth_Hub_SetEraseButtons == null) return;

        try
        {
            meth_Hub_SetEraseButtons.Invoke(hub, new object[] { value });
        }
        catch (Exception ex)
        {
            MyMod.ModLogger?.LogError($"Failed to invoke SetEraseButtons: {ex.Message}");
        }
    }

    
    /// 调用设置撤销按钮方法
    
    public static void InvokeSetUndoButtons(object hub)
    {
        if (hub == null || meth_Hub_SetUndoButtons == null) return;

        try
        {
            meth_Hub_SetUndoButtons.Invoke(hub, null);
        }
        catch (Exception ex)
        {
            MyMod.ModLogger?.LogError($"Failed to invoke SetUndoButtons: {ex.Message}");
        }
    }

    
    /// 设置正在绘制的形状
    
    public static void SetShapeInProgress(DrawModule drawModule, DrawableShape shape)
    {
        if (drawModule == null || fld_ShapeInProgress == null) return;

        try
        {
            fld_ShapeInProgress.SetValue(drawModule, shape);
        }
        catch (Exception ex)
        {
            MyMod.ModLogger?.LogError($"Failed to set shape in progress: {ex.Message}");
        }
    }

    
    /// 设置正在绘制的线条
    
    public static void SetLineInProgress(DrawModule drawModule, DrawableLine line)
    {
        if (drawModule == null || fld_LineInProgress == null) return;

        try
        {
            fld_LineInProgress.SetValue(drawModule, line);
        }
        catch (Exception ex)
        {
            MyMod.ModLogger?.LogError($"Failed to set line in progress: {ex.Message}");
        }
    }

    #endregion
}

#endregion

#region DrawModule Extensions (保持不变)


/// 撤销操作

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
                SendDeletePrecise(_drawModule, element.Ident);
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

    private static void SendDeletePrecise(DrawModule dm, int ident)
    {
        var method = AccessTools.Method(typeof(DrawModule), "OnSendDeletePrecise");
        method?.Invoke(dm, new object[] { ident });
    }
}


/// DrawModule 扩展方法

public static class DrawModuleExtensions
{
    /// 获取本地 Playfab ID
    public static string GetLocalPlayfabId(this DrawModule dm)
    {
        return DrawModuleReflection.GetLocalPlayfabId(dm);
    }

    
    public static void SendDeletePrecise(this DrawModule dm, int ident)
    {
        var method = AccessTools.Method(typeof(DrawModule), "OnSendDeletePrecise");
        method?.Invoke(dm, new object[] { ident });
    }

    public static IUndo CreateUndoMultiDrawElement(this DrawModule dm, IEnumerable<DrawableElement> elements)
    {
        return new UndoMultiDrawElement(elements, dm);
    }

    public static List<DrawableElement> GetAllDrawables(this DrawModule dm)
    {
        if (dm == null) return new List<DrawableElement>();

        var createdList = DrawModuleReflection.GetCreatedDrawablesByMe(dm);
        if (createdList is ObservableCollection<DrawableElement> collection)
        {
            return collection.ToList();
        }

        return new List<DrawableElement>();
    }
}

#endregion

#region Main Plugin
[HarmonyPatch(typeof(PaletteManager), "TryGetPaletteForKeyboardShortcut")]
public static class PaletteManager_TryGetPaletteForKeyboardShortcut_Patch { [HarmonyPrefix] static bool Prefix(out int index) { for (int i = 45; i < 50; i++) { if (Keyboard.current[(Key)i].wasPressedThisFrame) { index = i - 45; return false; } } index = -1; return false; } }
[BepInPlugin("com.hjjs.LayersAndPressure", "图层与压感补丁插件", "1.6")]
public class MyMod : BaseUnityPlugin
{
    internal static ManualLogSource ModLogger;

    private void Awake()
{
    ModLogger = Logger;

    // 应用Hook补丁
    ApplyHooks();
    

    // 其他初始化...
    RegisterDrawModuleEvents();
    CreateOrFindDrawLayerGUI();
    var harmony = new Harmony("com.hjjs.LayersAndPressure");
    harmony.PatchAll(Assembly.GetExecutingAssembly());
}


    private void ApplyHooks()
    {

        try
        {


            // DrawModule补丁
            new Hook(
                AccessTools.Method(typeof(DrawModule), "CreateNewLine"),
                typeof(MyMod).GetMethod(nameof(DrawModule_CreateNewLine_Hook), BindingFlags.NonPublic | BindingFlags.Static)
            );

            new Hook(
                AccessTools.Method(typeof(DrawModule), "GetLinesForFill"),
                typeof(MyMod).GetMethod(nameof(DrawModule_GetLinesForFill_Hook), BindingFlags.NonPublic | BindingFlags.Static)
            );

            new Hook(
                AccessTools.Method(typeof(DrawModule), "FinishLine"),
                typeof(MyMod).GetMethod(nameof(DrawModule_FinishLine_Hook), BindingFlags.NonPublic | BindingFlags.Static)
            );

            new Hook(
                AccessTools.Method(typeof(DrawModule), "OnFinishDrawingCurrent"),
                typeof(MyMod).GetMethod(nameof(DrawModule_OnFinishDrawingCurrent_Hook), BindingFlags.NonPublic | BindingFlags.Static)
            );

        }
        catch (Exception ex)
        {
        }
    }

    #region Hook Methods

    private static void DrawModule_CreateNewLine_Hook(Action<DrawModule> orig, DrawModule self)
    {
        DrawModulePressurePatch.GetState(self).isLineInDrawing = true;
        bool usePressure = MenuGameSettings_D.UsePenPressure.Value;

        if (!usePressure)
        {
            orig(self);
            return;
        }

        DrawModulePressurePatch.CreateNewPressureLine(self);
    }

    private static List<LineInformation> DrawModule_GetLinesForFill_Hook(
        Func<DrawModule, List<LineInformation>> orig,
        DrawModule self)
    {
        // 使用 DrawModuleAPI 获取绘图信息
        List<LineInformation> drawingAsLineInfo = DrawModuleAPI.GetDrawingInfo(true, false);

        // 处理压感线段
        foreach (LineInformation lineInfo in drawingAsLineInfo)
        {
            if (lineInfo.DrawElementType == 2 && lineInfo.Points != null)
            {
                DrawModulePressurePatch.ProcessPressureLineForFill(lineInfo);
            }
        }

        // 合并相邻线段
        List<LineInformation> output = DrawModulePressurePatch.MergeAdjacentLines(drawingAsLineInfo);
        return output;
    }

    private static void DrawModule_FinishLine_Hook(
        Action<DrawModule, bool> orig,
        DrawModule self,
        bool addFinishingPoint)
    {
        var state = DrawModulePressurePatch.GetState(self);
        state.isLineInDrawing = false;

        bool usePressure = MenuGameSettings_D.UsePenPressure.Value;
        var creator = state.pressureLineCreator;

        if (usePressure && creator != null)
        {
            creator.FinishLine();
            state.pressureLineCreator = null;
            return;
        }

        orig(self, addFinishingPoint);
    }

    private static void DrawModule_OnFinishDrawingCurrent_Hook(
     Action<DrawModule, DrawableElement> orig,
     DrawModule self,
     DrawableElement drawable)
    {
        if (drawable == null) return;

        // 使用 API 完成绘制
        DrawModuleAPI.FinishCurrentDrawing(drawable);
    }

    #endregion

    private void RegisterDrawModuleEvents()
    {
        // 使用新的 API
        DrawModuleAPI.OnDrawModuleReady(dm =>
        {
            Logger.LogInfo("DrawModule 已准备就绪");
        });
    }

    private void CreateOrFindDrawLayerGUI()
    {
        if (UnityEngine.Object.FindFirstObjectByType<MyDrawLayerGUI>() == null)
        {
            var go = new GameObject("MyDrawLayerGUI");
            DontDestroyOnLoad(go);
            go.AddComponent<MyDrawLayerGUI>();
            Logger.LogInfo("DrawLayerGUI 已创建");
        }
    }
}

public class MyDrawLayerGUI : MonoBehaviour
{
    private IngameDrawModule drawModule;

    void OnGUI()
    {
        if (drawModule == null)
        {
            drawModule = UnityEngine.Object.FindFirstObjectByType<IngameDrawModule>();
        }

        if (drawModule == null) return;

        var ext = drawModule.Ext();

        // 使用 API 检查绘图状态
        bool canDraw = DrawModuleAPI.CanDraw();
        bool hasOwnLines = DrawModuleAPI.HasOwnLines();
        bool hasSubmitted = DrawModuleAPI.HasSubmitted();

        // 根据状态调整UI显示
        if (canDraw && !ext.UIWasVisible)
        {
            drawModule.SwitchLayerReflect(0);
        }

        ext.UIWasVisible = canDraw;

        if (canDraw)
        {
            // 显示当前工具信息
            var currentTool = DrawModuleAPI.GetActiveTool();
            var currentColor = DrawModuleAPI.GetCurrentColor();
            var brushSize = DrawModuleAPI.GetBrushSize();

         
            // 原有的UI逻辑
            if (ext.LayerStyle == null)
            {
                drawModule.InitializeStyles();
            }

            drawModule.HandleToggleShortcut();
            drawModule.DrawToggleButton();
            drawModule.HandleKeyboardShortcuts();
            drawModule.HandlePointerInput();

            if (ext.LayersVisible)
            {
                drawModule.DrawLayerUI();
            }
        }
    }

   

    void LateUpdate()
    {
        if (drawModule != null)
        {
            drawModule.Ext().IsInputProcessed = false;
        }
    }
}

#endregion
