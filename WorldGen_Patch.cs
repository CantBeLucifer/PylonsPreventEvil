using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace PylonsPreventEvil
{
    [HarmonyPatch(typeof(WorldGen))]
    public static class WorldGen_Patch
    {
        static bool[] _corrupted;
        static bool[] _crimson;
        static bool[] _hallow;

        static bool[] _corruptedWalls;
        static bool[] _crimsonWalls;
        static bool[] _hallowWalls;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(WorldGen.Convert), new Type[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(bool) })]
        static bool Convert_Prefix(int i2, int j2, int conversionType)
        {
            if (conversionType == 1 || (conversionType == 2 && Mod.Instance.Config.CleanseHallow) || conversionType == 4)
            {
                // Return inverted result of InRangeOfActivePylon, since if it's in range of an active pylon we want to prevent the conversion
                return !InRangeOfActivePylon(i2, j2);
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(WorldGen.TryConvertingOrKillingTreesAboveIfTheyWouldBecomeInvalid))]
        static bool TryConvertingOrKillingTreesAboveIfTheyWouldBecomeInvalid_Prefix(int i, int j, int newFloorType)
        {
            if (_corrupted[newFloorType] || _crimson[newFloorType] || (Mod.Instance.Config.CleanseHallow && _hallow[newFloorType]))
            {
                return !InRangeOfActivePylon(i, j);
            }
            return true;
        }

        public static void InitEvilSets()
        {
            int[] corruptedTiles = new int[] { 23, 25, 32, 112, 163, 398, 400, 661 };
            int[] crimsonTiles = new int[] { 199, 200, 203, 234, 352, 399, 401, 662 };
            int[] hallowTiles = new int[] { 109, 116, 117, 164, 402, 403, 492 };

            _corrupted = TileID.Sets.Factory.CreateBoolSet(corruptedTiles);
            _crimson = TileID.Sets.Factory.CreateBoolSet(crimsonTiles);
            _hallow = TileID.Sets.Factory.CreateBoolSet(hallowTiles);

            int[] corruptedWalls = new int[] { 69, 3, 217, 220 };
            int[] crimsonWalls = new int[] { 81, 83, 218, 221 };
            int[] hallowWalls = new int[] { 70, 28, 219, 222 };

            _corruptedWalls = TileID.Sets.Factory.CreateBoolSet(corruptedWalls);
            _crimsonWalls = WallID.Sets.Factory.CreateBoolSet(crimsonWalls);
            _hallowWalls = WallID.Sets.Factory.CreateBoolSet(hallowWalls);

        }

        public static bool IsTileEvil(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (tile == null)
                return false;

            if (tile.active() && (_corrupted[tile.type] || _crimson[tile.type] || (Mod.Instance.Config.CleanseHallow && _hallow[tile.type])))
            {
                return true;
            }
            else if (tile.wall > 0 && (_corruptedWalls[tile.wall] || _crimsonWalls[tile.wall] || (Mod.Instance.Config.CleanseHallow && _hallowWalls[tile.wall])))
            {
                return true;
            }

            return false;
        }

        private static bool InRangeOfActivePylon(int i, int j)
        {
            foreach (ActivePylon pylon in ActivePylon.ActivePylons)
            {
                if (pylon.Enabled && pylon.IsInRange(i, j))
                    return true;
            }
            return false;
        }
    }
}
