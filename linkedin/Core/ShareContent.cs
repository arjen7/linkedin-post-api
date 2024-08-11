using System.Text.Json.Serialization;

namespace LinkedIn.Models;

public class ShareContent
{
    private const string ShareMediaCategoryNone = "NONE";
    private const string ShareMediaCategoryArticle = "ARTICLE";
    private const string ShareMediaCategoryImage = "IMAGE";
    private const string ShareMediaCategoryVideo = "VIDEO";

    /// <summary>
    /// Provides the primary content for the share.
    /// </summary>
    [JsonPropertyName("shareCommentary")]
    public TextProperties ShareCommentary { get; set; }

    /// <summary>
    /// Represents the media category attached to the share.
    /// </summary>
    [JsonPropertyName("shareMediaCategory")]
    public string ShareMediaCategory => ShareMediaCategoryEnum switch
    {
        ShareMediaCategoryEnum.None => ShareMediaCategoryNone,
        ShareMediaCategoryEnum.Article => ShareMediaCategoryArticle,
        ShareMediaCategoryEnum.Image => ShareMediaCategoryImage,
        ShareMediaCategoryEnum.Video => ShareMediaCategoryVideo,
        _ => ShareMediaCategoryNone
    };

    /// <summary>
    /// Represents the media assets attached to the share.
    /// </summary>
    [JsonIgnore]
    public ShareMediaCategoryEnum ShareMediaCategoryEnum { get; set; }

    /// <summary>
    /// If the shareMediaCategory is <see cref="ShareMediaCategoryEnum.Article"/>, <see cref="ShareMediaCategoryEnum.Image"/> or <see cref="ShareMediaCategoryEnum.Video"/>, define those media assets here.
    /// </summary>
    [JsonPropertyName("media")]
    public Media[] Media { get; set; }
}
