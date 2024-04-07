using System;
[Serializable]
public struct Log
{
    public string Msg;

    public Log(string msg)
    {
        Msg = msg;
    }
}

[Serializable]
public struct LogError
{
    public string Msg;

    public LogError(string msg)
    {
        Msg = msg;
    }
}

[Serializable]
public struct LogWarning
{
    public string Msg;

    public LogWarning(string msg)
    {
        Msg = msg;
    }
}

