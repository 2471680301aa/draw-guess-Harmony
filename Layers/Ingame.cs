using HarmonyLib;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace SG
{

    public class BasicSortOrderData
    {
        public int[] Layers { get; set; } = new int[4]; // 支持4个图层
        public int CurrentLayerIndex { get; set; } = 1; // 默认当前图层为第1层

        public BasicSortOrderData()
        {
            // 初始化每个层的基数
            Layers[0] = -4000;
            Layers[1] = -9000;
            Layers[2] = -14000;
            Layers[3] = -19000;
        }
    }

    public static class BasicSortOrderHelper
    {
        private static readonly ConditionalWeakTable<BasicSortOrder, BasicSortOrderData> DataStore = new();

        public static BasicSortOrderData GetData(this BasicSortOrder sortOrder)
        {
            return DataStore.GetOrCreateValue(sortOrder);
        }
    }

    public static class BasicSortOrderExtensions
    {
        public static void ToLayer(this BasicSortOrder sortOrder, int index)
        {
            var data = sortOrder.GetData();
            if (index >= 0 && index < data.Layers.Length)
                data.CurrentLayerIndex = index;
            else
                data.CurrentLayerIndex = 0;
        }

        public static int GetLayer(this BasicSortOrder sortOrder)
        {
            var data = sortOrder.GetData();
            return data.CurrentLayerIndex;
        }

        public static int[] GetLayers(this BasicSortOrder sortOrder)
        {
            var data = sortOrder.GetData();
            return (int[])data.Layers.Clone();
        }
    }

    [HarmonyPatch(typeof(BasicSortOrder), MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(int) })]
    public static class BasicSortOrder_Constructor_Patch
    {
        public static void Postfix(BasicSortOrder __instance)
        {
            var data = __instance.GetData();
            __instance.ResetStrokeCount();
        }
    }

    
    [HarmonyPatch(typeof(BasicSortOrder), "get_StrokeCount")]
    public static class BasicSortOrder_StrokeCount_Getter_Patch
    {
        public static bool Prefix(BasicSortOrder __instance, ref int __result)
        {
            var data = __instance.GetData();
            __result = data.Layers[data.CurrentLayerIndex];
            return false; // 阻止原始 getter 执行
        }
    }

    [HarmonyPatch(typeof(BasicSortOrder), "IncreaseStrokeCount")]
    public static class BasicSortOrder_IncreaseStrokeCount_Patch
    {
        public static bool Prefix(BasicSortOrder __instance)
        {
            var data = __instance.GetData();
            data.Layers[data.CurrentLayerIndex]++;
            return false; // 阻止原始方法执行
        }
    }

    [HarmonyPatch(typeof(BasicSortOrder), "SetStrokeCount")]
    public static class BasicSortOrder_SetStrokeCount_Patch
    {
        public static bool Prefix(BasicSortOrder __instance, int changeValue)
        {
            var data = __instance.GetData();
            data.Layers[data.CurrentLayerIndex] = changeValue;
            return false; // 阻止原始方法执行
        }
    }

    [HarmonyPatch(typeof(BasicSortOrder), "ResetStrokeCount")]
    public static class BasicSortOrder_ResetStrokeCount_Patch
    {
        public static bool Prefix(BasicSortOrder __instance)
        {
            var data = __instance.GetData();
            data.Layers[0] = -4000;
            data.Layers[1] = -9000;
            data.Layers[2] = -14000;
            data.Layers[3] = -19000;
            data.CurrentLayerIndex = 1;
            return false; // 阻止原始方法执行
        }
    }

    [HarmonyPatch(typeof(BasicSortOrder), "GetNextSortOrder")]
    public static class BasicSortOrder_GetNextSortOrder_Patch
    {
        public static bool Prefix(BasicSortOrder __instance, bool isFill, ref int __result)
        {
            var data = __instance.GetData();
            __result = data.Layers[data.CurrentLayerIndex] - (isFill ? 1000 : 0);
            return false; // 阻止原始方法执行
        }
    }

    [HarmonyPatch(typeof(IngameDrawModule), "Awake")]
    public static class IngameDrawModulePatches
    {
        [HarmonyPostfix]
        public static void Awake_Postfix(IngameDrawModule __instance)
        {
            if (__instance.GetComponent<IngameDrawModuleExtra>() == null)
            {
                __instance.gameObject.AddComponent<IngameDrawModuleExtra>();
            }

            if (__instance.GetComponent<OnGUIModule>() == null)
            {
                __instance.gameObject.AddComponent<OnGUIModule>();
            }
        }
    }


    public class OnGUIModule : MonoBehaviour
    {
        private IngameDrawModule targetModule;
        private BasicSortOrder basicSortOrder;
        private GUIStyle guiStyle;

        private void Awake()
        {
            // 获取目标 IngameDrawModule
            targetModule = GetComponent<IngameDrawModule>();
            if (targetModule == null)
            {
                Debug.LogError("IngameDrawModule not found on the GameObject.");
                return;
            }

            // 获取 BasicSortOrder
            basicSortOrder = targetModule.SortOrder as BasicSortOrder;
            if (basicSortOrder == null)
            {
                Debug.LogError("BasicSortOrder is null or not of type BasicSortOrder.");
                return;
            }

            // 初始化 GUI 样式
            guiStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };
        }


        private void DrawLayerSwitchGUI()
        {
            float scaleFactor = (float)Screen.width / 1920f;

            for (int layerIndex = 0; layerIndex < 4; layerIndex++)
            {
                // 获取当前图层
                int currentLayer = basicSortOrder.GetLayer();

                // 设置字体大小和颜色
                guiStyle.fontSize = (layerIndex == currentLayer) ? (int)(40f * scaleFactor) : (int)(30f * scaleFactor);
                guiStyle.normal.textColor = (layerIndex == currentLayer) ? Color.black : Color.gray;

                // 绘制图层切换按钮
                Rect buttonRect = new Rect(
                    scaleFactor * 1680f,
                    scaleFactor * (20f + 70f * layerIndex),
                    200f * scaleFactor,
                    50f * scaleFactor
                );

                bool isSelected = currentLayer == layerIndex;
            }
        }
    }

    [HarmonyPatch(typeof(BattleRoyalDrawModule), "Update", MethodType.Normal)]
    public static class BattleRoyalDrawModulePatch
    {
        // Postfix 补丁：在目标方法执行后运行
        [HarmonyPostfix]
        public static void Update_Postfix(BattleRoyalDrawModule __instance)
        {
            AdjustStrokeCountForBattleRoyal(__instance);
        }

        // 添加私有方法 AdjustStrokeCountForBattleRoyal 的逻辑
        private static void AdjustStrokeCountForBattleRoyal(BattleRoyalDrawModule instance)
        {
            if (instance.SortOrder is BasicSortOrder basicSortOrder)
            {
                var data = basicSortOrder.GetData();

                for (int i = 0; i < data.Layers.Length; i++)
                {
                    if (data.Layers[i] < 0)
                    {
                        data.Layers[i] += 22000;
                    }
                }
            }
        }
    }

    public static class HarmonyPatchInitializer
    {
        public static void ApplyPatches()
        {
            var harmony = new Harmony("com.hjjs.Layers"); // 使用唯一的ID
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    public class PatchInitializer : MonoBehaviour
    {
        void Awake()
        {
            HarmonyPatchInitializer.ApplyPatches();
        }
    }

    public class SortOrderController : MonoBehaviour
    {
        public BasicSortOrder sortOrder;

        void Start()
        {
            // 初始化 BasicSortOrder 实例
            sortOrder = new BasicSortOrder(0);

            // 切换到第2层（索引1）
            SwitchToLayer(sortOrder, 1);

            // 获取当前图层索引
            int currentLayer = GetCurrentLayer(sortOrder);
            Debug.Log($"Current Layer: {currentLayer}");

            // 获取所有图层的排序值
            int[] allLayers = GetAllLayers(sortOrder);
            Debug.Log("All Layers:");
            for (int i = 0; i < allLayers.Length; i++)
            {
                Debug.Log($"Layer {i + 1}: {allLayers[i]}");
            }

            // 获取下一个 SortOrder
            int nextSortOrder = GetNextSortOrder(sortOrder, false);
            Debug.Log($"Next Sort Order: {nextSortOrder}");
        }

        public void SwitchToLayer(BasicSortOrder sortOrder, int layerIndex)
        {
            sortOrder.ToLayer(layerIndex);
            Debug.Log($"Switched to Layer {layerIndex + 1}");
        }

        public int GetCurrentLayer(BasicSortOrder sortOrder)
        {
            return sortOrder.GetLayer();
        }

        public int[] GetAllLayers(BasicSortOrder sortOrder)
        {
            return sortOrder.GetLayers();
        }


        public int GetNextSortOrder(BasicSortOrder sortOrder, bool isFill)
        {
            return sortOrder.GetNextSortOrder(isFill);
        }
    }
}
