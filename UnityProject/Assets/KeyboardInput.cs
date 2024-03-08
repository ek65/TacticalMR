using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInput : MonoBehaviour
{
    // Start is called before the first frame update

    public float moveSpeed = 5f;
    private Rigidbody rb;

    void Start()
    {
        Debug.Log("start");
        rb = GetComponent<Rigidbody>();
        Debug.Log(rb);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKeyDown("a"))
        {
            Debug.Log("a key was pressed");
        }

        if (Input.GetKeyDown("d"))
        {
            Debug.Log("d key was pressed");
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * moveSpeed;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
        Debug.Log("horizontal: " + horizontalInput);
        Debug.Log("vertical: " + verticalInput);
    }
}
