using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using System.Net.Http;
using System.Threading.Tasks;


namespace Jorge_Silva_Utor_Crawler
{
    public static class Extensions
    {
        /// <summary>Returns a new string in which all occurrences of an specified Unicode character array
        /// in this instance are replaced with another specified Unicode character.
        /// <param name="chars">Array of characters to be replaced.</param>
        /// <param name="replaceChar">Unicode character to replace all other occurrences with.</param>
        /// <returns>A string that is equivalent to this instance except that all instances in the char array are replaced with replaceChar.</returns>
        /// </summary>
        public static string ReplaceAll(this string stringBody, char[] chars, char replaceChar)
        {
            return chars.Aggregate(stringBody, (str, charItem) => str.Replace(charItem, replaceChar));
        }
    }

    class Program
    {
        /// <summary>Makes a call to the Url and retuns the HttpResponse.
        /// <param name="url"></param>
        /// <returns>An HttpResponseMessage.</returns>
        /// </summary>
        public static async Task<HttpResponseMessage> GetResponse(string url)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

                var response = await client.GetAsync(new Uri(url));

                return response;
            }
        }

        static async Task Main(string[] args)
        {
            #region Declared Variables
            var url = "https://en.wikipedia.org/wiki/Microsoft";
            var isNumber = false;
            var crawl = false;
            var dic = new Dictionary<string, int>();
            char[] separators = new char[] { ' ', ';', ':', ',', '.', '{', '}', '[', ']', '(', ')', '"', '&', '#', '\r', '\t', '\n' };
            var wordCount = 10;
            #endregion

            try
            {
                #region Read Inputs
                Console.WriteLine("Web Crawler\n");                
                Console.WriteLine("\nPlease enter the number of words to return, or press enter to continue using default 10: ");
                var inputCount = Console.ReadLine();
                isNumber = int.TryParse(inputCount.Trim(), out var count);
                //if (!string.IsNullOrWhiteSpace(inputCount.Trim()))
                //{
                //    wordCount = count;
                //}
                while (!isNumber && !string.IsNullOrWhiteSpace(inputCount))
                {
                    Console.WriteLine("\nWarning: Enter ONLY numbers!!!");
                    Console.WriteLine("\nPlease enter the number of words to return, or press enter to continue using default 10: ");
                    inputCount = Console.ReadLine();
                    isNumber = int.TryParse(inputCount.Trim(), out count);
                }

                Console.WriteLine("\nPlease enter a comma-separated-list of words you want to omit: ");
                var inputList = Console.ReadLine();
                var excludeList = inputList.ToLower().Split(',');
                #endregion

                //Call URL and verify success response
                var response = await GetResponse(url);
                response.EnsureSuccessStatusCode();
                
                //Transform response to HtmlDocument
                HtmlDocument doc = new HtmlDocument();
                string docBody = await response.Content.ReadAsStringAsync();
                doc.LoadHtml(docBody);

                //Filter HtmlTags
                var allTags = doc.DocumentNode.Descendants();
                var filteredTags = allTags.Where(a => a.Name == "h2" || a.Name == "div" || a.Name == "p" || a.Name == "h3");

                #region Web Crawler
                foreach (var tag in filteredTags)
                {
                    if (tag.Name.Contains("h2") && tag.InnerHtml.Contains("History"))
                    {
                        crawl = true; //Start recording instances
                    }
                    else if (tag.Name.Contains("h2") && tag.InnerHtml.Contains("Corporate_affairs"))
                    {
                        crawl = false; //Stop recording instances
                    }

                    if (crawl)
                    {
                        var innerbody = tag.InnerText.ToLower();
                        var body = innerbody.ReplaceAll(separators, ' ').Split(' ');

                        foreach (var word in body)
                        {
                            var stdWord = word.Trim();
                            if (!string.IsNullOrWhiteSpace(stdWord) && !excludeList.Contains(stdWord) && !int.TryParse(stdWord,out var result))
                            {
                                if (dic.ContainsKey(stdWord)) //Add to existing {key,value}
                                {
                                    dic[stdWord] = ++dic[stdWord];
                                }
                                else //Create {key,value}
                                {
                                    dic.Add(stdWord, 1);
                                }
                            }
                        }
                    }
                }
                #endregion

                //Order Desc and limits number of results
                var sortedList = dic.OrderByDescending(x => x.Value).ThenBy(x => x.Key);
                var myList = sortedList.Take(wordCount);

                Console.WriteLine("\tWord\t\t\t# of Occurrences\n");
                Console.WriteLine("\t----\t\t\t----------------\n");
                foreach (var entry in myList)
                {
                    Console.WriteLine("\t{0}\t\t\t{1}\n", entry.Key, entry.Value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
