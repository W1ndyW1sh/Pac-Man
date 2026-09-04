using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusMaze
{
    public sealed class MainMenuView : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.08f);
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();
            MazeAudio audio = gameObject.AddComponent<MazeAudio>();
            audio.Initialize();
            audio.Play("menu");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                SceneManager.LoadScene("RecreatedLevel");
        }

        private void OnGUI()
        {
            BuildStyles();
            float width = Mathf.Min(620f, Screen.width - 28f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.08f, width, Screen.height * 0.84f);
            Color old = GUI.color;
            GUI.color = new Color(0.025f, 0.06f, 0.15f, 0.97f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = old;
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.05f, panel.width, panel.height * 0.22f), "CAMPUS MAZE", titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + panel.height * 0.3f, panel.width - 48f, panel.height * 0.26f), "Collect every data crystal.\nUse a power core to disable the patrol drones.", bodyStyle);
            if (GUI.Button(new Rect(panel.center.x - Mathf.Min(130f, panel.width * 0.34f), panel.y + panel.height * 0.61f, Mathf.Min(260f, panel.width * 0.68f), panel.height * 0.18f), "START", buttonStyle))
                SceneManager.LoadScene("RecreatedLevel");
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.82f, panel.width, panel.height * 0.12f), "WASD / ARROW KEYS", bodyStyle);
        }

        private void BuildStyles()
        {
            if (titleStyle != null)
                return;
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp(Screen.height / 10, 24, 48), fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(0.1f, 0.95f, 0.95f);
            bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp(Screen.height / 22, 13, 20) };
            bodyStyle.normal.textColor = new Color(0.78f, 0.9f, 1f);
            buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp(Screen.height / 15, 16, 28), fontStyle = FontStyle.Bold };
            buttonStyle.normal.textColor = Color.white;
        }
    }
}
