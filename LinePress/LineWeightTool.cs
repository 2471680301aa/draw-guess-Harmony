using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 拦截 Mathf.Clamp 调用来修改笔刷大小限制
[HarmonyPatch(typeof(Mathf), "Clamp", new System.Type[] { typeof(int), typeof(int), typeof(int) })]
public class Mathf_Clamp_Patch
{
    static bool Prefix(ref int __result, int value, int min, int max)
    {
        // 检查是否是笔刷大小的限制
        if ((min == 2 && max == 35) || (min == 2 && max == 28))
        {
            __result = Mathf.Clamp(value, 1, 199);
            return false;
        }

        return true;
    }
}

// UI 补丁：修改滑块范围
[HarmonyPatch(typeof(LineWeightTool), "Start")]
public class LineWeightTool_Start_Patch
{
    static void Postfix(LineWeightTool __instance)
    {
        var slider = AccessTools.Field(typeof(LineWeightTool), "Slider").GetValue(__instance) as Slider;
        if (slider != null)
        {
            slider.minValue = 1f;
            slider.maxValue = 199f;

            float currentValue = slider.value;
            slider.value = Mathf.Clamp(currentValue, 1f, 199f);

            __instance.OnSliderValueChanged();
        }
    }
}

// 修改滑块值变化处理
[HarmonyPatch(typeof(LineWeightTool), "OnSliderValueChanged")]
public class LineWeightTool_OnSliderValueChanged_Patch
{
    static bool Prefix(LineWeightTool __instance)
    {
        var slider = AccessTools.Field(typeof(LineWeightTool), "Slider").GetValue(__instance) as Slider;
        var scaleValueText = AccessTools.Field(typeof(LineWeightTool), "ScaleValue").GetValue(__instance) as TextMeshProUGUI;
        var scaleTargets = AccessTools.Field(typeof(LineWeightTool), "ScaleTargets").GetValue(__instance) as Transform[];
        var minScale = (float)AccessTools.Field(typeof(LineWeightTool), "MinScale").GetValue(__instance);
        var maxScale = (float)AccessTools.Field(typeof(LineWeightTool), "MaxScale").GetValue(__instance);
        var drawingToolHub = AccessTools.Field(typeof(LineWeightTool), "DrawingToolHub").GetValue(__instance) as DrawingToolHub;

        // 更新显示文本
        scaleValueText.text = slider.value.ToString();

        // 使用完整范围进行缩放
        float normalizedValue = (slider.value - 1f) / (199f - 1f);
        float scale = Mathf.Lerp(minScale, maxScale, normalizedValue);

        // 对所有目标应用缩放
        if (scaleTargets != null)
        {
            foreach (var target in scaleTargets)
            {
                if (target != null)
                {
                    target.localScale = new Vector3(scale, scale, scale);
                }
            }
        }

        // 设置笔刷大小
        DrawModule.BrushSize.Value = (int)slider.value;
        drawingToolHub.ForwardLineWeight((byte)Mathf.Clamp(slider.value, 1, 199));

        return false;
    }
}

// 确保 DrawingToolHub.ForwardLineWeight 不被重新限制
[HarmonyPatch(typeof(DrawingToolHub), "ForwardLineWeight")]
public class DrawingToolHub_ForwardLineWeight_Patch
{
    static bool Prefix(byte size)
    {
        int clampedSize = Mathf.Clamp((int)size, 1, 199);
        DrawModule.BrushSize.Value = clampedSize;
        return false;
    }
}
