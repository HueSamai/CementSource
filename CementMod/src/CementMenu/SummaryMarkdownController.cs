using MelonLoader;
using System.Reflection;
using LogicUI.FancyTextRendering;
using UnityEngine;
using System;
using CementGB.Mod.Utilities;

namespace CementGB.Mod.CementMenu;

[RegisterTypeInIl2Cpp]
public class SummaryMarkdownController : MonoBehaviour
{
    public SummaryMarkdownController(IntPtr ptr) : base(ptr) { }

    internal const string embeddedSummaryPath = "CementGB.Mod.Assets.summary.md";
    internal static string? SummaryFileContent => FileUtilities.ReadEmbeddedText(Assembly.GetExecutingAssembly(), embeddedSummaryPath);

    private void Start()
    {
        var mdRenderer = GetComponent<MarkdownRenderer>();
        if (SummaryFileContent != null) mdRenderer.Source = SummaryFileContent;
    }
}