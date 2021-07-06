using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;

public class Item : MonoBehaviour
{

    public string type;
    public bool isPickedUp = false;
    public int usesLeft = 10;
    public bool isActive = false;

    public Vector3 posOffset;
    public Vector3 rotOffset;

    public int strength = 20;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isPickedUp)
        {
            transform.Rotate(new Vector3(0f, 2.0f, 0f), Space.World);
            if (transform.position.y < -10)
                Destroy(this.gameObject);
        }
    }

    public void pickUp(Transform parent)
    {
        isPickedUp = true;
        transform.SetParent(parent);
        transform.localPosition = posOffset;
        transform.localRotation = Quaternion.Euler(rotOffset);
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public bool Used()
    {
        usesLeft -= 1;
        if (usesLeft <= 0)
            return false;
        return true;
    }

}
