using CementGB.Mod.Utilities;
using Il2CppTMPro;
using LogicUI.FancyTextRendering;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CementGB.Mod.CementMenu;

[RegisterTypeInIl2Cpp]
public class CementMenuManager : MonoBehaviour
{
    public CementMenuManager(IntPtr ptr) : base(ptr) { }

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

    private static readonly Dictionary<Transform, Transform> _tabs = new(); // { tabTransform : contentTransform }

    private static KeyValuePair<Transform, Transform>? ActiveTab
    {
        get
        {
            return _activeTab;
        }
        set
        {
            if (_menuCanvas == null || value == null || value.Equals(_activeTab) || !_tabs.ContainsKey(value.Value.Key)) return;

            _activeTab?.Value.gameObject.SetActive(false);
            _activeTab = value;
            _activeTab?.Value.gameObject.SetActive(true);
        }
    }
    private static KeyValuePair<Transform, Transform>? _activeTab;

    private static Transform? _tabsContainer;
    private static Transform? _contentContainer;

    internal static GameObject? Create()
    {
        if (Melon<Mod>.Instance.CementAssetBundle == null) return null;

        var ret = Instantiate(Melon<Mod>.Instance.CementAssetBundle.LoadPersistentAsset<GameObject>("CMTMenuCanvas"));
        if (ret == null) return null;

        ret.SetActive(false);

        var possibleSummaries = ret.GetComponentsInChildren<TMP_Text>(true);
        foreach (var possibleSummary in possibleSummaries)
        {
            if (possibleSummary.name != "SummaryText") continue;

            // possibleSummary is summary, adding markdown stuff
            possibleSummary.gameObject.AddComponent<MarkdownRenderer>();
            possibleSummary.gameObject.AddComponent<SummaryMarkdownController>();
        }

        _tabsContainer = ret.transform.Find("Panel/Elements/TabMenu/Tabs");
        if (_tabsContainer == null)
        {
            Melon<Mod>.Logger.Warning("Could not find CementMenu tab container!!");
            return ret;
        }

        _contentContainer = ret.transform.Find("Panel/Elements/TabMenu/Content");
        if (_contentContainer == null)
        {
            Melon<Mod>.Logger.Warning("Could not find CementMenu content container!!");
            return ret;
        }

        for (int i = 0; i < _tabsContainer?.childCount; i++)
        {
            var child = _tabsContainer.GetChild(i);
            var gotComp = child.TryGetComponent<Button>(out var tabButton);

            if (!child.name.EndsWith("Button") || !gotComp || tabButton == null) continue;

            var contentObj = _contentContainer.Find(child.name.Replace("Button", ""));
            _tabs.Add(child, contentObj);
            tabButton.onClick.AddListener(new Action(() =>
            {
                ActiveTab = new(child, contentObj);
            }));
        } // TODO: Find a better way to pair up tabs and content

        ActiveTab = _tabs.First();

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