/*  A small windows form application to watch for vacancies in Gen Con's hotel block
 *  Copyright (C) 2017 Aaron Echols (aaronly@gmail.com)
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using Gen_Con_Hotel_Watch.Hotels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gen_Con_Hotel_Watch.Scraper
{
    class Scraper
    {

        public static async Task<List<Hotel>> FindHotels(string key, Filter filter)
        {
            // Scrape the information from the website
            string searchResults = await ScrapeHotelInfo(key, filter);

            if (string.IsNullOrEmpty(searchResults))
                return new List<Hotel>();

            // Find available vacancies
            return ParseHotels(searchResults);
        }

        private static async Task<string> ScrapeHotelInfo(string key, Filter filter)
        {
            // create filter
            Dictionary<string, string> filters = new Dictionary<string, string>
            {
                { "hotelId", "0" },
                { "blockMap.blocks[0].blockId", "0" },
                { "blockMap.blocks[0].checkIn", filter.CheckInDate.ToString("yyyy-MM-dd") },
                { "blockMap.blocks[0].checkOut", filter.CheckOutDate.ToString("yyyy-MM-dd") },
                { "blockMap.blocks[0].numberOfGuests", filter.NumOfGuests.ToString() },
                { "blockMap.blocks[0].numberOfRooms", filter.NumOfRooms.ToString() },
                { "blockMap.blocks[0].numberOfChildren", "0" }
            };
            FormUrlEncodedContent query = new FormUrlEncodedContent(filters);

            string mainSite = string.Format("https://aws.passkey.com/reg/{0}/null/null/1/0/null", key);
            Uri MainAddress = new Uri(mainSite);
            Uri HotelsAddress = new Uri(MainForm.hotelSite);
            TimeSpan timeout = new TimeSpan(0, 2, 0);

            CookieContainer cookies;
            try
            {
                #region Get Initial Cookies
                using (HttpClient mainClient = new HttpClient()
                {
                    BaseAddress = MainAddress,
                    Timeout = timeout
                })
                {
                    HttpResponseMessage mainResponse = await mainClient.GetAsync(MainAddress);
                    if (mainResponse.IsSuccessStatusCode)
                        cookies = ReadCookies(mainResponse);
                    else return null;
                }
                using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = cookies })
                #endregion

                #region Get Search Results
                using (HttpClient hotelClient = new HttpClient(handler)
                {
                    BaseAddress = HotelsAddress,
                    Timeout = timeout
                })
                {
                    HttpResponseMessage hotelResponse = await hotelClient.PostAsync(HotelsAddress, query);
                    if (hotelResponse.IsSuccessStatusCode)
                        return await hotelResponse.Content.ReadAsStringAsync();
                    else
                        return null;
                }
                #endregion
            }
            catch (TaskCanceledException ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }

        private static CookieContainer ReadCookies(HttpResponseMessage response)
        {
            var pageUri = response.RequestMessage.RequestUri;
            var cookieContainer = new CookieContainer();
            if (response.Headers.TryGetValues("set-cookie", out IEnumerable<string> cookies))
            {
                foreach (var c in cookies)
                    cookieContainer.SetCookies(pageUri, c);
            }
            return cookieContainer;
        }

        private static List<Hotel> ParseHotels(string response)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(response);

            var node = doc.DocumentNode.Descendants().SingleOrDefault(x => x.Id == "last-search-results");
            if (node == null)
            {
                MessageBox.Show("Could not find hotel data in the response. The page format may have changed.");
                return new List<Hotel>();
            }

            string results = @"{""hotels"":" + node.InnerHtml + "}";

            List<Hotel> hotels;
            try
            {
                hotels = JsonConvert.DeserializeObject<List<Hotel>>(results);
            }
            catch (JsonException ex)
            {
                MessageBox.Show("Failed to parse hotel data: " + ex.Message);
                return new List<Hotel>();
            }

            List<Hotel> filtered = new List<Hotel>();
            foreach (Hotel hotel in hotels)
            {
                if (hotel.Blocks.Length > 0)
                {
                    hotel.Name = HotelManager.GetPrettyName(hotel.Name);
                    filtered.Add(hotel);
                }
            }
            return filtered;
        }

    }
}
