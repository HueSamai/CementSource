using CementGB.Mod.Utilities;
using Il2CppGB.Platform.Utils;
using Il2CppGB.UI.Menu;
using Il2CppGB.UI.Utils.Settings;
using Il2CppTMPro;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace CementGB.Mod.Modules.PreferenceModule;

internal static class PreferenceModule 
{
    public const string PrefsMenuObjName = "Cement Mod Settings";

    internal static void Initialize()
    {
        MelonEvents.OnSceneWasLoaded.Subscribe(SceneLoaded);
    }

    private static void SceneLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Menu")
        {
            // TODO: Create in-game live prefs menu
            return;
        }

        CreateMenuPrefsUI();
    }

    private static void CreateMenuPrefsUI()
    {
        var uiScreen = CreatePrefsMenuScreen();
        var uiButton = CreatePrefsMenuEnterButton();

        uiButton.onClick.AddListener(new Action(() =>
        {
            var menu = GameObject.Find("Managers/Menu").GetComponent<MenuController>();
            menu?.PushScreen(uiScreen);
        }));
    }

    private static BaseMenuScreen CreatePrefsMenuScreen()
    {
        var inputRoot = GameObject.Find("Managers/Menu/Settings Menu/Canvas/Input Root");
        var newMenu = Object.Instantiate(inputRoot, inputRoot.transform.parent, true);
        var emptyButton = newMenu.transform.Find("Reset All").GetComponent<Button>();

        for (int i = 0; i < newMenu.transform.childCount; i++)
            if (newMenu.transform.GetChild(i).name != "Reset All") Object.Destroy(newMenu.transform.GetChild(i).gameObject);

        Object.Destroy(emptyButton.GetComponent<DisabledIfPlatform>());
        Object.Destroy(emptyButton.GetComponent<LocalizeStringEvent>());
        Object.Destroy(emptyButton.GetComponent<GameObjectLocalizer>());

        emptyButton.name = "Empty Button";
        emptyButton.GetComponent<TextMeshProUGUI>().text = emptyButton.name;
        emptyButton.onClick = new Button.ButtonClickedEvent();

        Object.Destroy(newMenu.GetComponent<InputOptions>());

        var menuScreen = newMenu.GetComponent<BaseMenuScreen>();
        menuScreen.defaultSelection = emptyButton;
        menuScreen.defaultSelectionFallback = emptyButton;

        //menuScreen.cancelEvent = new BaseMenuScreen.CancelEvent();
        menuScreen.cancelEvent.AddListener(new Action(() =>
        {
            var menu = GameObject.Find("Managers/Menu").GetComponent<MenuController>();
            menu?.PopScreen();
        }));

        return menuScreen;
    }

    private static Button CreatePrefsMenuEnterButton()
    {
        LoggingUtilities.VerboseLog("Creating RootSettingsMenu preferences button. . .");

        var menu = GameObject.Find("Managers/Menu/Settings Menu/Canvas/Root Settings");
        var baseButton = menu.transform.Find("Audio");
        var newButtonObj = Object.Instantiate(baseButton, menu.transform, true);

        newButtonObj.name = PrefsMenuObjName;
        newButtonObj.transform.SetSiblingIndex(baseButton.transform.GetSiblingIndex());
        newButtonObj.transform.localPosition = Vector3.up * 460;

        var audioButton = baseButton.GetComponent<Button>();
        var inputButton = menu.transform.Find("Input").GetComponent<Button>();
        var newButton = newButtonObj.GetComponent<Button>();
        audioButton.ReconstructNavigationByChildren();
        inputButton.ReconstructNavigationByChildren();
        newButton.ReconstructNavigationByChildren();

        Object.Destroy(newButtonObj.GetComponent<LocalizeStringEvent>());
        newButtonObj.GetComponent<TextMeshProUGUI>().text = PrefsMenuObjName;

        // remove all click events
        newButton.onClick = new Button.ButtonClickedEvent();
        return newButton;
    }
}