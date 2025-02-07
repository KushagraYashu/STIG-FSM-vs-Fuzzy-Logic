using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bounds : MonoBehaviour
{
    // References to FSM and Fuzzy Logic Controllers in the parent GameObject
    private FSMController fsmController;
    private FuzzyLogicController fuzzyLogicController;

    [SerializeField] private LayerMask defaultLayerMask; // Layer mask for collision detection

    public bool inCollision; // Tracks whether the object is in collision

    public bool isFront; // Determines if this is the front detector

    public float colliderWidth; // Width of the detected collider

    public GameObject obstacle; // Reference to the detected obstacle

    [SerializeField] private string tagName; // Tag name to filter collisions

    private Renderer renderer; // Renderer for visual debugging
    private Color iniColor; // Initial color of the object

    // Initialize references and settings
    void Start()
    {
        fsmController = gameObject.GetComponentInParent<FSMController>();
        fuzzyLogicController = gameObject.GetComponentInParent<FuzzyLogicController>();
        renderer = gameObject.GetComponent<Renderer>();
        iniColor = renderer.material.color; // Store the initial color
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFront)
        {
            // Handle front detector logic
            if (other.CompareTag(tagName))
            {
                renderer.material.color = Color.red; // Visual feedback for collision
                inCollision = true;
                colliderWidth = other.bounds.size.x; // Store the width of the collider

                // Notify FSM Controller to handle obstacle detection
                if (fsmController != null)
                {
                    fsmController.CheckObstacles();
                }

                // Notify Fuzzy Logic Controller to update obstacle reference
                if (fuzzyLogicController != null)
                {
                    fuzzyLogicController.obstacle = other.gameObject;
                }
            }
        }
        else
        {
            // Handle side detector logic
            if (((1 << other.gameObject.layer) & defaultLayerMask) != 0)
            {
                Debug.Log($"Collided with: {other.gameObject.name}");
                renderer.material.color = Color.red; // Visual feedback for collision
                inCollision = true;
                obstacle = other.gameObject; // Store reference to the obstacle
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isFront)
        {
            // Handle exit logic for front detector
            if (other.CompareTag(tagName))
            {
                inCollision = false; // Reset collision state
                renderer.material.color = iniColor; // Restore initial color

                if (fuzzyLogicController != null)
                {
                    fuzzyLogicController.obstacle = null; // Clear obstacle reference
                }
            }
        }
        else
        {
            // Handle exit logic for side detector
            if (((1 << other.gameObject.layer) & defaultLayerMask) != 0)
            {
                renderer.material.color = iniColor; // Restore initial color
                inCollision = false; // Reset collision state
                obstacle = null; // Clear obstacle reference
            }
        }
    }
}