using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;

namespace PylonsPreventEvil
{
    [HarmonyPatch(typeof(Player))]
    public static class Player_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("TileInteractionsMouseOver")]
        static void TileInteractionsMouseOver_Postfix(Player __instance, int myX, int myY)
        {
            if (Main.tile[myX, myY].type == 597)
            {
                if (ActivePylon.IndicatorItems.Contains(__instance.HeldItem.type))
                {
                    __instance.cursorItemIconID = __instance.HeldItem.type;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("TileInteractionsUse")]
        static bool TileInteractionsUse_Prefix(Player __instance, int myX, int myY)
        {
            if (WiresUI.Open)
            {
                return false;
            }
            if (__instance.ownedProjectileCounts[651] > 0)
            {
                return false;
            }
            if (__instance.tileInteractAttempted)
            {
                if (Main.tile[myX, myY].type == 597)
                {
                    if (__instance.HeldItem.type == ItemID.PurificationPowder)
                    {
                        if (__instance.releaseUseTile)
                            ActivePylon.IncreaseRadius(myX, myY, true);
                        else
                            ActivePylon.IncreaseRadius(myX, myY);

                        return false;
                    }
                    else if (__instance.HeldItem.type == ItemID.VilePowder || __instance.HeldItem.type == ItemID.ViciousPowder)
                    {
                        if (__instance.releaseUseTile)
                            ActivePylon.DecreaseRadius(myX, myY, true);
                        else
                            ActivePylon.DecreaseRadius(myX, myY);

                        return false;
                    }
                    else if (__instance.HeldItem.type == ItemID.DirtBlock)
                    {
                        if (__instance.releaseUseTile)
                        {
                            ActivePylon.Toggle(myX, myY);

                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
