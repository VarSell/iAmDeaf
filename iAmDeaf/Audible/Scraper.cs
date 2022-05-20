using System.Text;
using Newtonsoft.Json;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Drawing;
using System.Diagnostics;

namespace iAmDeaf.Audible
{
    internal class Scraper
    {
        public static string[]? Scrape(string title)
        {
            string audibleBase = $@"https://www.audible.com/search?keywords={Find.Term(title)}&skip_spell_correction=true&ipRedirectOverride=true&ipRedirectFrom=com";
            string audibleBook = $@"https://www.audible.com/pd/";


            string listItems = Find.Source(audibleBase);
            string ID = Find.BookID(listItems, 3);

            if (ID == string.Empty)
            {
                Alert.Error("ASIN Error");
                return null;
            }

            string bookPage = Find.Source(string.Concat(audibleBook, ID, "?ipRedirectOverride=true&ipRedirectFrom=com"));
            string[] Series = Find.BookSeries(bookPage);
            string json = Find.BookJson(bookPage).Trim();

            string Author = string.Empty;
            string Genre = string.Empty;
            string Title = string.Empty;
            string Narrator = string.Empty;

            if (json == String.Empty)
            {
                Alert.Error($"Error loading json");
                return null;
            }

            try
            {
                var Book = JsonConvert.DeserializeObject<List<Class1>>(json);

                if (Book[0].duration == string.Empty || Book[0].readBy[0].name == string.Empty)
                {
                    return null;
                }
                Title = Book[0].name;
                //Description = Book[0].description.Replace("/", string.Empty).Replace("<p>", string.Empty).Replace("<i>", string.Empty).Replace("<b>", string.Empty).Replace("<br>", "\n").Replace("<br >", "\n").Replace("<ul>", string.Empty).Replace("<li>", string.Empty).Trim();
                //amznImage = Book[0].image;
                //ReleaseDate = Book[0].datePublished;
                //Length = Book[0].duration.Replace("PT", string.Empty).Replace("H", " hrs ").Replace("M", " mins");
                //Publisher = Book[0].publisher;

                //Alert.Notify($"Image  : {amznImage}");

                try
                {
                    if (Book[1].itemListElement.Length > 1)
                    {
                        int j = 0;
                        if (Book[1].itemListElement[0].item.name.Trim() == "Home") { j = 1; } else { j = 0; }
                        for (int i = j; i < Book[1].itemListElement.Length; i++)
                        {
                            Genre += string.Concat(Book[1].itemListElement[i].item.name, ", ");
                        }
                        Genre = Genre.Trim().TrimEnd(',');
                    }
                    else
                    {
                        if (!(Book[1].itemListElement[0].item.name.Trim() == "Home"))
                        {
                            Genre = Book[1].itemListElement[0].item.name;
                        }
                        else
                        {
                            Genre = Book[1].itemListElement[1].item.name;
                        }
                    }
                    Alert.Notify($"Genre: {Genre}");
                }
                catch (Exception ex)
                {
                    Genre = String.Empty;
                    Alert.Notify($"Genre: {ex.Message}");
                }

                if (Book[0].author.Length > 1)
                {
                    for (int i = 0; i < Book[0].author.Length; i++)
                    {
                        Author += string.Concat(Book[0].author[i].name, ", ");
                    }
                    Author = Author.Trim().TrimEnd(',');
                }
                else
                {
                    Author = Book[0].author[0].name;
                }
                Alert.Notify($"Author: {Author}");

                if (Book[0].readBy.Length > 1)
                {
                    for (int i = 0; i < Book[0].author.Length; i++)
                    {
                        Narrator += string.Concat(Book[0].readBy[i].name, ", ");
                    }
                    Narrator = Narrator.Trim().TrimEnd(',');
                }
                else
                {
                    Narrator = Book[0].readBy[0].name;
                }
                Alert.Notify($"Narrator: {Narrator}");

            }
            catch (Exception ex)
            {
                Alert.Error(ex.Message);
                Alert.Error("Json parsing failed");
            }

            // author series title

            string[] notif = new string[5];
            notif[0] = Genre;
            notif[1] = Narrator;
            notif[2] = Author;
            notif[3] = Series[0];
            notif[4] = Title;

            return notif;
        }
    }

    class Find
    {

        public static string BookJson(string html)
        {
            try
            {
                string[] match = html.Split(new string[] { "<script type=\"application/ld+json\">" }, StringSplitOptions.RemoveEmptyEntries);
                match[2] = match[2].Substring(1, match[2].IndexOf("</script>"));
                match[2] = (match[2].Remove(match[2].Length - 1)).Trim();
                return match[2];
            }
            catch (Exception ex)
            {
                //Alert.Error($"Json: {ex.Message}");
                return String.Empty;
            }
        }
        public static string[] BookSeries(string html)
        {
            string[] parts = new string[2];
            parts[0] = string.Empty;
            parts[1] = string.Empty;

            try
            {   // First attempt to parse book series and index from ASIN html page
                string[] match = html.Split(new string[] { $"href=\"/series/" }, StringSplitOptions.RemoveEmptyEntries);
                match[1] = match[1].Substring(0, match[1].IndexOf("li>")).Trim();
                string temp = match[1];
                match = html.Split(new string[] { $"Book " }, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    parts[1] = match[1].Substring(0, match[1].IndexOf("</"));
                    try { parts[1] = match[1].Substring(0, match[1].IndexOf(")")).Trim(); } catch { }
                }
                catch
                {
                    parts[1] = String.Empty;
                }

                match = temp.Split(new string[] { $"\">" }, StringSplitOptions.RemoveEmptyEntries);
                match[1] = match[1].Substring(0, match[1].IndexOf("</a>")).Trim();
                parts[0] = match[1].Replace(":", "").Trim();


                if (parts[1].All(Char.IsDigit))
                {
                    parts[1] = string.Concat($"Book ", parts[1]);
                    //Alert.Success($"Series : {string.Concat(parts[0], ", ", parts[1])}");
                }
                //else { Alert.Success($"Series : {parts[0]}"); }

                return parts;
            }
            catch
            {   // Second parsing attempt before giving up
                try
                {
                    string[] match = html.Split(new string[] { $" bc-size-medium\"  >" }, StringSplitOptions.RemoveEmptyEntries);

                    // try in future maybe match[1] = match[1].Substring(0, match[1].IndexOf("\n")).Trim();
                    match[1] = match[1].Substring(0, match[1].IndexOf("</span>")).Trim();

                    match[0] = match[1].Split(",")[0].Trim();
                    match[1] = match[1].Split(",")[1].Trim();

                    //Alert.Success($"Series : {string.Concat(match[0], ", ", match[1])}");
                    return match;
                }
                catch
                {
                    //Alert.Notify("Series not applicable");
                    return parts;
                }
            }
        }
        public static string BookID(string html, int n)
        {
            try
            {
                string[] match = html.Split(new string[] { $"product-list-flyout" }, StringSplitOptions.RemoveEmptyEntries);
                match[n] = match[n].Substring(1, match[n].IndexOf("\""));
                match[n] = (match[n].Remove(match[n].Length - 1)).Trim();
                //Alert.Success($"ASIN   : {match[n]}");
                return match[n];
            }
            catch (Exception ex)
            {
                //Alert.Error($"ASIN: {ex.Message}");
                return String.Empty;
            }
        }
        public static string Term(string title)
        {
            title = Regex.Replace(title, @"[\d-]", string.Empty);
            title = Regex.Replace(title.Replace("-", null), @"\s+", " ").Trim();
            title = Regex.Replace(title, @"\s", "+");
            return title;
        }
        public static string Source(string url)
        {
            string data = string.Empty;
            using (WebClient wClient = new WebClient())
            {
                data = wClient.DownloadString(url);

            }
            return data;
        }
    }






    public class Rootobject
    {
        public Class1[] Property1 { get; set; }
    }

    public class Class1
    {
        public string context { get; set; }
        public string type { get; set; }
        public string bookFormat { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string image { get; set; }
        public string abridged { get; set; }
        public Author[] author { get; set; }
        public Readby[] readBy { get; set; }
        public string publisher { get; set; }
        public string datePublished { get; set; }
        public string inLanguage { get; set; }
        public string duration { get; set; }
        public string[] regionsAllowed { get; set; }
        public Aggregaterating aggregateRating { get; set; }
        public Offers offers { get; set; }
        public Itemlistelement[] itemListElement { get; set; }
    }

    public class Aggregaterating
    {
        public string type { get; set; }
        public string ratingValue { get; set; }
        public string ratingCount { get; set; }
    }

    public class Offers
    {
        public string type { get; set; }
        public string availability { get; set; }
        public string lowPrice { get; set; }
        public string highPrice { get; set; }
        public string priceCurrency { get; set; }
    }

    public class Author
    {
        public string type { get; set; }
        public string name { get; set; }
    }

    public class Readby
    {
        public string type { get; set; }
        public string name { get; set; }
    }

    public class Itemlistelement
    {
        public string type { get; set; }
        public int position { get; set; }
        public Item item { get; set; }
    }

    public class Item
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}
