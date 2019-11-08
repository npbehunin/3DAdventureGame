using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeDetectionNode : MonoBehaviour
{
    //Ledge detection testing
    private Vector3[] ledgeNodeVectorArray;
    private bool[] ledgeNodeBoolArray;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        TestLedgeDetection();
    }
    
    private void TestLedgeDetection()
    {
        //Send raycasts out.
        int raycastCount = 8;
        ledgeNodeVectorArray = new Vector3[8];
        ledgeNodeVectorArray[0] = (transform.position + Vector3.forward).normalized;
        ledgeNodeBoolArray = new bool[8];
        for (int i = 1; i < ledgeNodeVectorArray.Length; i++)
        {
            //Send out a direction
            ledgeNodeVectorArray[i] = Vector3.Slerp(ledgeNodeVectorArray[i - 1],
                Vector3.Cross(ledgeNodeVectorArray[i - 1], Vector3.up), .5f).normalized;
        }

        for (int i = 0; i < ledgeNodeVectorArray.Length; i++)
        {
            Debug.DrawRay(transform.position, ledgeNodeVectorArray[i], Color.red);
            Debug.DrawRay(ledgeNodeVectorArray[i], Vector3.down, Color.yellow);
        }
    }
}
