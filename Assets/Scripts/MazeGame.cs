using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusMaze
{
    public sealed class MazeGame : MonoBehaviour
    {
        [SerializeField] private TextAsset levelMap;
        [SerializeField] private float playerSpeed = 7f;
        [SerializeField] private float ghostSpeed = 5.2f;
        private LevelGenerator level;
        private GridMover player;
        private readonly List<GhostController> ghosts = new List<GhostController>();
        private MazeAudio audioController;
        private GameObject bonus;
        private int score;
        private int lives = 3;
        private float frightenedTimer;
        private float bonusTimer;
        private bool paused;
        private bool roundTransition;
        private bool gameEnded;
        private string message;
        private GUIStyle hudStyle;
        private GUIStyle messageStyle;

        public bool IsPlaying => !paused && !roundTransition && !gameEnded;
        public bool IsFrightened => frightenedTimer > 0f;
        public float PlayerSpeed => playerSpeed;
        public float GhostSpeed => ghostSpeed;

        private void Start()
        {
            level = GetComponent<LevelGenerator>();
            if (level == null)
                level = gameObject.AddComponent<LevelGenerator>();
            level.mapCsv = levelMap != null ? levelMap : Resources.Load<TextAsset>("PacMan Level Map");
            if (level.Width == 0)
                level.Generate();
            SetupCamera();
            audioController = gameObject.AddComponent<MazeAudio>();
            audioController.Initialize();
            audioController.Play("start");
            SpawnActors();
            PlayerArrived(player.Cell);
            message = "READY";
            StartCoroutine(ReadyDelay());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                paused = !paused;
                if (paused) audioController.StopMusic(); else audioController.ResumeMusic();
            }
            if (Input.GetKeyDown(KeyCode.R) && gameEnded)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            if (!IsPlaying)
                return;
            if (frightenedTimer > 0f)
            {
                frightenedTimer -= Time.deltaTime;
                if (frightenedTimer <= 0f)
                    audioController.SetMood(false);
            }
            bonusTimer += Time.deltaTime;
            if (bonus == null && bonusTimer >= 28f && level.RemainingPellets > 0)
                SpawnBonus();
            CheckCollisions();
        }

        public void PlayerArrived(Vector2Int cell)
        {
            bool power;
            if (level.Collect(cell, out power))
            {
                score += power ? 50 : 10;
                audioController.Play(power ? "power" : "pellet");
                if (power)
                {
                    frightenedTimer = 9f;
                    audioController.SetMood(true);
                }
                if (level.RemainingPellets == 0)
                    Win();
            }
            else
                audioController.Play("move");
        }

        public void HitWall()
        {
            if (!audioController.IsCueCoolingDown("wall"))
                audioController.Play("wall");
        }

        public void GhostRecovered()
        {
            for (int i = 0; i < ghosts.Count; i++)
                if (ghosts[i].Dead)
                    return;
            audioController.SetDead(false);
        }

        private void CheckCollisions()
        {
            for (int i = 0; i < ghosts.Count; i++)
            {
                GhostController ghost = ghosts[i];
                if (ghost.Dead || Vector3.SqrMagnitude(ghost.transform.position - player.transform.position) > 0.28f)
                    continue;
                if (IsFrightened)
                {
                    ghost.MarkDead();
                    score += 200;
                    audioController.Play("ghost");
                    audioController.SetDead(true);
                }
                else
                {
                    StartCoroutine(LoseLife());
                    return;
                }
            }
            if (bonus != null && Vector3.SqrMagnitude(bonus.transform.position - player.transform.position) < 0.3f)
            {
                score += 500;
                Destroy(bonus);
                audioController.Play("bonus");
            }
        }

        private void SpawnActors()
        {
            GameObject actors = new GameObject("Actors");
            GameObject playerObject = new GameObject("PacStudent_DataCourier");
            playerObject.transform.SetParent(actors.transform);
            player = playerObject.AddComponent<GridMover>();
            player.Initialize(level, this, level.PlayerSpawn);
            Vector2Int[] offsets = { Vector2Int.zero, Vector2Int.left, Vector2Int.right, Vector2Int.down };
            for (int i = 0; i < 4; i++)
            {
                GameObject ghostObject = new GameObject("PatrolDrone_" + (i + 1));
                ghostObject.transform.SetParent(actors.transform);
                GhostController ghost = ghostObject.AddComponent<GhostController>();
                ghost.Initialize(level, this, player, i, level.GhostHome + offsets[i]);
                ghosts.Add(ghost);
            }
        }

        private void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.008f, 0.013f, 0.05f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();
            float vertical = level.Height * 0.55f;
            float horizontal = level.Width * 0.5f / Mathf.Max(0.2f, camera.aspect);
            camera.orthographicSize = Mathf.Max(vertical, horizontal) + 1.4f;
        }

        private void SpawnBonus()
        {
            bonus = new GameObject("Bonus_DataChip");
            SpriteRenderer renderer = bonus.AddComponent<SpriteRenderer>();
            renderer.sprite = MazeArt.Bonus();
            renderer.sortingOrder = 18;
            bonus.transform.localScale = Vector3.one * 1.35f;
            bonus.transform.position = level.CellToWorld(new Vector2Int(level.Width / 2, level.Height / 2 + 3));
        }

        private IEnumerator ReadyDelay()
        {
            roundTransition = true;
            yield return new WaitForSeconds(1.4f);
            message = string.Empty;
            roundTransition = false;
        }

        private IEnumerator LoseLife()
        {
            roundTransition = true;
            lives--;
            audioController.Play("death");
            message = lives > 0 ? "SIGNAL LOST" : "MISSION FAILED";
            yield return new WaitForSeconds(1.5f);
            if (lives <= 0)
            {
                gameEnded = true;
                message = "MISSION FAILED\nPRESS R TO RETRY";
                yield break;
            }
            player.ResetAt(level.PlayerSpawn);
            Vector2Int[] offsets = { Vector2Int.zero, Vector2Int.left, Vector2Int.right, Vector2Int.down };
            for (int i = 0; i < ghosts.Count; i++)
                ghosts[i].ResetAt(level.GhostHome + offsets[i]);
            frightenedTimer = 0f;
            audioController.SetMood(false);
            audioController.SetDead(false);
            message = "READY";
            yield return new WaitForSeconds(0.8f);
            message = string.Empty;
            roundTransition = false;
        }

        private void Win()
        {
            gameEnded = true;
            message = "NETWORK RESTORED\nPRESS R TO PLAY AGAIN";
            audioController.Play("win");
        }

        private void OnGUI()
        {
            BuildStyles();
            GUI.Label(new Rect(24f, 15f, 360f, 50f), "SCORE  " + score.ToString("000000"), hudStyle);
            GUI.Label(new Rect(Screen.width - 245f, 15f, 220f, 50f), "LIVES  " + lives, hudStyle);
            if (paused)
                GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 100f), "PAUSED\nESC TO CONTINUE", messageStyle);
            else if (!string.IsNullOrEmpty(message))
                GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 120f), message, messageStyle);
        }

        private void BuildStyles()
        {
            if (hudStyle != null)
                return;
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            hudStyle.normal.textColor = new Color(0.3f, 1f, 0.92f);
            messageStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, fontStyle = FontStyle.Bold };
            messageStyle.normal.textColor = Color.white;
        }
    }
}
