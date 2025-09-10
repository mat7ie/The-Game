using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ControlManager : MonoBehaviour
{
    private CharacterController characterController;
    private Animator animator;
    private bool isHoldingObject = false;
    private GameObject heldObject;

    [Header("Movement speed")]
    public float speed = 2.0f;
    public float rotationSpeed = 45.0f; // Degrees per second
    public float gravity = -9.81f; // Gravity force

    private Vector3 moveDirection = Vector3.zero;
    private bool isWalking = false;

    [Header("Respawn Settings")]
    public Transform spawnPoint; // Reference to the spawn point

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // Handle continuous movement
        if (Input.GetKey(KeyCode.W))
        {
            StartWalking();
        }
        else if (Input.GetKey(KeyCode.S))
        {
            StopWalking();
        }

        // Apply gravity
        moveDirection.y += gravity * Time.deltaTime;

        // Move the character
        characterController.Move(moveDirection * Time.deltaTime);

        // Handle rotation
        if (Input.GetKeyDown(KeyCode.A))
        {
            TurnLeft();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            TurnRight();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            PickUpObject();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            PutDownObject();
        }

        // Check if the player has fallen off the platform
        if (transform.position.y < -10)
        {
            Respawn();
        }
    }

    public void MoveCharacter(string command)
    {
        switch (command.ToLower())
        {
            case "go":
                StartWalking();
                break;
            case "stop":
                StopWalking();
                break;
            case "left":
                TurnLeft();
                break;
            case "right":
                TurnRight();
                break;
            case "up":
                PickUpObject();
                break;
            case "down":
                PutDownObject();
                break;
        }
    }

    private void StartWalking()
    {
        isWalking = true;
        moveDirection = transform.forward * speed;
        animator.SetBool("isWalking", true);
    }

    private void StopWalking()
    {
        isWalking = false;
        moveDirection = Vector3.zero;
        animator.SetBool("isWalking", false);
    }

    private void TurnLeft()
    {
        transform.Rotate(0, -45, 0);
        if (isWalking)
        {
            moveDirection = transform.forward * speed;
        }
    }

    private void TurnRight()
    {
        transform.Rotate(0, 45, 0);
        if (isWalking)
        {
            moveDirection = transform.forward * speed;
        }
    }

    private void PickUpObject()
    {
        if (!isHoldingObject)
        {
            RaycastHit hit;
            float pickupRange = 2.5f; // Increased range for better detection
            Vector3 rayOrigin = transform.position + Vector3.up * 0.2f; // Adjust the ray origin to be slightly above the ground

            Debug.DrawRay(rayOrigin, transform.forward * pickupRange, Color.green, 2.0f); // Visualize the raycast in the scene view

            if (Physics.Raycast(rayOrigin, transform.forward, out hit, pickupRange))
            {
                if (hit.collider.CompareTag("Pickup"))
                {
                    heldObject = hit.collider.gameObject;
                    heldObject.transform.SetParent(transform);
                    heldObject.transform.localPosition = new Vector3(0, 0.673f, -0.005f);
                    isHoldingObject = true;
                    animator.SetTrigger("PickUp");
                }
            }
        }
    }

    private void PutDownObject()
    {
        if (isHoldingObject)
        {
            heldObject.transform.SetParent(null);
            heldObject.transform.position = transform.position + 0.5f * transform.forward;
            isHoldingObject = false;
            heldObject = null;
        }
    }

    private void Respawn()
    {
        // Disable the CharacterController to allow manual position update
        characterController.enabled = false;

        // Move the player to the spawn point
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        // Re-enable the CharacterController
        characterController.enabled = true;

        // Reset movement and animation states
        moveDirection = Vector3.zero;
        isWalking = false;
        animator.SetBool("isWalking", false);
    }
}