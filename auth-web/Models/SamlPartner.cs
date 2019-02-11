namespace SarData.Auth.Models
{
  public class SamlPartner
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public string ACS { get; set; }
    public SamlNameIdFormat IdFormat { get; set; }
  }

  public enum SamlNameIdFormat
  {
    Email
  }
}
