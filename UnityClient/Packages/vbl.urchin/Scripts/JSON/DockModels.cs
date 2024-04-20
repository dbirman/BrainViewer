using System;
[Serializable]
public struct BucketRequest
{
    public string Token;
    public string Password;

    public BucketRequest(string token, string password)
    {
        Token = token;
        Password = password;
    }
}

[Serializable]
public struct DockModel
{
    public string DockUrl;

    public DockModel(string dockUrl)
    {
        DockUrl = dockUrl;
    }
}

[Serializable]
public struct DownloadRequest
{
    public string Password;

    public DownloadRequest(string password)
    {
        Password = password;
    }
}

[Serializable]
public struct DownloadResponse
{
    public string Type;
    public string Data;

    public DownloadResponse(string type, string data)
    {
        Type = type;
        Data = data;
    }
}

[Serializable]
public struct LoadModel
{
    public int[] Types;
    public string[] Data;

    public LoadModel(int[] types, string[] data)
    {
        Types = types;
        Data = data;
    }
}

[Serializable]
public struct LoadRequest
{
    public string Filename;
    public string Bucket;
    public string Password;

    public LoadRequest(string filename, string bucket, string password)
    {
        Filename = filename;
        Bucket = bucket;
        Password = password;
    }
}

[Serializable]
public struct SaveRequest
{
    public string Filename;
    public string Bucket;
    public string Password;

    public SaveRequest(string filename, string bucket, string password)
    {
        Filename = filename;
        Bucket = bucket;
        Password = password;
    }
}

[Serializable]
public struct UploadRequest
{
    public int Type;
    public string Data;
    public string Password;

    public UploadRequest(int type, string data, string password)
    {
        Type = type;
        Data = data;
        Password = password;
    }
}

