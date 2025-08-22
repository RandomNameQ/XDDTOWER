using UnityEngine;

public static class q
{
    public static int value;
    public static void log()
    {
        Debug.Log("log: " + value);
        value++;
    }

    public static void log(int str = default)
    {
        if (str == default)
        {
            Debug.Log("log: " + value);
            value++;
        }
        else
            Debug.Log(str);


    }


}
