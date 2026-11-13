namespace Bloom.Core.Constants;

public class WebPushOptions
{
    public const string SectionName = "WebPush";

    public string Subject { get; set; } = "mailto:support@bloom.cat";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}

