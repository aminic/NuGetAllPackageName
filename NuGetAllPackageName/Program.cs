using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Winista.Text.HtmlParser;
using Winista.Text.HtmlParser.Filters;
using Winista.Text.HtmlParser.Tags;

namespace NuGetAllPackageName
{


    class Program
    {
        const string PackageNameUrlFormat = "https://www.nuget.org/packages?page={0}";
        const string PagePackageContentBeginTag = "<ul id=\"searchResults\">";
        const string PagePackageConentEndTag = "<ul class=\"pager\">";


        static void Main(string[] args)
        {
            string outFileDir = System.Environment.CurrentDirectory + "\\packages.txt";
            GetPackageName(outFileDir);



        }

        static string GetPage(string pageUrl)
        {
            Console.Write("#READ PackageName from {0}......", pageUrl);
            byte[] pageData;
            try
            {
                using (var c = new WebClient())
                {
                    pageData = c.DownloadData(pageUrl);
                }
                if (pageData.Length == 0)
                {
                    Console.WriteLine("can't get package url page content.");
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("READ error {0}",e.Message);
                return string.Empty;
            }
            string pageHtml = Encoding.UTF8.GetString(pageData);
            return pageHtml;
        }


        static void GetPackageName(string outFileFullPath)
        {
            int i = 1;
            Random random = new Random();
            while (true)
            {
                var pageUrl = string.Format(PackageNameUrlFormat, i);
                Console.Write("#Read Page {0} ....", i);
                var htmlPageContent = GetPage(pageUrl);
                if (string.IsNullOrEmpty(htmlPageContent))
                    break;
                var beginIndex = htmlPageContent.IndexOf(PagePackageContentBeginTag);
                if (beginIndex <= 0)
                {
                    Console.WriteLine("can't find package list html section in package url search page content.");
                    break;
                }
                var htmlPackageList = htmlPageContent.Substring(beginIndex);
                var endIndex = htmlPackageList.IndexOf(PagePackageConentEndTag);
                if (endIndex <= 0)
                {
                    Console.WriteLine("can't find package list end html section in package url search page content.");
                    break;
                }
                htmlPackageList = htmlPackageList.Substring(0, endIndex);
                if (htmlPackageList.Length < 30)
                    return;

                AutoResetEvent waitHandler = new AutoResetEvent(false);
                waitHandler.Set();
                var parser = Parser.CreateParser(htmlPackageList, null);
                var array = parser.ExtractAllNodesThatMatch(new TagNameFilter("h1")).ToNodeArray();
                var atags = array
                    .Where(x =>
                        ((x as HeadingTag).FirstChild as ATag).Link.StartsWith("/packages/")
                    )
                    .Select(x => x.FirstChild as ATag).ToList();

                waitHandler.WaitOne();

                Console.WriteLine(" Get package number : {0}", atags.Count);
                if (atags.Count <= 0)
                    break;

                List<string> names = new List<string>();
                foreach (var atag in atags)
                {
                    var a = atag as ATag;
                    var name = a.Link.Substring(10, a.Link.Length - 11);
                    if (name.Contains('/'))
                    {
                        var namespans = name.Split('/');
                        name = namespans[0];
                    }

                    names.Add(name);
                }
                try
                {
                    File.AppendAllLines(outFileFullPath, names.ToArray());
                    Console.WriteLine("# {0} Package Name Wrote.", atags.Count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("#Package Name Write error {0}", ex.Message);
                }

                //var time = random.Next(4, 10);
                //System.Threading.Thread.Sleep(time * 1000);

                i++;
            }
            Console.WriteLine("#Get Package Name END.");
        }
    }
}
