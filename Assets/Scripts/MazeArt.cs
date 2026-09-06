using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusMaze
{
    public static class MazeArt
    {
        const int Size = 64;
        const float PixelsPerUnit = 64f;

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        static readonly Color32 Navy = new Color32(4, 12, 30, 255);
        static readonly Color32 NavyRaised = new Color32(8, 24, 48, 255);
        static readonly Color32 NavyLight = new Color32(12, 42, 67, 255);
        static readonly Color32 Cyan = new Color32(57, 230, 255, 255);
        static readonly Color32 Teal = new Color32(25, 202, 190, 255);
        static readonly Color32 Cream = new Color32(255, 239, 188, 255);
        static readonly Color32 White = new Color32(238, 250, 255, 255);
        static readonly Color32 Coral = new Color32(255, 91, 105, 255);
        static readonly Color32 Violet = new Color32(180, 105, 255, 255);
        static readonly Color32 Blue = new Color32(66, 145, 255, 255);
        static readonly Color32 Orange = new Color32(255, 161, 67, 255);

        public static Sprite Wall(int tileType)
        {
            int type = Mathf.Clamp(tileType, 1, 8);
            return Get("Wall_" + type, pixels => DrawWall(pixels, type));
        }

        public static Sprite Pellet(bool power)
        {
            return Get(power ? "PowerPellet" : "Pellet", pixels => DrawPellet(pixels, power));
        }

        public static Sprite Floor()
        {
            return Get("Floor", DrawFloor);
        }

        public static Sprite Player(Vector2Int direction, int frame)
        {
            Vector2Int dir = Cardinal(direction);
            int pose = Mathf.Abs(frame) % 2;
            string key = "Player_" + dir.x + "_" + dir.y + "_" + pose;
            return Get(key, pixels => DrawPlayer(pixels, dir, pose));
        }

        public static Sprite PlayerDead(int frame)
        {
            int pose = Mathf.Abs(frame) % 2;
            return Get("PlayerDead_" + pose, pixels => DrawPlayerDead(pixels, pose));
        }

        public static Sprite Ghost(int index, Vector2Int direction, int frame, bool scared, bool dead)
        {
            Vector2Int dir = Cardinal(direction);
            int pose = Mathf.Abs(frame) % 2;
            int colour = ((index % 4) + 4) % 4;
            string key = "Ghost_" + colour + "_" + dir.x + "_" + dir.y + "_" + pose + "_" + scared + "_" + dead;
            return Get(key, pixels => DrawGhost(pixels, colour, dir, pose, scared, dead));
        }

        public static Sprite Bonus()
        {
            return Get("Bonus", DrawBonus);
        }

        public static Sprite Glow()
        {
            return Get("Glow", DrawGlow);
        }

        public static Sprite Life()
        {
            return Get("Life", DrawLife);
        }

        public static IEnumerable<KeyValuePair<string, Sprite>> AllSprites()
        {
            for (int type = 1; type <= 8; type++)
            {
                if (type == 5 || type == 6)
                    continue;
                yield return new KeyValuePair<string, Sprite>("Wall_" + type, Wall(type));
            }

            yield return new KeyValuePair<string, Sprite>("Floor", Floor());
            yield return new KeyValuePair<string, Sprite>("Pellet", Pellet(false));
            yield return new KeyValuePair<string, Sprite>("PowerPellet", Pellet(true));
            yield return new KeyValuePair<string, Sprite>("Bonus", Bonus());
            yield return new KeyValuePair<string, Sprite>("Glow", Glow());
            yield return new KeyValuePair<string, Sprite>("Life", Life());

            Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            for (int d = 0; d < directions.Length; d++)
            {
                Vector2Int direction = directions[d];
                for (int frame = 0; frame < 2; frame++)
                {
                    string playerKey = "Player_" + direction.x + "_" + direction.y + "_" + frame;
                    yield return new KeyValuePair<string, Sprite>(playerKey, Player(direction, frame));
                }
            }

            for (int frame = 0; frame < 2; frame++)
                yield return new KeyValuePair<string, Sprite>("PlayerDead_" + frame, PlayerDead(frame));

            for (int ghost = 0; ghost < 4; ghost++)
            {
                for (int d = 0; d < directions.Length; d++)
                {
                    Vector2Int direction = directions[d];
                    for (int frame = 0; frame < 2; frame++)
                    {
                        string prefix = "Ghost_" + ghost + "_" + direction.x + "_" + direction.y + "_" + frame + "_";
                        yield return new KeyValuePair<string, Sprite>(prefix + "False_False", Ghost(ghost, direction, frame, false, false));
                        yield return new KeyValuePair<string, Sprite>(prefix + "True_False", Ghost(ghost, direction, frame, true, false));
                        yield return new KeyValuePair<string, Sprite>(prefix + "False_True", Ghost(ghost, direction, frame, false, true));
                    }
                }
            }
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }

        static Sprite Get(string key, Action<Color32[]> painter)
        {
            Sprite sprite;
            if (Cache.TryGetValue(key, out sprite) && sprite != null)
                return sprite;

            sprite = Resources.Load<Sprite>("characters/" + key);
            if (sprite != null)
            {
                Cache[key] = sprite;
                return sprite;
            }

            Color32[] pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Transparent;
            painter(pixels);

            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.name = "CampusMaze_" + key;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.None;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = key;
            sprite.hideFlags = HideFlags.None;
            Cache[key] = sprite;
            return sprite;
        }

        static void DrawWall(Color32[] pixels, int type)
        {
            if (type == 1)
            {
                DrawLine(pixels, 24f, 0f, 24f, 40f, 1.5f, Cyan);
                DrawLine(pixels, 24f, 40f, 64f, 40f, 1.5f, Cyan);
                DrawLine(pixels, 40f, 0f, 40f, 24f, 1.5f, Cyan);
                DrawLine(pixels, 40f, 24f, 64f, 24f, 1.5f, Cyan);
            }
            else if (type == 2)
            {
                DrawLine(pixels, 0f, 24f, 64f, 24f, 1.5f, Cyan);
                DrawLine(pixels, 0f, 40f, 64f, 40f, 1.5f, Cyan);
            }
            else if (type == 3)
            {
                DrawLine(pixels, 32f, 0f, 32f, 32f, 2f, Teal);
                DrawLine(pixels, 32f, 32f, 64f, 32f, 2f, Teal);
            }
            else if (type == 4)
                DrawLine(pixels, 0f, 32f, 64f, 32f, 2f, Teal);
            else if (type == 5 || type == 6)
                DrawPellet(pixels, type == 6);
            else if (type == 7)
            {
                DrawLine(pixels, 0f, 24f, 64f, 24f, 1.5f, Cyan);
                DrawLine(pixels, 0f, 40f, 64f, 40f, 1.5f, Cyan);
                DrawLine(pixels, 32f, 0f, 32f, 24f, 2f, Teal);
            }
            else if (type == 8)
                DrawLine(pixels, 0f, 32f, 64f, 32f, 1.8f, Coral);
        }

        static void DrawFloor(Color32[] pixels)
        {
            FillRect(pixels, 0, 0, 63, 63, Navy);
            Color32 dot = new Color32(15, 47, 68, 130);
            FillCircle(pixels, 15f, 15f, 0.9f, dot);
            FillCircle(pixels, 47f, 47f, 0.9f, dot);
            FillCircle(pixels, 47f, 15f, 0.65f, dot);
            FillCircle(pixels, 15f, 47f, 0.65f, dot);
        }

        static void DrawPellet(Color32[] pixels, bool power)
        {
            if (power)
            {
                FillCircle(pixels, 32f, 32f, 12.5f, WithAlpha(Cyan, 55));
                Ring(pixels, 32f, 32f, 8.5f, 3f, Cyan);
                FillRect(pixels, 28, 27, 35, 36, NavyRaised);
                FillRect(pixels, 30, 29, 33, 34, Cream);
                FillRect(pixels, 24, 30, 27, 33, Teal);
                FillRect(pixels, 36, 30, 39, 33, Teal);
            }
            else
            {
                for (int y = -5; y <= 5; y++)
                {
                    int half = 5 - Mathf.Abs(y);
                    FillRect(pixels, 32 - half, 32 + y, 32 + half, 32 + y, y == 0 ? Cream : Teal);
                }
            }
        }

        static void DrawPlayer(Color32[] pixels, Vector2Int direction, int frame)
        {
            Vector2 dir = new Vector2(direction.x, direction.y);
            Vector2 side = new Vector2(-dir.y, dir.x);
            Vector2 center = new Vector2(32f, 31f + frame);

            FillCircle(pixels, center.x, center.y, 22f, WithAlpha(Cyan, 65));
            FillRoundedRect(pixels, 13, 13 + frame, 50, 49 + frame, 10f, Teal);
            FillRoundedRect(pixels, 17, 17 + frame, 46, 46 + frame, 8f, new Color32(39, 224, 214, 255));
            DrawLine(pixels, 18f, 22f + frame, 46f, 44f + frame, 1.4f, WithAlpha(Cyan, 125));

            Vector2 visor = center + dir * 8f;
            FillEllipse(pixels, visor.x, visor.y, direction.x == 0 ? 11f : 8f, direction.y == 0 ? 11f : 8f, Navy);
            Vector2 sight = visor + dir * 3.5f;
            FillCircle(pixels, sight.x, sight.y, 3.5f, White);
            FillCircle(pixels, sight.x + dir.x * 1.4f, sight.y + dir.y * 1.4f, 1.5f, Blue);

            Vector2 antennaBase = center + side * 5f - dir * 10f;
            Vector2 antennaTip = antennaBase + side * 7f - dir * 4f;
            DrawLine(pixels, antennaBase.x, antennaBase.y, antennaTip.x, antennaTip.y, 1.5f, Cyan);
            FillCircle(pixels, antennaTip.x, antennaTip.y, 2.4f, Cream);

            float fin = frame == 0 ? 7f : 11f;
            Vector2 rear = center - dir * 17f;
            DrawLine(pixels, rear.x + side.x * 7f, rear.y + side.y * 7f, rear.x + side.x * fin - dir.x * 6f, rear.y + side.y * fin - dir.y * 6f, 3f, Cyan);
            DrawLine(pixels, rear.x - side.x * 7f, rear.y - side.y * 7f, rear.x - side.x * fin - dir.x * 6f, rear.y - side.y * fin - dir.y * 6f, 3f, Cyan);
            FillCircle(pixels, center.x - dir.x * 14f, center.y - dir.y * 14f, 3f, NavyLight);
        }

        static void DrawPlayerDead(Color32[] pixels, int frame)
        {
            FillCircle(pixels, 32f, 32f, frame == 0 ? 18f : 11f, WithAlpha(Teal, frame == 0 ? (byte)125 : (byte)75));
            Ring(pixels, 32f, 32f, frame == 0 ? 14f : 8f, 2f, Cyan);
            DrawLine(pixels, 20f, 20f, 44f, 44f, 2.2f, Coral);
            DrawLine(pixels, 20f, 44f, 44f, 20f, 2.2f, Coral);
            int reach = frame == 0 ? 26 : 22;
            DrawLine(pixels, 32f, 32f, reach, 54f, 1.5f, Cream);
            DrawLine(pixels, 32f, 32f, 54f, reach, 1.5f, Cream);
            DrawLine(pixels, 32f, 32f, 64f - reach, 10f, 1.5f, Cream);
            DrawLine(pixels, 32f, 32f, 10f, 64f - reach, 1.5f, Cream);
        }

        static void DrawGhost(Color32[] pixels, int index, Vector2Int direction, int frame, bool scared, bool dead)
        {
            Vector2 dir = new Vector2(direction.x, direction.y);
            Vector2 side = new Vector2(-dir.y, dir.x);
            Vector2 center = new Vector2(32f, 32f);
            Color32[] colours = { Coral, Violet, Blue, Orange };
            Color32 identity = colours[index];

            if (dead)
            {
                float spread = frame == 0 ? 12f : 17f;
                for (int i = -1; i <= 1; i += 2)
                {
                    DronePolygon(pixels, center, dir, side, WithAlpha(identity, 190),
                        -9, i * spread, -2, i * (spread + 4), 3, i * spread, -4, i * (spread - 2));
                    DroneLine(pixels, center, dir, side, -17, i * 7, -24 - frame * 3, i * 7, 1f, Cyan);
                }
                DronePolygon(pixels, center, dir, side, NavyLight, -9, -7, 5, -7, 12, 0, 5, 7, -9, 7);
                DronePolygon(pixels, center, dir, side, Cyan, -5, -4, 4, -4, 8, 0, 4, 4, -5, 4);
                DroneLine(pixels, center, dir, side, -3, 0, frame == 0 ? 2 : 5, 0, 1.4f, White);
                DroneLine(pixels, center, dir, side, -12, 0, -19 - frame * 7, 0, 1.5f, WithAlpha(Cyan, 170));
                return;
            }

            Color32 shell = scared ? new Color32(57, 91, 133, 255) : identity;
            Color32 edge = scared ? new Color32(118, 168, 198, 255) : White;
            Color32 signal = scared ? Orange : Cyan;
            float thrust = scared ? (frame == 0 ? 3f : 7f) : (frame == 0 ? 6f : 11f);
            float fin = frame == 0 ? 17f : 20f;

            for (int i = -1; i <= 1; i += 2)
            {
                DronePolygon(pixels, center, dir, side, WithAlpha(signal, 150),
                    -12, i * 13, -14 - thrust, i * 16, -12, i * 19);
                DronePolygon(pixels, center, dir, side, NavyLight,
                    -14, i * 12, 2, i * 12, 7, i * 17, -1, i * 22, -14, i * 20);
                DroneLine(pixels, center, dir, side, -10, i * 17, 0, i * fin, 1.7f, shell);
                DroneLine(pixels, center, dir, side, -13, i * 15, -13 - thrust * 0.5f, i * 16, 1f, Cream);
            }

            switch (index)
            {
                case 0:
                    DronePolygon(pixels, center, dir, side, shell,
                        25, 0, -13, -17, -8, -5, -17, 0, -8, 5, -13, 17);
                    DronePolygon(pixels, center, dir, side, Navy,
                        16, 0, -6, -9, -3, 0, -6, 9);
                    DroneLine(pixels, center, dir, side, 20, 0, -8, 12, 0.7f, edge);
                    break;
                case 1:
                    DronePolygon(pixels, center, dir, side, shell,
                        21, 0, 0, -21, -17, -9, -12, 0, -17, 9, 0, 21);
                    DronePolygon(pixels, center, dir, side, Navy,
                        12, 0, -1, -12, -10, 0, -1, 12);
                    DroneLine(pixels, center, dir, side, 0, 17, 17, 0, 0.7f, edge);
                    break;
                case 2:
                    DronePolygon(pixels, center, dir, side, shell,
                        15, -14, 22, -7, 22, 7, 15, 14, -15, 14, -15, -14);
                    DronePolygon(pixels, center, dir, side, Navy,
                        -10, -9, 12, -9, 16, -5, 16, 5, 12, 9, -10, 9);
                    DroneLine(pixels, center, dir, side, -11, 11, 13, 11, 0.7f, edge);
                    DroneLine(pixels, center, dir, side, 18, -5, 18, 5, 1f, signal);
                    break;
                default:
                    DronePolygon(pixels, center, dir, side, shell,
                        20, -8, 20, 8, 6, 18, -10, 18, -18, 9, -18, -9, -10, -18, 6, -18);
                    DronePolygon(pixels, center, dir, side, Navy,
                        14, -5, 14, 5, 3, 11, -9, 11, -12, 0, -9, -11, 3, -11);
                    DroneLine(pixels, center, dir, side, -9, 14, 5, 14, 0.7f, edge);
                    DroneLine(pixels, center, dir, side, 17, -5, 17, 5, 1f, signal);
                    break;
            }

            if (scared)
            {
                DronePolygon(pixels, center, dir, side, frame == 0 ? Orange : Cream, 9, 0, -6, -8, -6, 8);
                DroneLine(pixels, center, dir, side, 3, 0, -1, 0, 0.9f, Navy);
                Vector2 dot = center - dir * 4f;
                FillCircle(pixels, dot.x, dot.y, 1.1f, Navy);
                float spark = frame == 0 ? -1f : 1f;
                DroneLine(pixels, center, dir, side, -18, spark * 20, -21, spark * 24, 0.8f, Orange);
                DroneLine(pixels, center, dir, side, -21, spark * 24, -17, spark * 27, 0.8f, Cream);
            }
            else
            {
                float pulse = frame == 0 ? 3f : 5f;
                DronePolygon(pixels, center, dir, side, signal, 9, 0, 1, -pulse, -5, 0, 1, pulse);
                DroneLine(pixels, center, dir, side, 2, 0, 6, 0, 0.9f, White);
                for (int i = 0; i <= index; i++)
                    DroneLine(pixels, center, dir, side, -8 - i * 2, -3, -8 - i * 2, 3, 0.55f, WithAlpha(shell, 230));
            }
        }

        static void DronePolygon(Color32[] pixels, Vector2 center, Vector2 dir, Vector2 side, Color32 colour, params float[] coordinates)
        {
            Vector2[] points = new Vector2[coordinates.Length / 2];
            for (int i = 0; i < points.Length; i++)
                points[i] = center + dir * coordinates[i * 2] + side * coordinates[i * 2 + 1];
            FillPolygon(pixels, points, colour);
        }

        static void DroneLine(Color32[] pixels, Vector2 center, Vector2 dir, Vector2 side, float x0, float y0, float x1, float y1, float width, Color32 colour)
        {
            Vector2 start = center + dir * x0 + side * y0;
            Vector2 end = center + dir * x1 + side * y1;
            DrawLine(pixels, start.x, start.y, end.x, end.y, width, colour);
        }

        static void DrawBonus(Color32[] pixels)
        {
            FillCircle(pixels, 32f, 32f, 21f, WithAlpha(Violet, 45));
            FillRoundedRect(pixels, 18, 18, 46, 46, 5f, Violet);
            FillRoundedRect(pixels, 22, 22, 42, 42, 4f, NavyRaised);
            FillCircle(pixels, 32f, 32f, 7f, Orange);
            FillCircle(pixels, 32f, 32f, 3f, Cream);
            for (int i = 0; i < 4; i++)
            {
                int p = 22 + i * 7;
                FillRect(pixels, p, 13, p + 2, 18, Cyan);
                FillRect(pixels, p, 46, p + 2, 51, Cyan);
                FillRect(pixels, 13, p, 18, p + 2, Cyan);
                FillRect(pixels, 46, p, 51, p + 2, Cyan);
            }
        }

        static void DrawGlow(Color32[] pixels)
        {
            FillCircle(pixels, 32f, 32f, 25f, new Color32(33, 222, 255, 20));
            FillCircle(pixels, 32f, 32f, 18f, new Color32(33, 222, 255, 28));
            Ring(pixels, 32f, 32f, 23f, 1.5f, new Color32(88, 236, 255, 85));
        }

        static void DrawLife(Color32[] pixels)
        {
            FillCircle(pixels, 32f, 32f, 19f, WithAlpha(Cyan, 45));
            FillRoundedRect(pixels, 17, 18, 46, 45, 8f, Teal);
            FillEllipse(pixels, 36f, 33f, 7f, 9f, Navy);
            FillCircle(pixels, 39f, 33f, 2.8f, White);
            DrawLine(pixels, 24f, 45f, 20f, 52f, 1.5f, Cyan);
            FillCircle(pixels, 19f, 53f, 2.2f, Cream);
        }

        static Vector2Int Cardinal(Vector2Int direction)
        {
            if (direction == Vector2Int.zero)
                return Vector2Int.right;
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                return direction.x >= 0 ? Vector2Int.right : Vector2Int.left;
            return direction.y >= 0 ? Vector2Int.up : Vector2Int.down;
        }

        static Color32 WithAlpha(Color32 colour, byte alpha)
        {
            return new Color32(colour.r, colour.g, colour.b, alpha);
        }

        static void FillRect(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 colour)
        {
            int minX = Mathf.Clamp(Mathf.Min(x0, x1), 0, Size - 1);
            int maxX = Mathf.Clamp(Mathf.Max(x0, x1), 0, Size - 1);
            int minY = Mathf.Clamp(Mathf.Min(y0, y1), 0, Size - 1);
            int maxY = Mathf.Clamp(Mathf.Max(y0, y1), 0, Size - 1);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    Blend(pixels, x, y, colour);
        }

        static void FillCircle(Color32[] pixels, float cx, float cy, float radius, Color32 colour)
        {
            FillEllipse(pixels, cx, cy, radius, radius, colour);
        }

        static void FillEllipse(Color32[] pixels, float cx, float cy, float rx, float ry, Color32 colour)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1f));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + rx + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1f));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + ry + 1f));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - cy) / ry;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float coverage = Mathf.Clamp01((1.025f - distance) * Mathf.Max(rx, ry));
                    if (coverage > 0f)
                        Blend(pixels, x, y, WithAlpha(colour, (byte)(colour.a * coverage)));
                }
            }
        }

        static void FillRoundedRect(Color32[] pixels, int x0, int y0, int x1, int y1, float radius, Color32 colour)
        {
            FillRect(pixels, x0 + Mathf.CeilToInt(radius), y0, x1 - Mathf.CeilToInt(radius), y1, colour);
            FillRect(pixels, x0, y0 + Mathf.CeilToInt(radius), x1, y1 - Mathf.CeilToInt(radius), colour);
            FillCircle(pixels, x0 + radius, y0 + radius, radius, colour);
            FillCircle(pixels, x1 - radius, y0 + radius, radius, colour);
            FillCircle(pixels, x0 + radius, y1 - radius, radius, colour);
            FillCircle(pixels, x1 - radius, y1 - radius, radius, colour);
        }

        static void Ring(Color32[] pixels, float cx, float cy, float radius, float width, Color32 colour)
        {
            int min = Mathf.Max(0, Mathf.FloorToInt(cx - radius - width));
            int max = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + radius + width));
            for (int y = min; y <= max; y++)
            {
                for (int x = min; x <= max; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float coverage = Mathf.Clamp01(width * 0.5f + 0.75f - Mathf.Abs(distance - radius));
                    if (coverage > 0f)
                        Blend(pixels, x, y, WithAlpha(colour, (byte)(colour.a * coverage)));
                }
            }
        }

        static void DrawLine(Color32[] pixels, float x0, float y0, float x1, float y1, float width, Color32 colour)
        {
            Vector2 a = new Vector2(x0, y0);
            Vector2 b = new Vector2(x1, y1);
            Vector2 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - width - 1f));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + width + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - width - 1f));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + width + 1f));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float t = lengthSquared <= 0f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
                    float distance = Vector2.Distance(point, a + ab * t);
                    float coverage = Mathf.Clamp01(width + 0.75f - distance);
                    if (coverage > 0f)
                        Blend(pixels, x, y, WithAlpha(colour, (byte)(colour.a * coverage)));
                }
            }
        }

        static void FillPolygon(Color32[] pixels, Vector2[] points, Color32 colour)
        {
            float minXValue = points[0].x;
            float maxXValue = points[0].x;
            float minYValue = points[0].y;
            float maxYValue = points[0].y;
            for (int i = 1; i < points.Length; i++)
            {
                minXValue = Mathf.Min(minXValue, points[i].x);
                maxXValue = Mathf.Max(maxXValue, points[i].x);
                minYValue = Mathf.Min(minYValue, points[i].y);
                maxYValue = Mathf.Max(maxYValue, points[i].y);
            }

            int minX = Mathf.Max(0, Mathf.FloorToInt(minXValue));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(maxXValue));
            int minY = Mathf.Max(0, Mathf.FloorToInt(minYValue));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(maxYValue));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = false;
                    for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                    {
                        Vector2 pi = points[i];
                        Vector2 pj = points[j];
                        if ((pi.y > point.y) != (pj.y > point.y) && point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x)
                            inside = !inside;
                    }
                    if (inside)
                        Blend(pixels, x, y, colour);
                }
            }
        }

        static void Blend(Color32[] pixels, int x, int y, Color32 colour)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size || colour.a == 0)
                return;
            int index = y * Size + x;
            Color32 beneath = pixels[index];
            float a = colour.a / 255f;
            float inverse = 1f - a;
            byte outA = (byte)Mathf.Clamp(Mathf.RoundToInt(colour.a + beneath.a * inverse), 0, 255);
            if (outA == 0)
            {
                pixels[index] = Transparent;
                return;
            }
            float outputAlpha = outA / 255f;
            byte r = (byte)Mathf.Clamp(Mathf.RoundToInt((colour.r * a + beneath.r * (beneath.a / 255f) * inverse) / outputAlpha), 0, 255);
            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt((colour.g * a + beneath.g * (beneath.a / 255f) * inverse) / outputAlpha), 0, 255);
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt((colour.b * a + beneath.b * (beneath.a / 255f) * inverse) / outputAlpha), 0, 255);
            pixels[index] = new Color32(r, g, b, outA);
        }
    }
}
