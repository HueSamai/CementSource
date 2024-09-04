using UnityEngine;

namespace JimmysUnityUtilities
{
    public static class GameObjectExtensions
    {
        /// <summary> Sets the layer of the object as well as all of its children. </summary>
        public static void SetLayerRecursively(this GameObject go, int layer)
        {
            go.layer = layer;

            for (int i = 0; i < go.transform.childCount; i++)
                go.transform.GetChild(i).gameObject.SetLayerRecursively(layer);
        }
    }
}