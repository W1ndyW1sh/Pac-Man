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
                yield return new KeyValuePair<string, Sprite>("Wall_" + type, Wall(type));

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

            sprite = Resources.Load<Sprite>("Generated/" + key);
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
            FillRect(pixels, 0, 0, 63, 63, Navy);
            FillRect(pixels, 2, 2, 61, 61, NavyRaised);
            FillRect(pixels, 5, 5, 58, 58, NavyLight);
            FillRect(pixels, 7, 7, 56, 56, NavyRaised);

            Color32 edge = type % 3 == 0 ? Teal : Cyan;
            DrawLine(pixels, 3f, 3f, 60f, 3f, 1.2f, edge);
            DrawLine(pixels, 3f, 60f, 60f, 60f, 1.2f, edge);
            DrawLine(pixels, 3f, 3f, 3f, 60f, 1.2f, edge);
            DrawLine(pixels, 60f, 3f, 60f, 60f, 1.2f, edge);

            Color32 inner = WithAlpha(edge, 135);
            bool left = type == 1 || type == 4 || type == 6 || type == 7 || type == 8;
            bool right = type == 1 || type == 3 || type == 5 || type == 7 || type == 8;
            bool up = type == 2 || type == 5 || type == 6 || type == 7 || type == 8;
            bool down = type == 2 || type == 3 || type == 4 || type == 7;
            FillCircle(pixels, 31.5f, 31.5f, 3.2f, inner);
            if (left) DrawLine(pixels, 8f, 31.5f, 31.5f, 31.5f, 1.1f, inner);
            if (right) DrawLine(pixels, 31.5f, 31.5f, 55f, 31.5f, 1.1f, inner);
            if (up) DrawLine(pixels, 31.5f, 31.5f, 31.5f, 55f, 1.1f, inner);
            if (down) DrawLine(pixels, 31.5f, 8f, 31.5f, 31.5f, 1.1f, inner);
            FillCircle(pixels, 31.5f, 31.5f, 1.5f, Cream);
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
            Vector2 center = new Vector2(32f, 34f);

            if (dead)
            {
                DrawEyes(pixels, center, dir, side, true);
                DrawLine(pixels, 22f, 21f, 31f, 16f, 1.4f, WithAlpha(Cyan, 110));
                DrawLine(pixels, 31f, 16f, 41f, 21f, 1.4f, WithAlpha(Cyan, 110));
                return;
            }

            Color32[] colours = { Coral, Violet, Blue, Orange };
            Color32 shell = scared ? new Color32(38, 91, 204, 255) : colours[index];
            Color32 glow = scared ? new Color32(68, 138, 255, 70) : WithAlpha(shell, 60);
            FillCircle(pixels, center.x, center.y, 23f, glow);

            Vector2[] body =
            {
                new Vector2(13f, 20f), new Vector2(16f, 48f), new Vector2(25f, 55f),
                new Vector2(39f, 55f), new Vector2(48f, 48f), new Vector2(51f, 20f),
                new Vector2(44f, frame == 0 ? 12f : 16f), new Vector2(37f, frame == 0 ? 18f : 11f),
                new Vector2(29f, frame == 0 ? 11f : 18f), new Vector2(21f, frame == 0 ? 18f : 12f)
            };
            FillPolygon(pixels, body, shell);
            DrawLine(pixels, 16f, 46f, 25f, 53f, 1.2f, WithAlpha(White, 120));
            DrawLine(pixels, 25f, 53f, 39f, 53f, 1.2f, WithAlpha(White, 120));

            if (scared)
            {
                FillCircle(pixels, 24f, 38f, 3.2f, White);
                FillCircle(pixels, 40f, 38f, 3.2f, White);
                DrawLine(pixels, 22f, 26f, 27f, 30f, 1.7f, Cream);
                DrawLine(pixels, 27f, 30f, 32f, 26f, 1.7f, Cream);
                DrawLine(pixels, 32f, 26f, 37f, 30f, 1.7f, Cream);
                DrawLine(pixels, 37f, 30f, 42f, 26f, 1.7f, Cream);
            }
            else
            {
                DrawEyes(pixels, center + dir * 2f, dir, side, false);
                FillRect(pixels, 28, 22, 35, 24, Navy);
            }
        }

        static void DrawEyes(Color32[] pixels, Vector2 center, Vector2 dir, Vector2 side, bool dead)
        {
            Vector2 first = center + side * 8f;
            Vector2 second = center - side * 8f;
            FillEllipse(pixels, first.x, first.y, 5f, 7f, White);
            FillEllipse(pixels, second.x, second.y, 5f, 7f, White);
            Color32 pupil = dead ? Cyan : Navy;
            Vector2 look = dir * 2.6f;
            FillCircle(pixels, first.x + look.x, first.y + look.y, 2.3f, pupil);
            FillCircle(pixels, second.x + look.x, second.y + look.y, 2.3f, pupil);
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
