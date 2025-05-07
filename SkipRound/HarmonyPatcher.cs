using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using BepInEx;
using System.Reflection;

[BepInPlugin("com.example.SyncRound", "SyncRound Patch Plugin", "1.1")]
public class SyncRound : BaseUnityPlugin
{
    // 在游戏启动时应用补丁
    private void Awake()
    {
        var harmony = new Harmony("com.example.SyncRound"); 
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(SyncRoundManager))]
[HarmonyPatch("Update")]
public class SyncRoundManagerPatch
{
    // Postfix 在目标方法执行后被调用
    public static void Postfix(SyncRoundManager __instance)
    {
        if (__instance.SyncController.IsHost() &&
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed &&
            Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            __instance.NextTurn();
            MethodInfo methodInfo = typeof(SyncRoundManager).GetMethod("UpdatePossibleScoresInState", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null)
            {
                methodInfo.Invoke(__instance, null);
            }
        }
    }
}