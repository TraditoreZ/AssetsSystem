using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class ObjectHelper
{

    private static Stack<Transform> stk = new Stack<Transform>();
    public static Transform FindChildStk(Transform rootTrans, string goName)
    {
        stk.Clear();
        stk.Push(rootTrans);
        Transform tempRoot = null;
        Transform child = null;
        while (stk.Count > 0)
        {
            tempRoot = stk.Pop();
            child = tempRoot.Find(goName);
            if (child != null) return child;
            for (int i = 0; i < tempRoot.childCount; i++)
            {
                child = tempRoot.GetChild(i);
                stk.Push(child);
            }
        }
        return null;
    }

    public static Transform[] FindChildsStk(Transform rootTrans, string goName)
    {
        stk.Clear();
        stk.Push(rootTrans);
        List<Transform> temporaryList = new List<Transform>();
        Transform tempRoot = null;
        Transform child = null;
        while (stk.Count > 0)
        {
            tempRoot = stk.Pop();
            for (int i = 0; i < tempRoot.childCount; i++)
            {
                child = tempRoot.GetChild(i);
                stk.Push(child);
                if (child.name == goName)
                {
                    temporaryList.Add(child);
                }
            }
        }
        return temporaryList.ToArray();
    }


    public static T FindChildStk<T>(Transform rootTrans, string goName)
    {
        Transform tempTrans = FindChildStk(rootTrans, goName);
        if (tempTrans)
            return tempTrans.GetComponent<T>();
        else
            return default(T);
    }

    public delegate bool FindHandler(Component item);
    public static List<T> GetChildren<T>(Transform root, FindHandler handler)
    {
        List<T> temporaryList = new List<T>();
        var childs = root.GetComponentsInChildren<Transform>();

        for (int i = 0; i < childs.Length; i++)
        {
            if (handler(childs[i]))
                temporaryList.Add(childs[i].GetComponent<T>());
        }
        return temporaryList;
    }

    public static List<T> GetList<T>(T[] TArray)
    {
        List<T> temp = new List<T>();
        for (int i = 0; i < TArray.Length; i++)
        {
            temp.Add(TArray[i]);
        }
        return temp;
    }

    public static bool IsArrival(Transform fromTrans, Vector3 targetPos, float arrivalDistance)
    {
        return Vector3.Distance(fromTrans.position, targetPos) <= arrivalDistance;
    }

    public static void LookAtTarget(Vector3 targetDirection, Transform transform, float rotationSpeed)
    {
        if (targetDirection != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed);
        }
    }

    public static Vector3 GetRandomForwardTarget(Transform reqRoot, float distance)
    {
        Vector3 randomForward = reqRoot.forward * 4 + reqRoot.right * UnityEngine.Random.Range(-1f, 1f);
        randomForward = reqRoot.position + randomForward.normalized * distance;
        return randomForward;
    }

}