using EPiServer.Core;
using EPiServer.Web;
using Geta.Optimizely.Sitemaps.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Sitemaps.Utils;

public class VssContentFilter : ContentFilter, IVssContentFilter
{
    public VssContentFilter(TemplateResolver templateResolver, ILogger<ContentFilter> logger, IOptions<SitemapOptions> options)
        : base(templateResolver, logger, options) { }

    public override bool ShouldExcludeContent(IContent content)
    {
        string PrintContent(string why)
        {
            return $"Skipping {why} '{content.ContentLink.ID}: {content.Name}";
        }

        if (content == null)
        {
            return true;
        }

        if (content.IsDeleted)
        {
            _logger.LogInformation(PrintContent("deleted"));
            return true;
        }

        if (!IsPublished(content))
        {
            _logger.LogInformation(PrintContent("unpublished"));
            return true;
        }

        if (!IsSitemapPropertyEnabled(content))
        {
            _logger.LogInformation(PrintContent("SEOdisabled"));
            return true;
        }

        if (IsAutoArchived(content))
        {
            _logger.LogInformation(PrintContent("autoarchived"));
            return true;
        }

        if (!IsVisibleOnSite(content))
        {
            _logger.LogInformation(PrintContent("not visisble"));
            return true;
        }

        if (content.ContentLink.CompareToIgnoreWorkID(ContentReference.WasteBasket))
        {
            _logger.LogInformation(PrintContent("ignored"));
            return true;
        }

        if (content is BlockData || content is MediaData)
        {
            _logger.LogInformation(PrintContent("non-pagedata"));
            return true;
        }

        var page = content as PageData;

        if (page != null && IsLink(page))
        {
            _logger.LogInformation(PrintContent("external"));
            return true;
        }

        return false;
    }

    private static bool IsAutoArchived(IContentData content)
    {
        if (content is PageData page)
        {
            var property = content.Property["AutoArchived"] as PropertyBoolean;
            return property?.Boolean ?? false;
        }

        return false;
    }
}
