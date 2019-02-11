namespace SarData.Auth.Models
{
  public class OidcConfig
  {
    public string Id { get; set; }
    public string Caption { get; set; }
    public string Icon { get; set; }
    public string IconColor { get; set; }
    public string Authority { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public bool Trusted { get; set; }
  }
}
