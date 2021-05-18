using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

public class FTPUploader
{

    public delegate void FTPOverDelegate();
    public event FTPOverDelegate FTPOverEvent;

    public string FTPHost { get; private set; }
    public string FTPUserName { get; private set; }
    public string FTPPassword { get; private set; }
    private Queue<string> fileQueue;

    public FTPUploader(string host, string userName, string password, string path)
    {
        FTPHost = host;
        FTPUserName = userName;
        FTPPassword = password;
    }



    public void UploadFile(string filePath)
    {
        Debug.Log("Path: " + filePath);
        WebClient client = new System.Net.WebClient();
        Uri uri = new Uri(FTPHost + new FileInfo(filePath).Name);
        client.UploadProgressChanged += new UploadProgressChangedEventHandler(OnFileUploadProgressChanged);
        client.UploadFileCompleted += new UploadFileCompletedEventHandler(OnFileUploadCompleted);
        client.Credentials = new System.Net.NetworkCredential(FTPUserName, FTPPassword);
        client.UploadFileAsync(uri, "STOR", filePath);
    }

    public void UploadFiles(string[] filesPath)
    {
        if (filesPath != null && filesPath.Length > 0)
        {
            fileQueue = new Queue<string>(filesPath.Length);
            foreach (var path in filesPath)
            {
                fileQueue.Enqueue(path);
            }
            UploadFile(fileQueue.Dequeue());
        }
    }

    void OnFileUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
    {
        Debug.Log("Uploading Progreess: " + e.ProgressPercentage);
    }

    void OnFileUploadCompleted(object sender, UploadFileCompletedEventArgs e)
    {
        Debug.Log("File Uploaded");
        if (fileQueue != null && fileQueue.Count > 0)
        {
            UploadFile(fileQueue.Dequeue());
        }
        else
        {
            FTPOverEvent?.Invoke();
        }
    }

}
