using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewListOfVector3", menuName = "ScriptableObject/List (Vector3)")]
public class ListVector3 : ScriptableObject
{
    public List<Vector3> list = new List<Vector3>();
}
