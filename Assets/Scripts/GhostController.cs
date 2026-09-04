using System.Collections.Generic;
using UnityEngine;

namespace CampusMaze
{
    public sealed class GhostController : MonoBehaviour
    {
        private LevelGenerator level;
        private MazeGame game;
        private GridMover player;
        private SpriteRenderer spriteRenderer;
        private Vector2Int cell;
        private Vector2Int direction;
        private Vector2Int destination;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float elapsed;
        private float duration;
        private float animationTimer;
        private int animationFrame;
        private int index;
        private bool travelling;
        private bool dead;

        public Vector2Int Cell => cell;
        public bool Dead => dead;

        public void Initialize(LevelGenerator generator, MazeGame mazeGame, GridMover playerMover, int ghostIndex, Vector2Int spawn)
        {
            level = generator;
            game = mazeGame;
            player = playerMover;
            index = ghostIndex;
            cell = spawn;
            direction = ghostIndex % 2 == 0 ? Vector2Int.left : Vector2Int.right;
            transform.position = level.CellToWorld(cell);
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 19;
            UpdateSprite();
        }

        private void Update()
        {
            if (!game.IsPlaying)
                return;
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
                    if (dead && cell == level.GhostHome)
                    {
                        dead = false;
                        game.GhostRecovered();
                    }
                }
                return;
            }
            ChooseDirection();
            destination = level.Step(cell, direction);
            startPosition = transform.position;
            targetPosition = level.CellToWorld(destination);
            if (Mathf.Abs(destination.x - cell.x) > 1)
                startPosition.x = targetPosition.x - Mathf.Sign(destination.x - cell.x);
            duration = 1f / (dead ? game.GhostSpeed * 1.45f : game.GhostSpeed);
            elapsed = 0f;
            travelling = true;
        }

        public void MarkDead()
        {
            dead = true;
            travelling = false;
            UpdateSprite();
        }

        public void ResetAt(Vector2Int spawn)
        {
            cell = spawn;
            direction = Vector2Int.up;
            dead = false;
            travelling = false;
            transform.position = level.CellToWorld(cell);
            UpdateSprite();
        }

        private void ChooseDirection()
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right };
            List<Vector2Int> candidates = new List<Vector2Int>(4);
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = level.Step(cell, directions[i]);
                if (level.IsWalkable(next, true) && directions[i] != -direction)
                    candidates.Add(directions[i]);
            }
            if (candidates.Count == 0)
            {
                direction = -direction;
                return;
            }
            if (game.IsFrightened && !dead)
            {
                direction = candidates[Random.Range(0, candidates.Count)];
                return;
            }
            Vector2Int target = dead ? level.GhostHome : TargetForGhost();
            float bestDistance = float.MaxValue;
            Vector2Int best = candidates[0];
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2Int next = level.Step(cell, candidates[i]);
                float distance = (next - target).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidates[i];
                }
            }
            direction = best;
        }

        private Vector2Int TargetForGhost()
        {
            Vector2Int playerCell = player.Cell;
            if (index == 0) return playerCell;
            if (index == 1) return playerCell + player.Direction * 4;
            if (index == 2) return playerCell + new Vector2Int(-player.Direction.y, player.Direction.x) * 3;
            return (cell - playerCell).sqrMagnitude > 64 ? playerCell : new Vector2Int(level.Width - 2, 1);
        }

        private void Animate()
        {
            animationTimer += Time.deltaTime;
            if (animationTimer < 0.18f)
                return;
            animationTimer = 0f;
            animationFrame = 1 - animationFrame;
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            spriteRenderer.sprite = MazeArt.Ghost(index, new Vector2Int(direction.x, -direction.y), animationFrame, game != null && game.IsFrightened, dead);
        }
    }
}
