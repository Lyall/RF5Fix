using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;


using UnityEngine;
using UnityEngine.UI;


namespace RF5Fix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RF5 : BasePlugin
    {
        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> bUltrawideFixes;
        public static ConfigEntry<bool> bIntroSkip;
        public static ConfigEntry<bool> bIncreaseQuality;
        public static ConfigEntry<float> fUpdateRate;
        public static ConfigEntry<bool> bCustomResolution;
        public static ConfigEntry<float> fDesiredResolutionX;
        public static ConfigEntry<float> fDesiredResolutionY;
        public static ConfigEntry<bool> bFullscreen;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            bUltrawideFixes = Config.Bind("Ultrawide UI Fixes",
                                "UltrawideFixes",
                                true,
                                "Set to true to enable ultrawide UI fixes.");

            fUpdateRate = Config.Bind("Physics Update Rate",
                                "UpdateRate",
                                (float)240,
                                "Set desired update rate. Default = 50. (You can try raising this to improve smoothness.)");

            bIntroSkip = Config.Bind("Skip Logos",
                                "IntroSkip",
                                 true,
                                "Skip intro logos.");

            bIncreaseQuality = Config.Bind("Increase Graphics Quality",
                                "Fullscreen",
                                 true,
                                "Enables/enhances various aspects of the game to improve graphical quality.");

            bCustomResolution = Config.Bind("Set Custom Resolution",
                                "CustomResolution",
                                false, // Disable by default as the launcher resolution list will likely be sufficient.
                                "Enable the usage of a custom resolution.");

            fDesiredResolutionX = Config.Bind("Set Custom Resolution",
                                "ResolutionWidth",
                                (float)Display.main.systemWidth, // Set default to display width so we don't leave an unsupported resolution as default.
                                "Set desired resolution width.");

            fDesiredResolutionY = Config.Bind("Set Custom Resolution",
                                "ResolutionHeight",
                                (float)Display.main.systemHeight, // Set default to display height so we don't leave an unsupported resolution as default.
                                "Set desired resolution height.");

            bFullscreen = Config.Bind("Set Custom Resolution",
                                "Fullscreen",
                                 true,
                                "Set to true for fullscreen or false for windowed.");

            // Set custom resolution
            if (bCustomResolution.Value)
            {
                Screen.SetResolution((int)fDesiredResolutionX.Value, (int)fDesiredResolutionY.Value, bFullscreen.Value);
                Log.LogInfo($"Custom screen resolution set to {fDesiredResolutionX.Value}x{fDesiredResolutionY.Value}. Fullscreen = {bFullscreen.Value}");
            }

            // Run UltrawidePatches
            if (bUltrawideFixes.Value)
            {
                Harmony.CreateAndPatchAll(typeof(UltrawidePatches));
            }

            // Run IntroSkipPatch
            if (bIntroSkip.Value)
            {
                Harmony.CreateAndPatchAll(typeof(IntroSkipPatch));
            }

            // Run IncreaseQualityPatch
            if (bIncreaseQuality.Value)
            {
                Harmony.CreateAndPatchAll(typeof(IncreaseQualityPatch));
            }

            // Unity update rate
            // TODO: Replace this with camera movement interpolation?
            if (fUpdateRate.Value > 50)
            {
                Time.fixedDeltaTime = (float)1 / fUpdateRate.Value;
                Log.LogInfo($"fixedDeltaTime set to {Time.fixedDeltaTime}");
            }

        }
        
        [HarmonyPatch]
        public class UltrawidePatches
        {
            public static float DefaultAspectRatio = (float)16 / 9;
            public static float NewAspectRatio = (float)Screen.width / Screen.height; // This should be correct even with a custom resolution applied.
            public static float AspectMultiplier = NewAspectRatio / DefaultAspectRatio;

            // Set screen match mode when object has canvasscaler enabled
            [HarmonyPatch(typeof(CanvasScaler), nameof(CanvasScaler.OnEnable))]
            [HarmonyPostfix]
            public static void SetScreenMatchMode(CanvasScaler __instance)
            {
                __instance.m_ScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }

            // ViewportRect
            [HarmonyPatch(typeof(ViewportRectController), nameof(ViewportRectController.OnEnable))]
            [HarmonyPatch(typeof(ViewportRectController), nameof(ViewportRectController.ResetRect))]
            [HarmonyPatch(typeof(ViewportRectController), nameof(ViewportRectController.ResetRectAll))]
            [HarmonyPostfix]
            public static void ViewportRectDisable(ViewportRectController __instance)
            {
                __instance.m_Camera.rect = new Rect(0f, 0f, 1f, 1f);
                Log.LogInfo($"Camera viewport rect patched.");
            }

            // Letterbox
            [HarmonyPatch(typeof(LetterBoxController), nameof(LetterBoxController.OnEnable))]
            [HarmonyPostfix]
            public static void LetterboxDisable(LetterBoxController __instance)
            {
                __instance.gameObject.SetActive(false);
                Log.LogInfo($"Disabled letterboxing game object.");

            }

            // Span UI fade to black
            [HarmonyPatch(typeof(UIFadeScreen), nameof(UIFadeScreen.ScreenFade))]
            [HarmonyPostfix]
            public static void UIFadeScreenFix(UIFadeScreen __instance)
            {
                __instance.BlackOutPanel.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                //Log.LogInfo($"UI fade to black spanned.");
            }

            // Span UI load fade
            // Shouldn't really do this on an update method. There should be a better way to do this.
            [HarmonyPatch(typeof(UILoaderFade), nameof(UILoaderFade.Update))]
            [HarmonyPostfix]
            public static void UILoaderFadeFix(UILoaderFade __instance)
            {
                __instance.gameObject.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                // Log.LogInfo($"UI Load fade to black spanned.");
            }

        }

        [HarmonyPatch]
        public class IntroSkipPatch
        {
            // Intro logos skip
            [HarmonyPatch(typeof(UILogoControl), nameof(UILogoControl.Start))]
            [HarmonyPostfix]
            public static void SkipIntroLogos(UILogoControl __instance)
            {
                __instance.m_mode = UILogoControl.MODE.END;
                Log.LogInfo("Skipped intro logos.");
            }

        }

        [HarmonyPatch]
        public class IncreaseQualityPatch
        {
            // Adjust graphical quality
            [HarmonyPatch(typeof(GameMain), nameof(GameMain.FieldLoadStart))]
            [HarmonyPostfix]
            public static void AdjustGraphicalQuality(TitleMenu __instance)
            {
                // Anisotropic Filtering to 16x
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                Texture.SetGlobalAnisotropicFilteringLimits(16, 16);

                // Shadows
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowCascades = 4;

                // LOD Bias
                QualitySettings.lodBias = 4.0f;

                Log.LogInfo("Adjusted graphics settings.");
            }
        }

    }
}
