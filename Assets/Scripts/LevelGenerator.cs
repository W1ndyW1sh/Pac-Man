using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusMaze
{
    [DefaultExecutionOrder(-100)]
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
        private int[,] levelMap =
        {
            { 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7 },
            { 2, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4 },
            { 2, 5, 3, 4, 4, 3, 5, 3, 4, 4, 4, 3, 5, 4 },
            { 2, 6, 4, 0, 0, 4, 5, 4, 0, 0, 0, 4, 5, 4 },
            { 2, 5, 3, 4, 4, 3, 5, 3, 4, 4, 4, 3, 5, 3 },
            { 2, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 },
            { 2, 5, 3, 4, 4, 3, 5, 3, 3, 5, 3, 4, 4, 4 },
            { 2, 5, 3, 4, 4, 3, 5, 4, 4, 5, 3, 4, 4, 3 },
            { 2, 5, 5, 5, 5, 5, 5, 4, 4, 5, 5, 5, 5, 4 },
            { 1, 2, 2, 2, 2, 1, 5, 4, 3, 4, 4, 3, 0, 4 },
            { 0, 0, 0, 0, 0, 2, 5, 4, 3, 4, 4, 3, 0, 3 },
            { 0, 0, 0, 0, 0, 2, 5, 4, 4, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 2, 5, 4, 4, 0, 3, 4, 4, 8 },
            { 2, 2, 2, 2, 2, 1, 5, 3, 3, 0, 4, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 4, 0, 0, 0 }
        };

        private void Start()
        {
            if (Width == 0)
                Generate();
        }

        public void Generate()
        {
            int[,] source = levelMap;
            if (source == null || source.GetLength(0) == 0 || source.GetLength(1) == 0)
                throw new InvalidOperationException("LevelGenerator requires a non-empty levelMap array.");
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
                    int tile = source[sourceY, sourceX];
                    if (tile < 0 || tile > 8)
                        throw new InvalidOperationException("Level map tile values must be between 0 and 8.");
                    tiles[y, x] = tile;
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
            RuntimeAnimatorController powerAnimator = Resources.Load<RuntimeAnimatorController>("Animators/PowerPelletAnimator");

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
                    {
                        GameObject wall = CreateSprite("Wall " + x + " " + y, MazeArt.Wall(tile), cell, wallsRoot, 1.01f, 0);
                        OrientWall(wall.transform, cell, tile);
                    }

                    if (tile == 5 || tile == 6)
                    {
                        bool power = tile == 6;
                        GameObject pellet = CreateSprite(power ? "Power Pellet" : "Pellet", MazeArt.Pellet(power), cell, pelletsRoot, power ? 1.25f : 1.1f, 3);
                        if (power && powerAnimator != null)
                            pellet.AddComponent<Animator>().runtimeAnimatorController = powerAnimator;
                        pellets.Add(cell, pellet);
                        RemainingPellets++;
                    }
                }
            }
            TotalPellets = RemainingPellets;
        }

        private void OrientWall(Transform wall, Vector2Int cell, int tile)
        {
            bool mirrorX = cell.x >= Width / 2;
            bool mirrorY = cell.y > Height / 2;
            bool right = WallsConnect(tile, TileAt(cell + Vector2Int.right));
            bool left = WallsConnect(tile, TileAt(cell + Vector2Int.left));
            bool up = WallsConnect(tile, TileAt(cell + Vector2Int.down));
            bool down = WallsConnect(tile, TileAt(cell + Vector2Int.up));

            if (mirrorX)
            {
                bool swap = left;
                left = right;
                right = swap;
            }
            if (mirrorY)
            {
                bool swap = up;
                up = down;
                down = swap;
            }

            float angle = 0f;
            if (tile == 1 || tile == 3)
            {
                if (right && down)
                    angle = 0f;
                else if (right && up)
                    angle = 90f;
                else if (left && up)
                    angle = 180f;
                else if (left && down)
                    angle = 270f;
                else if (up)
                    angle = right ? 90f : 180f;
                else if (left)
                    angle = 270f;
            }
            else if (tile == 2 || tile == 4)
            {
                if (!(left && right) && ((up && down) || ((up || down) && !(left || right))))
                    angle = 90f;
            }
            else if (tile == 7)
            {
                if (!left && up && right && down)
                    angle = 90f;
                else if (!down && left && up && right)
                    angle = 180f;
                else if (!right && up && left && down)
                    angle = 270f;
            }

            if (mirrorX != mirrorY)
                angle = -angle;
            wall.localRotation = Quaternion.Euler(0f, 0f, angle);
            wall.localScale = new Vector3(mirrorX ? -1.01f : 1.01f, mirrorY ? -1.01f : 1.01f, 1f);
        }

        private bool WallsConnect(int tile, int neighbor)
        {
            bool outer = neighbor == 1 || neighbor == 2;
            bool inner = neighbor == 3 || neighbor == 4 || neighbor == 8;
            if (neighbor == 7)
                return true;
            if (tile == 7)
                return outer || inner;
            return tile == 1 || tile == 2 ? outer : inner;
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
