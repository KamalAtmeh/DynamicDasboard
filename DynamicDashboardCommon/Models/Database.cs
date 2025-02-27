public class Database
{
    public int DatabaseID { get; set; }
    public string Name { get; set; }
    public int TypeID { get; set; }
    public string ServerAddress { get; set; }
    public string DatabaseName { get; set; }
    public int? Port { get; set; }
    public string Username { get; set; }
    public string EncryptedCredentials { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public bool LastConnectionStatus { get; set; }
}