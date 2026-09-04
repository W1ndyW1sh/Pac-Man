using UnityEngine;

namespace CampusMaze
{
    public sealed class GridMover : MonoBehaviour
    {
        private LevelGenerator level;
        private MazeGame game;
        private SpriteRenderer spriteRenderer;
        private Vector2Int cell;
        private Vector2Int direction;
        private Vector2Int queuedDirection;
        private Vector2Int destination;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float elapsed;
        private float duration;
        private bool travelling;
        private int animationFrame;
        private float animationTimer;
        private bool manualControl;

        public Vector2Int Cell => cell;
        public Vector2Int Direction => direction;

        public void Initialize(LevelGenerator levelGenerator, MazeGame mazeGame, Vector2Int spawn)
        {
            level = levelGenerator;
            game = mazeGame;
            cell = spawn;
            destination = spawn;
            transform.position = level.CellToWorld(cell);
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 20;
            direction = Vector2Int.right;
            queuedDirection = direction;
            spriteRenderer.sprite = MazeArt.Player(new Vector2Int(direction.x, -direction.y), 0);
        }

        private void Update()
        {
            if (!game.IsPlaying)
                return;
            ReadInput();
            Animate();
            if (travelling)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
                if (t >= 1f)
                {
                    cell = destination;
                    transform.position = level.CellToWorld(cell);
                    travelling = false;
                    game.PlayerArrived(cell);
                }
                return;
            }
            Vector2Int chosen = level.IsWalkable(level.Step(cell, queuedDirection)) ? queuedDirection : direction;
            if (!level.IsWalkable(level.Step(cell, chosen)))
            {
                if (queuedDirection != Vector2Int.zero)
                    game.HitWall();
                return;
            }
            direction = chosen;
            destination = level.Step(cell, direction);
            startPosition = transform.position;
            targetPosition = level.CellToWorld(destination);
            if (Mathf.Abs(destination.x - cell.x) > 1)
                startPosition.x = targetPosition.x - Mathf.Sign(destination.x - cell.x);
            duration = 1f / game.PlayerSpeed;
            elapsed = 0f;
            travelling = true;
        }

        public void ResetAt(Vector2Int spawn)
        {
            cell = spawn;
            destination = spawn;
            direction = Vector2Int.right;
            queuedDirection = direction;
            travelling = false;
            transform.position = level.CellToWorld(cell);
            spriteRenderer.sprite = MazeArt.Player(new Vector2Int(direction.x, -direction.y), 0);
        }

        private void ReadInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) { queuedDirection = Vector2Int.left; manualControl = true; }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) { queuedDirection = Vector2Int.right; manualControl = true; }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { queuedDirection = Vector2Int.down; manualControl = true; }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { queuedDirection = Vector2Int.up; manualControl = true; }
            if (!manualControl)
            {
                if (cell == new Vector2Int(6, 1)) queuedDirection = Vector2Int.up;
                else if (cell == new Vector2Int(6, 5)) queuedDirection = Vector2Int.left;
                else if (cell == new Vector2Int(1, 5)) queuedDirection = Vector2Int.down;
                else if (cell == new Vector2Int(1, 1)) queuedDirection = Vector2Int.right;
            }
        }

        private void Animate()
        {
            animationTimer += Time.deltaTime;
            if (animationTimer < 0.14f)
                return;
            animationTimer = 0f;
            animationFrame = 1 - animationFrame;
            spriteRenderer.sprite = MazeArt.Player(new Vector2Int(direction.x, -direction.y), animationFrame);
        }
    }
}
