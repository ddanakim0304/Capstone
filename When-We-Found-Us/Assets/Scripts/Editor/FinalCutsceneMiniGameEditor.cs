#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FinalCutsceneMiniGame))]
public class FinalCutsceneMiniGameEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FinalCutsceneMiniGame game = (FinalCutsceneMiniGame)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Servo Debug ──", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("▶  Send SERVO90 to P1 ESP32", GUILayout.Height(32)))
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ServoDebug] Enter Play Mode first — UDP requires a running game.");
            }
            else
            {
                var controller = HardwareManager.Instance?.GetController(1);
                if (controller == null)
                {
                    Debug.LogWarning("[ServoDebug] No controller found for P1.");
                }
                else if (!controller.IsHardwareConnected)
                {
                    Debug.LogWarning("[ServoDebug] P1 controller exists but hardware is not connected (no UDP port open).");
                }
                else
                {
                    controller.SendCommand("SERVO90");
                    Debug.Log("<color=lime>[ServoDebug] Sent SERVO90</color>");
                }
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif
