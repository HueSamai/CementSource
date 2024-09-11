using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NetBeard
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    internal class AlwaysDisable : MonoBehaviour
    {
        void Update()
        {
            gameObject.SetActive(false); // Update only runs if the object is active so this wont tank performance
        }
    }
}
