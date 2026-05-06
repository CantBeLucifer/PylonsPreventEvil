using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace PylonsPreventEvil
{
    [HarmonyPatch(typeof(Main), "DoUpdateInWorld")]
    public static class Main_DoUpdateInWorld_Patch
    {
        private static int _counter = 0;

        [HarmonyPrefix]
        static void Prefix()
        {
            _counter++;

            if (_counter >= 120) 
            { 
                _counter = 0;
                ActivePylon.Refresh();
            }

            ActivePylon.Tick();
        }
    }
}
