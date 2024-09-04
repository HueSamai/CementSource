using CementGB.Mod.Utilities;
using Il2CppTMPro;
using LogicUI.FancyTextRendering;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CementGB.Mod.CementMenu;

[RegisterTypeInIl2Cpp]
public class CementMenuManager : MonoBehaviour
{
    public CementMenuManager(IntPtr ptr) : base(ptr) { }
    //TODO: Manage tab switching

    public static GameObject? MenuCanvas 
    {
        get
        {
            _menuCanvas ??= Create();
            return _menuCanvas;
        }
        private set
        {
            _menuCanvas = value;
        }
    }
    private static GameObject? _menuCanvas;

    internal static GameObject? Create()
    {
        if (Melon<Mod>.Instance.CementAssetBundle == null) return null;

        var ret = Instantiate(Melon<Mod>.Instance.CementAssetBundle.LoadPersistentAsset<GameObject>("CMTMenuCanvas"));
        if (ret == null) return null;

        var possibleSummaries = ret.GetComponentsInChildren<TMP_Text>(true);
        foreach (var possibleSummary in possibleSummaries)
        {
            if (possibleSummary.name != "SummaryText") continue;

            // possibleSummary is summary, adding markdown stuff
            possibleSummary.gameObject.AddComponent<MarkdownRenderer>();
            possibleSummary.gameObject.AddComponent<SummaryMarkdownController>();
        }
        ret.SetActive(false);
        return ret;
    }

    private void Start()
    {
        var possibleCloseButtons = GetComponentsInChildren<Button>();

        foreach (var possibleCloseButton in possibleCloseButtons)
        {
            if (possibleCloseButton.name != "CloseButton") continue;

            possibleCloseButton.onClick.AddListener(new Action(() =>
            {
                MenuCanvas?.SetActive(false);
            }));
        }
    }
}