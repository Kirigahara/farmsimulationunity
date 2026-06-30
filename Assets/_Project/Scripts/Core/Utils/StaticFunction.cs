using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public class StaticFunction
{
    public static List<string> goldText = new List<string>
        {
            "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "k", "l", "m", "n"
        };
    public static int RandomInt(int min, int max) => UnityEngine.Random.Range(min, max);
    public static float RandomFloat(float min, float max) => UnityEngine.Random.Range(min, max);
    /// <summary>
    /// Random node value on string with format 0_1_2_3_4_5_....
    /// </summary>
    /// <param name="OriStr">
    /// - Format 0_1_2_3_4_5_6_....
    /// </param>
    /// <returns></returns>
    public static string[] RandomStringArray(string OriStr, char splitCharacter = '_')
    {
        var node = OriStr.Split(splitCharacter);
        if (node.Length == 0)
        {
            Debug.Log("Input is Invalid");
            return node;
        }

        if (node.Length == 1) 
        {
            return node;
        }

        for (int i = 0; i < node.Length; i++)
        {
            int si = RandomInt(0, node.Length);
            string temp = node[i];
            node[i] = node[si];
            node[si] = temp;
        }
        return node;
    }

    /// <summary>
    /// Create Node array on string order
    /// </summary>
    /// <param name="OriStr">
    /// - Format 0_1_2_3_4_5_6_....
    /// </param>
    /// <returns></returns>
    public static string[] ConvertLevelOrder(string OriStr)
    {
        var node = OriStr.Split('_');
        if (node.Length == 0)
        {
            Debug.Log("Input is Invalid");
            return node;
        }

        return node;
    }

    public static string AmountToStringX(int amount) => ("x" + Mathf.Max(0, amount).ToString());
    public static string AmountToStringM(int amount) => (amount.ToString() + "m");
    public static string AmountToString(int amount) => (Mathf.Max(0, amount).ToString());

    /// <summary>
    /// Convert Second(Double) to Hour with format hh mm ss
    /// </summary>
    /// <param name="totalSecond">Double second</param>
    /// <returns>String with format hh mm ss</returns>
    public static string SecondToHour(double totalSecond, bool detail = true)
    {
        int second = (int)totalSecond % 60;

        int totalMinute = ((int)totalSecond - second) / 60;
        int minute = (int)totalMinute % 60;

        int hour = (totalMinute - minute) / 60;

        string content = "";

        if (detail == true)
        {
            if (hour > 0)
            {
                if (hour < 10)
                {
                    content += "0" + hour.ToString() + "h ";
                }
                else
                {
                    content += hour.ToString() + "h ";
                }
            }

            if (minute > 0)
            {
                if (minute < 10)
                {
                    content += "0" + minute.ToString() + "m ";
                }
                else
                {
                    content += minute.ToString() + "m ";
                }
            }

            if (second > 0)
            {
                if (second < 10)
                {
                    content += "0" + second.ToString() + "s";
                }
                else
                {
                    content += second.ToString() + "s";
                }
            }
        }
        else
        {
            if (hour > 0)
            {
                content = 
                    hour.ToString() + ":" + 
                    (minute > 9 ? minute.ToString() : "0" + minute.ToString()) + ":" +
                    (second > 9 ? second.ToString() : "0" + second.ToString());
            }
            else if (totalMinute > 0)
            {
                content = 
                    totalMinute.ToString() + ":" + 
                    (second > 9 ? second.ToString() : "0" + second.ToString());
            }
            else
            {
                content = 
                    (second > 9 ? second.ToString() : "0" + second.ToString()) + "s";
            }
        }

        return content;
    }

    /// <summary>
    /// Convert Second(Double) to Date with format dd hh mm ss
    /// </summary>
    /// <param name="totalSecond">Double second</param>
    /// <param name="Shorten">True is make the result string Short with 2 first Units of time</param>
    /// <returns>
    /// Shorten = false => dd hh mm ss
    /// Shorten = true => dd hh || hh mm || mm ss
    /// </returns>
    public static string SecondToDate(double totalSecond, bool Shorten = true)
    {
        int second = (int)totalSecond % 60;

        int totalMinute = ((int)totalSecond - second) / 60;
        int minute = (int)totalMinute % 60;

        int totalHour = (totalMinute - minute) / 60;
        int hour = (int)totalHour % 24;

        int totalDate = (totalHour - hour) / 24;

        string content = "";
        if (Shorten == true)
        {
            if (totalDate > 0)
            {
                content =
                    (totalDate > 9 ? totalDate.ToString() : "0" + totalDate.ToString()) + "d " +
                    (hour > 9 ? hour.ToString() : "0" + hour.ToString()) + "h";
            }
            else if (totalHour > 0)
            {
                content = 
                    (totalHour > 9 ? totalHour.ToString() : "0" + totalHour.ToString()) + "h " + 
                    (minute > 9 ? minute.ToString() : "0" + minute.ToString()) + "m";
            }
            else if (totalMinute > 0)
            {
                content = 
                    (totalMinute > 9 ? totalMinute.ToString() : "0" + totalMinute.ToString()) + "m " + 
                    (second > 9 ? second.ToString() : "0" + second.ToString()) + "s";
            }
            else
            {
                content = (second > 9 ? second.ToString() : "0" + second.ToString()) + "s";
            }
        }
        else
        {
            if (totalDate < 10)
            {
                content += "0" + totalDate.ToString() + "d ";
            }
            else
            {
                content += totalDate.ToString() + "d ";
            }

            if (hour < 10)
            {
                content += "0" + hour.ToString() + "h ";
            }
            else
            {
                content += hour.ToString() + "h ";
            }

            if (minute < 10)
            {
                content += "0" + minute.ToString() + "m ";
            }
            else
            {
                content += minute.ToString() + "m ";
            }

            if (second < 10)
            {
                content += "0" + second.ToString() + "s";
            }
            else
            {
                content += second.ToString() + "s";
            }
        }
        return content;
    }

    /// <summary>
    /// Convert Second(Double) to Shortest format
    /// </summary>
    /// <param name="totalSecond"></param>
    /// <returns></returns>
    public static string SecondToShortestDate(double totalSecond)
    {
        int second = (int)totalSecond % 60;

        int totalMinute = ((int)totalSecond - second) / 60;
        int minute = (int)totalMinute % 60;

        int totalHour = (totalMinute - minute) / 60;
        int hour = (int)totalHour % 24;

        int totalDate = (totalHour - hour) / 24;

        if (totalDate > 0)
        {
            return totalDate.ToString() + "d ";
        }

        if (totalHour > 0)
        {
            return totalHour.ToString() + "h ";
        }

        if (totalMinute > 0)
        {
            return totalMinute.ToString() + "m ";
        }

        return second.ToString() + "s";
    }

    public static string ConvertDateTimeToMillisecond(DateTime time)
        => time.ToString("yyyyMMddHHmmssfff");
    
    public static string ConvertDateTimeToSecond(DateTime time)
        => time.ToString("yyyyMMddHHmmss");
    

    /// <summary>
    /// Create Parabolic Ratio calculation
    /// </summary>
    /// <param name="Mid">Start Value of the Parabol Mid.x must be 1</param>
    /// <param name="Max">End Value of the Parabol Max.y must be 1</param>
    /// <returns></returns>
    public static NumbericParabolEquation BuffEquation(Vector2 Mid, Vector2 Max)
        => new NumbericParabolEquation(Mid, Max);
    

    /// <summary>
    /// Mix input string randomly with origin string characters
    /// </summary>
    /// <returns>Mixed string</returns>
    public static string MixString(string origin)
    {
        StringBuilder oriStr = new StringBuilder(origin);
        for (int i = 0; i < oriStr.Length; i++)
        {
            int index = UnityEngine.Random.Range(0, oriStr.Length);
            if (i == index)
            {
                index = (index + 1) % oriStr.Length;
            }

            char temp = oriStr[i];
            oriStr[i] = oriStr[index];
            oriStr[index] = temp;
        }
        return oriStr.ToString();
    }


    /// <summary>
    /// Change a Character from a input string
    /// </summary>
    /// <param name="origin">Original String</param>
    /// <param name="index">Index of Character</param>
    /// <param name="value">Character Value</param>
    /// <returns></returns>
    public static string SaveCharString(string origin, int index, char value)
    {
        StringBuilder receiveCode = new StringBuilder(origin);
        if (index >= receiveCode.Length)
        {
            return origin;
        }
        receiveCode[index] = value;

        return receiveCode.ToString();
    }

    /// <summary>
    /// Convert Hex To color
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    public static Color HexToColor(string hex)
    {
        Color newCol;

        if (ColorUtility.TryParseHtmlString(hex, out newCol) == false)
        {
            return Color.gray;
        }
        return newCol;
    }

    /// <summary>
    /// Random to get Ratio on Gaussian Equation
    /// </summary>
    /// <returns></returns>
    public static float Gaussian()
    {
        float rate = UnityEngine.Random.Range(-3.0f, 3.0f);

        return Mathf.Exp(-rate * rate);
    }

    /// <summary>
    /// Get Random to return -1 or 1
    /// </summary>
    /// <returns></returns>
    public static int GetRandomYinYang()
    {
        float value = UnityEngine.Random.Range(0.0f, 360.0f);
        value = Mathf.Sin(value * Mathf.Deg2Rad);
        return (int)Mathf.Sign(value);
    }

    /// <summary>
    /// Convert String to Datetime with format yyyyMMddHHmmss
    /// </summary>
    /// <param name="Require">String needed to be convert</param>
    /// <param name="FallBack">Value if convert fail</param>
    /// <param name="CallBack">Result of convert section (Not require)</param>
    /// <returns></returns>
    public static DateTime StringToDateTime(
        string Require,
        DateTime FallBack,
        Action<bool> CallBack = null)
    {
        DateTime result;
        if (DateTime.TryParseExact(
            Require,
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result) == false)
        {
            CallBack?.Invoke(false);
            return FallBack;
        }

        CallBack?.Invoke(true);
        return result;
    }

    public static DateTime StringToBeginOfDay(
        string Require,
        DateTime FallBack,
        Action<bool> CallBack = null)
    {
        DateTime result;
        if (DateTime.TryParseExact(
            Require,
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result) == false)
        {
            CallBack?.Invoke(false);
            return FallBack;
        }

        CallBack?.Invoke(true);
        return new DateTime(
            result.Year,
            result.Month,
            result.Day,
            0, 0, 0);
    }

    /// <summary>
    /// Convert DateTime to string with format yyyyMMddHHmmss, use for savable data
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static string DateTimeToString(DateTime time) => time.ToString("yyyyMMddHHmmss");
    

    static float _RanVecVal = 0.0f;
    /// <summary>
    /// Get Random Point between 2 point
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <returns>
    /// Vector3 Position between 2 input Vector3 Position
    /// </returns>
    public static Vector3 RandomBetween2Point(Vector3 p0, Vector3 p1)
    {
        Vector3 offset = p1 - p0;
        float min = _RanVecVal * 0.25f;
        float max = (_RanVecVal + 1) * 0.25f;
        _RanVecVal = (_RanVecVal + 1) % 4;

        float x = UnityEngine.Random.Range(
            Mathf.Min(min, max),
            Mathf.Max(min, max));

        Debug.Log(
            "X: " + x.ToString() +
            " Min: " + min.ToString() +
            " Max: " + max.ToString());

        return p0 + offset * x;
    }

    /// <summary>
    /// Get Random Point around a Center point
    /// </summary>
    /// <param name="center">Focus point</param>
    /// <param name="Radius">Radius of circle</param>
    /// <returns></returns>
    public static Vector3 RandomOnCircle(Vector3 center, float Radius)
    {
        Vector3 offset = Vector3.up;
        offset = Quaternion.AngleAxis(RandomFloat(0.0f,360.0f), Vector3.back) * offset;

        return center + offset * RandomFloat(Radius / 0.4f, Radius);
    }

    public static string ConvertGold(double num, string free = "Free")
    {
        int index = 0;

        while (num >= 1000)
        {
            num /= 1000;
            index++;
        }
        //num = Math.Round(num, 2);
        num = Math.Round(num, 1);

        if (!string.IsNullOrEmpty(free) && num <= 0)
        {
            return free;
        }

        return $"{num}{goldText[index]}";
    }

    public static (float, float) RandomPointOnPolygon(Vector3[] polygon)
    {
        int[] index = GetRandomUniqueIndices(3, polygon.Length);
        Vector3 A = polygon[index[0]];
        Vector3 B = polygon[index[1]];
        Vector3 C = polygon[index[2]];

        int selector = RandomInt(0, 90);
        if (selector < 30)
        {
            return RandomPointOnTriangle(A, B, C);
        }
        else if (selector < 60)
        {
            return RandomPointOnTriangle(C, B, A);
        }
        else
        {
            return RandomPointOnTriangle(A, C, B);
        }
    }

    public static (float, float) RandomPointOnTriangle(Vector3 A, Vector3 B, Vector3 C)
    {
        //Debug.DrawLine(A, B, Color.red, 6.0f);
        //Debug.DrawLine(C, B, Color.red, 6.0f);
        //Debug.DrawLine(A, C, Color.red, 6.0f);

        Vector3 D = (A - B) * RandomFloat(0, 1.0f) + B; //D point on AB
        Vector3 E = (C - D) * RandomFloat(0, 1.0f) + D; //E point on CD

        //Debug.DrawLine(A, D, Color.red, 6.0f);
        //Debug.DrawLine(C, E, Color.red, 6.0f);

        return (E.x, E.z);
    }

    /// <summary>
    /// Random 1 điểm trong hình chữ nhật trên mặt phẳng 2D.
    /// Hình chữ nhật định nghĩa bởi tâm, độ dài đường chéo và góc nghiêng so với trục X.
    /// </summary>
    /// <param name="center">Tâm hình chữ nhật.</param>
    /// <param name="diagonal">Độ dài đường chéo.</param>
    /// <param name="angleRad">Góc tạo với trục X (radian). Dùng Mathf.Deg2Rad nếu có góc độ.</param>
    public static (float, float) RandomPointInRectangle(
        float centerX, float centerY, 
        float diagonal, float angleRad)
    {
        // Tính half-width và half-height từ đường chéo và góc
        float halfWidth = (diagonal / 2f) * Mathf.Cos(angleRad);
        float halfHeight = (diagonal / 2f) * Mathf.Sin(angleRad);

        // Random điểm trong hình chữ nhật ở local space (chưa xoay)
        float localX = RandomFloat(-halfWidth, halfWidth);
        float localY = RandomFloat(-halfHeight, halfHeight);

        return(centerX + localX, centerY + localY);
    }

    static int[] GetRandomUniqueIndices(int count, int maxExclusive)
    {
        if (count > maxExclusive)
        {
            Debug.LogError("[GrassArea] count lớn hơn số phần tử có thể chọn!");
            return null;
        }

        // Tạo mảng index ban đầu [0, 1, 2, ..., maxExclusive-1]
        int[] pool = new int[maxExclusive];
        for (int i = 0; i < maxExclusive; i++) pool[i] = i;

        // Fisher-Yates — chỉ shuffle đúng count phần tử đầu, không shuffle toàn bộ
        for (int i = 0; i < count; i++)
        {
            int j = RandomInt(i, maxExclusive);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        // Lấy count phần tử đầu
        int[] result = new int[count];
        System.Array.Copy(pool, result, count);

        //DebugArray(result);
        return result;
    }

    static void DebugArray(int[] array)
    {
        string str = "[";
        for (int i = 0; i < array.Length; i++)
        {
            str += array[i].ToString() + (i < array.Length - 1 ? "," : "");
        }
        str += "]";

        Debug.Log(str);
    }

    public static T GetRandomList<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("[GetRandom] List null hoặc rỗng.");
            return default;
        }
        return list[RandomInt(0, list.Count)];
    }
}

public class NumbericParabolEquation
{
    float _A;
    float _B;
    float _C = 0;

    public NumbericParabolEquation() { }

    public NumbericParabolEquation(Vector2 Mid, Vector2 Max)
    {
        float x1 = Mathf.Max(Mid.x, 1);
        float y1 = Mid.y;

        float x2 = Mathf.Max(Max.x, 1);
        float y2 = Max.y;

        _A = ((y2 * x1) - (y1 * x2)) / (x2 * x1 * (x2 - x1));
        _B = (y1 - _A * x1 * x1) / x1;
        _C = 0;
    }

    public float GetValue(int x) => _A * x * x + _B * x + _C;
    

    /// <summary>
    /// Check for Qualifed
    /// </summary>
    /// <param name="count"></param>
    /// <returns>True is Qualified</returns>
    public bool CheckEquality(int count)
    {
        float condition = GetValue(count) * 100.0f;
        float value = UnityEngine.Random.Range(0.0f, 100.0f);

        if (value <= condition)
        {
            return true;
        }

        return false;
    }

}

[Serializable]
public struct BigNumber
{
    public double Value;      // 1.5
    public int Tier;          // 0=none, 1=K, 2=M, 3=B, 4=T...

    private static readonly string[] _suffix = { "", "K", "M", "B", "T", "Qa", "Qi" };

    public static BigNumber operator +(BigNumber a, BigNumber b)
    {
        // Quy về cùng tier rồi cộng
        double aVal = a.Value * Math.Pow(1000, a.Tier);
        double bVal = b.Value * Math.Pow(1000, b.Tier);
        return FromRaw(aVal + bVal);
    }

    public static BigNumber operator -(BigNumber a, BigNumber b)
    {
        double aVal = a.Value * Math.Pow(1000, a.Tier);
        double bVal = b.Value * Math.Pow(1000, b.Tier);
        return FromRaw(aVal - bVal);
    }

    public static BigNumber FromRaw(double raw)
    {
        if (raw <= 0) return new BigNumber { Value = 0, Tier = 0 };
        int tier = (int)Math.Floor(Math.Log10(raw) / 3);
        tier = Math.Clamp(tier, 0, _suffix.Length - 1);
        return new BigNumber
        {
            Value = raw / Math.Pow(1000, tier),
            Tier = tier
        };
    }

    public bool CanAfford(BigNumber cost)
    {
        double aVal = Value * Math.Pow(1000, Tier);
        double bVal = cost.Value * Math.Pow(1000, cost.Tier);
        return aVal >= bVal;
    }

    public override string ToString()
    {
        return Tier < _suffix.Length
            ? $"{Value:F1}{_suffix[Tier]}"
            : $"{Value:F1}e{Tier * 3}";
    }
}
