using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCam : MonoBehaviour
{
    public float moveSpeed = 5.0f; // Adjust the speed as needed
    public float height;

    private void Start()
    {
        transform.position = new Vector3(550f, height, 250f);
        transform.Rotate(25f, -90, 0);
    }
    void Update()
    {
        Vector3 motion = Vector3.zero;
        motion.x = Input.GetAxis("Horizontal");
        
        motion.z = Input.GetAxis("Vertical");

        // Calculate the movement direction based on input

        // Normalize the direction vector to ensure consistent speed in all directions
        //moveDirection.Normalize();

        // Move the GameObject
        transform.position += motion * moveSpeed * Time.deltaTime;
        
    }
}
