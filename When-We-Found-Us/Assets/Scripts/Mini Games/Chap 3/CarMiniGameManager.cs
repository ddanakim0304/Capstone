using UnityEngine;

public class CarMiniGameManager : MiniGameManager
{
    public static CarMiniGameManager Instance { get; private set; }

    [Header("References")]
    public Transform car;
    public CarCameraFollow cameraFollow;

    [Header("Road Boundaries (World X)")]
    public float startPositionX = 0f;
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

        // Win condition: car has reached the end of the road
        if (car.position.x >= endPositionX)
        {
            Debug.Log("[CarMiniGameManager] Car reached the end! Triggering final cutscene.");
            gameStarted = false;

            CarController carController = car.GetComponent<CarController>();
            if (carController != null) carController.StopCar();

            if (cameraFollow != null) cameraFollow.TriggerArrivalLookAhead();

            if (FinalCutsceneMiniGame.Instance != null)
                FinalCutsceneMiniGame.Instance.TriggerCutscene();
            else
                Debug.LogError("[CarMiniGameManager] FinalCutsceneMiniGame not found in scene!");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(startPositionX, -10, 0), new Vector3(startPositionX, 10, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(endPositionX, -10, 0), new Vector3(endPositionX, 10, 0));
    }
}
