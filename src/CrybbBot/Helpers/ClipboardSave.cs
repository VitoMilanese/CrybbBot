using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CrybbBot.Helpers
{
    internal static class ClipboardSave
    {
        public enum ContentType
        {
            Unknown,
            Image,
            File,
            Stream
        }

        public struct ClipboardElement
        {
            public string Path { get; set; }
            public string? OriginalPath { get; set; }
            public ContentType ContentType { get; set; }
        }

        public static async Task<ClipboardElement[]?> TrySaveClipboardContentAsync(string baseFolder)
        {
            // Clipboard can be locked; retry.
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    // 1) Images
                    if (Clipboard.ContainsImage())
                    {
                        var img = Clipboard.GetImage();
                        if (img != null)
                        {
                            if (img.CanFreeze) img.Freeze();
                            var path = Path.Combine(baseFolder, $"paste_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
                            SaveBitmapSourceToPng(img, path);
                            //return $"Image saved:\n{path}";
                            return new[] { new ClipboardElement { Path = path, ContentType = ContentType.Image } };
                        }
                    }

                    // 2) Files copied from Explorer (most common for "non-image files")
                    if (Clipboard.ContainsFileDropList())
                    {
                        var files = Clipboard.GetFileDropList();
                        if (files.Count > 0)
                        {
                            var destFolder = Path.Combine(baseFolder, $"files_{DateTime.Now:yyyyMMdd_HHmmss_fff}");
                            Directory.CreateDirectory(destFolder);

                            var copied = 0;
                            var result = new List<ClipboardElement>();
                            foreach (string src in files.Cast<string>())
                            {
                                // src might be a file or a folder.
                                if (File.Exists(src))
                                {
                                    var dst = GetUniquePath(Path.Combine(destFolder, Path.GetFileName(src)));
                                    File.Copy(src, dst, overwrite: false);
                                    result.Add(new ClipboardElement
                                    {
                                        Path = dst,
                                        OriginalPath = src,
                                        ContentType = ContentType.File
                                    });
                                    copied++;
                                }
                                else if (Directory.Exists(src))
                                {
                                    // Optional: copy directory recursively
                                    var dstDir = GetUniquePath(Path.Combine(destFolder, Path.GetFileName(src)));
                                    var copiedFiles = CopyDirectoryRecursive(src, dstDir);
                                    if (copiedFiles.Any())
                                    {
                                        result.AddRange(copiedFiles);
                                    }
                                    copied++;
                                }
                            }
                            //return $"Copied {copied} item(s) to:\n{destFolder}";
                            return result.ToArray();
                        }
                    }

                    // 3) Text
                    //if (Clipboard.ContainsText())
                    //{
                    //    string text = Clipboard.GetText();
                    //    var path = Path.Combine(baseFolder, $"text_{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt");
                    //    await File.WriteAllTextAsync(path, text);
                    //    return $"Text saved:\n{path}";
                    //}

                    // 4) Best-effort: some apps put data as a stream (rare/generic)
                    // This is not guaranteed to work for arbitrary clipboard formats, but can catch some cases.
                    //var data = Clipboard.GetDataObject();
                    //if (data != null)
                    //{
                    //    // If the data object provides a MemoryStream for some format, save it.
                    //    foreach (var format in data.GetFormats())
                    //    {
                    //        var obj = data.GetData(format);
                    //        if (obj is MemoryStream ms && ms.Length > 0)
                    //        {
                    //            var path = Path.Combine(baseFolder, $"bin_{DateTime.Now:yyyyMMdd_HHmmss_fff}.bin");
                    //            ms.Position = 0;
                    //            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                    //            ms.CopyTo(fs);
                    //            //return $"Binary stream saved (format: {format}):\n{path}";
                    //            return new[] { new ClipboardElement { Path = path, ContentType = ContentType.Stream } };
                    //        }
                    //    }
                    //}

                    return null;
                }
                catch
                {
                    await Task.Delay(60);
                }
            }

            return null;
        }

        private static void SaveBitmapSourceToPng(BitmapSource source, string path)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(fs);
        }

        private static string GetUniquePath(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return path;

            string dir = Path.GetDirectoryName(path)!;
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);

            for (int i = 1; i < 10_000; i++)
            {
                string candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                    return candidate;
            }

            // Fallback
            return Path.Combine(dir, $"{name}_{Guid.NewGuid():N}{ext}");
        }

        private static List<ClipboardElement> CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            var files = new List<ClipboardElement>();
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var dst = GetUniquePath(Path.Combine(destDir, Path.GetFileName(file)));
                File.Copy(file, dst, overwrite: false);
                files.Add(new ClipboardElement
                {
                    Path = dst,
                    OriginalPath = file,
                    ContentType = ContentType.File
                });
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dst = GetUniquePath(Path.Combine(destDir, Path.GetFileName(dir)));
                var moreFiles = CopyDirectoryRecursive(dir, dst);
                if (moreFiles.Any())
                {
                    files.AddRange(moreFiles);
                }
            }
            return files;
        }
    }
}
