using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace PylonsPreventEvil
{
    [HarmonyPatch(typeof(MessageBuffer), nameof(MessageBuffer.GetData))]
    public static class MessageBuffer_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(MessageBuffer __instance, int start, int length, out int messageType)
        {
            byte b = __instance.readBuffer[start];
            messageType = b;
            if (b == 250)
            {
                long originalPosotion = __instance.reader.BaseStream.Position;

                __instance.reader.BaseStream.Position = start + 1;

                string payload = __instance.reader.ReadString();

                if (payload.StartsWith("PYLON_RAD|"))
                {
                    Mod.Instance.Log.Info($"Recieved pylon radius tagged message");

                    string[] parts = payload.Split('|');
                    if (parts.Length > 3)
                    {
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        int radius = int.Parse(parts[3]);

                        ActivePylon pylon = ActivePylon.ActivePylons.FirstOrDefault(p => p.X == x && p.Y == y);
                        if (pylon != null)
                        {
                            pylon.SyncRadiusFromNetwork(radius);
                        }

                        return false;
                    }
                }
                else if (payload.StartsWith("PYLON_ON|"))
                {
                    Mod.Instance.Log.Info($"Recieved pylon enabled tagged message");

                    string[] parts = payload.Split('|');
                    if (parts.Length > 3)
                    {
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        bool enabled = parts[3] == "1" ? true : false;

                        ActivePylon pylon = ActivePylon.ActivePylons.FirstOrDefault(p => p.X == x && p.Y == y);
                        if (pylon != null)
                        {
                            pylon.SyncEnabledFromNetwork(enabled);
                        }

                        return false;
                    }
                }

                __instance.reader.BaseStream.Position = originalPosotion;
            }
            return true;
        }
    }
}
