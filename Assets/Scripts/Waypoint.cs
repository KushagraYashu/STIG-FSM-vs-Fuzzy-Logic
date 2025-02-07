using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public Waypoint nextWaypoint; // Reference to the next waypoint in the path

    // Visualize the waypoint and its connection to the next waypoint in the Editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan; // Set the gizmo color to cyan

        // Draw a sphere to represent the waypoint
        Gizmos.DrawSphere(transform.position, 0.5f);

        // Draw a line to the next waypoint if it exists
        if (nextWaypoint != null)
        {
            Gizmos.DrawLine(transform.position, nextWaypoint.transform.position);
        }
    }
}