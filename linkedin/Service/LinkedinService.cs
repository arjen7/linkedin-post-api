using linkedin.Core;
using LinkedIn.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace linkedin.Service
{
    public class LinkedinService
    {

        public async Task<(string MediaUrn, string UploadUrl)> RegisterMediaUploadAsync(string accessToken, ShareMediaCategoryEnum mediaType)
        {
            var apiUrl = "https://api.linkedin.com/v2/assets?action=registerUpload";

            // Determining the correct recipe based on the mediaType
            var recipe = mediaType switch
            {
                ShareMediaCategoryEnum.Image => "urn:li:digitalmediaRecipe:feedshare-image",
                ShareMediaCategoryEnum.Video => "urn:li:digitalmediaRecipe:feedshare-video",
                _ => throw new ArgumentException("Unsupported media type.")
            };

            var uploadData = new
            {
                registerUploadRequest = new
                {
                    owner = await GetLinkedInUserIdAsync(accessToken), // Example organization URN; replace with the actual URN
                    recipes = new[] { recipe },
                    serviceRelationships = new[]
                    {
                        new
                        {
                            identifier = "urn:li:userGeneratedContent",
                            relationshipType = "OWNER"
                        }
                    },
                    supportedUploadMechanism = new[] { "SYNCHRONOUS_UPLOAD" }
                }
            };

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true // Optional: for better readability
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(uploadData, options);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await httpClient.PostAsync(apiUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var uploadResponse = JsonConvert.DeserializeObject<LinkedInUploadResponse>(responseContent);
                    var uploadUrl = uploadResponse.Value.UploadMechanism.FirstOrDefault().Value.UploadUrl;
                    var mediaUrn = uploadResponse.Value.Asset;
                    return (mediaUrn, uploadUrl);
                }
                else
                {
                    throw new Exception($"Upload URL kaydı başarısız: {responseContent}");
                }
            }
        }

        public async Task UploadMediaToLinkedInAsync(IFormFile mediaFile, string uploadUrl, ShareMediaCategoryEnum mediaType)
        {
            using (var stream = mediaFile.OpenReadStream())
            {
                var contentType = mediaType switch
                {
                    ShareMediaCategoryEnum.Image => "image/jpeg",  // Default image type, can be adjusted based on file extension
                    ShareMediaCategoryEnum.Video => "video/mp4",
                    _ => throw new ArgumentException("Unsupported media type.")
                };

                var mediaContent = new StreamContent(stream);
                mediaContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.PutAsync(uploadUrl, mediaContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Medya yükleme başarısız oldu.");
                    }
                }
            }
        }

        public async Task<string> ShareLinkedInPostAsync(string accessToken, string message, IFormFile mediaFile = null)
        {
            var apiUrl = "https://api.linkedin.com/v2/ugcPosts";

            var mediaUrn = string.Empty;
            var shareMediaCategory = ShareMediaCategoryEnum.None;

            if (mediaFile != null)
            {
                var mediaType = mediaFile.ContentType;
                shareMediaCategory = mediaType.StartsWith("video/") ? ShareMediaCategoryEnum.Video : ShareMediaCategoryEnum.Image;

                var mediaResponse = await RegisterMediaUploadAsync(accessToken,shareMediaCategory );
                mediaUrn = mediaResponse.MediaUrn;

                await UploadMediaToLinkedInAsync(mediaFile, mediaResponse.UploadUrl, shareMediaCategory);
            }

            var shareRequest = new ShareRequest
            {
                Author = await GetLinkedInUserIdAsync(accessToken),
                Visibility = new Visibility { VisibilityEnum = VisibilityEnum.Anyone },
                SpecificContent = new SpecificContent
                {
                    
                    ShareContent = new ShareContent
                    {
                        ShareCommentary = new TextProperties()
                        {
                            Text = message,
                        },
                        ShareMediaCategoryEnum =shareMediaCategory ,
                        Media = string.IsNullOrEmpty(mediaUrn) ? null : new[]
                        {
                            new Media
                            {
                                Description = new TextProperties { Text = "Açıklama" },
                                MediaUrn = mediaUrn,
                                Title = new TextProperties { Text = "Başlık" }
                            }
                        }
                            },
                },
            };

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true // Optional: for better readability
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(shareRequest, options);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await httpClient.PostAsync(apiUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return "Ok";
                }
                else
                {
                    return $"Post paylaşma başarısız: {responseContent}";
                }
            }
        }

        public async Task<string> GetLinkedInUserIdAsync(string accessToken)
        {
            var apiUrl = "https://api.linkedin.com/v2/userinfo";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await httpClient.GetAsync(apiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var meData = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    Console.WriteLine(meData);  
                    string userId = meData.sub;
                    return $"urn:li:person:{userId}";
                }
                else
                {
                    throw new Exception($"Kullanıcı kimliği alınamadı: {responseContent}");
                }
            }
        }

        public async Task<string> GetAccessTokenAsync(string code, string clientId, string clientSecret, string redirectUri)
        {
            var apiUrl = "https://www.linkedin.com/oauth/v2/accessToken";

            var postData = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

            var encodedContent = new FormUrlEncodedContent(postData);

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(apiUrl, encodedContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    Console.WriteLine(tokenResponse);
                    return tokenResponse.access_token;
                }
                else
                {
                    throw new Exception($"Failed to get access token: {responseContent}");
                }
            }
        }

    }
}
