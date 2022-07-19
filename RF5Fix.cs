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
        public static ConfigEntry<bool> bFOVAdjust;
        public static ConfigEntry<float> fFOVAdjust;
        public static ConfigEntry<float> fUpdateRate;
        public static ConfigEntry<bool> bMouseSensitivity;
        public static ConfigEntry<int> iMouseSensitivity;
        public static ConfigEntry<int> iAnisotropicFiltering;
        public static ConfigEntry<int> iShadowResolution;
        public static ConfigEntry<float> fLODBias;
        public static ConfigEntry<int> iShadowCascades;
        public static ConfigEntry<float> fShadowDistance;

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
                                (float)240, // Default = 50
                                new ConfigDescription("Set desired update rate. Default = 50. (You can raise this to improve camera smoothness for example.)",
                                new AcceptableValueRange<float>(1f,9999f)));

            bIntroSkip = Config.Bind("Intro Skip",
                                "IntroSkip",
                                 true,
                                "Skip intro logos.");

            bFOVAdjust = Config.Bind("FOV Adjustment",
                                "FOVAdjustment",
                                false, // Disable by default.
                                "Set to true to enable adjustment of the FOV.");

            fFOVAdjust = Config.Bind("FOV Adjustment",
                                "FOV.Value",
                                (float)50,
                                new ConfigDescription("Set desired FOV.", 
                                new AcceptableValueRange<float>(1f,180f)));

            bMouseSensitivity = Config.Bind("Mouse Sensitivity",
                                "MouseSensitivity.Override",
                                false, // Disable by default.
                                "Set to true to enable mouse sensitivity override.");

            iMouseSensitivity = Config.Bind("Mouse Sensitivity",
                                "MouseSensitivity.Value",
                                (int)100, // Default = 100
                                new ConfigDescription("Set desired mouse sensitivity.",
                                new AcceptableValueRange<int>(1,9999)));

            // Graphical Settings

            iAnisotropicFiltering = Config.Bind("Graphical Tweaks", 
                                "AnisotropicFiltering.Value", 
                                (int)1, 
                                new ConfigDescription("Set Anisotropic Filtering level. 16 is recommended for quality.", 
                                new AcceptableValueRange<int>(1, 16)));

            fLODBias = Config.Bind("Graphical Tweaks",
                                "LODBias.Value",
                                (float)1.5f, // Default = 1.5f
                                new ConfigDescription("Set LOD Bias. Controls distance for level of detail switching. 4 is recommended for quality.",
                                new AcceptableValueRange<float>(0.1f, 10f)));

            iShadowResolution = Config.Bind("Graphical Tweaks",
                                "ShadowResolution.Value",
                                (int)4096, // Default = Very High (4096)
                                new ConfigDescription("Set Shadow Resolution. 4096 is recommended for quality.",
                                new AcceptableValueRange<int>(64, 32768)));

            iShadowCascades = Config.Bind("Graphical Tweaks",
                                "ShadowCascades.Value",
                                (int)1, // Default = 1
                                new ConfigDescription("Set number of Shadow Cascades. 4 is recommended for quality but 2 is decent.",
                                new AcceptableValueList<int>(1, 2, 4)));

            fShadowDistance = Config.Bind("Graphical Tweaks",
                                "ShadowDistance.Value",
                                (float)120f, // Default = 120
                                new ConfigDescription("Set Shadow Distance. Controls distance at which shadows render. 180 is recommended for quality.",
                                new AcceptableValueRange<float>(0.1f, 999f)));

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
                else if (NewAspectRatio > DefaultAspectRatio)
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
                else if (NewAspectRatio > DefaultAspectRatio)
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
                //Log.LogInfo($"PlayerTrackingCamera FOV set to {__instance.m_Setting.minFov}");
            }
        }

        [HarmonyPatch]
        public class MiscellaneousPatch
        {
            // Load game settings
            [HarmonyPatch(typeof(BootSystem), nameof(BootSystem.ApplyOption))]
            [HarmonyPostfix]
            public static void GameSettingsOverride(BootSystem __instance)
            {
                // Anisotropic Filtering
                if (iAnisotropicFiltering.Value > 0)
                {
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    Texture.SetGlobalAnisotropicFilteringLimits(iAnisotropicFiltering.Value, iAnisotropicFiltering.Value);
                    Log.LogInfo($"Anisotropic filtering force enabled. Value = {iAnisotropicFiltering.Value}");
                }

                // Shadow Cascades
                if (iShadowCascades.Value == 4)
                {
                    QualitySettings.shadowCascades = 4; // Default = 1
                    // Need to set ShadowProjection to CloseFit or we get visual glitches at 4 cascades.
                    QualitySettings.shadowProjection = ShadowProjection.CloseFit; // Default = StableFit
                    Log.LogInfo($"Shadow Cascades set to {QualitySettings.shadowCascades}. ShadowProjection = CloseFit");
                }
                else if (iShadowCascades.Value == 2)
                {
                    QualitySettings.shadowCascades = 2; // Default = 1
                    Log.LogInfo($"Shadow Cascades set to {QualitySettings.shadowCascades}");
                }

                // Shadow Distance
                if (fShadowDistance.Value > 0.1f)
                {
                    QualitySettings.shadowDistance = fShadowDistance.Value; // Default = 120f
                    Log.LogInfo($"Shadow Distance set to {QualitySettings.shadowDistance}");
                }

                // LOD Bias
                if (fLODBias.Value >= 0.1f)
                {
                    QualitySettings.lodBias = fLODBias.Value; // Default = 1.5f    
                    Log.LogInfo($"LOD Bias set to {fLODBias.Value}");
                }
                
                // Mouse Sensitivity
                if (bMouseSensitivity.Value)
                {
                    BootSystem.m_Option.MouseSensitivity = iMouseSensitivity.Value;
                    Log.LogInfo($"Mouse sensitivity override. Value = {BootSystem.m_Option.MouseSensitivity}");
                }
            }

            // Sun & Moon | Shadow Resolution
            [HarmonyPatch(typeof(Funly.SkyStudio.OrbitingBody), nameof(Funly.SkyStudio.OrbitingBody.LayoutOribit))]
            [HarmonyPostfix]
            public static void AdjustSunMoonLight(Funly.SkyStudio.OrbitingBody __instance)
            {
                if (iShadowResolution.Value >= 64)
                {
                    __instance.BodyLight.shadowCustomResolution = iShadowResolution.Value; // Default = ShadowQuality (i.e VeryHigh = 4096)
                    Log.LogInfo($"Set light.shadowCustomResolution to {__instance.BodyLight.shadowCustomResolution}.");
                } 
            }

            // RealtimeBakeLight | Shadow Resolution
            [HarmonyPatch(typeof(RealtimeBakeLight), nameof(RealtimeBakeLight.Start))]
            [HarmonyPostfix]
            public static void AdjustLightShadow(RealtimeBakeLight __instance)
            {
                if (iShadowResolution.Value >= 64)
                {
                    __instance.Light.shadowCustomResolution = iShadowResolution.Value; // Default = ShadowQuality (i.e VeryHigh = 4096)
                    Log.LogInfo($"Set RealtimeBakeLight.light.shadowCustomResolution to {__instance.Light.shadowCustomResolution}.");
                }
            }
        }
    }
}
