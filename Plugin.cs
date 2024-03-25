﻿using System.Collections.Generic;
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

        private static ConfigEntry<string> usedMessage;
        private static ConfigEntry<string> staleMessage;

        private static GameObject crosshair;
        private static GameObject textObject;
        private static Vector3 textHomePos;
        private static TMPro.TextMeshProUGUI textComp;

        private static float freshMin;
        private static float usedMin;

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

            if (amt < usedMin) {
                if (millisecond / 4 % 2 == 0) {
                    color = "red";
                } else {
                    color = "orange";
                }

                textComp.text = $"<color={color}>{staleMessage.Value}</color>";
                textComp.fontSize = 20;

                textObject.transform.localPosition = new Vector3(
                    textHomePos.x + UnityEngine.Random.Range(-3, 3),
                    textHomePos.y + UnityEngine.Random.Range(-3, 3),
                    textHomePos.z
                );
            } else if (amt < freshMin) {
                if (millisecond / 6 % 2 == 0) {
                    color = "orange";
                } else {
                    color = "white";
                }

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
    }
}