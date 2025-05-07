using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;

namespace SG
{
    [BepInPlugin("com.hjjs.Layers", "Layers Patch Plugin", "1.1")]
    public class MyMod : BaseUnityPlugin
    {

        private void Awake()
        {
        

            // 初始化 Harmony 并应用所有补丁
            var harmony = new Harmony("com.example.mymod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}