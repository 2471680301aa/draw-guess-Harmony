using HarmonyLib;
using UnityEngine.InputSystem;
namespace SG
{
    [HarmonyPatch(typeof(PaletteManager), "TryGetPaletteForKeyboardShortcut")]
    public static class PaletteManager_TryGetPaletteForKeyboardShortcut_Prefix
    {
        // 返回 false 表示跳过原方法
        static bool Prefix(out int index)
        {
            // 使用 45 作为起始值
            for (int i = 45; i < 50; i++)
            {
                if (Keyboard.current[(Key)i].wasPressedThisFrame)
                {
                    index = i - 45;
                    return false; // 跳过原方法执行
                }
            }
            index = -1;
            return false; // 完全跳过原方法
        }
    }

}
