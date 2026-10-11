using System;
using System.IO;
using System.Xml;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;

namespace ComicReader.Services
{
    public class ComicInfoMetadata
    {
        public string Title { get; set; }
        public string Series { get; set; }
        public int Number { get; set; }
        public int Volume { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string Publisher { get; set; }
        public string Writer { get; set; }
        public string Penciller { get; set; }
        public string Genre { get; set; }
        public string Summary { get; set; }
        public int PageCount { get; set; }
    }

    public static class ComicInfoParserService
    {
        public static ComicInfoMetadata ParseFromArchive(string archivePath)
        {
            if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath))
                return null;

            try
            {
                var ext = Path.GetExtension(archivePath).ToLowerInvariant();
                if (ext != ".cbz" && ext != ".zip")
                    return null;

                using var archive = ZipArchive.Open(archivePath);
                foreach (var entry in archive.Entries)
                {
                    if (entry.IsDirectory) continue;
                    if (string.Equals(entry.Key, "ComicInfo.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = entry.OpenEntryStream();
                        return ParseFromStream(stream);
                    }
                }
            }
            catch
            {
                // Si no se puede parsear o no contiene ComicInfo.xml
            }

            return null;
        }

        public static ComicInfoMetadata ParseFromStream(Stream xmlStream)
        {
            if (xmlStream == null) return null;

            try
            {
                var doc = new XmlDocument();
                doc.Load(xmlStream);

                var meta = new ComicInfoMetadata();
                var root = doc.DocumentElement;
                if (root == null) return null;

                meta.Title = GetNodeText(root, "Title");
                meta.Series = GetNodeText(root, "Series");
                meta.Publisher = GetNodeText(root, "Publisher");
                meta.Writer = GetNodeText(root, "Writer");
                meta.Penciller = GetNodeText(root, "Penciller");
                meta.Genre = GetNodeText(root, "Genre");
                meta.Summary = GetNodeText(root, "Summary");

                if (int.TryParse(GetNodeText(root, "Number"), out int num)) meta.Number = num;
                if (int.TryParse(GetNodeText(root, "Volume"), out int vol)) meta.Volume = vol;
                if (int.TryParse(GetNodeText(root, "Year"), out int yr)) meta.Year = yr;
                if (int.TryParse(GetNodeText(root, "Month"), out int mth)) meta.Month = mth;
                if (int.TryParse(GetNodeText(root, "PageCount"), out int pages)) meta.PageCount = pages;

                return meta;
            }
            catch
            {
                return null;
            }
        }

        private static string GetNodeText(XmlNode parent, string childName)
        {
            var node = parent.SelectSingleNode(childName);
            return node?.InnerText?.Trim();
        }
    }
}