// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyPage
    {
        public static void Convert(
            Docset docset,
            Context context,
            Document doc,
            LegacyManifestItem legacyManifestItem)
        {
            // OPS build use TOC ouput as data page
            var legacyManifestOutput = legacyManifestItem.Output;
            if (legacyManifestOutput.TocOutput != null)
            {
                var outputPath = legacyManifestOutput.TocOutput.ToLegacyOutputPath(docset, legacyManifestItem.Group);
                var model = JsonUtility.Deserialize<PageModel>(File.ReadAllText(docset.GetAbsoluteOutputPathFromRelativePath(outputPath)));
                context.Delete(doc.OutputPath);
                context.WriteJson(model.Content, outputPath);
            }

            JObject rawMetadata = null;
            if (legacyManifestOutput.PageOutput != null)
            {
                var rawPageOutputPath = legacyManifestOutput.PageOutput.ToLegacyOutputPath(docset, legacyManifestItem.Group);
                LegacyUtility.MoveFileSafe(
                    docset.GetAbsoluteOutputPathFromRelativePath(doc.OutputPath),
                    docset.GetAbsoluteOutputPathFromRelativePath(rawPageOutputPath));

                var pageModel = JsonUtility.Deserialize<PageModel>(File.ReadAllText(docset.GetAbsoluteOutputPathFromRelativePath(rawPageOutputPath)));

                var content = pageModel.Content as string;
                if (!string.IsNullOrEmpty(content))
                {
                    content = HtmlUtility.TransformHtml(
                        content,
                        node => node.AddLinkType(docset.Locale, docset.Legacy)
                                    .RemoveRerunCodepenIframes());
                }

                var outputRootRelativePath =
                    PathUtility.NormalizeFolder(Path.GetRelativePath(
                        PathUtility.NormalizeFolder(Path.GetDirectoryName(rawPageOutputPath)),
                        PathUtility.NormalizeFolder(docset.Config.DocumentId.SiteBasePath)));

                var themesRelativePathToOutputRoot = "_themes/";

                if (!string.IsNullOrEmpty(doc.RedirectionUrl))
                {
                    rawMetadata = LegacyMetadata.GenerateLegacyRedirectionRawMetadata(docset, pageModel);
                    context.WriteJson(new { outputRootRelativePath, rawMetadata, themesRelativePathToOutputRoot }, rawPageOutputPath);
                }
                else
                {
                    rawMetadata = LegacyMetadata.GenerateLegacyRawMetadata(pageModel, content, doc, legacyManifestItem.Group);
                    var pageMetadata = LegacyMetadata.CreateHtmlMetaTags(rawMetadata);
                    context.WriteJson(new { outputRootRelativePath, content, rawMetadata, pageMetadata, themesRelativePathToOutputRoot }, rawPageOutputPath);
                }
            }

            if (legacyManifestOutput.MetadataOutput != null && rawMetadata != null)
            {
                var metadataOutputPath = legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset, legacyManifestItem.Group);
                var metadate = LegacyMetadata.GenerateLegacyMetadateOutput(rawMetadata);
                context.WriteJson(metadate, metadataOutputPath);
            }
        }
    }
}