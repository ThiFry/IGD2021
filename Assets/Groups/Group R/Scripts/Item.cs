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
        freezePosition();
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

    void freezePosition()
    {
        GameObject gameObj = GameObject.FindWithTag("Player");
        GameObject item = GameObject.FindWithTag("Item");
        Vector3 pos = item.transform.position;
        BoxCollider collider = item.GetComponent<BoxCollider>();
        Physics.IgnoreLayerCollision(18, 18);

        if (!gameObj.GetComponent<OurMinifigController>().hasItem)
        {
            if (!isPickedUp)
            {
                item.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY;
                Physics.IgnoreLayerCollision(18, 9, false);


            }
            else if(isPickedUp)
            {
                Physics.IgnoreLayerCollision(18, 9,true);
              
            }
        }
        
    }
}