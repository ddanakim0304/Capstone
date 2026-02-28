using UnityEngine;
public class CarMiniGameManager : MiniGameManager
{
    public static CarMiniGameManager Instance { get; private set; }
    [Header("References")]
    [Tooltip("The car Transform to track.")]
    public Transform car;

    [Tooltip("Camera follow script – used to trigger the look-ahead lerp on arrival.")]
    public CarCameraFollow cameraFollow;

    [Header("Road Boundaries (World X)")]
    [Tooltip("World-space X of the leftmost start position. Used by CarController to clamp movement.")]
    public float startPositionX = 0f;

    [Tooltip("World-space X the car must reach to win. " +
             "Set this to the right edge of your road sprite in the scene.")]
    public float endPositionX = 50f;
    private bool gameStarted = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Snap car to start position on game load
        if (car != null)
        {
            car.position = new Vector3(startPositionX, car.position.y, car.position.z);
        }
        gameStarted = true;
    }

    void Update()
    {
        if (!gameStarted || isGameWon || car == null) return;

        // Win condition: car has reached the end of the road → stop the car, then hand off to the cutscene
        if (car.position.x >= endPositionX)
        {
            Debug.Log("[CarMiniGameManager] Car reached the end! Triggering final cutscene.");
            gameStarted = false;   // stop checking every frame

            // Freeze the car immediately
            CarController carController = car.GetComponent<CarController>();
            if (carController != null) carController.StopCar();

            // Lerp the camera look-ahead
            if (cameraFollow != null) cameraFollow.TriggerArrivalLookAhead();

            if (FinalCutsceneMiniGame.Instance != null)
                FinalCutsceneMiniGame.Instance.TriggerCutscene();
            else
                Debug.LogError("[CarMiniGameManager] FinalCutsceneMiniGame not found in scene!");
        }
    }

    //visualise start / end lines in the Scene view
    void OnDrawGizmosSelected()
    {
        // Start line (green)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(startPositionX, -10, 0), new Vector3(startPositionX, 10, 0));

        // End / finish line (red)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(endPositionX, -10, 0), new Vector3(endPositionX, 10, 0));
    }
}
