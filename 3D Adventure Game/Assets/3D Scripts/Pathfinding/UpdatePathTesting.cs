using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatePathTesting : MonoBehaviour
{
    public GridNew grid;
    public Transform player;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            transform.position = player.position;
            UpdatePathGrid();
        }
    }

    void UpdatePathGrid()
    {
        grid.CreateGrid();
        //grid.OnDrawGizmos();
    }
}
