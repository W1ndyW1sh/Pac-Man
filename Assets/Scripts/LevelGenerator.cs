using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusMaze
{
    public sealed class LevelGenerator : MonoBehaviour
    {
        public TextAsset mapCsv;
        public Transform levelRoot;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RemainingPellets { get; private set; }
        public int TotalPellets { get; private set; }
        public Vector2Int PlayerSpawn { get { return new Vector2Int(1, 1); } }
        public Vector2Int GhostHome { get { return new Vector2Int(Width / 2 - 1, Height / 2); } }
        public Vector2Int GateExit { get { return new Vector2Int(Width / 2 - 1, Height / 2 - 3); } }

        private int[,] tiles;
        private bool[,] exterior;
        private readonly Dictionary<Vector2Int, GameObject> pellets = new Dictionary<Vector2Int, GameObject>();

        public void Generate()
        {
            if (mapCsv == null)
                throw new InvalidOperationException("LevelGenerator requires a mapCsv TextAsset.");

            int[,] source = ParseMap(mapCsv.text);
            int sourceHeight = source.GetLength(0);
            int sourceWidth = source.GetLength(1);
            Width = sourceWidth * 2;
            Height = sourceHeight * 2 - 1;
            tiles = new int[Height, Width];

            for (int y = 0; y < Height; y++)
            {
                int sourceY = y < sourceHeight ? y : Height - 1 - y;
                for (int x = 0; x < Width; x++)
                {
                    int sourceX = x < sourceWidth ? x : Width - 1 - x;
                    tiles[y, x] = source[sourceY, sourceX];
                }
            }

            FindExterior();
            ReplaceLevelRoot();
            BuildVisuals();
        }

        public int TileAt(Vector2Int cell)
        {
            if (!Contains(cell))
                return -1;
            return tiles[cell.y, cell.x];
        }

        public bool IsWalkable(Vector2Int cell, bool allowGate = false)
        {
            if (!Contains(cell) || exterior[cell.y, cell.x])
                return false;

            int tile = tiles[cell.y, cell.x];
            if (tile == 8)
                return allowGate;
            return tile == 0 || tile == 5 || tile == 6;
        }

        public Vector2Int Step(Vector2Int cell, Vector2Int direction)
        {
            Vector2Int next = cell + direction;
            int tunnelRow = Height / 2;
            if (cell.y == tunnelRow && direction.y == 0)
            {
                if (next.x < 0)
                    next.x = Width - 1;
                else if (next.x >= Width)
                    next.x = 0;
            }
            return next;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            Vector3 local = new Vector3(cell.x - (Width - 1) * 0.5f, (Height - 1) * 0.5f - cell.y, 0f);
            return transform.TransformPoint(local);
        }

        public Vector2Int CellAtWorld(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            int x = Mathf.RoundToInt(local.x + (Width - 1) * 0.5f);
            int y = Mathf.RoundToInt((Height - 1) * 0.5f - local.y);
            return new Vector2Int(x, y);
        }

        public bool Collect(Vector2Int cell, out bool power)
        {
            power = false;
            if (!Contains(cell))
                return false;

            int tile = tiles[cell.y, cell.x];
            if (tile != 5 && tile != 6)
                return false;

            power = tile == 6;
            tiles[cell.y, cell.x] = 0;
            GameObject pellet;
            if (pellets.TryGetValue(cell, out pellet))
            {
                pellets.Remove(cell);
                if (pellet != null)
                {
                    if (Application.isPlaying)
                        Destroy(pellet);
                    else
                        DestroyImmediate(pellet);
                }
            }
            RemainingPellets--;
            return true;
        }

        private int[,] ParseMap(string raw)
        {
            string normalized = raw.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\r\n", "\n").Replace('\r', '\n').TrimStart('\uFEFF');
            string[] lines = normalized.Split('\n');
            List<int[]> rows = new List<int[]>();
            int width = -1;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex].Trim();
                if (line.Length == 0 || line[0] == ',')
                {
                    if (rows.Count > 0)
                        break;
                    continue;
                }

                string[] values = line.Split(',');
                int[] row = new int[values.Length];
                bool numeric = true;
                for (int x = 0; x < values.Length; x++)
                {
                    int value;
                    if (!int.TryParse(values[x].Trim(), out value))
                    {
                        numeric = false;
                        break;
                    }
                    if (value < 0 || value > 8)
                        throw new InvalidOperationException("Level map tile values must be between 0 and 8.");
                    row[x] = value;
                }

                if (!numeric)
                {
                    if (rows.Count > 0)
                        break;
                    continue;
                }

                if (width < 0)
                    width = row.Length;
                else if (row.Length != width)
                    throw new InvalidOperationException("Level map numeric rows must have equal widths.");
                rows.Add(row);
            }

            if (rows.Count == 0 || width == 0)
                throw new InvalidOperationException("Level map CSV contains no numeric map rows.");

            int[,] result = new int[rows.Count, width];
            for (int y = 0; y < rows.Count; y++)
                for (int x = 0; x < width; x++)
                    result[y, x] = rows[y][x];
            return result;
        }

        private void FindExterior()
        {
            exterior = new bool[Height, Width];
            Vector2Int[] queue = new Vector2Int[Width * Height];
            int read = 0;
            int write = 0;
            int tunnelRow = Height / 2;

            for (int x = 0; x < Width; x++)
            {
                AddExterior(x, 0, tunnelRow, queue, ref write);
                AddExterior(x, Height - 1, tunnelRow, queue, ref write);
            }
            for (int y = 1; y < Height - 1; y++)
            {
                AddExterior(0, y, tunnelRow, queue, ref write);
                AddExterior(Width - 1, y, tunnelRow, queue, ref write);
            }

            while (read < write)
            {
                Vector2Int cell = queue[read++];
                AddExterior(cell.x + 1, cell.y, tunnelRow, queue, ref write);
                AddExterior(cell.x - 1, cell.y, tunnelRow, queue, ref write);
                AddExterior(cell.x, cell.y + 1, tunnelRow, queue, ref write);
                AddExterior(cell.x, cell.y - 1, tunnelRow, queue, ref write);
            }
        }

        private void AddExterior(int x, int y, int tunnelRow, Vector2Int[] queue, ref int write)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height || y == tunnelRow || exterior[y, x] || tiles[y, x] != 0)
                return;
            exterior[y, x] = true;
            queue[write++] = new Vector2Int(x, y);
        }

        private void ReplaceLevelRoot()
        {
            if (levelRoot != null && levelRoot != transform)
            {
                if (Application.isPlaying)
                    Destroy(levelRoot.gameObject);
                else
                    DestroyImmediate(levelRoot.gameObject);
            }
            GameObject root = new GameObject("Generated Level");
            root.transform.SetParent(transform, false);
            levelRoot = root.transform;
            pellets.Clear();
            RemainingPellets = 0;
            TotalPellets = 0;
        }

        private void BuildVisuals()
        {
            Transform floorsRoot = NewGroup("Floor");
            Transform wallsRoot = NewGroup("Walls");
            Transform pelletsRoot = NewGroup("Pellets");

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    int tile = tiles[y, x];
                    bool walkable = IsWalkable(cell, true);
                    if (walkable)
                        CreateSprite("Floor " + x + " " + y, MazeArt.Floor(), cell, floorsRoot, 1f, -10);

                    if (tile >= 1 && tile <= 4 || tile == 7 || tile == 8)
                        CreateSprite("Wall " + x + " " + y, MazeArt.Wall(tile), cell, wallsRoot, 1.01f, 0);

                    if (tile == 5 || tile == 6)
                    {
                        bool power = tile == 6;
                        GameObject pellet = CreateSprite(power ? "Power Pellet" : "Pellet", MazeArt.Pellet(power), cell, pelletsRoot, power ? 0.46f : 0.2f, 3);
                        pellets.Add(cell, pellet);
                        RemainingPellets++;
                    }
                }
            }
            TotalPellets = RemainingPellets;
        }

        private Transform NewGroup(string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(levelRoot, false);
            return group.transform;
        }

        private GameObject CreateSprite(string name, Sprite sprite, Vector2Int cell, Transform parent, float scale, int sortingOrder)
        {
            GameObject item = new GameObject(name);
            Transform itemTransform = item.transform;
            itemTransform.SetParent(parent, false);
            itemTransform.position = CellToWorld(cell);
            itemTransform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return item;
        }

        private bool Contains(Vector2Int cell)
        {
            return tiles != null && cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
        }
    }
}
