using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using static System.Net.Mime.MediaTypeNames;

namespace PylonsPreventEvil
{
    public class ActivePylon
    {
        public static List<ActivePylon> ActivePylons = new List<ActivePylon>();
        public static HashSet<int> IndicatorItems = new HashSet<int> { ItemID.PurificationPowder, ItemID.ViciousPowder, ItemID.VilePowder, ItemID.DirtBlock };
        private static int _radiusChangeCooldown = 0;

        public int X { get; private set; }
        public int Y { get; private set; }
        public bool Enabled { get; private set; } = true;
        private int _radius;
        private int _radiusSq;

        private List<Point16> _radialPath;
        private int _cooldown = 0;
        private int _pathIndex = 0;
        private bool _foundEvilInCurrentPass = false;
        private string _lastPopup = string.Empty;

        private const int _scannedTilesPerFrame = 200;

        public ActivePylon(int x, int y, int radius)
        {
            X = x;
            Y = y;
            _radius = radius;
            _radiusSq = radius * radius;
            _radialPath = Mod.BaseRadialPath;
        }

        public static void SendPylonSyncRadius(int x, int y, int radius)
        {
            if (Main.netMode == 0) return;

            string payload = $"$PYLON_RAD|{x}|{y}|{radius}";
            NetworkText text = NetworkText.FromLiteral(payload);

            // msgType = 250 for mod traffic
            NetMessage.SendData(250, -1, -1, text);
        }

        public static void SendPylonSyncEnabled(int x, int y, bool enabled)
        {
            if (Main.netMode == 0) return;

            string payload = $"$PYLON_ON|{x}|{y}|{(enabled ? "1" : "0")}";
            NetworkText text = NetworkText.FromLiteral(payload);

            NetMessage.SendData(250, -1, -1, text);
        }

        public static void Refresh()
        {
            Dictionary<(int, int), ActivePylon> existingMap = new Dictionary<(int, int), ActivePylon>();
            foreach (ActivePylon p in ActivePylons) existingMap[(p.X, p.Y)] = p;

            List<ActivePylon> newActivePylons = new List<ActivePylon>();

            foreach (TeleportPylonInfo pylon in Main.PylonSystem.Pylons)
            {
                if (Mod.Instance.Config.RequirePylonActive && TeleportPylonsSystem.DoesPositionHaveEnoughNPCs(2, pylon.PositionInTiles)
                    || !Mod.Instance.Config.RequirePylonActive)
                {
                    int targetX = pylon.PositionInTiles.X + 1;
                    int targetY = pylon.PositionInTiles.Y + 2;

                    if (existingMap.TryGetValue((targetX, targetY), out ActivePylon existingPylon))
                    {
                        newActivePylons.Add(existingPylon);
                    }
                    else
                    {
                        newActivePylons.Add(new ActivePylon(targetX, targetY, Mod.Instance.Config.PylonRadius));
                    }
                }
            }

            ActivePylons = newActivePylons;
        }

        public static void Tick()
        {
            if (_radiusChangeCooldown > 0) _radiusChangeCooldown--;

            foreach (ActivePylon pylon in ActivePylons)
            {
                if (Main.netMode != 1 && pylon.Enabled)
                    pylon.Update();

                if (Main.netMode != 2)
                {
                    Vector2 worldPos = new Vector2(pylon.X * 16, pylon.Y * 16);
                    if (Vector2.Distance(Main.LocalPlayer.Center, worldPos) < 2000f)
                    {
                        pylon.DrawRangeVisuals();
                    } 
                }
            }
        }

        public static void Toggle(int x, int y)
        {
            int x1 = x - 1;
            int x2 = x + 1;
            int y1 = y - 1;
            int y2 = y + 2;

            ActivePylon pylon = ActivePylons.FirstOrDefault(p => p.X >= x1 && p.X <= x2 && p.Y >= y1 && p.Y <= y2);
            if (pylon != null)
            {
                pylon.Enabled = !pylon.Enabled;

                AdvancedPopupRequest apr = default(AdvancedPopupRequest);
                apr.DurationInFrames = 60;
                apr.Velocity = new Vector2(0, -10f);
                apr.Color = pylon.Enabled ? Color.LightYellow : Color.LightPink;
                apr.Text = $"Pylon toggled! Pylon is now {(pylon.Enabled ? "ENABLED" : "DISABLED")}";
                Vector2 pos = new Vector2(pylon.X * 16, pylon.Y * 16);

                PopupText existingText = PopupText.popupText.FirstOrDefault(pt => pt.name == pylon._lastPopup);
                if (existingText != default(PopupText) && existingText.active)
                {
                    existingText.name = apr.Text;
                    existingText.displayText = apr.Text;
                    existingText.lifeTime = apr.DurationInFrames;
                    existingText.color = apr.Color;
                    existingText.velocity = apr.Velocity;
                    Vector2 vector = FontAssets.MouseText.Value.MeasureString(apr.Text);
                    vector.Y += 80f;
                    existingText.position = pos - vector / 2f;
                    PopupText.ResetText(existingText);
                    existingText.scale = 1;
                }
                else
                {
                    PopupText.NewText(apr, pos);
                }

                pylon._lastPopup = apr.Text;

                SoundEngine.PlaySound(SoundID.MaxMana, pos);

                SendPylonSyncEnabled(pylon.X, pylon.Y, pylon.Enabled);
            }
        }

        public static void IncreaseRadius(int x, int y, int cooldown = 10)
        {
            UpdateRadius(x, y, 1, cooldown);
        }
        
        public static void DecreaseRadius(int x, int y, int cooldown = 10)
        {
            UpdateRadius(x, y, -1, cooldown);
        }

        public static void UpdateRadius(int x, int y, int by, int cooldown = 10)
        {
            if (_radiusChangeCooldown > 0 || by == 0) return;

            int x1 = x - 1;
            int x2 = x + 1;
            int y1 = y - 1;
            int y2 = y + 2;

            ActivePylon pylon = ActivePylons.FirstOrDefault(p => p.X >= x1 && p.X <= x2 && p.Y >= y1 && p.Y <= y2);
            if (pylon != null)
            {
                int oldRadius = pylon._radius;

                pylon._radius = (int)MathHelper.Clamp(pylon._radius + by, 6, Mod.Instance.Config.MaxRadius);
                pylon._radiusSq = pylon._radius * pylon._radius;
                pylon.RecalculateRadialPath();


                AdvancedPopupRequest apr = default(AdvancedPopupRequest);
                apr.DurationInFrames = 60;
                apr.Velocity = new Vector2(0, -10f);
                apr.Color = by <= 0 ? Color.LightPink : Color.LightYellow;
                apr.Text = oldRadius != pylon._radius ? $"Updated radius by {by}. Radius is now {pylon._radius}." : $"Max/min value reached! Radius is {pylon._radius}.";
                if (!pylon.Enabled)
                    apr.Text += "\nPylon DISABLED! Toggle with Dirt.";
                Vector2 pos = new Vector2(pylon.X * 16, pylon.Y * 16);

                PopupText existingText = PopupText.popupText.FirstOrDefault(pt => pt.name == pylon._lastPopup);
                if (existingText != default(PopupText) && existingText.active) 
                {
                    existingText.name = apr.Text;
                    existingText.displayText = apr.Text;
                    existingText.lifeTime = apr.DurationInFrames;
                    existingText.color = apr.Color;
                    existingText.velocity = apr.Velocity;
                    Vector2 vector = FontAssets.MouseText.Value.MeasureString(apr.Text);
                    vector.Y += 80f;
                    existingText.position = pos - vector / 2f;
                    PopupText.ResetText(existingText);
                    existingText.scale = 1;
                }
                else
                {
                    PopupText.NewText(apr, pos);
                }

                pylon._lastPopup = apr.Text;

                if (by < 0)
                {
                    SoundEngine.PlaySound(SoundID.MenuClose, pos);
                }
                else if (by > 0)
                {
                    SoundEngine.PlaySound(SoundID.MaxMana, pos);
                }

                _radiusChangeCooldown = cooldown;

                SendPylonSyncRadius(pylon.X, pylon.Y, pylon._radius);
            }
        }

        public void Update()
        {
            if (_cooldown > 0)
            {
                _cooldown--;
                return;
            }
            for (int i = 0; i < _scannedTilesPerFrame; i++)
            {
                if (_pathIndex >= _radialPath.Count)
                {
                    _pathIndex = 0;
                    _cooldown = _foundEvilInCurrentPass ? 5 : 600;
                    _foundEvilInCurrentPass = false;
                }

                Point16 offset = _radialPath[_pathIndex];
                int tx = X + offset.X;
                int ty = Y + offset.Y;

                if (WorldGen_Patch.IsTileEvil(tx, ty))
                {
                    WorldGen.Convert(tx, ty, 0, 0);
                    DoPoof(tx, ty);

                    _foundEvilInCurrentPass = true;
                    _cooldown = 5;
                    _pathIndex++;
                    break;
                }

                _pathIndex++;
            }
        }

        private void DoPoof(int tx, int ty)
        {
            int x = tx << 4;
            int y = ty << 4;

            for (int i = 0; i < 30; i++)
            {
                Dust.NewDust(new Vector2(x, y), 16, 16, 20);
            }
        }

        private void DrawRangeVisuals()
        {
            int dustType = Enabled ? 133 : 130;

            Item heldItem = Main.LocalPlayer.HeldItem;
            if (heldItem == null || !IndicatorItems.Contains(heldItem.type))
                return;

            float speed = 1.5f;
            float baseAngle = Main.GlobalTimeWrappedHourly * speed;
            float worldRadius = _radius * 16f;
            int dustNum = (int)Math.Ceiling(_radius / 5f);

            Vector2 center = new Vector2(X * 16, Y * 16);

            for (int i = 0; i < dustNum; i++)
            {
                float angle = baseAngle + i * MathHelper.TwoPi / dustNum;

                Vector2 spawnPos = center + angle.ToRotationVector2() * worldRadius;

                int dustId = Dust.NewDust(spawnPos, 0, 0, dustType, 0, 0, 150, default, 1f);
                Main.dust[dustId].noGravity = true;
                Main.dust[dustId].fadeIn = 1.2f;
                Main.dust[dustId].velocity *= 0.25f;
            }
        }

        public void RecalculateRadialPath(bool reset = true)
        {
            _radialPath = Mod.GenerateRadialPath(_radius);
            if (reset)
            {
                _pathIndex = 0;
            }
        }

        public void SyncRadiusFromNetwork(int radius)
        {
            _radius = radius;
            _radiusSq = radius * radius;
            Mod.Instance.Log.Info($"Recieved radius {radius} from server");
        }

        public void SyncEnabledFromNetwork(bool enabled)
        {
            Enabled = enabled;
            Mod.Instance.Log.Info($"Recieved enabled = {enabled} from server");
        }

        public bool IsInRange(int i, int j)
        {
            int dx = i;
            int dy = j;
            return dx * dx + dy * dy <= _radiusSq;
        }
    }
}
