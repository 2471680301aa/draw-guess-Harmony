using System.Runtime.CompilerServices;

// 添加这个类来管理压感线段标记
public static class DrawableShapeExtensions
{
    // 使用ConditionalWeakTable来存储压感线段标记，避免内存泄漏
    private static readonly ConditionalWeakTable<DrawableShape, PressureSegmentData> _pressureData =
        new ConditionalWeakTable<DrawableShape, PressureSegmentData>();

    private class PressureSegmentData
    {
        public bool IsPressureSegment { get; set; }
    }

    /// <summary>
    /// 检查是否是压感线段
    /// </summary>
    public static bool GetIsPressureSegment(this DrawableShape shape)
    {
        if (shape == null) return false;

        if (_pressureData.TryGetValue(shape, out var data))
        {
            return data.IsPressureSegment;
        }

        return false;
    }

    /// <summary>
    /// 设置是否是压感线段
    /// </summary>
    public static void SetIsPressureSegment(this DrawableShape shape, bool isPressureSegment)
    {
        if (shape == null) return;

        var data = _pressureData.GetOrCreateValue(shape);
        data.IsPressureSegment = isPressureSegment;
    }

    /// <summary>
    /// 清理压感线段数据（当形状被销毁时调用）
    /// </summary>
    public static void ClearPressureSegmentData(this DrawableShape shape)
    {
        if (shape == null) return;

        _pressureData.Remove(shape);
    }
}
