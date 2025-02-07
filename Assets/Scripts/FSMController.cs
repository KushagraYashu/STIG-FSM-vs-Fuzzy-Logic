using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class FSMController : MonoBehaviour
{
    private string fileName = "Time.txt"; // File to store simulation results

    public float time; // Elapsed time during the simulation

    private bool FSM = true; // Toggle for enabling/disabling FSM
    private string name = "FSM"; // Name identifier for the FSM

    // FSM states
    public enum States
    {
        Idle,           // Do nothing
        MoveForward,    // Move forward
        AvoidLeft,      // Avoid obstacle by shifting left
        AvoidRight      // Avoid obstacle by shifting right
    }

    public States currentState = States.Idle; // Current FSM state
    public TextMeshProUGUI currentStateTxt; // Current FSM state visual

    public float moveSpeed = 10f; // Speed of the car
    public Vector3 boxSize; // Size of the box used for calculations

    public Transform carFront; // Front point for obstacle detection

    public Waypoint curWaypoint; // Current waypoint for navigation
    public float distanceToWaypoint; // Distance to the current waypoint
    public float waypointThreshold; // Threshold distance to switch waypoints

    public float timeToRot = 5f; // Time interval for rotation
    private float curTimeToRot = 0f; // Current rotation timer

    [SerializeField] private int collisions = 0; // Number of collisions recorded
    [SerializeField] private TextMeshProUGUI collisionsTxt; // UI text for displaying collisions

    public Bounds leftBounds; // Bounds for left detection
    public Bounds rightBounds; // Bounds for right detection
    public Bounds frontBounds; // Bounds for front detection

    [SerializeField] private float shiftDistance; // Distance to shift during obstacle avoidance

    void Start()
    {
        currentState = States.MoveForward; // Initialize FSM state
    }

    void FixedUpdate()
    {
        if (FSM)
        {
            HandleStates();

            if (curTimeToRot >= timeToRot)
            {
                curTimeToRot = 0f;
                if (currentState != States.AvoidLeft && currentState != States.AvoidRight)
                {
                    RotateToWaypoint();
                }
            }

            if (currentState != States.AvoidLeft && currentState != States.AvoidRight)
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
            currentState = States.Idle;
            currentStateTxt.text = "Current State: " + currentState.ToString();

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return; // Skip if script is disabled

        if (other.CompareTag("waypoint"))
        {
            curWaypoint = other.GetComponentInParent<Waypoint>().nextWaypoint; // Update waypoint
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
            Timer.instance.timer = true; // Start the timer
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enabled) return; // Skip if script is disabled

        if (other.CompareTag("FinishLine"))
        {
            Timer.instance.timer = false; // Stop the timer
            FSM = false; // Disable FSM
            time = Timer.instance.getElapsedTime();

            // Save results to file
            using (var sw = File.Exists(fileName) ? new StreamWriter(fileName, true) : File.CreateText(fileName))
            {
                sw.WriteLine($"{SceneManager.GetActiveScene().name}\n{name}\nTime: {time}\nCollisions: {collisions}");
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; // Set Gizmo color

        if (curWaypoint != null)
        {
            Gizmos.DrawLine(carFront.position, curWaypoint.transform.position); // Draw line to waypoint
        }
    }

    void HandleStates()
    {
        currentStateTxt.text = "Current State: " + currentState.ToString();
        switch (currentState)
        {
            case States.Idle:
                break;

            case States.MoveForward:
                MoveForward();
                break;

            case States.AvoidLeft:
                AvoidLeft();
                break;

            case States.AvoidRight:
                AvoidRight();
                break;
        }
    }

    void RotateToWaypoint()
    {
        if (curWaypoint == null) return; // Skip if no waypoint

        Vector3 direction = curWaypoint.transform.position - transform.position;
        direction.y = 0; // Ignore height differences

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            targetRotation *= Quaternion.Euler(0, 90, 0);
            transform.rotation = targetRotation; // Apply rotation
        }
    }

    public void CheckObstacles()
    {
        bool left = leftBounds.inCollision;
        bool right = rightBounds.inCollision;
        shiftDistance = frontBounds.colliderWidth / 2 + boxSize.x / 2; // Calculate shift distance

        if (!left)
        {
            currentState = States.AvoidLeft; // Shift left if possible
        }
        else if (!right)
        {
            currentState = States.AvoidRight; // Shift right if possible
        }
        else
        {
            Debug.LogError("Couldn’t find a way, going through");
            currentState = States.MoveForward; // Default to moving forward
        }
    }

    bool PathClear()
    {
        return frontBounds.inCollision; // Check if the path ahead is clear
    }

    void MoveForward()
    {
        transform.Translate(moveSpeed * Time.deltaTime * -transform.right, Space.World); // Move forward
    }

    void AvoidLeft()
    {
        if (shiftDistance == 0)
        {
            currentState = States.MoveForward; // Stop avoiding if no shift distance
            return;
        }

        transform.Translate(shiftDistance * Time.deltaTime * -transform.forward, Space.World); // Shift left

        if (!PathClear())
        {
            currentState = States.MoveForward; // Resume forward movement
        }

        curTimeToRot = 0;
    }

    void AvoidRight()
    {
        if (shiftDistance == 0)
        {
            currentState = States.MoveForward; // Stop avoiding if no shift distance
            return;
        }

        transform.Translate(shiftDistance * Time.deltaTime * transform.forward, Space.World); // Shift right

        if (!PathClear())
        {
            currentState = States.MoveForward; // Resume forward movement
        }

        curTimeToRot = 0;
    }
}