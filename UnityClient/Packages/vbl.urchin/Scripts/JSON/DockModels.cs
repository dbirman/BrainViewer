using System;
[Serializable]
public struct BucketModel
{
    public string Token;
    public string Password;

    public BucketModel(string token, string password)
    {
        Token = token;
        Password = password;
    }
}

[Serializable]
public struct DownloadModel
{
    public string Password;

    public DownloadModel(string password)
    {
        Password = password;
    }
}

[Serializable]
public struct LoadModel
{
    public string Filename;
    public string Bucket;
    public string Password;

    public LoadModel(string filename, string bucket, string password)
    {
        Filename = filename;
        Bucket = bucket;
        Password = password;
    }
}

[Serializable]
public struct SaveModel
{
    public string Filename;
    public string Bucket;
    public string Password;

    public SaveModel(string filename, string bucket, string password)
    {
        Filename = filename;
        Bucket = bucket;
        Password = password;
    }
}

[Serializable]
public struct UploadModel
{
    public string Data;
    public string Password;

    public UploadModel(string data, string password)
    {
        Data = data;
        Password = password;
    }
}

