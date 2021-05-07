using System.Collections;
using System.Collections.Generic;
using AssetSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMain : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        Asset.Initialize("Assets", LoadType.AssetBundle);
        //AssetSystemCore.Instance.Initialize("", LoadType.Resource);
        //AssetSystemCore.Instance.Initialize("Assets", LoadType.AssetDatabase);
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
            var go = Asset.Load<GameObject>("Res/Actor/2001_player_wumingdj/2001_player_wumingdj");
            var instance = Instantiate(go, new Vector3(Random.Range(-3f, 3f), 0, 0), Quaternion.identity);
            objs.Add(instance);
        }

        if (GUILayout.Button("LoadAsync"))
        {
            Asset.LoadAsync("Res/Actor/2001_player_wumingdj/2001_player_wumingdj", (go) =>
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
            Asset.Unload("Res/Actor/2001_player_wumingdj/2001_player_wumingdj");
        }

        if (GUILayout.Button("UnloadAll"))
        {
            foreach (var item in objs)
            {
                Destroy(item);
            }
            objs.Clear();
            Asset.UnloadAll("Res/2001_player_wumingdj_prefab");
        }

        if (GUILayout.Button("判断资源是否存在"))
        {
            Debug.Log(Asset.ExistAsset("Res/Actor/2001_player_wumingdj/2001_player_wumingdj"));
        }

        // if (GUILayout.Button("同步加载场景"))
        // {
        //     string name = Asset.LoadScene("Scenes/test");
        //     Debug.Log("scene:" + name);
        //     SceneManager.LoadScene(name);
        // }

        // if (GUILayout.Button("异步加载场景"))
        // {
        //     Asset.LoadSceneAsync("Scenes/test", (scene) =>
        //     {
        //         SceneManager.LoadScene(scene);
        //     });
        // }


    }





}
