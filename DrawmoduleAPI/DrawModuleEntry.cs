using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace DrawModuleLib
{
    [BepInPlugin("yourname.drawmodulelib", MOD_NAME, "1.0.0")]
    internal sealed class DrawModuleEntry : BaseUnityPlugin
    {
        private const string MOD_NAME = "Draw Module Lib";

        internal static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);

        #region 核心钩子

        // DrawModule 启动钩子
        private static void DrawModule_StartHook(Action<DrawModule> orig, DrawModule self)
        {
            orig.Invoke(self);

            // 初始化模板
            DrawModuleTemplates.InitializeTemplates();

            // 触发准备就绪事件
            DrawModuleAPI.TriggerDrawModuleReady(self);
        }

        #endregion

        private void Awake()
        {
            try
            {
                logger.LogDebug("Hooking `DrawModule.Start`");
                new Hook(AccessTools.Method(typeof(DrawModule), "Start"),
                    new Action<Action<DrawModule>, DrawModule>(DrawModule_StartHook));

                logger.LogInfo("DrawModuleLib hooks applied successfully");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to apply hooks: {ex}");
            }
        }
    }
}
