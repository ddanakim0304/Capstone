using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SlimeRopeController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D slime1;
    public Rigidbody2D slime2;

    [Header("Rope Physics Settings")]
    [Tooltip("The maximum length of the rope.")]
    public float maxRopeLength = 5f;

    [Header("Rope Visual Settings")]
    [Tooltip("Number of points in the line renderer (Smoother curve).")]
    public int lineResolution = 20;
    [Tooltip("How much the rope hangs down when players are close.")]
    public float slackCurveStrength = 1.5f;
    [Tooltip("Thickness of the rope.")]
    public float ropeWidth = 0.15f;

    private LineRenderer lineRenderer;
    private DistanceJoint2D ropeJoint;

    void Start()
    {
        // 1. Setup Visuals
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = lineResolution;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
        lineRenderer.useWorldSpace = true;
        
        // Make sure it uses a material, otherwise it might be invisible
        if (lineRenderer.material == null)
             lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // 2. Setup Physics
        CreatePhysicsJoint();
    }

    void CreatePhysicsJoint()
    {
        if (slime1 == null || slime2 == null)
        {
            Debug.LogError("Rope needs two Rigidbodies assigned!");
            return;
        }

        // Add the joint to Slime 1
        ropeJoint = slime1.gameObject.AddComponent<DistanceJoint2D>();
        ropeJoint.connectedBody = slime2;
        
        // AUTO CONFIGURE OFF: We set the length manually
        ropeJoint.autoConfigureDistance = false;
        ropeJoint.distance = maxRopeLength;

        // --- CRITICAL SETTING ---
        // true = Rope (can get closer, but not further)
        // false = Stick (fixed distance)
        ropeJoint.maxDistanceOnly = true; 

        // Optional: Make it never break
        ropeJoint.breakForce = Mathf.Infinity;
    }

    void Update()
    {
        if (slime1 == null || slime2 == null) return;
        DrawRope();
    }

    void DrawRope()
    {
        Vector3 pos1 = slime1.transform.position;
        Vector3 pos2 = slime2.transform.position;

        float currentDistance = Vector3.Distance(pos1, pos2);
        
        // Calculate slack: The closer they are, the more it hangs
        float slackFactor = Mathf.Max(0, maxRopeLength - currentDistance);

        // Visual optimization: If tight, draw straight line. If loose, draw curve.
        if (currentDistance >= maxRopeLength - 0.05f) // Small buffer
        {
            DrawStraightLine(pos1, pos2);
        }
        else
        {
            DrawCurvedLine(pos1, pos2, slackFactor);
        }
    }

    void DrawStraightLine(Vector3 start, Vector3 end)
    {
        if (lineRenderer.positionCount != 2) lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    void DrawCurvedLine(Vector3 start, Vector3 end, float slack)
    {
        if (lineRenderer.positionCount != lineResolution) lineRenderer.positionCount = lineResolution;

        Vector3 midPoint = (start + end) / 2f;
        
        // Drop the midpoint downwards to simulate gravity/slack
        midPoint.y -= slack * slackCurveStrength;

        // Draw Bezier Curve
        for (int i = 0; i < lineResolution; i++)
        {
            float t = i / (float)(lineResolution - 1);
            Vector3 point = (1 - t) * (1 - t) * start + 
                            2 * (1 - t) * t * midPoint + 
                            t * t * end;
            lineRenderer.SetPosition(i, point);
        }
    }
}