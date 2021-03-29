using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMain : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        Debug.Log("asd");
    }

    // Update is called once per frame
    void Update()
    {

    }



    void OnGUI()
    {
        if (GUILayout.Button("Load"))
        {
			AssetSystemCore.Instance.Load(null);
        }
    }





}
