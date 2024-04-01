using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace HtmlToImageMaster
{
    /// <summary>
    /// Html Converter. Converts HTML string and URLs to image bytes
    /// </summary>
    public static class HtmlConverter
    {
        private static string toolFilename;

        private static string directory;

        private static string toolFilepath;

        static HtmlConverter()
        {
            toolFilename = "wkhtmltoimage";
            directory = AppContext.BaseDirectory;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                toolFilepath = Path.Combine(directory, toolFilename + ".exe");
                if (File.Exists(toolFilepath))
                {
                    return;
                }

                Assembly assembly = typeof(HtmlConverter).GetTypeInfo().Assembly;
                string @namespace = typeof(HtmlConverter).Namespace;
                using (Stream stream = assembly.GetManifestResourceStream(@namespace + "." + toolFilename + ".exe"))
                {
                    using (FileStream destination = File.OpenWrite(toolFilepath))
                    {
                        stream.CopyTo(destination);
                    }
                    
                }

                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process process = Process.Start(new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = "/bin/",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = "/bin/bash",
                    Arguments = "which wkhtmltoimage"
                });
                string text = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(text) && text.Contains("wkhtmltoimage"))
                {
                    toolFilepath = "wkhtmltoimage";
                    return;
                }

                throw new Exception("wkhtmltoimage does not appear to be installed on this linux system according to which command; go to https://wkhtmltopdf.org/downloads.html");
            }

            throw new Exception("OSX Platform not implemented yet");
        }

        /// <summary>
        /// Converts a HTML-String into an Image-File as Byte format.
        /// </summary>
        /// <param name="html">Raw HTLM as String.</param>
        /// <param name="width">Width in mm.(Default width 1024)</param>
        /// <param name="format">JPG or PNG (Default format is JPG)</param>
        /// <param name="quality">Set image quality between 0 to 100</param>
        /// <returns></returns>
        public static byte[] FromHtmlString(string html, int width = 1024, ImageFormat format=ImageFormat.Jpg, int quality = 100)
        {
            string text = Path.Combine(directory, $"{Guid.NewGuid()}.html");
            File.WriteAllText(text, html);
            byte[] result = FromUrl(text, width, format, quality);
            File.Delete(text);
            return result;
        }
        /// <summary>
        /// Converts a HTML-Page into an Image-File as Byte format.
        /// </summary>
        /// <param name="url">Valid http(s)://example.com URL</param>
        /// <param name="width">Width in mm.(Default width 1024)</param>
        /// <param name="format">JPG or PNG (Default format is JPG)</param>
        /// <param name="quality">Set image quality between 0 to 100</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static byte[] FromUrl(string url, int width = 1024, ImageFormat format = ImageFormat.Jpg, int quality = 100)
        {
            string text = format.ToString().ToLower();
            string text2 = Path.Combine(directory, Guid.NewGuid().ToString() + "." + text);
            Process process = Process.Start(new ProcessStartInfo(arguments: (!IsLocalPath(url)) ? $"--quality {quality} --width {width} -f {text} {url} \"{text2}\"" : $"--quality {quality} --width {width} -f {text} \"{url}\" \"{text2}\"", fileName: toolFilepath)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = directory,
                RedirectStandardError = true
            });
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.WaitForExit();
            if (File.Exists(text2))
            {
                byte[] result = File.ReadAllBytes(text2);
                File.Delete(text2);
                return result;
            }

            throw new Exception("Something went wrong. Please check input parameters");
        }

        private static bool IsLocalPath(string path)
        {
            if (path.StartsWith("http"))
            {
                return false;
            }

            return new Uri(path).IsFile;
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new Exception(e.Data);
        }
    }
}