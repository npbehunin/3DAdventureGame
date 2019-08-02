using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class DialogueTest2Dialogue : MonoBehaviour
{
    //public DialogueTodorokiEng todoroki;
    
    void Awake()
    {
        DialogueTodorokiEng todoroki = new DialogueTodorokiEng();
        SaveDialogue(todoroki);
        Debug.Log(todoroki);
       // string todorokiEnJson = JsonUtility.ToJson(todorokiEn);
    }
    
    public void SaveDialogue(DialogueTodorokiEng dict)
    {
        //Send out a signal to SaveSystem that passes through this object and fileName.
        string path = System.IO.Path.Combine(Application.persistentDataPath, dict + ".json");

       // string jsontest = JsonUtility.ToJson(hello);
        string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
        
        File.WriteAllText(path, json);
        Debug.Log(json); //Only prints the string "Hello", not the dict.
        
       // using (FileStream stream = new FileStream(path, FileMode.Create))
       // {
       //     
       // }
    } 

    public void LoadDialogue()
    {
        //Same thing, except we return the data back into each of our subclass's strings.
        
        //Run a for loop that takes every string array from our dialogue in unity and sets it to be equal to the string
        //array from the JSON.
    }
}

//TO DO:

//Try to pass in the class itself one more time and get any data to appear in the JSON.
//If that doesn't work, we need to convert the dictionary into strings.

//The format for dictionary strings: "dictKey" + "=" + "dictValue" as one string. Then each of these will make up
//the array, which we caaaan store in json.

//NOTES:

//Still not sure how to implement FileStreams yet. Some tutorials don't use them, and some (like JSON.net) do. For now
//we can just use File.WriteAllText(path, json).