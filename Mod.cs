using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using TerrariaModder.Core;
using TerrariaModder.Core.Config;
using TerrariaModder.Core.Logging;

namespace PylonsPreventEvil
{
    public class PPEModConcifg : ModConfig
    {
        public override int Version => 1;

        [Client, Label("Default Pylon Radius"), Description("The default radius of a pylon's effects in tiles, only applies to newly placed pylons.")] 
        public int PylonRadius { get; set; } = 40;

        [Client, Label("Require Pylon Active"), Description("Whether pylons must be active for its effect to apply.")]
        public bool RequirePylonActive { get; set; } = true;

        [Client, Label("Cleanse Hallow"), Description("Whether pylons should cleanse the Hallow.")]
        public bool CleanseHallow { get; set; } = true;

        [Client, Label("Max Radius"), Description("The maximum radius of a Pylon"), Range(6, 1024)]
        public int MaxRadius { get; set; } = 160;
    }

    public class Mod : IMod
    {
        public string Id => "pylons-prevent-evil";
        public string Name => "Pylons Prevent Evil";
        public string Version => "1.1.0";

        public static Mod Instance { get; private set; }
        public ILogger Log { get; private set; }
        public ModContext Context { get; private set; }
        public PPEModConcifg Config { get; private set; }

        public static List<Point16> BaseRadialPath = new List<Point16>();

        public void Initialize(ModContext context)
        {
            Instance = this;
            Log = context.Logger;
            Context = context;
            Config = context.GetConfig<PPEModConcifg>();
        }

        public void Unload() { }

        public static void OnGameReady()
        {
            BaseRadialPath = GenerateRadialPath(Mod.Instance.Config.PylonRadius);
            WorldGen_Patch.InitEvilSets();
        }

        public void OnConfigChanged()
        {
            if (Config.PylonRadius > Config.MaxRadius)
            {
                Config.PylonRadius = Config.MaxRadius;
                Config.Save();
            }
            else if (Config.PylonRadius < 6)
            {
                Config.PylonRadius = 6;
                Config.Save();
            }

            BaseRadialPath.Clear();
            BaseRadialPath = GenerateRadialPath(Mod.Instance.Config.PylonRadius);
            ActivePylon.Refresh();
        }

        public static List<Point16> GenerateRadialPath(int radius)
        {
            List<Point16> radialPath = new List<Point16>();
            int radiusSq = radius * radius;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radiusSq)
                    {
                        radialPath.Add(new Point16(x, y));
                    }
                }
            }

            radialPath.Sort((a, b) =>
            {
                int distA = a.X * a.X + a.Y * a.Y;
                int distB = b.X * b.X + b.Y * b.Y;
                return distA.CompareTo(distB);
            });

            return radialPath;
        }
    }
}
