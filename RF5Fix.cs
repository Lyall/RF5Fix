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
        public static ConfigEntry<bool> bCampRenderTextureFix;
        public static ConfigEntry<bool> bFOVAdjust;
        public static ConfigEntry<float> fAdditionalFOV;
        public static ConfigEntry<float> fUpdateRate;
        public static ConfigEntry<bool> bMouseSensitivity;
        public static ConfigEntry<int> iMouseSensitivity;
        public static ConfigEntry<int> iAnisotropicFiltering;
        public static ConfigEntry<int> iShadowResolution;
        public static ConfigEntry<float> fLODBias;
        public static ConfigEntry<float> fNPCDistance;
        public static ConfigEntry<int> iShadowCascades;
        public static ConfigEntry<float> fShadowDistance;
        public static ConfigEntry<bool> bCustomResolution;
        public static ConfigEntry<float> fDesiredResolutionX;
        public static ConfigEntry<float> fDesiredResolutionY;
        public static ConfigEntry<int> iWindowMode;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // Features
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
                                (float)0f, // 0 = Auto (Set to refresh rate) || Default = 50
                                new ConfigDescription("Set desired update rate. This will improve camera smoothness in particular. \n0 = Auto (Set to refresh rate). Game default = 50",
                                new AcceptableValueRange<float>(0f,5000f)));

            bIntroSkip = Config.Bind("Intro Skip",
                                "IntroSkip",
                                 true,
                                "Skip intro logos.");

            bCampRenderTextureFix = Config.Bind("Low-res Menu Fix",
                               "LowResCharactersFix",
                                true,
                               "Fixes low-resolution 3D models in the equip menu/3D model viewer.");

            // Game Overrides
            bFOVAdjust = Config.Bind("FOV Adjustment",
                                "FOVAdjustment",
                                true, // True by default to enable Vert+ for narrow aspect ratios.
                                "Set to true to enable adjustment of the FOV. \nIt will also adjust the FOV to be Vert+ if your aspect ratio is narrower than 16:9.");

            fAdditionalFOV = Config.Bind("FOV Adjustment",
                                "AdditionalFOV.Value",
                                (float)0f,
                                new ConfigDescription("Set additional FOV in degrees. This does not adjust FOV in cutscenes.", 
                                new AcceptableValueRange<float>(0f,180f)));

            bMouseSensitivity = Config.Bind("Mouse Sensitivity",
                                "MouseSensitivity.Override",
                                false, // Disable by default.
                                "Set to true to enable mouse sensitivity override.");

            iMouseSensitivity = Config.Bind("Mouse Sensitivity",
                                "MouseSensitivity.Value",
                                (int)100, // Default = 100
                                new ConfigDescription("Set desired mouse sensitivity.",
                                new AcceptableValueRange<int>(1,9999)));

            // Custom Resolution
            bCustomResolution = Config.Bind("Set Custom Resolution",
                                "CustomResolution",
                                 false, // Disable by default as launcher should suffice.
                                "Set to true to enable the custom resolution below.");

            fDesiredResolutionX = Config.Bind("Set Custom Resolution",
                                "ResolutionWidth",
                                (float)Display.main.systemWidth, // Set default to display width so we don't leave an unsupported resolution as default.
                                "Set desired resolution width.");

            fDesiredResolutionY = Config.Bind("Set Custom Resolution",
                                "ResolutionHeight",
                                (float)Display.main.systemHeight, // Set default to display height so we don't leave an unsupported resolution as default.
                                "Set desired resolution height.");

            iWindowMode = Config.Bind("Set Custom Resolution",
                                "WindowMode",
                                 1,
                                new ConfigDescription("Set window mode. 1 = Fullscreen, 2 = Borderless, 3 = Windowed.",
                                new AcceptableValueRange<int>(1, 3)));

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

            fNPCDistance = Config.Bind("Graphical Tweaks",
                                "NPCDistance.Value",
                                (float)2025f, // Default = 2025f (High)
                                new ConfigDescription("Set NPC Draw Distance. Controls distance at which NPCs render. 10000 is recommended for quality.",
                                new AcceptableValueRange<float>(1f, 100000f)));


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
                                new AcceptableValueRange<float>(1f, 999f)));

            // Run CustomResolutionPatch
            if (bCustomResolution.Value)
            {
                Harmony.CreateAndPatchAll(typeof(CustomResolutionPatch));
            }

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
                Log.LogInfo($"Letterboxing assigned.");
            }

            // Enable Letterboxing
            [HarmonyPatch(typeof(CampMenuMain), nameof(CampMenuMain.StartCamp))] // Camp menu
            [HarmonyPatch(typeof(UILoader), nameof(UILoader.OpenCanvas))] // UILoader - Map, Calendar, Task Board, Movie Player, Shop etc
            [HarmonyPostfix]
            public static void EnableLetterboxing()
            {
                letterboxing.SetActive(true);
                Log.LogInfo("Enabled UI letterboxing.");
            }

            // Disable Letterboxing
            [HarmonyPatch(typeof(GameMain), nameof(GameMain.FieldLoadStart))] // Load game
            [HarmonyPatch(typeof(CampMenuMain), nameof(CampMenuMain.CloseCamp))] // Camp menu
            [HarmonyPatch(typeof(UILoader), nameof(UILoader.DoCloseCanvas))] // UILoader - Map, Calendar, Task Board, Movie Player, Shop etc
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
            static float NewAspectRatio = (float)Screen.width / Screen.height;
            static float DefaultAspectRatio = (float)16 / 9;

            static bool farmFOVHasRun = false;
            static bool trackingFOVHasRun = false;

            // Adjust tracking camera FOV
            // Indoor, outdoor, dungeon
            [HarmonyPatch(typeof(PlayerTrackingCamera), nameof(PlayerTrackingCamera.Start))]
            [HarmonyPostfix]
            static void TrackingFOV(PlayerTrackingCamera __instance)
            {
                // Only run this once
                if (!trackingFOVHasRun)
                {
                    var InDoor = __instance.GetSetting(Define.TrackinCameraType.InDoor);
                    var OutDoor = __instance.GetSetting(Define.TrackinCameraType.OutDoor);
                    var Dangeon = __instance.GetSetting(Define.TrackinCameraType.Dangeon);

                    Log.LogInfo($"Tracking Camera. Current InDoor FOV = {InDoor.minFov}. Current OutDoor FOV = {OutDoor.minFov}. Current Dangeon FOV = {Dangeon.minFov}");

                    // Vert+ FOV
                    if (NewAspectRatio < DefaultAspectRatio)
                    {
                        float newInDoorFOV = Mathf.Floor(Mathf.Atan(Mathf.Tan(InDoor.minFov * Mathf.PI / 360) / NewAspectRatio * DefaultAspectRatio) * 360 / Mathf.PI);
                        float newOutDoorFOV = Mathf.Floor(Mathf.Atan(Mathf.Tan(OutDoor.minFov * Mathf.PI / 360) / NewAspectRatio * DefaultAspectRatio) * 360 / Mathf.PI);
                        float newDangeonFOV = Mathf.Floor(Mathf.Atan(Mathf.Tan(Dangeon.minFov * Mathf.PI / 360) / NewAspectRatio * DefaultAspectRatio) * 360 / Mathf.PI);

                        InDoor.minFov = newInDoorFOV;
                        OutDoor.minFov = newOutDoorFOV;
                        Dangeon.minFov = newDangeonFOV;

                    }
                    // Add FOV
                    if (fAdditionalFOV.Value > 0f)
                    {
                        InDoor.minFov += fAdditionalFOV.Value;
                        OutDoor.minFov += fAdditionalFOV.Value;
                        Dangeon.minFov += fAdditionalFOV.Value;
                    }

                    Log.LogInfo($"Tracking Camera: New InDoor FOV = {InDoor.minFov}. New OutDoor FOV = {OutDoor.minFov}. New Dangeon FOV = {Dangeon.minFov}");
                    trackingFOVHasRun = true;
                }
            }


            // Farming FOV
            [HarmonyPatch(typeof(PlayerFarmingCamera), nameof(PlayerFarmingCamera.Awake))]
            [HarmonyPrefix]
            public static void FarmingFOV(PlayerFarmingCamera __instance)
            {
                // Only run this once
                if (!farmFOVHasRun)
                {
                    var battleInst = BattleConst.Instance;
                    Log.LogInfo($"PlayerFarmingCamera: Current FOV = {battleInst.FarmCamera_FOV}.");

                    // Vert+ FOV
                    if (NewAspectRatio < DefaultAspectRatio)
                    {
                        float newFOV = Mathf.Floor(Mathf.Atan(Mathf.Tan(battleInst.FarmCamera_FOV * Mathf.PI / 360) / NewAspectRatio * DefaultAspectRatio) * 360 / Mathf.PI);
                        battleInst.FarmCamera_FOV = newFOV;

                    }
                    // Add FOV
                    if (fAdditionalFOV.Value > 0f)
                    {
                        battleInst.FarmCamera_FOV += fAdditionalFOV.Value;
                    }

                    Log.LogInfo($"PlayerFarmingCamera: New FOV = {battleInst.FarmCamera_FOV}.");
                    farmFOVHasRun = true;
                }
            }

            // FOV adjustment for every camera
            // Does not effect tracking camera and farm camera, maybe more. They are forced to a specific FOV
            // Adjust to Vert+ at narrower than 16:9
            [HarmonyPatch(typeof(Cinemachine.CinemachineVirtualCamera), nameof(Cinemachine.CinemachineVirtualCamera.OnEnable))]
            [HarmonyPostfix]
            public static void GlobalFOV(Cinemachine.CinemachineVirtualCamera __instance)
            {
                var currLens = __instance.m_Lens;
                var currFOV = currLens.FieldOfView;

                Log.LogInfo($"Cinemachine VCam: Current camera name = {__instance.name}.");
                Log.LogInfo($"Cinemachine VCam: Current camera FOV = {currFOV}.");

                // Vert+ FOV
                if (NewAspectRatio < DefaultAspectRatio)
                {
                    float newFOV = Mathf.Floor(Mathf.Atan(Mathf.Tan(currFOV * Mathf.PI / 360) / NewAspectRatio * DefaultAspectRatio) * 360 / Mathf.PI);
                    currLens.FieldOfView = Mathf.Clamp(newFOV, 1f, 180f);
                }
                // Add FOV for everything but cutscenes
                if (fAdditionalFOV.Value > 0f && __instance.name != "CMvcamCutBuffer" && __instance.name != "CMvcamShortPlay")
                {
                    currLens.FieldOfView += fAdditionalFOV.Value;
                    Log.LogInfo($"Cinemachine VCam: Cam name = {__instance.name}. Added gameplay FOV = {fAdditionalFOV.Value}.");
                }

                __instance.m_Lens = currLens;
                Log.LogInfo($"Cinemachine VCam: New camera FOV = {__instance.m_Lens.FieldOfView}.");
            }
        }

        [HarmonyPatch]
        public class CustomResolutionPatch
        {
            [HarmonyPatch(typeof(ScreenUtil), nameof(ScreenUtil.SetResolution), new Type[] { typeof(int), typeof(int), typeof(BootOption.WindowMode) })]
            [HarmonyPrefix]
            public static bool SetCustomRes(ref int __0, ref int __1, ref BootOption.WindowMode __2)
            {
                var fullscreenMode = iWindowMode.Value switch
                {
                    1 => BootOption.WindowMode.FullScreen,
                    2 => BootOption.WindowMode.Borderless,
                    3 => BootOption.WindowMode.Window,
                    _ => BootOption.WindowMode.FullScreen,
                };

                Log.LogInfo($"Original resolution is {__0}x{__1}. Fullscreen = {__2}.");

                __0 = (int)fDesiredResolutionX.Value;
                __1 = (int)fDesiredResolutionY.Value;
                __2 = fullscreenMode;

                Log.LogInfo($"Custom resolution set to {__0}x{__1}. Fullscreen = {__2}.");
                return true;
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
                if (fShadowDistance.Value >= 1f)
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

                // NPC Distances
                if (fNPCDistance.Value >= 1f)
                {
                    NpcSetting.ShowDistance = fNPCDistance.Value;
                    NpcSetting.HideDistance = fNPCDistance.Value;
                    Log.LogInfo($"NPC Distance set to {NpcSetting.ShowDistance}");
                }

                // Unity update rate
                // TODO: Replace this with camera movement interpolation?
                if (fUpdateRate.Value == 0) // Set update rate to screen refresh rate
                {
                    Time.fixedDeltaTime = (float)1 / Screen.currentResolution.refreshRate;
                    Log.LogInfo($"fixedDeltaTime set to {(float)1} / {Screen.currentResolution.refreshRate} = {Time.fixedDeltaTime}");
                }
                else if (fUpdateRate.Value > 50)
                {
                    Time.fixedDeltaTime = (float)1 / fUpdateRate.Value;
                    Log.LogInfo($"fixedDeltaTime set to {(float)1} / {fUpdateRate.Value} = {Time.fixedDeltaTime}");
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
                    //Log.LogInfo($"Set light.shadowCustomResolution to {__instance.BodyLight.shadowCustomResolution}.");
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
                    //Log.LogInfo($"Set RealtimeBakeLight.light.shadowCustomResolution to {__instance.Light.shadowCustomResolution}.");
                }
            }

            // Fix low res render textures
            [HarmonyPatch(typeof(CampMenuMain), nameof(CampMenuMain.Start))]
            [HarmonyPostfix]
            public static void CampRenderTextureFix(CampMenuMain __instance)
            {
                if (bCampRenderTextureFix.Value)
                {
                    float DefaultAspectRatio = (float)16 / 9;

                    // Render from UI camera at higher resolution and with anti-aliasing
                    float newHorizontalRes = Mathf.Floor(fDesiredResolutionY.Value * DefaultAspectRatio);
                    RenderTexture rt = new RenderTexture((int)newHorizontalRes, (int)fDesiredResolutionY.Value, 24, RenderTextureFormat.ARGB32);
                    rt.antiAliasing = QualitySettings.antiAliasing;
                    __instance.MyCamera.targetTexture = rt;
                    __instance.MyCamera.Render();

                    // Model viewer
                    GameObject modelPreview = __instance.ModelViewerMenu.transform.GetChild(1).gameObject;
                    RawImage modelPreviewRawImg = modelPreview.GetComponent<UnityEngine.UI.RawImage>();
                    modelPreviewRawImg.m_Texture = rt;

                    // Equipment view
                    GameObject equipPreview = __instance.CenterMenuObj.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject;
                    RawImage equipPreviewRawImg = equipPreview.GetComponent<UnityEngine.UI.RawImage>();
                    equipPreviewRawImg.m_Texture = rt;

                    Log.LogInfo($"Re-rendered low-res render textures in camp menu.");
                } 

            }
        }
    }
}
