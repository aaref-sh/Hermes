namespace HStore.Domain.Settings;

public class LocalStorageSettings
{
    public string BasePath { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string RequestPath { get; set; } = "/files";
}

