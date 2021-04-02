using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMain : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        AssetSystemCore.Instance.Initialize("Assets/Resources", LoadType.AssetBundle);
        //AssetSystemCore.Instance.Initialize("", LoadType.Resource);
        //AssetSystemCore.Instance.Initialize("Assets/Resources", LoadType.AssetDatabase);
    }

    // Update is called once per frame
    void Update()
    {

    }

    List<GameObject> objs = new List<GameObject>();

    void OnGUI()
    {
        if (GUILayout.Button("Load"))
        {
            var go = AssetSystemCore.Instance.Load<GameObject>("Actor/2001_player_wumingdj/2001_player_wumingdj");
            var instance = Instantiate(go, new Vector3(Random.Range(-3f, 3f), 0, 0), Quaternion.identity);
            objs.Add(instance);
        }

        if (GUILayout.Button("LoadAsync"))
        {
            AssetSystemCore.Instance.LoadAsync("Actor/2001_player_wumingdj/2001_player_wumingdj", (go) =>
            {
                objs.Add(Instantiate(go, new Vector3(Random.Range(-3f, 3f), 0, 0), Quaternion.identity) as GameObject);
            });
        }

        if (GUILayout.Button("Unload"))
        {
            foreach (var item in objs)
            {
                Destroy(item);
            }
            objs.Clear();
            AssetSystemCore.Instance.Unload("Actor/2001_player_wumingdj/2001_player_wumingdj");
        }

        if (GUILayout.Button("UnloadAll"))
        {
            foreach (var item in objs)
            {
                Destroy(item);
            }
            objs.Clear();
            AssetSystemCore.Instance.UnloadAll("2001_player_wumingdj_prefab");
        }


    }





}
