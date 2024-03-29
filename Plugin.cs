using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace UKFreshnessCoach
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("ULTRAKILL.exe")]
    public class UKFreshnessCoach : BaseUnityPlugin
    {
        private static Harmony harmony;
        private static BepInEx.Logging.ManualLogSource _logger;

        // Configuration values
        
        // Messages
        private static ConfigEntry<string> usedMessage;
        private static ConfigEntry<string> staleMessage;
        private static ConfigEntry<string> dullMessage;
        // Colors
        private static ConfigEntry<string> usedPrimaryColor;
        private static ConfigEntry<string> usedSecondaryColor;
        private static ConfigEntry<string> stalePrimaryColor;
        private static ConfigEntry<string> staleSecondaryColor;
        private static ConfigEntry<string> dullPrimaryColor;
        private static ConfigEntry<string> dullSecondaryColor;

        private static GameObject crosshair;
        private static GameObject textObject;
        private static Vector3 textHomePos;
        private static TMPro.TextMeshProUGUI textComp;

        private static float freshMin;
        private static float usedMin;
        private static float staleMin;

        private void Awake()
        {
            harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(UKFreshnessCoach));

            // Plugin startup logic
            _logger = Logger;
            Logger.LogDebug($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // Load config
            usedMessage = Config.Bind("General",
                                      "UsedMessage",
                                      "USE A DIFFERENT FUCKING GUN",
                                      "Message displayed when the current freshness is \"Used\".");
            staleMessage = Config.Bind("General",
                                       "StaleMessage",
                                       "YOU'RE NOT VERY GOOD AT THIS",
                                       "Message displayed when the current freshness is \"Stale\".");
            dullMessage = Config.Bind("General",
                                      "DullMessage",
                                      "GO PLAY A VISUAL NOVEL",
                                      "Message displayed when the current freshness is \"Dull\".");
            usedPrimaryColor = Config.Bind("Colors",
                                           "UsedPrimaryColor",
                                           "white",
                                           "Primary color used when the current freshness is \"Dull\".");
            usedSecondaryColor = Config.Bind("Colors",
                                             "UsedSecondaryColor",
                                             "orange",
                                             "Secondary color used when the current freshness is \"Dull\".");
            stalePrimaryColor = Config.Bind("Colors",
                                            "StalePrimaryColor",
                                            "orange",
                                            "Primary color used when the current freshness is \"Stale\".");
            staleSecondaryColor = Config.Bind("Colors",
                                              "StaleSecondaryColor",
                                              "red",
                                              "Secondary color used when the current freshness is \"Stale\".");
            dullPrimaryColor = Config.Bind("Colors",
                                             "DullPrimaryColor",
                                             "red",
                                             "Primary color used when the current freshness is \"Dull\".");
            dullSecondaryColor = Config.Bind("Colors",
                                            "DullSecondaryColor",
                                            "black",
                                            "Secondary color used when the current freshness is \"Dull\".");
        }

        // Retrieve the current settings for when the current weapon becomes used and stale
        [HarmonyPatch(typeof(StyleHUD), "Start")]
        [HarmonyPostfix]
        static void InspectStyleHUD(StyleHUD __instance) {
            var traverse = Traverse.Create(__instance);
            var freshnessStateData = traverse.Field("freshnessStateData").GetValue<List<StyleFreshnessData>>();
            foreach (var item in freshnessStateData) {
                if (item.state == StyleFreshnessState.Fresh) {
                    freshMin = item.min;
                } else if (item.state == StyleFreshnessState.Used) {
                    usedMin = item.min;
                } else if (item.state == StyleFreshnessState.Stale) {
                    staleMin = item.min;
                }
            }
        }

        // Initialize the text objects
        [HarmonyPatch(typeof(Crosshair), "Start")]
        [HarmonyPrefix]
        static void GetCrosshair()
        {
            crosshair = GameObject.Find("Canvas/Crosshair Filler/Crosshair");

            textObject = new GameObject("FreshnessCoach");
            textObject = UnityEngine.Object.Instantiate(textObject, crosshair.transform);
            textObject.SetActive(false);
            textHomePos = new Vector3 (
                textObject.transform.localPosition.x,
                textObject.transform.localPosition.y - 60,
                textObject.transform.localPosition.z
            );
            textObject.transform.localPosition = textHomePos;

            textComp = textObject.AddComponent<TMPro.TextMeshProUGUI>();

            // steal font from another object
            var vcrOsdMono = GameObject.Find("Canvas/Boss Healths/Boss Health 1/Panel/Filler/Slider/HP Text")
                .GetComponent<TMPro.TextMeshProUGUI>().font;
            textComp.font = vcrOsdMono;

            textComp.color = Color.red;
            textComp.fontSize = 16;
            textComp.alignment = TMPro.TextAlignmentOptions.Center;
        }

        // Update the text object based on the current freshness
        [HarmonyPatch(typeof(StyleHUD), nameof(StyleHUD.SetFreshness))]
        [HarmonyPostfix]
        static void SetFreshnessCoach(GameObject sourceWeapon, float amt, ref GunControl ___gc)
        {
            if (sourceWeapon != ___gc.currentWeapon) return;

            textObject.SetActive(amt < freshMin);

            int millisecond = (int) ((UnityEngine.Time.fixedTime - System.Math.Truncate(UnityEngine.Time.fixedTime)) * 100);
            string color;

            if (amt < staleMin) {
                color = millisecond / 4 % 2 == 0 ? dullPrimaryColor.Value : dullSecondaryColor.Value;

                textComp.text = $"<color={color}>{dullMessage.Value}</color>";
                textComp.fontSize = 22;

                textObject.transform.localPosition = new Vector3(
                    textHomePos.x + UnityEngine.Random.Range(-4, 4),
                    textHomePos.y + UnityEngine.Random.Range(-4, 4),
                    textHomePos.z
                );
            } else if (amt < usedMin) {
                color = millisecond / 4 % 2 == 0 ? stalePrimaryColor.Value : staleSecondaryColor.Value;

                textComp.text = $"<color={color}>{staleMessage.Value}</color>";
                textComp.fontSize = 20;

                textObject.transform.localPosition = new Vector3(
                    textHomePos.x + UnityEngine.Random.Range(-3, 3),
                    textHomePos.y + UnityEngine.Random.Range(-3, 3),
                    textHomePos.z
                );
            } else if (amt < freshMin) {
                color = millisecond / 6 % 2 == 0 ? usedPrimaryColor.Value : usedSecondaryColor.Value;

                textComp.text = $"<color={color}>{usedMessage.Value}</color>";
                textComp.fontSize = 16;

                textObject.transform.localPosition = new Vector3(
                    textHomePos.x + UnityEngine.Random.Range(-2, 2),
                    textHomePos.y + UnityEngine.Random.Range(-2, 2),
                    textHomePos.z
                );
            }
        }

        // When the player dies, clear the text from the screen
        [HarmonyPatch(typeof(NewMovement), "GetHurt")]
        [HarmonyPostfix]
        static void ClearTextOnDeath(NewMovement __instance) {
            if (__instance.dead) {
                textObject.SetActive(false);
            }
        }

        // When the combo ends, clear the text from the screen
        [HarmonyPatch(typeof(StyleHUD), "ComboOver")]
        [HarmonyPostfix]
        static void ClearTextOnComboOver() {
            if (textObject)
                textObject.SetActive(false);
        }
    }
}
