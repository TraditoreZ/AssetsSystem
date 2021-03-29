
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MathHelper
{
    /// <summary> 浮点数转定点数 </summary>
    /// <param name="value">值</param>
    /// <param name="accuracy">精度</param>
    /// <returns>定点数</returns>
    public static float FixedPoint(float value, int accuracy = 3)
    {
        int magnification = 1;
        for (int i = 0; i < accuracy; i++)
        {
            magnification = magnification * 10;
        }
        return (Mathf.Floor(value * magnification)) / magnification;
    }

    /// <summary> 浮点数转定点数 </summary>
    /// <param name="value">值</param>
    /// <param name="accuracy">精度</param>
    /// <returns>定点数</returns>
    public static Vector3 FixedPoint(Vector3 value, int accuracy = 3)
    {
        return new Vector3(FixedPoint(value.x, accuracy), FixedPoint(value.y, accuracy), FixedPoint(value.z, accuracy));
    }

    /// <summary> 浮点数转定点数 </summary>
    /// <param name="value">值</param>
    /// <param name="accuracy">精度</param>
    /// <returns>定点数</returns>
    public static Quaternion FixedPoint(Quaternion value, int accuracy = 3)
    {
        return new Quaternion(FixedPoint(value.x, accuracy), FixedPoint(value.y, accuracy), FixedPoint(value.z, accuracy), FixedPoint(value.w, accuracy));
    }

    /// <summary> 浮点数四舍五入 </summary>
    /// <param name="value">值</param>
    /// <param name="accuracy">精度</param>
    /// <returns>四舍五入后的值</returns>
    public static float Round(float value, int accuracy = 0)
    {
        int magnification = 1;
        for (int i = 0; i < accuracy; i++)
        {
            magnification = magnification * 10;
        }
        return Mathf.Round(value * magnification) / magnification;
    }

    /// <summary> 随机一个圆(内聚合, 外松散) </summary>
    /// <param name="radomAngle">随机角度</param>
    /// <param name="radomradius">随机半径</param>
    /// <returns>随机点</returns>
    public static Vector2 RadomCircle(float radomAngle, float radomradius)
    {
        float x = 0, y = 0;
        if (radomAngle >= 0 && radomAngle < 90)
        {
            x = Mathf.Sin(radomAngle * Mathf.Deg2Rad) * radomradius;
            y = Mathf.Cos(radomAngle * Mathf.Deg2Rad) * radomradius;
        }
        else if (radomAngle >= 90 && radomAngle < 180)
        {
            x = Mathf.Sin((180 - radomAngle) * Mathf.Deg2Rad) * radomradius;
            y = Mathf.Cos((180 - radomAngle) * Mathf.Deg2Rad) * radomradius;
        }
        else if (radomAngle >= 180 && radomAngle < 270)
        {
            x = Mathf.Sin((270 - radomAngle) * Mathf.Deg2Rad) * radomradius;
            y = Mathf.Cos((270 - radomAngle) * Mathf.Deg2Rad) * radomradius;
        }
        else if (radomAngle >= 270 && radomAngle <= 360)
        {
            x = Mathf.Sin((360 - radomAngle) * Mathf.Deg2Rad) * radomradius;
            y = Mathf.Cos((360 - radomAngle) * Mathf.Deg2Rad) * radomradius;
        }
        return new Vector2(x, y);
    }


    /// <summary>
    /// 获取单个随机值
    /// </summary>
    /// <param name="setting">string 为 随机的Key value 为随机概率</param>
    /// <returns></returns>
    public static int RadomValues(Dictionary<int, float> setting)
    {
        float total = 0;
        foreach (int key in setting.Keys)
        {
            total += setting[key];  //计算总概率值
        }

        float r = UnityEngine.Random.Range(0, 1f) * total;   //取一个随机数，乘以总概率值，映射到总概率值的区间内
        total = 0;
        foreach (int key in setting.Keys)
        {
            total += setting[key];  //按顺序累加概率值
            if (total > r)  //如果前面随机的数在对应区间内，则返回该数
            {
                return key;
            }
        }

        return setting.Keys.Last<int>();    //返回最后一个数，对应rand.NextDouble()随机到1.0的情况
    }

    /// <summary> 四元数求物体Z轴正方向 </summary>
    /// <param name="qua">角度</param>
    /// <returns>正方向</returns>
    public static Vector3 QuaternionToForward(Quaternion qua)
    {
        return new Vector3(Mathf.Sin(qua.eulerAngles.y * Mathf.Deg2Rad) * Mathf.Cos(qua.eulerAngles.x * Mathf.Deg2Rad), -Mathf.Sin(qua.eulerAngles.x * Mathf.Deg2Rad), Mathf.Cos(qua.eulerAngles.y * Mathf.Deg2Rad) * Mathf.Cos(qua.eulerAngles.x * Mathf.Deg2Rad));
    }

    ///<summary> 四元数求物体左方向 </summary>
    ///<param name="qua"> 角度 </param>
    /// <returns>左方向</returns>
    public static Vector3 QuaternionToRight(Quaternion qua)
    {
        return new Vector3(Mathf.Cos(qua.eulerAngles.y * Mathf.Deg2Rad) * Mathf.Cos(qua.eulerAngles.z * Mathf.Deg2Rad), -Mathf.Sin(qua.eulerAngles.z * Mathf.Deg2Rad), -Mathf.Sin(qua.eulerAngles.y * Mathf.Deg2Rad) * Mathf.Cos(qua.eulerAngles.z * Mathf.Deg2Rad));
    }

    ///<summary> 四元数求物体上方向 </summary>
    ///<param name="qua"> 角度 </param>
    /// <returns>上方向</returns>
    public static Vector3 QuaternionToUp(Quaternion qua)
    {
        return new Vector3(Mathf.Sin(qua.eulerAngles.y * Mathf.Deg2Rad) * Mathf.Sin(qua.eulerAngles.x * Mathf.Deg2Rad), Mathf.Cos(qua.eulerAngles.x * Mathf.Deg2Rad), Mathf.Sin(qua.eulerAngles.x * Mathf.Deg2Rad) * Mathf.Cos(qua.eulerAngles.y * Mathf.Deg2Rad));
    }
}
