using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public int Damage;
    public static float KnockbackPower;
    
    public Vector3 PlayerDirection;
    public GameObject PlayerObject;
    
    protected virtual void Start()
    {
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //Debug.Log(Thrust);
    }
}
