using linkedin.Core;
using linkedin.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace linkedin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinkedinController : ControllerBase
    {
        private readonly LinkedinResponse _linkedinResponse;
        private readonly LinkedinService _linkedinService;
        private  LinkedInAccessToken linkedInAccessToken { get; set; }
        public LinkedinController(LinkedinResponse linkedinResponse, LinkedinService linkedinService, LinkedInAccessToken linkedInAccessToken)
        {
            _linkedinResponse = linkedinResponse;
            _linkedinService = linkedinService;
            this.linkedInAccessToken = linkedInAccessToken;
        }

        [HttpGet]
        public IActionResult GetLinkedInAuthorization()
        {
            var clientId = _linkedinResponse.ClientId;
            var redirectUri = Uri.EscapeDataString(_linkedinResponse.RedirectUri); // URL kodlaması yapıyoruz
            var scope = "openid%20profile%20w_member_social%20email";
            var state = Guid.NewGuid().ToString(); // Güvenlik için rastgele bir dize

            var authUrl = $"https://www.linkedin.com/oauth/v2/authorization?response_type=code" +
                          $"&client_id={clientId}" +
                          $"&redirect_uri={redirectUri}" +
                          $"&scope={scope}" +
                          $"&state={state}"; // state parametresini ekliyoruz

            return Redirect(authUrl);
        }


        [HttpGet("callback")]
        public async Task<IActionResult> LinkedInCallbackAsync([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Authorization code not provided.");
            }

            try
            {
                // Erişim token'ını almak için gerekli bilgiler
                var clientId = _linkedinResponse.ClientId;
                var clientSecret = _linkedinResponse.ClientSecret;
                var redirectUri = _linkedinResponse.RedirectUri;

                var accessToken = await _linkedinService.GetAccessTokenAsync(code, clientId, clientSecret, redirectUri);
                linkedInAccessToken.AccessToken = accessToken;
                return Ok(new { AccessToken = accessToken });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting access token: {ex.Message}");
            }
        }

        [HttpPost("share-post")]
        public async Task<IActionResult> SharePostAsync([FromForm] SharePostResponse response)
        {
            var accessToken =linkedInAccessToken.AccessToken;

            try
            {

                var result=await _linkedinService.ShareLinkedInPostAsync(accessToken, response.Message,response.Media);
                if (result == "Ok") 
                {
                    return Ok();
                }
                return BadRequest(result);  
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

}
