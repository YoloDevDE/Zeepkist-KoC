using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace KoC.utils;

// public static class SteamWorkshopContributors
// {
//     private static readonly HttpClient httpClient = new HttpClient();
//
//     // Retrieve contributors' SteamIDs by workshop ID
//     public static async Task<List<ulong>> GetContributorsSteamIdsAsync(ulong workshopId)
//     {
//         string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopId}";
//         List<ulong> steamIds = new List<ulong>();
//
//         try
//         {
//             // Fetch the HTML content of the workshop page
//             string response = await httpClient.GetStringAsync(url);
//
//             // Load the HTML document
//             HtmlDocument htmlDoc = new HtmlDocument();
//             htmlDoc.LoadHtml(response);
//
//             // Select nodes with class "friendBlockLinkOverlay"
//             HtmlNodeCollection contributorNodes = htmlDoc.DocumentNode.SelectNodes("//a[@class='friendBlockLinkOverlay']");
//
//             if (contributorNodes != null)
//             {
//                 foreach (HtmlNode node in contributorNodes)
//                 {
//                     string profileUrl = node.GetAttributeValue("href", null);
//                     if (!string.IsNullOrEmpty(profileUrl))
//                     {
//                         // Check if the profile URL contains a SteamID or a custom ID
//                         ulong steamId = await ResolveProfileUrlToSteamIdAsync(profileUrl);
//                         if (steamId != 0)
//                         {
//                             steamIds.Add(steamId);
//                         }
//                     }
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Error fetching contributors: {ex.Message}");
//         }
//
//         return steamIds;
//     }
//
//     // Resolve profile URL to SteamID64, whether custom or SteamID64 format
//     private static async Task<ulong> ResolveProfileUrlToSteamIdAsync(string profileUrl)
//     {
//         if (profileUrl.Contains("/profiles/"))
//         {
//             // Extract SteamID64 directly from URL
//             string steamIdStr = profileUrl.Split(new[] { "/profiles/" }, StringSplitOptions.None)[1];
//             return ulong.TryParse(steamIdStr, out ulong steamId) ? steamId : 0;
//         }
//
//         if (profileUrl.Contains("/id/"))
//         {
//             // Extract custom ID and resolve to SteamID64
//             string customId = profileUrl.Split(new[] { "/id/" }, StringSplitOptions.None)[1];
//             return await ResolveVanityUrlToSteamIdAsync(customId);
//         }
//
//         return 0;
//     }
//
//     // Helper method to resolve custom ID to SteamID64 via Steam API
//     private static async Task<ulong> ResolveVanityUrlToSteamIdAsync(string customId)
//     {
//         const string SteamApiKey = "2E10580502CFD867C9F86EC3080C7C48"; // Replace with your API key
//         string apiUrl = $"https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1/?key={SteamApiKey}&vanityurl={customId}";
//
//         try
//         {
//             string response = await httpClient.GetStringAsync(apiUrl);
//             JObject jsonResponse = JObject.Parse(response);
//
//             if (jsonResponse["response"]?["success"]?.ToString() == "1")
//             {
//                 string steamIdStr = jsonResponse["response"]["steamid"]?.ToString();
//                 return ulong.TryParse(steamIdStr, out ulong steamId) ? steamId : 0;
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Error resolving vanity URL: {ex.Message}");
//         }
//
//         return 0;
//     }
// }

public static class SteamWorkshopContributors
{
    private static readonly HttpClient httpClient = new HttpClient();

    // Retrieve contributors by workshop ID
    public static async Task<List<string>> GetContributorsAsync(ulong workshopId)
    {
        string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopId}";
        List<string> contributors = new List<string>();

        try
        {
            // Fetch the HTML content of the workshop page
            string response = await httpClient.GetStringAsync(url);

            // Load the HTML document
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response);

            // Select nodes with class "friendBlockLinkOverlay"
            HtmlNodeCollection contributorNodes = htmlDoc.DocumentNode.SelectNodes("//a[@class='friendBlockLinkOverlay']");

            if (contributorNodes != null)
            {
                foreach (HtmlNode node in contributorNodes)
                {
                    // Extract Steam profile URL from href attribute
                    string profileUrl = node.GetAttributeValue("href", null);
                    if (!string.IsNullOrEmpty(profileUrl))
                    {
                        contributors.Add(profileUrl);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching contributors: {ex.Message}");
        }

        return contributors;
    }
}