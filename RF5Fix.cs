using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using System;
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
        public static ConfigEntry<bool> bLetterboxing;
        public static ConfigEntry<bool> bIncreaseQuality;
        public static ConfigEntry<bool> bFOVAdjust;
        public static ConfigEntry<float> fFOVAdjust;
        public static ConfigEntry<float> fUpdateRate;
        public static ConfigEntry<bool> bMouseSensitivity;
        public static ConfigEntry<int> iMouseSensitivity;
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

            bLetterboxing = Config.Bind("Ultrawide UI Fixes",
                                "Letterboxing",
                                 true,
                                "Letterboxes UI (not gameplay). Set to false to disable letterboxing everywhere.");

            fUpdateRate = Config.Bind("Physics Update Rate",
                                "PhysicsUpdateRate",
                                (float)240,
                                "Set desired update rate. Default = 50. (You can raise this to improve camera smoothness for example.)");

            bIntroSkip = Config.Bind("Intro Skip",
                                "IntroSkip",
                                 true,
                                "Skip intro logos.");

            bIncreaseQuality = Config.Bind("Increase Graphical Quality",
                                "IncreaseGraphicalQuality",
                                 false, // Disable by default, performance impact is significant on lower end hardware.
                                "Enables/enhances various aspects of the game to improve graphical quality.");

            bFOVAdjust = Config.Bind("FOV Adjustment",
                                "FOVAdjustment",
                                false, // Disable by default.
                                "Set to true to enable adjustment of the FOV.");

            fFOVAdjust = Config.Bind("FOV Adjustment",
                                "FOV.Value",
                                (float)50,
                                "Set desired FOV.");

            bMouseSensitivity = Config.Bind("Mouse Sensitivity",
                                "MouseSensitivity.Override",
                                false, // Disable by default.
                                "Set to true to enable mouse sensitivity override.");

            iMouseSensitivity = Config.Bind("Mouse Sensitivity",
                                "MouseSensitivity.Value",
                                (int)100,
                                "Set desired mouse sensitivity.");

            // Run UltrawidePatches
            if (bUltrawideFixes.Value)
            {
                Harmony.CreateAndPatchAll(typeof(UltrawidePatches));
            }

            // Run LetterboxingPatch
            if (bLetterboxing.Value)
            {
                Harmony.CreateAndPatchAll(typeof(LetterboxingPatch));
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

            // Run FOVPatch
            if (bFOVAdjust.Value)
            {
                Harmony.CreateAndPatchAll(typeof(FOVPatch));
            }

            // Run MiscellaneousPatch
            Harmony.CreateAndPatchAll(typeof(MiscellaneousPatch));

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
            public static float NewAspectRatio = (float)Screen.width / Screen.height;
            public static float AspectMultiplier = NewAspectRatio / DefaultAspectRatio;
            public static float AspectDivider = DefaultAspectRatio / NewAspectRatio;

            // Set screen match mode when object has canvasscaler enabled
            [HarmonyPatch(typeof(CanvasScaler), nameof(CanvasScaler.OnEnable))]
            [HarmonyPostfix]
            public static void SetScreenMatchMode(CanvasScaler __instance)
            {
                if (NewAspectRatio > DefaultAspectRatio || NewAspectRatio < DefaultAspectRatio)
                {
                    __instance.m_ScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                }
            }

            // ViewportRect
            [HarmonyPatch(typeof(ViewportRectController), nameof(ViewportRectController.OnEnable))]
            [HarmonyPatch(typeof(ViewportRectController), nameof(ViewportRectController.ResetRect))]
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
                if (bLetterboxing.Value)
                {
                    // Do nothing if UI letterboxing is enabled
                }
                else
                {
                    // If letterboxing is disabled
                    __instance.transform.parent.gameObject.SetActive(false);
                    Log.LogInfo("Letterboxing disabled. For good.");
                }
            }

            // Span UI fade to black
            [HarmonyPatch(typeof(UIFadeScreen), nameof(UIFadeScreen.ScreenFade))]
            [HarmonyPostfix]
            public static void UIFadeScreenFix(UIFadeScreen __instance)
            {
                if (NewAspectRatio < DefaultAspectRatio)
                {
                    // Increase height to scale correctly
                    __instance.BlackOutPanel.transform.localScale = new Vector3(1f, 1 * AspectDivider, 1f);
                }
                else
                {
                    // Increase width to scale correctly
                    __instance.BlackOutPanel.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                }
            }

            // Span UI load fade
            // Can't find a better way to hook this. It shouldn't impact performance much and even if it does it's only during UI loading fades.
            [HarmonyPatch(typeof(UILoaderFade), nameof(UILoaderFade.Update))]
            [HarmonyPostfix]
            public static void UILoaderFadeFix(UILoaderFade __instance)
            {
                if (NewAspectRatio < DefaultAspectRatio)
                {
                    // Increase height to scale correctly
                    __instance.gameObject.transform.localScale = new Vector3(1f, 1 * AspectDivider, 1f);
                }
                else
                {
                    // Increase width to scale correctly
                    __instance.gameObject.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                } 
            }

        }

        [HarmonyPatch]
        public class LetterboxingPatch
        {
            public static GameObject letterboxing;

            // Letterbox
            [HarmonyPatch(typeof(LetterBoxController), nameof(LetterBoxController.OnEnable))]
            [HarmonyPostfix]
            public static void LetterboxAssign(LetterBoxController __instance)
            {
                letterboxing = __instance.transform.parent.gameObject;
            }

            // Enable Letterboxing
            [HarmonyPatch(typeof(UICalendarMenu), nameof(UICalendarMenu.Start))] // Calendar UI
            [HarmonyPatch(typeof(UINamingWindow), nameof(UINamingWindow.Start))] // Naming window
            [HarmonyPatch(typeof(CampMenuMain), nameof(CampMenuMain.StartCamp))] // Camp menu
            [HarmonyPatch(typeof(MovieRoom), nameof(MovieRoom.Start))] // Movie gallery
            [HarmonyPatch(typeof(MapControl), nameof(MapControl.Start))] // Map
            [HarmonyPostfix]
            public static void EnableLetterboxing()
            {
                letterboxing.SetActive(true);
                Log.LogInfo("Enabled UI letterboxing.");
            }

            // Disable Letterboxing
            [HarmonyPatch(typeof(GameMain), nameof(GameMain.FieldLoadStart))] // Load game
            [HarmonyPatch(typeof(UICalendarMenu), nameof(UICalendarMenu.OnDestroy))] // Calendar UI
            [HarmonyPatch(typeof(UINamingWindow), nameof(UINamingWindow.OnDestroy))] // Naming window
            [HarmonyPatch(typeof(CampMenuMain), nameof(CampMenuMain.CloseCamp))] // Camp menu
            [HarmonyPatch(typeof(MovieRoom), nameof(MovieRoom.Close))] // Movie gallery
            [HarmonyPatch(typeof(MapControl), nameof(MapControl.OnDestroy))] // Map
            [HarmonyPostfix]
            public static void DisableLetterboxing()
            {
                letterboxing.SetActive(false);
                Log.LogInfo("Disabled UI letterboxing.");
            }
        }

        [HarmonyPatch]
        public class IntroSkipPatch
        {
            public static bool IsInAutoSaveInfo;

            // TitleMenu Skip
            // Should be okay using the update method as it's only in the title menu and shouldn't tank performance.
            [HarmonyPatch(typeof(TitleMenu), nameof(TitleMenu.Update))]
            [HarmonyPostfix]
            public static void GetTitleState(TitleMenu __instance)
            {
                if (__instance.m_mode == TitleMenu.MODE.INIT_OP)
                {
                    __instance.m_mode = TitleMenu.MODE.INIT_END_OP;
                    Log.LogInfo($"Title state = {__instance.m_mode}. Skipping.");
                }

                if (__instance.m_mode == TitleMenu.MODE.SHOW_SYSTEM_INFOAUTOSAVE)
                {
                    Log.LogInfo($"Title state = {__instance.m_mode}. Skipping.");
                    __instance.m_mode = TitleMenu.MODE.END_SYSTEM;
                }
            }

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
            // Sun & Moon
            [HarmonyPatch(typeof(Funly.SkyStudio.OrbitingBody), nameof(Funly.SkyStudio.OrbitingBody.LayoutOribit))]
            [HarmonyPostfix]
            public static void AdjustSunMoonLight(Funly.SkyStudio.OrbitingBody __instance)
            {
                __instance.BodyLight.shadowCustomResolution = 8192; // Default = ShadowQuality (i.e VeryHigh = 4096)
                Log.LogInfo($"Set light.shadowCustomResolution to {__instance.BodyLight.shadowCustomResolution}.");
            }

            // RealtimeBakeLight
            [HarmonyPatch(typeof(RealtimeBakeLight), nameof(RealtimeBakeLight.Start))]
            [HarmonyPostfix]
            public static void AdjustLightShadow(RealtimeBakeLight __instance)
            {
                __instance.Light.shadowCustomResolution = 8192; // Default = ShadowQuality (i.e VeryHigh = 4096)
                Log.LogInfo($"Set RealtimeBakeLight.light.shadowCustomResolution to {__instance.Light.shadowCustomResolution}.");
            }
        }

        [HarmonyPatch]
        public class FOVPatch
        {
            // Player Tracking Camera FOV
            //[HarmonyPatch(typeof(PlayerTrackingCamera), nameof(PlayerTrackingCamera.GetSetting), new Type[] { typeof(Define.TrackinCameraType) })]
            [HarmonyPatch(typeof(PlayerTrackingCamera), nameof(PlayerTrackingCamera.GetSetting), new Type[] { })]
            [HarmonyPostfix]
            public static void ChangePlayerTrackingFOV(PlayerTrackingCamera __instance)
            {
                float FOV = fFOVAdjust.Value;
                __instance.m_Setting.minFov = Mathf.Clamp(FOV, 1f, 180f);
                //Log.LogInfo($"PlayerTrackingCamera FOV set to 50 + {fFOVAdjust.Value} = {__instance.m_Setting.minFov}");
            }
        }

        [HarmonyPatch]
        public class MiscellaneousPatch
        {
            // Load game settings
            [HarmonyPatch(typeof(BootOptionSave), nameof(BootOptionSave.LoadOption))]
            [HarmonyPostfix]
            public static void GameSettingsOverride(ref BootOptionData option)
            {
                // Graphical adjustments
                if (bIncreaseQuality.Value)
                {
                    // Anisotropic Filtering to 16x
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    Texture.SetGlobalAnisotropicFilteringLimits(16, 16);

                    // Shadows
                    // These are too glitchy right now. Probably need to tweak cascade splitting distance
                    //QualitySettings.shadowCascades = 4; // Default = 2
                    //QualitySettings.shadowDistance = 120f; // Default = 120f

                    // LOD Bias
                    QualitySettings.lodBias = 4.0f; // Default = 1.5f

                    Log.LogInfo("Adjusted graphics settings.");
                }

                // Adjust mouse sensitivity
                if (bMouseSensitivity.Value)
                {
                    option.MouseSensitivity = iMouseSensitivity.Value;
                    Log.LogInfo($"Mouse sensitivity override. Value = {option.MouseSensitivity}");
                }
            }
        }
    }
}
