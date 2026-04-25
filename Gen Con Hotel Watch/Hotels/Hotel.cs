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
using Gen_Con_Hotel_Watch.Hotels.Info;
using Newtonsoft.Json;

namespace Gen_Con_Hotel_Watch.Hotels
{
    [JsonObject]
    public class Hotel
    {
        public Address Address { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ChildPolicy { get; set; }
        public string RewardsPrograms { get; set; }
        public BlockInfo[] Blocks { get; set; }
        public float MinAvgRate { get; set; }
        public float MaxAvgRate { get; set; }
        public int MaxGuests { get; set; }
        public bool ChildrenAffectRate { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public DistanceUnit DistanceUnit { get; set; }
        public bool Marker { get; set; }
        public int MultiPayment { get; set; }
        public float DistanceFromEvent { get; set; }
        public string PhoneNumber2 { get; set; }
        public string TollFreeNumber { get; set; }
        public string FaxNumber { get; set; }
        public string FaxNumber2 { get; set; }
        public Amenity[] Amenities { get; set; }
        public ExtraInfo AccomodationType { get; set; }
        public ExtraInfo StarRating { get; set; }
        public bool ShowAccessible { get; set; }
        public string MarketingMessage { get; set; }
        public string MessageMap { get; set; }
        public bool Hqhotel { get; set; }
        public int[] ResAccessDate { get; set; }
        public int[] HotelCloseDate { get; set; }
        public int SplitFolio { get; set; }
        public string FacebookPage { get; set; }
        public int UserDefinedRank { get; set; }
        public bool SuppressChildrenCount { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
    }

}
