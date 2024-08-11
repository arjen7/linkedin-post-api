namespace linkedin.Core
{
    public class LinkedinResponse
    {
        public LinkedinResponse(string clientId, string clientSecret, string redirectUri)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            RedirectUri = redirectUri;
        }
    
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
    }

    public class LinkedInUploadResponse
    {
        public UploadValue Value { get; set; }
    }

    public class UploadValue
    {
        public string Asset { get; set; }  // Media URN
        public Dictionary<string, UploadMechanism> UploadMechanism { get; set; }
    }

    public class UploadMechanism
    {
        public string UploadUrl { get; set; }  // Upload URL
    }

}
