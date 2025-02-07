using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FuzzyLogicController : MonoBehaviour
{
    // File for storing results
    private string fileName = "Time.txt";

    public bool fuzzyLogic = true; // Toggle for enabling fuzzy logic
    private string name = "Fuzzy Logic";

    public float time; // Elapsed time

    // Movement settings
    public float moveSpeed = 10f;
    public float turnSpeed = 5f;

    // Reference points for detecting obstacles and boundaries
    public Transform carFront, carLeft, carRight;

    [SerializeField] private float speed;    // Final speed output (0 to 1)
    [SerializeField] private float turn;     // Final turn output (-1 = sharp left, +1 = sharp right)

    public TextMeshProUGUI speedTurnText;    // UI element to visualise speed and turn

    [SerializeField] private bool isCommitted = false; // Tracks whether the car is committed to a turn
    [SerializeField] private float commitmentDirection = 0f; // Stores committed direction (-1 = left, 1 = right)

    public Waypoint curWaypoint;             // Current navigation waypoint
    public float distanceToWaypoint;         // Distance to the current waypoint
    public float waypointThreshold;          // Distance at which to switch to the next waypoint

    public float timeToRot;                  // Waypoint rotation time interval
    public float curTimeToRot = 0f;          // Current time since last rotation

    // Obstacle detection objects
    public GameObject obstacle;
    public GameObject leftObstacle;
    public GameObject rightObstacle;

    [SerializeField] private int collisions = 0; // Number of collisions recorded
    [SerializeField] public TextMeshProUGUI collisionsTxt; // UI element for displaying collisions

    // Distances from detected obstacles
    public float leftSpace;
    public float rightSpace;
    public float distance;

    // Boundaries for obstacle detection
    public Bounds leftBound;
    public Bounds rightBound;
    public Bounds frontBound;

    private void Start()
    {
        // Configure boundary bounds
        leftBound.transform.localPosition = new Vector3(0, 0, -1);
        leftBound.transform.localScale = new Vector3(1, 1, 1);

        rightBound.transform.localPosition = new Vector3(0, 0, 1);
        rightBound.transform.localScale = new Vector3(1, 1, 1);

        frontBound.transform.localScale = new Vector3(3, 1, 1.28f);
        frontBound.transform.localPosition = new Vector3(-2f, 0, 0);
    }

    void FixedUpdate()
    {
        if (fuzzyLogic)
        {
            // Detect distances to obstacles
            if (obstacle != null)
            {
                distance = Vector3.Distance(carFront.position, obstacle.GetComponent<Collider>().ClosestPointOnBounds(carFront.position));
            }
            else
            {
                distance = 100f; // Default distance if no obstacle is detected
            }

            // Detect left and right obstacle distances
            leftObstacle = leftBound.obstacle;
            rightObstacle = rightBound.obstacle;

            leftSpace = leftObstacle != null ? Vector3.Distance(carLeft.position, leftObstacle.GetComponent<Collider>().ClosestPointOnBounds(carLeft.position)) : 100f;
            rightSpace = rightObstacle != null ? Vector3.Distance(carRight.position, rightObstacle.GetComponent<Collider>().ClosestPointOnBounds(carRight.position)) : 100f;

            // Apply fuzzy logic to inputs
            FuzzifyInputs(distance, leftSpace, rightSpace);

            // Move the car based on fuzzy outputs
            MoveCar();

            // Rotate towards the current waypoint
            if (curTimeToRot >= timeToRot)
            {
                curTimeToRot = 0f;
                if (!isCommitted)
                {
                    RotateToWaypoint();
                }
            }

            // Update rotation timer
            if (!isCommitted)
            {
                curTimeToRot += Time.deltaTime;
            }
            else
            {
                curTimeToRot = 0;
            }
        }
        else
        {
            speed = 0;
            turn = 0;
            speedTurnText.text = "Speed: " + (speed * moveSpeed).ToString() + "\tTurn: " + (turn * turnSpeed).ToString();

        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!enabled) return; // Skip if script is disabled

        if (other.CompareTag("waypoint"))
        {
            curWaypoint = other.GetComponentInParent<Waypoint>().nextWaypoint;
            RotateToWaypoint();
        }

        if (other.gameObject.CompareTag("Obstacle"))
        {
            Debug.LogError("Collided with an Obstacle");
            collisions++;
            collisionsTxt.text = $"Collided with {collisions} Obstacles";
        }

        if (other.CompareTag("StartLine"))
        {
            Timer.instance.timer = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enabled) return; // Skip if script is disabled

        if (other.CompareTag("FinishLine"))
        {
            Timer.instance.timer = false;
            fuzzyLogic = false;
            time = Timer.instance.getElapsedTime();

            // Save results to file
            using (var sw = File.Exists(fileName) ? new StreamWriter(fileName, true) : File.CreateText(fileName))
            {
                sw.WriteLine($"{SceneManager.GetActiveScene().name}\n{name}\nTime: {time}\nCollisions: {collisions}");
            }
        }
    }

    void RotateToWaypoint()
    {
        if (curWaypoint == null) return;

        Vector3 direction = curWaypoint.transform.position - transform.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            targetRotation *= Quaternion.Euler(0, 90, 0);
            transform.rotation = targetRotation;
        }
    }

    void FuzzifyInputs(float distance, float leftSpace, float rightSpace)
    {
        // Calculate membership values for distance
        float close = DistanceClose(distance);
        float medium = DistanceMedium(distance);
        float far = DistanceFar(distance);

        // Handle commitment logic
        if (isCommitted)
        {
            if (distance > 20f || (turn < 0 && leftSpace <= 0.2f) || (turn > 0 && rightSpace <= 0.2f))
            {
                isCommitted = false; // Reset commitment when safe
            }
            else
            {
                turn = commitmentDirection; // Continue in committed direction
            }
        }
        else
        {
            // Decide turning direction based on space
            if (leftSpace >= rightSpace)
            {
                float wideLeft = SpaceWide(leftSpace);

                // Apply fuzzy rules for left turn
                float ruleSharpLeft = Mathf.Min(close, wideLeft);
                float ruleSlightLeft = Mathf.Min(medium, wideLeft);

                turn = Defuzzify(new[] { ruleSharpLeft, ruleSlightLeft }, new[] { -1f, -0.5f });

                if (turn != 0)
                {
                    isCommitted = true;
                    commitmentDirection = turn;
                }
            }
            else
            {
                float wideRight = SpaceWide(rightSpace);

                // Apply fuzzy rules for right turn
                float ruleSharpRight = Mathf.Min(close, wideRight);
                float ruleSlightRight = Mathf.Min(medium, wideRight);

                turn = Defuzzify(new[] { ruleSharpRight, ruleSlightRight }, new[] { 1f, 0.5f });

                if (turn != 0)
                {
                    isCommitted = true;
                    commitmentDirection = turn;
                }
            }
        }

        // Fuzzy rules for speed
        float ruleFast = far;
        float ruleSlow = medium;
        float ruleStop = close;

        speed = Defuzzify(new[] { ruleStop, ruleSlow, ruleFast }, new[] { 0f, 0.5f, 1f });
    }

    float TrapezoidalMembership(float x, float a, float b, float c, float d)
    {
        if (x < a || x > d) return 0f;
        if (x >= b && x <= c) return 1f;
        if (x < b) return (x - a) / (b - a);
        return (d - x) / (d - c);
    }

    // Membership functions for distance
    float DistanceClose(float x) => TrapezoidalMembership(x, 0f, 0f, 2.5f, 3.75f);
    float DistanceMedium(float x) => TrapezoidalMembership(x, 2.5f, 5f, 7.5f, 8.75f);
    float DistanceFar(float x) => TrapezoidalMembership(x, 7.5f, 10f, Mathf.Infinity, Mathf.Infinity);

    // Membership function for space
    float SpaceWide(float x) => TrapezoidalMembership(x, 0f, 3f, Mathf.Infinity, Mathf.Infinity);

    float Defuzzify(float[] levels, float[] values)
    {
        float numerator = 0f, denominator = 0f;

        for (int i = 0; i < levels.Length; i++)
        {
            numerator += levels[i] * values[i];
            denominator += levels[i];
        }

        return denominator > 0 ? numerator / denominator : 0f;
    }

    void MoveCar()
    {
        speedTurnText.text = "Speed: " + (speed * moveSpeed).ToString() + "\tTurn: " + (turn * turnSpeed).ToString();

        // Apply turning
        transform.Translate(turn * turnSpeed * Time.deltaTime * transform.forward, Space.World);

        // Apply forward movement
        transform.Translate(speed * moveSpeed * Time.deltaTime * -transform.right, Space.World);
    }
}
