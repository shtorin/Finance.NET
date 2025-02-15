namespace Finance.Net.Utilities.ProxySupport;

public class ProxyServer
{
    public int Id { get; set; } 
    public ProxyType ProxyType { get; set; }
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool UseCredentials { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

    public ProxyServer()
    {
        
    }
    public ProxyServer(int id, ProxyType proxyType, string address, int port, bool useCredentials, string username, string password)
    {
        Id = id;
        ProxyType = proxyType;
        Address = address;
        Port = port;
        UseCredentials = useCredentials;
        Username = username;
        Password = password;
    }    
}