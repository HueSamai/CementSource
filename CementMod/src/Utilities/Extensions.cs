using CementGB.Mod.Utilities;
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
    public static void ReconstructNavigation(this Button toChange, Button up, Button down)
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
        Button[] buttons = toChange.transform.parent.GetComponentsInChildren<Button>();

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
