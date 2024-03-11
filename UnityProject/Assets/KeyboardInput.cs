using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInput : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] public float moveSpeed = 4f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.y));
        transform.LookAt(mousePos);

        if (Input.GetKeyDown("shift"))
        {
            moveSpeed = 2f;
        } else {
            moveSpeed = 4f;
        }
    }

    void FixedUpdate()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        //Vector3 moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;

        //Vector3 velocity = moveDirection * moveSpeed;
        //rb.velocity = velocity;

        Vector3 forwardDirection = transform.forward;
        Vector3 movement = (forwardDirection * verticalInput + transform.right * horizontalInput).normalized * moveSpeed;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }
}
