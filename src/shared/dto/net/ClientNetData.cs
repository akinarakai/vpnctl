public class ClientNetData
{
    public ProtocolType Protocol { get; set; }
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public string? ConfigStr { get; set; }
    public ClientOnlineNetStats? Stats { get; set; }
}