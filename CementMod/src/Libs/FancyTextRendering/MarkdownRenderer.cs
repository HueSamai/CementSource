using System;
using CementGB.Mod.CementMenu;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
#nullable disable
namespace LogicUI.FancyTextRendering
{
    [RegisterTypeInIl2Cpp]
    public class MarkdownRenderer : MonoBehaviour
    {
        public MarkdownRenderer(IntPtr ptr) : base(ptr) { }

        string _Source;

        public string Source
        {
            get => _Source;
            set
            {
                _Source = value;
                RenderText();
            }
        }

        TMP_Text _TextMesh;
        public TMP_Text TextMesh
        {
            get
            {
                if (_TextMesh == null)
                    _TextMesh = GetComponent<TMP_Text>();

                return _TextMesh;
            }
        }

        private void OnValidate()
        {
            RenderText();
        }

        public MarkdownRenderingSettings RenderSettings = MarkdownRenderingSettings.Default;

        private void RenderText()
        {
            Markdown.RenderToTextMesh(Source, TextMesh, RenderSettings);
        }
    }
}