namespace Gateway.Models;

public class UploadSettings
{
    public string ImagePath { get; set; }
    public string CacheName { get; set; }
    public string UploadDir { 
        get {
            return Path.Combine(Directory.GetCurrentDirectory(), "./data", ImagePath ?? "BookImages");
        }
    }
}