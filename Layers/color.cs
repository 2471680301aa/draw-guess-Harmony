using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace SG
{

    public class IngameDrawModuleExtra : MonoBehaviour
    {
        // 取色列表和上次取色时鼠标的位置
        private List<Color> picks;
        private Vector3 lastPickPos;
        private Coroutine pickerCoroutine;

        void Awake()
        {
            picks = new List<Color>();
            lastPickPos = new Vector3(-1f, -1f, -1f);
        }


        private bool showLayerOptions = true;
        void OnGUI()
        {
            IngameDrawModule module = GetComponent<IngameDrawModule>();
            if (module == null) return;
            if (!module.CanDraw()) return;
            if (module.SortOrder == null) return;

          

            // 数字键盘检测逻辑（使用新版输入系统）
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    bool keyPressed = false;
                    switch (i)
                    {
                        case 0: keyPressed = kb.digit1Key.wasPressedThisFrame; break;
                        case 1: keyPressed = kb.digit2Key.wasPressedThisFrame; break;
                        case 2: keyPressed = kb.digit3Key.wasPressedThisFrame; break;
                        case 3: keyPressed = kb.digit4Key.wasPressedThisFrame; break;
                    }
                    if (keyPressed)
                    {
                        ((BasicSortOrder)module.SortOrder).ToLayer(i);
                    }
                }
            }

            // 绘制右侧图层切换按钮
            GUIStyle guistyle = new GUIStyle { alignment = TextAnchor.MiddleRight };
            for (int j = 0; j < 4; j++)
            {
                float scale = (float)Screen.width / 1920f;
                guistyle.fontSize = (int)(30f * scale);
                guistyle.normal.textColor = Color.gray;
                if (((BasicSortOrder)module.SortOrder).GetLayer() == j)
                {
                    guistyle.fontSize = (int)(40f * scale);
                    guistyle.normal.textColor = Color.black;
                }
                if (GUI.Toggle(new Rect(scale * 1680f, scale * (20f + 70f * j), 70f * scale, 80f * scale),
                               ((BasicSortOrder)module.SortOrder).GetLayer() == j,
                               "图层" + (j + 1).ToString(), guistyle))
                {
                    ((BasicSortOrder)module.SortOrder).ToLayer(j);
                }
            }
        }


        public IEnumerator UpdatePicker()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                // 确保 ChainDGPlayer 和 DrawModule 已经初始化
                if (ChainDGPlayer.ChainLocalPlayer == null || ChainDGPlayer.ChainLocalPlayer.DrawModule == null)
                    continue;
                ChainDrawModule drawModule = ChainDGPlayer.ChainLocalPlayer.DrawModule;

                // 鼠标右键刚按下时，清空取色列表
                if (Input.GetMouseButtonDown(1))
                {
                    picks = new List<Color>();
                    lastPickPos = new Vector3(-1f, -1f, -1f);
                }
                // 鼠标右键持续按下时，采集当前像素颜色
                if (Input.GetMouseButton(1))
                {
                    Vector3 currentMousePos = Input.mousePosition;
                    if (lastPickPos != currentMousePos)
                    {
                        // 注意：Input.mousePosition 坐标原点在屏幕左下角，与 ReadPixels 坐标一致
                        Rect rect = new Rect(currentMousePos.x, currentMousePos.y, 1f, 1f);
                        Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGB24, false);
                        texture2D.ReadPixels(rect, 0, 0);
                        texture2D.Apply();
                        Color pixel = texture2D.GetPixel(0, 0);
                        Destroy(texture2D);  // 释放创建的贴图

                        // 如果列表中没有该颜色，则添加
                        bool exists = false;
                        foreach (Color c in picks)
                        {
                            if (c.Equals(pixel))
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            picks.Add(pixel);
                        }

                        // 计算取色列表的平均颜色
                        Color avgColor = Color.black;
                        foreach (Color c in picks)
                        {
                            avgColor += c / picks.Count;
                        }
                        drawModule.SetRGBColor(avgColor, true, false, false, true);
                    }
                    lastPickPos = currentMousePos;
                }
                // 鼠标右键松开时，再次设置平均颜色
                if (Input.GetMouseButtonUp(1))
                {
                    Color avgColor = Color.black;
                    foreach (Color c in picks)
                    {
                        avgColor += c / picks.Count;
                    }
                    drawModule.SetRGBColor(avgColor, true, false, false, true);
                }
            }
        }

        // OnEnable 中启动一次协程即可，不需要递归调用
        private void OnEnable()
        {
            if (pickerCoroutine != null)
            {
                StopCoroutine(pickerCoroutine);
            }
            pickerCoroutine = StartCoroutine(UpdatePicker());
        }

        private void OnDisable()
        {
            if (pickerCoroutine != null)
            {
                StopCoroutine(pickerCoroutine);
                pickerCoroutine = null;
            }
        }

        // Harmony 补丁，确保在 IngameDrawModule OnEnable 时附加本组件
        [HarmonyPatch(typeof(IngameDrawModule), "OnEnable")]
        public static class IngameDrawModule_OnEnable_Patch
        {
            static void Postfix(IngameDrawModule __instance)
            {
                if (__instance.gameObject.GetComponent<IngameDrawModuleExtra>() == null)
                {
                    __instance.gameObject.AddComponent<IngameDrawModuleExtra>();
                }
            }
        }

        [HarmonyPatch(typeof(DrawModule), "get_Prepared")]
        public static class DrawModule_GetPrepared_Patch
        {
            static void Postfix(ref bool __result)
            {
                __result = true;
            }
        }
    }
}
