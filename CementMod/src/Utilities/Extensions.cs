using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// Reconstructs the up and down navigation buttons based off of the children surrounding it. Just make sure the children have buttons
    /// </summary>
    /// <param name="toChange"></param>
    public static void ReconstructByChildren(this Button toChange)
    {
        Navigation nav = new Navigation();
        nav.selectOnLeft = toChange.navigation.selectOnLeft;
        nav.selectOnRight = toChange.navigation.selectOnRight;
        nav.selectOnUp = toChange.transform.parent.GetChild(toChange.transform.GetSiblingIndex() - 1).GetComponent<Button>();
        nav.selectOnDown = toChange.transform.parent.GetChild(toChange.transform.GetSiblingIndex() + 1).GetComponent<Button>();

        toChange.navigation = nav;
    }
}
