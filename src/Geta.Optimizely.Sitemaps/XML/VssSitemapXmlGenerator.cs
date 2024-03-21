using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Cache;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.Optimizely.Sitemaps.Models;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Services;
using Geta.Optimizely.Sitemaps.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Geta.Optimizely.Sitemaps.XML;

public class VssSitemapXmlGenerator : SitemapXmlGenerator, IVssSitemapXmlGenerator
{
    private ISet<int> PageIdSet { get; }

    public VssSitemapXmlGenerator(
        ISitemapRepository sitemapRepository,
        IContentRepository contentRepository,
        IUrlResolver urlResolver,
        ISiteDefinitionRepository siteDefinitionRepository,
        ILanguageBranchRepository languageBranchRepository,
        IVssContentFilter contentFilter,
        IUriAugmenterService uriAugmenterService,
        ISynchronizedObjectInstanceCache objectCache,
        IMemoryCache memoryCache,
        ILogger<SitemapXmlGenerator> logger)
        : base(
            sitemapRepository,
            contentRepository,
            urlResolver,
            siteDefinitionRepository,
            languageBranchRepository,
            contentFilter,
            uriAugmenterService,
            objectCache,
            memoryCache,
            logger)
    {
        PageIdSet = new HashSet<int>();
    }

    protected override IEnumerable<XElement> GenerateXmlElements(IEnumerable<ContentReference> pages)
    {
            var sitemapXmlElements = new List<XElement>();

            foreach (var contentReference in pages)
            {
                if (StopGeneration)
                {
                    return Enumerable.Empty<XElement>();
                }

                if (ContentReference.IsNullOrEmpty(contentReference) || TryGet<IExcludeFromSitemap>(contentReference, out _))
                {
                    continue;
                }

                var pageRef = contentReference;
                if (ContentRepository.Get<IContent>(contentReference) is PageData pageData)
                {
                    //
                    // VSS: If the current page is a chapter, only log that chapters root page.
                    //
                    var root = GetChaperRoot(pageData);
                    if (root != null)
                    {
                        if (PageIdSet.Contains(root.ContentLink.ID)) continue;
                        PageIdSet.Add(root.ContentLink.ID);
                        pageRef = root.ContentLink;
                    }
                }

                var contentLanguages = GetLanguageBranches(pageRef)
                    .Where(x => x.Content is not ILocale localeContent
                                || !ExcludeContentLanguageFromSitemap(localeContent.Language));

                foreach (var contentLanguageInfo in contentLanguages)
                {
                    if (StopGeneration)
                    {
                        return Enumerable.Empty<XElement>();
                    }

                    if (UrlSet.Count >= MaxSitemapEntryCount)
                    {
                        SitemapData.ExceedsMaximumEntryCount = true;
                        return sitemapXmlElements;
                    }

                    AddFilteredContentElement(contentLanguageInfo, sitemapXmlElements);
                }
            }

            return sitemapXmlElements;
    }

    private PageData GetChaperRoot(PageData page)
    {
        var pageTypeName = "ChaptersPage";
        if (string.IsNullOrEmpty(pageTypeName) || !pageTypeName.Equals(page.PageTypeName))
        {
            return null;
        }

        var currentPage = page;
        var root = currentPage;

        do
        {
            if (!currentPage.PageTypeName.Equals(pageTypeName))
            {
                break;
            }
            root = currentPage;
            var parentRef = currentPage.ParentLink as ContentReference;
            currentPage = ContentRepository.Get<IContent>(parentRef) as PageData;
        } while (currentPage != null);
        return root;
    }
}
