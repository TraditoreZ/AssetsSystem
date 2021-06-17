using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using AssetSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class GameMain : MonoBehaviour
{

    public GameObject login;

    public GameObject hotdown;

    public Button startBtn;

    public Button hotdownBtn;

    public Slider progress;

    public Text progressText;

    public Button clearResBtn;

    public Button quitBtn;

    public RectTransform canvas;

    public VideoPlayer videoPlayer;

    // Use this for initialization
    void Start()
    {
        Asset.Initialize("Assets", LoadType.AssetBundle);
        //AssetSystemCore.Instance.Initialize("", LoadType.Resource);
        //AssetSystemCore.Instance.Initialize("Assets", LoadType.AssetDatabase);

        AssetDownload.instance.DownloadEvent += DownloadCallBack;
        AssetDownload.instance.ProcessEvent += ProcessCallBack;
        AssetDownload.instance.HotDownloadOverEvent += DownloadOverCallback;
        AssetDownload.instance.ErrorEvent += HotDownErrorCall;
        hotdownBtn.onClick.AddListener(HotDown);
        startBtn.onClick.AddListener(StartCall);
        clearResBtn.onClick.AddListener(clearCallback);
        quitBtn.onClick.AddListener(quitCallback);
    }

    private void quitCallback()
    {
        Application.Quit();
    }

    private void HotDownErrorCall(string error)
    {
        Debug.LogError(error);
    }

    private void clearCallback()
    {
        System.IO.Directory.Delete(Application.persistentDataPath, true);
        System.IO.Directory.CreateDirectory(AssetBundlePathResolver.instance.GetBundlePersistentFile());
    }

    private void StartCall()
    {
        login.SetActive(false);
    }

    private void DownloadOverCallback()
    {
        progress.value = 1;
        SceneManager.LoadScene("main");
    }

    private void HotDown()
    {
        AssetDownload.ResourceUpdateOnRemote("http://192.168.11.20:8080/assetbundle", new BaseHotDownload());
        hotdown.SetActive(true);
        startBtn.gameObject.SetActive(false);
        hotdownBtn.gameObject.SetActive(false);
        clearResBtn.gameObject.SetActive(false);
        progress.value = 0;
    }

    void OnEnable()
    {
        login.SetActive(true);
        hotdown.SetActive(false);
        startBtn.gameObject.SetActive(true);
        hotdownBtn.gameObject.SetActive(true);
        clearResBtn.gameObject.SetActive(true);
        videoPlayer.gameObject.SetActive(false);
    }

    private void DownloadCallBack(EHotDownloadProgress progress)
    {
        Debug.Log("[AssetDownload] => " + progress);
        progressText.text = progress.ToString();
    }

    private void ProcessCallBack(string assetName, long currtSize, long maxSize, int index, int count)
    {
        progress.value = (float)currtSize / maxSize;
        progressText.text = string.Format("{0} kb / {1} kb  {2:f2}%    {3:f2} mb/s", currtSize / 1024, maxSize / 1024, ((float)currtSize / maxSize) * 100, AssetDownload.instance.downloader.DownloadSpeed() / (1024 * 1024));
    }



    List<GameObject> objs = new List<GameObject>();

    void OnGUI()
    {
        if (login.activeSelf)
        {
            return;
        }
        if (GUILayout.Button("Load", GUILayout.Width(200), GUILayout.Height(70)))
        {
            var go = Asset.Load<GameObject>("Res/Actor/2001_player_wumingdj/2001_player_wumingdj");
            var instance = Instantiate(go, new Vector3(Random.Range(-3f, 3f), 0, 0), Quaternion.identity);
            objs.Add(instance);
        }

        if (GUILayout.Button("LoadAsync", GUILayout.Width(200), GUILayout.Height(70)))
        {
            Asset.LoadAsync("Res/Actor/2001_player_wumingdj/2001_player_wumingdj", (go) =>
             {
                 objs.Add(Instantiate(go, new Vector3(Random.Range(-3f, 3f), 0, 0), Quaternion.identity) as GameObject);
             });
        }

        if (GUILayout.Button("Unload", GUILayout.Width(200), GUILayout.Height(70)))
        {
            foreach (var item in objs)
            {
                Destroy(item);
            }
            objs.Clear();
            Asset.Unload("Res/Actor/2001_player_wumingdj/2001_player_wumingdj");
        }

        if (GUILayout.Button("UnloadAll", GUILayout.Width(200), GUILayout.Height(70)))
        {
            foreach (var item in objs)
            {
                Destroy(item);
            }
            objs.Clear();
            Asset.UnloadAll("Res/2001_player_wumingdj_prefab");
        }

        if (GUILayout.Button("返回大厅", GUILayout.Width(200), GUILayout.Height(70)))
        {
            foreach (var item in objs)
            {
                Destroy(item);
            }
            objs.Clear();
            Asset.UnloadAll("Res/2001_player_wumingdj_prefab");
            SceneManager.LoadScene("main");
        }
        if (GUILayout.Button("加载视频", GUILayout.Width(200), GUILayout.Height(70)))
        {
            videoPlayer.gameObject.SetActive(true);
            if (File.Exists(AssetBundlePathResolver.instance.GetBundlePersistentFile("customvideo/movie.mp4")))
            {
                Debug.Log("存在资源");
                videoPlayer.url = AssetBundlePathResolver.instance.GetBundlePersistentFile("customvideo/movie.mp4", true);
            }
            videoPlayer.Play();
        }
        // if (GUILayout.Button("热更新"))
        // {
        //     //AssetDownload.ResourceUpdateOnRemote(@"E:\AssetsSystem\HotDownload", new BaseHotDownload());
        //     AssetDownload.ResourceUpdateOnRemote("http://192.168.11.20:8080/assetbundle", new BaseHotDownload());
        // }
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

    private byte[] ConvetToObj(object obj)
    {
        BinaryFormatter se = new BinaryFormatter();
        MemoryStream memStream = new MemoryStream();
        se.Serialize(memStream, obj);
        byte[] bobj = memStream.ToArray();
        memStream.Close();
        return bobj;

    }



}
