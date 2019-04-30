using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraySword : Sword
{
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        Damage = 1;
    }
    
    void OnEnable()
    {
        Debug.Log("Gray Sword Active");
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }
}
