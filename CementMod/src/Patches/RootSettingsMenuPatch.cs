using CementGB.Mod.CementMenu;
using HarmonyLib;
using Il2CppGB.UI.Utils.Settings;
using Il2CppTMPro;
using System;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace CementGB.Mod.Patches;

public static class RootSettingsMenuPatch
{
    [HarmonyPatch(typeof(OptionsMenu), nameof(OptionsMenu.OnEnable))]
    private static class OnEnable
    {
        private static bool _executed = false;

        private static void Postfix(OptionsMenu __instance)
        {
            var castedInstance = __instance.TryCast<RootSettingsMenu>();

            if (_executed || castedInstance == null) return;
            _executed = true;

            var inputButton = castedInstance.transform.Find("Input");
            var cementButton = UnityEngine.Object.Instantiate(inputButton.gameObject, castedInstance.transform);

            cementButton.name = "CementMenuButton";
            cementButton.GetComponent<TextMeshProUGUI>().text = "Cement";

            UnityEngine.Object.Destroy(cementButton.GetComponent<LocalizeStringEvent>());

            // Remove click events
            var cementButtonComp = cementButton.GetComponent<Button>();
            cementButtonComp.onClick = new Button.ButtonClickedEvent();

            cementButtonComp.onClick.AddListener(new Action(static () =>
            {
                CementMenuManager.MenuCanvas?.SetActive(!CementMenuManager.MenuCanvas.active);
            }));

            cementButton.transform.position = new Vector3(8.85f, -3.2669f, -0.11f);
        }
    }
}
