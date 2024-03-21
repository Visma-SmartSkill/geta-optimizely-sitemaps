using EPiServer.Core;
using EPiServer.Web;
using Microsoft.Extensions.Logging;

namespace Geta.Optimizely.Sitemaps.Utils;

public class VssContentFilter : ContentFilter, IVssContentFilter
{
    public VssContentFilter(TemplateResolver templateResolver, ILogger<ContentFilter> logger)
        : base(templateResolver, logger) { }

    public override bool ShouldExcludeContent(IContent content)
    {
        if (content == null)
        {
            return true;
        }

        if (content.IsDeleted)
        {
            return true;
        }

        if (!IsPublished(content))
        {
            return true;
        }

        if (!IsSitemapPropertyEnabled(content))
        {
            return true;
        }

        if (IsAutoArchived(content))
        {
            return true;
        }

        if (!IsVisibleOnSite(content))
        {
            return true;
        }

        if (content.ContentLink.CompareToIgnoreWorkID(ContentReference.WasteBasket))
        {
            return true;
        }

        if (content is BlockData || content is MediaData)
        {
            return true;
        }

        var page = content as PageData;

        if (page != null && IsLink(page))
        {
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
