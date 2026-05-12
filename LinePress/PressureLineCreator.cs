using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;

#region Pressure Line Extensions

public static class PressureLineDataExtension
{
    private class LineMetadata
    {
        public bool IsSegmentOfPressureLine;
        public string ParentLineIdentifier;
        public int GroupIdentifier = -1;
    }

    private static readonly ConditionalWeakTable<DrawableElement, LineMetadata> metadataStorage =
        new ConditionalWeakTable<DrawableElement, LineMetadata>();

    private static LineMetadata GetMetadata(DrawableElement element)
    {
        return element == null ? null : metadataStorage.GetOrCreateValue(element);
    }

    public static int GetLineGroupID(this DrawableElement element)
    {
        return GetMetadata(element)?.GroupIdentifier ?? -1;
    }

    public static void SetLineGroupID(this DrawableElement element, int identifier)
    {
        var metadata = GetMetadata(element);
        if (metadata != null) metadata.GroupIdentifier = identifier;
    }

    public static bool GetIsPressureSegment(this DrawableElement element)
    {
        return GetMetadata(element)?.IsSegmentOfPressureLine ?? false;
    }

    public static void SetIsPressureSegment(this DrawableElement element, bool isSegment)
    {
        var metadata = GetMetadata(element);
        if (metadata != null) metadata.IsSegmentOfPressureLine = isSegment;
    }

    public static string GetPressureLineId(this DrawableElement element)
    {
        return GetMetadata(element)?.ParentLineIdentifier;
    }

    public static void SetPressureLineId(this DrawableElement element, string lineId)
    {
        var metadata = GetMetadata(element);
        if (metadata != null) metadata.ParentLineIdentifier = lineId;
    }
}

#endregion

#region Pressure Line Creator

public class PressureLineCreator : MonoBehaviour
{
    [SerializeField] private float minPointDistanceMin = 0.01f;
    [SerializeField] private float minPointDistanceMax = 0.07f;
    [SerializeField] private int segmentSizeMin = 10;
    [SerializeField] private int segmentSizeMax = 35;

    private DrawModule drawModule;
    private List<DrawableShape> drawElements = new List<DrawableShape>();
    private DrawableShape currentDrawElement;
    private Polygon currentPolygon;

    private List<Vector2> mainPoints = new List<Vector2>();
    private List<float> brushSizes = new List<float>();

    // 注意：如果你原本有 PressureValueSmoother 类，请将 SmoothedValue 替换为你自己的平滑类
    private SmoothedValue pressureSmoother;

    private List<Vector2> currentSegmentPoints = new List<Vector2>();
    private List<float> currentSegmentBrushSizes = new List<float>();

    private float minBrushSize = 1f;
    private float maxBrushSize = 99f;
    private List<DrawableShape> fullLineSegments = new List<DrawableShape>();

    private int lineGroupID = -1;
    private static int lastLineGroupID = 0;

    public bool Finished { get; private set; }
    public int LineGroupID => lineGroupID;

    private float GetDynamicMinPointDistance(float brushSize)
    {
        if (brushSize <= 2f)
        {
            return minPointDistanceMin;
        }
        float num = (brushSize - 2f) / (maxBrushSize - 2f);
        return Mathf.Lerp(minPointDistanceMin, minPointDistanceMax, num);
    }

    private int GetDynamicSegmentSize(float brushSize)
    {
        if (brushSize <= 2f)
        {
            return 4;
        }
        float num = (brushSize - 2f) / (maxBrushSize - 2f);
        return Mathf.RoundToInt(Mathf.Lerp((float)segmentSizeMin, (float)segmentSizeMax, num));
    }

    public void SetBrushSizeRange(float min, float max)
    {
        minBrushSize = min;
        maxBrushSize = max;
    }

    public void SetDrawModule(DrawModule dm)
    {
        drawModule = dm;
        pressureSmoother = new SmoothedValue(5); // 对应原版的 new SmoothedValue(5)
    }

    private void Update()
    {
        if (drawModule == null || Finished)
        {
            return;
        }
        Vector2 vector;
        if (!DrawInput.GetPrimary() || !drawModule.GetWorldCoordsIfOverDrawSurface(out vector))
        {
            return;
        }

        Pen current = Pen.current;
        float num = ((current == null) ? 1f : current.pressure.ReadValue());
        num = Mathf.Clamp(num, 0.001f, 1f);

        pressureSmoother.AddValue((int)(num * 1000f)); // 对应 AddValue
        float num2 = (float)pressureSmoother.SmoothValue(5) / 1000f; // 对应 SmoothValue(5)

        float num3;
        if (num < 0.15f)
        {
            num3 = 0f;
        }
        else
        {
            num3 = (num - 0.15f) / 0.85f;
        }

        float num4 = Mathf.Lerp(minBrushSize, maxBrushSize, num3);
        float dynamicMinPointDistance = GetDynamicMinPointDistance(num4);
        int dynamicSegmentSize = GetDynamicSegmentSize(num4);

        if (mainPoints.Count != 0)
        {
            if (Vector2.Distance(vector, mainPoints[mainPoints.Count - 1]) >= dynamicMinPointDistance)
            {
                mainPoints.Add(vector);
                brushSizes.Add(num4);
                currentSegmentPoints.Add(vector);
                currentSegmentBrushSizes.Add(num4);
                if (currentSegmentPoints.Count >= dynamicSegmentSize)
                {
                    izeCurrentSegment();
                    StartNewSegment();
                    return;
                }
                UpdateCurrentPolygon(false);
            }
            return;
        }

        mainPoints.Add(vector);
        brushSizes.Add(num4);
        currentSegmentPoints.Add(vector);
        currentSegmentBrushSizes.Add(num4);
    }

    private void izeCurrentSegment()
    {
        if (currentSegmentPoints.Count < 2)
        {
            return;
        }
        List<Vector2> list = GeneratePolygonPoints(false);
        if (CheckSelfIntersection(list))
        {
            HandleSelfIntersection();
            return;
        }
        izeCurrentSegmentInternal();
    }

    private void izeCurrentSegmentInternal()
    {
        currentDrawElement.SetLineGroupID(lineGroupID);
        currentPolygon.SetPoints(GeneratePolygonPoints(true));
        Vector2[] array = currentPolygon.points.ToArray();
        currentDrawElement.SetColliderPoints(array);
        drawElements.Add(currentDrawElement);
        drawModule.CheckSpecialMode(currentDrawElement, true);
        drawModule.OnFinishDrawingCurrent(currentDrawElement);
        fullLineSegments.Add(currentDrawElement);
    }

    private List<Vector2> GeneratePolygonPoints(bool isizing)
    {
        List<Vector2> list = new List<Vector2>();
        bool flag;
        if (!(flag = (list.Count == 0 && drawElements.Count > 0)))
        {
            list.Add(currentSegmentPoints[0]);
        }
        for (int i = 1; i < currentSegmentPoints.Count; i++)
        {
            bool flag2;
            if ((flag2 = (i == currentSegmentPoints.Count - 1)) && Finished)
            {
                Vector2 vector = currentSegmentPoints[i - 1];
                Vector2 vector2 = currentSegmentPoints[i];
                Vector2 normalized = (vector2 - vector).normalized;
                Vector2 vector3 = new Vector2(-normalized.y, normalized.x);
                float num = 0.005f;
                Vector2 vector4 = vector2 + vector3 * num;
                Vector2 vector5 = vector2 - vector3 * num;
                list.Add(vector4);
                list.Insert(0, vector5);
            }
            else
            {
                Vector2 vector6 = currentSegmentPoints[i - 1];
                Vector2 vector7 = currentSegmentPoints[i];
                Vector2 vector8 = (flag2 ? vector7 : currentSegmentPoints[i + 1]);
                Vector2 normalized2 = (vector7 - vector6).normalized;
                Vector2 normalized3 = (vector8 - vector7).normalized;
                Vector2 normalized4 = (normalized2 + normalized3).normalized;
                if (normalized4 == Vector2.zero)
                {
                    normalized4 = new Vector2(-normalized2.y, normalized2.x);
                }
                float num2 = currentSegmentBrushSizes[i] * 0.005f;
                Vector2 vector9 = vector7 + new Vector2(-normalized4.y, normalized4.x) * num2;
                Vector2 vector10 = vector7 - new Vector2(-normalized4.y, normalized4.x) * num2;
                if (flag && i == 1)
                {
                    List<Vector2> points = drawElements[drawElements.Count - 1].Polygon.points;
                    list.Add(points[points.Count - 1]);
                    list.Insert(0, points[0]);
                }
                else
                {
                    list.Add(vector9);
                    list.Insert(0, vector10);
                }
            }
        }
        return list;
    }

    private void StartNewSegment()
    {
        Vector2 vector = currentSegmentPoints[currentSegmentPoints.Count - 1];
        float num = currentSegmentBrushSizes[currentSegmentBrushSizes.Count - 1];

        // 注意：原版传入了三个参数，根据你的环境适当调整。如果编译报错请去掉第三个 drawModule
        currentDrawElement = drawModule.CreateNewPressureSegment(vector, num);
        currentPolygon = currentDrawElement.Polygon;

        currentSegmentPoints.Clear();
        currentSegmentBrushSizes.Clear();
        currentSegmentPoints.Add(vector);
        currentSegmentBrushSizes.Add(num);
    }

    public void FinishLine()
    {
        if (mainPoints.Count >= 2)
        {
            Finished = true;
            if (currentSegmentPoints.Count > 1)
            {
                UpdateCurrentPolygon(false);
                izeCurrentSegment();
                fullLineSegments.Add(currentDrawElement);
            }
            List<DrawableElement> list = new List<DrawableElement>();
            foreach (DrawableShape drawableShape in fullLineSegments)
            {
                if (drawableShape != null)
                {
                    list.Add(drawableShape);
                }
            }
            if (list.Count > 1 && fullLineSegments.Count > 0)
            {
                fullLineSegments.RemoveAll((DrawableShape segment) => segment == null);
                if (fullLineSegments.Count > 0)
                {
                    foreach (DrawableShape drawableShape2 in fullLineSegments)
                    {
                        drawableShape2.SetIsPressureSegment(true);
                        drawableShape2.SetPressureLineId(Guid.NewGuid().ToString());
                    }
                    drawModule.UndoSystem.AddEvent(drawModule.CreateUndoMultiDrawElement(fullLineSegments));
                }
            }
            base.enabled = false;
            return;
        }
        if (currentDrawElement != null)
        {
            UnityEngine.Object.Destroy(currentDrawElement.gameObject);
        }
    }

    public void StartNewLine(Vector2 startPoint, DrawableShape drawableShape)
    {
        lineGroupID = ++lastLineGroupID;
        fullLineSegments.Clear();
        fullLineSegments.Add(drawableShape);
        currentDrawElement = drawableShape;
        currentDrawElement.SetLineGroupID(lineGroupID);
        currentPolygon = currentDrawElement.Polygon;
        drawElements.Clear();
        mainPoints.Clear();
        brushSizes.Clear();
        currentSegmentPoints.Clear();
        currentSegmentBrushSizes.Clear();
        Finished = false;
        base.enabled = true;
        mainPoints.Add(startPoint);

        List<float> list = brushSizes;
        float num = 1f;
        float num2 = 99f;
        Pen current = Pen.current;
        list.Add(Mathf.Lerp(num, num2, (current == null) ? 1f : current.pressure.ReadValue()));

        currentSegmentPoints.Add(startPoint);
        currentSegmentBrushSizes.Add(brushSizes[0]);
    }

    private void UpdateCurrentPolygon(bool isizing = false)
    {
        if (currentSegmentPoints.Count < 2)
        {
            return;
        }
        List<Vector2> list = new List<Vector2>();
        bool flag;
        if (!(flag = (list.Count == 0 && drawElements.Count > 0)))
        {
            list.Add(currentSegmentPoints[0]);
        }
        for (int i = 1; i < currentSegmentPoints.Count; i++)
        {
            bool flag2;
            if ((flag2 = (i == currentSegmentPoints.Count - 1)) && Finished)
            {
                Vector2 vector = currentSegmentPoints[i - 1];
                Vector2 vector2 = currentSegmentPoints[i];
                Vector2 normalized = (vector2 - vector).normalized;
                Vector2 vector3 = new Vector2(-normalized.y, normalized.x);
                float num = 0.005f;
                Vector2 vector4 = vector2 + vector3 * num;
                Vector2 vector5 = vector2 - vector3 * num;
                list.Add(vector4);
                list.Insert(0, vector5);
            }
            else
            {
                Vector2 vector6 = currentSegmentPoints[i - 1];
                Vector2 vector7 = currentSegmentPoints[i];
                Vector2 vector8 = (flag2 ? vector7 : currentSegmentPoints[i + 1]);
                Vector2 normalized2 = (vector7 - vector6).normalized;
                Vector2 normalized3 = (vector8 - vector7).normalized;
                Vector2 normalized4 = (normalized2 + normalized3).normalized;
                if (normalized4 == Vector2.zero)
                {
                    normalized4 = new Vector2(-normalized2.y, normalized2.x);
                }
                float num2 = currentSegmentBrushSizes[i] * 0.005f;
                Vector2 vector9 = vector7 + new Vector2(-normalized4.y, normalized4.x) * num2;
                Vector2 vector10 = vector7 - new Vector2(-normalized4.y, normalized4.x) * num2;
                if (flag && i == 1)
                {
                    List<Vector2> points = drawElements[drawElements.Count - 1].Polygon.points;
                    list.Add(points[points.Count - 1]);
                    list.Insert(0, points[0]);
                }
                else
                {
                    list.Add(vector9);
                    list.Insert(0, vector10);
                }
            }
        }
        if (CheckSelfIntersection(list))
        {
            HandleSelfIntersection();
            return;
        }
        currentPolygon.SetPoints(list);
    }

    private void HandleSelfIntersection()
    {
        if (currentSegmentPoints.Count >= 2)
        {
            Vector2 vector = currentSegmentPoints[currentSegmentPoints.Count - 1];
            float num = currentSegmentBrushSizes[currentSegmentBrushSizes.Count - 1];
            izeCurrentSegmentInternal();
            StartNewSegment();
            currentSegmentPoints.Add(vector);
            currentSegmentBrushSizes.Add(num);
            UpdateCurrentPolygon(false);
        }
    }

    private bool CheckSelfIntersection(List<Vector2> points)
    {
        int count = points.Count;
        if (count < 4)
        {
            return false;
        }
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 2; j < count; j++)
            {
                if (Math.Abs(i - j) != 1 && Math.Abs(i - j) != count - 1)
                {
                    Vector2 vector = points[i % count];
                    Vector2 vector2 = points[(i + 1) % count];
                    Vector2 vector3 = points[j % count];
                    Vector2 vector4 = points[(j + 1) % count];
                    if (LineSegmentsIntersect(vector, vector2, vector3, vector4))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool LineSegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float num = Direction(c, d, a);
        float num2 = Direction(c, d, b);
        float num3 = Direction(a, b, c);
        float num4 = Direction(a, b, d);
        return num * num2 < 0f && num3 * num4 < 0f;
    }

    private float Direction(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x);
    }

    private bool IsOnSegment(Vector2 p1, Vector2 p2, Vector2 p)
    {
        return Mathf.Min(p1.x, p2.x) <= p.x && p.x <= Mathf.Max(p1.x, p2.x) && Mathf.Min(p1.y, p2.y) <= p.y && p.y <= Mathf.Max(p1.y, p2.y);
    }
}
#endregion
