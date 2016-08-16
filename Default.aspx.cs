using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using HtmlAgilityPack;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Globalization;

public class Item
{
    private string title;
    public string Title { get { return title; } }

    private string link;
    public string Link { get { return link; } }

    private string description;
    public string Description { get { return description; } }

    private string author;
    public string Author { get { return author; } }

    private DateTime pubDate;
    public DateTime PublishDate { get { return pubDate; } }

    public Item( string title, string link, string description, string author, string pubDate )
    {
        this.title = title;
        this.link = link;
        this.description = description;
        this.author = author;

        this.pubDate = new DateTime();
        pubDate = pubDate.Replace( ",", "" );
        pubDate = pubDate.Substring( 0, pubDate.Length - 4 );
        DateTime.TryParseExact( 
            pubDate,
            new string[] { "ddd dd MMM yyyy HH:mm:ss", "ddd d MMM yyyy HH:mm:ss" }, 
            CultureInfo.InvariantCulture, 
            DateTimeStyles.None,
            out this.pubDate );
    }

    public override string ToString()
    {
        return "<a href=\"" + link + "\">" + title + "</a><br />" + pubDate + "<br />";    
    }
}

public class Feed
{
    private string link;
    public string Link { get { return link; } }

    private List<Item> items;
    public List<Item> Items { get { return items; } }

    public Feed( string link )
    {
        this.link = link;
        items = new List<Item>();
    }

    public void AddItem( Item itemToAdd )
    {
        items.Add( itemToAdd );
    }
}

public partial class _Default : System.Web.UI.Page
{
    Dictionary<string, List<Feed>> linkToFeeds  = new Dictionary<string, List<Feed>>();
    List<string> validSearchLinks               = new List<string>();
    Random rand                                 = new Random();

    protected void Page_Load(object sender, EventArgs e)
    {
        
    }

    protected void DownloadData( object sender, EventArgs e )
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();

        using ( WebClient client = new WebClient() )
        {
            // Download TechTarget's network html
            string html = client.DownloadString( "http://www.techtarget.com/network" );

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml( html );

            // Parse the html for all list items with the specified class
            List<HtmlNode> links = doc.DocumentNode.SelectNodes( "//li[@class='nav-list-sublist-item']" ).ToList();
            foreach ( HtmlNode child in links )
            {
                HtmlNode firstChild = child.FirstChild;
                if ( firstChild != null )
                {
                    HtmlAttribute href = firstChild.Attributes["href"];
                    if ( href != null )
                    {
                        string link = href.Value;

                        // Validate that this is a search link for TechTarget
                        if ( link.Contains( "search" ) && link.Contains( ".techtarget." ) && link.EndsWith( ".com" ) )
                        {
                            // Prevent duplicates
                            if ( linkToFeeds.ContainsKey( link ) )
                                continue;

                            validSearchLinks.Add( link );
                            linkToFeeds.Add( link, new List<Feed>() );

                            // Download the RSS feed html
                            string rssFeed = client.DownloadString( link + "/rss" );
                            HtmlDocument rssDoc = new HtmlDocument();
                            rssDoc.LoadHtml( rssFeed );

                            // Parse the RSS feed html for valid RSS feed links
                            List<HtmlNode> xmlLinks = rssDoc.DocumentNode.SelectNodes( "//a[@class='rssFeed']" ).ToList();
                            foreach ( HtmlNode xmlLink in xmlLinks )
                            {
                                HtmlAttribute xmlHref = xmlLink.Attributes["href"];
                                if ( xmlHref != null )
                                {
                                    // Download the xml and create a new Feed class
                                    try
                                    {
                                        string xml = client.DownloadString( xmlHref.Value );
                                        Feed feed = new Feed( xmlHref.Value );

                                        using ( XmlReader reader = XmlReader.Create( new StringReader( xml ) ) )
                                        {
                                            while ( reader.Read() )
                                            {
                                                // Parse the xml for an item
                                                if ( reader.NodeType == XmlNodeType.Element && reader.Name == "item" )
                                                {
                                                    string title = "";
                                                    string contentLink = "";
                                                    string description = "";
                                                    string author = "";
                                                    string pubDate = "";

                                                    // Advance to the title element
                                                    reader.ReadToFollowing( "title" );
                                                    if ( reader.NodeType != XmlNodeType.None )
                                                        title = reader.ReadElementContentAsString();

                                                    // Advance to the link element
                                                    reader.ReadToFollowing( "link" );
                                                    if ( reader.NodeType != XmlNodeType.None )
                                                        contentLink = reader.ReadElementContentAsString();

                                                    // Advance to the description element
                                                    reader.ReadToFollowing( "description" );
                                                    if ( reader.NodeType != XmlNodeType.None )
                                                        description = reader.ReadElementContentAsString();

                                                    // Advance to the author element
                                                    reader.ReadToFollowing( "author" );
                                                    if ( reader.NodeType != XmlNodeType.None )
                                                        author = reader.ReadElementContentAsString();

                                                    // Advance to the publish date element
                                                    reader.ReadToFollowing( "pubDate" );
                                                    if ( reader.NodeType != XmlNodeType.None )
                                                        pubDate = reader.ReadElementContentAsString();

                                                    // Add the item to the feed
                                                    Item item = new Item( title, contentLink, description, author, pubDate );
                                                    feed.AddItem( item );
                                                }
                                            }
                                        }
                                        linkToFeeds[link].Add( feed );
                                    }
                                    catch
                                    {
                                        // If something was caught here, it was because the item was not formatted as expected
                                        // or there was an issue downloading the xml string
                                        continue;
                                    }
                                }
                            }

                            // No valid RSS feeds
                            if ( linkToFeeds[link].Count == 0 )
                            {
                                validSearchLinks.Remove( link );
                                linkToFeeds.Remove( link );
                            }
                        }
                    }
                }
            }
        }

        watch.Stop();
        Response.Write( "Downloaded in: " + string.Format( "{0:D2}m:{1:D1}s:{2:D3}ms", watch.Elapsed.Minutes, watch.Elapsed.Seconds, watch.Elapsed.Milliseconds ) + "<br />" );
        Session["ValidSearchLinks"] = validSearchLinks;
        Session["LinkToFeeds"] = linkToFeeds;

        downloadButton.Visible = false;
        randButton.Visible = true;
    }

    protected void Randomize( object sender, EventArgs e )
    {
        items.Attributes.Clear();
        Response.Clear();
        DisplayRandomFeeds();
    }

    void DisplayRandomFeeds()
    {
        validSearchLinks = (List<string>)Session["ValidSearchLinks"];
        linkToFeeds = (Dictionary<string, List<Feed>>)Session["LinkToFeeds"];

        List<string> copy = new List<string>();
        foreach ( string link in validSearchLinks )
            copy.Add( link );

        List<string> selectedLinks = new List<string>();
        List<Feed> selectedFeeds = new List<Feed>();

        // Select ten random links
        for ( int i = 0; i < 10; i++ )
        {
            int randIndex = rand.Next( 0, copy.Count );
            string selectedLink = copy[randIndex];
            copy.RemoveAt( randIndex );
            selectedLinks.Add( selectedLink );
        }

        // Select a random feed for each of those links
        foreach ( string link in selectedLinks )
        {
            List<Feed> feeds = linkToFeeds[link];
            int randIndex = rand.Next( 0, feeds.Count );
            Feed selectedFeed = feeds[randIndex];
            selectedFeeds.Add( selectedFeed );
        }

        // Order the feed items
        List<Item> orderedItems = selectedFeeds
            .SelectMany( feed => feed.Items )
            .OrderBy( item => item.PublishDate )
            .ToList();

        string orderedString = "";
        foreach ( Item item in orderedItems )
        {
            orderedString += item.ToString() + "<br />";
        }

        items.InnerHtml = orderedString;

        Session["ValidSearchLinks"] = validSearchLinks;
        Session["LinkToFeeds"] = linkToFeeds;
    }
}