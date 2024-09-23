using CementGB.Mod.Utilities;
using Il2CppGB.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CementGB.Mod.src.Utilities;

public static class Extensions
{
    /// <summary>
    /// Somebody write a summary for this I'm dying inside tee hee
    /// </summary>
    /// <param name="toChange"></param>
    /// <param name="up"></param>
    /// <param name="down"></param>
    public static void ReconstructNavigation(this Button toChange, Button? up, Button? down)
    {
        Navigation nav = new Navigation();
        nav.selectOnLeft = toChange.navigation.selectOnLeft;
        nav.selectOnRight = toChange.navigation.selectOnRight;
        nav.selectOnUp = up == null ? toChange.navigation.selectOnUp : up;
        nav.selectOnDown = down == null ? toChange.navigation.selectOnDown : down;

        toChange.navigation = nav;
    }

    /// <summary>
    /// Reconstructs the up and down navigation buttons based off of the button children surrounding it
    /// </summary>
    /// <param name="toChange"></param>
    public static void ReconstructNavigationByChildren(this Button toChange)
    {
        Selectable[] buttons = toChange.transform.parent.GetComponentsInChildren<Selectable>();

        Navigation nav = new Navigation();
        int sIndex = -1;

        // Stupid fucking fucker button wouldn't work with Array.IndexOf so I had to do a for loop and that saddens me
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject == toChange.gameObject)
            {
                sIndex = i;
                break;
            }
        }

        int up = sIndex == 0 ? buttons.Length - 1 : sIndex - 1;
        int down = sIndex == buttons.Length - 1 ? 0 : sIndex + 1;

        nav.selectOnLeft = toChange.navigation.selectOnLeft;
        nav.selectOnRight = toChange.navigation.selectOnRight;
        nav.selectOnUp = buttons[up];
        nav.selectOnDown = buttons[down];

        toChange.navigation = nav;
    }
}
