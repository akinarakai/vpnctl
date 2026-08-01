public class ProfileStorage
{
    public string? CurrentProfile { get; set; }
    public List<ServerProfile> Profiles { get; set; } = new();
}