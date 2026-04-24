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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gen_Con_Hotel_Watch
{
    public partial class MainForm : Form
    {
        public static string hotelSite;
        public static string housingSite;

        private string title = "Gen Con Hotel Search";
        private string key;
        private DateTime FirstNightAvailable;
        private DateTime LastNightAvailable;

        private int maxCounter = 0;
        private int maxNotifications;
        private bool repeat = false;
        private DateTime startTime;
        private DateTime endTime;
        private string timeFormat = "c";

        private MapManager mapManager;

        private List<Hotel> vacancies;

        private int sortedColumnIndex = -1;

        public MainForm()
        {
            InitializeComponent();
            mapManager = new MapManager(comboBoxMap, myMap, trackBarMap);
        }

        private void DisableControls()
        {
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
            toolStripMenuItemStartSearch.Enabled = false;
            toolStripMenuItemStopSearch.Enabled = true;
        }
        private void EnableControls()
        {
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            toolStripMenuItemStartSearch.Enabled = true;
            toolStripMenuItemStopSearch.Enabled = false;
        }
        private void ClearForm()
        {
            listViewDates.Items.Clear();
            listViewHotels.Items.Clear();
            labelRoomType.Text = "";
            textBoxOverallRate.Clear();
            mapManager.ClearMarkers();
        }

        private void FormSearch_Load(object sender, EventArgs e)
        {
            comboBoxUnits.Text = "blocks";
            numericUpDownMaxNotify.Value = 5;
            ApplyConventionSettings();
        }

        private void ApplyConventionSettings()
        {
            hotelSite = textBoxHotelSite.Text;
            housingSite = textBoxHousingSite.Text;
            FirstNightAvailable = dateTimePickerStart.Value.Date;
            LastNightAvailable = dateTimePickerEnd.Value.Date;
            monthCalendarSelection.MinDate = FirstNightAvailable;
            monthCalendarSelection.MaxDate = LastNightAvailable;
        }

        private void ConventionSettings_Changed(object sender, EventArgs e)
        {
            ApplyConventionSettings();
        }

        private HotelFilter GetHotelFilter()
        {
            DateTime CheckInDate = monthCalendarSelection.SelectionStart;
            DateTime CheckOutDate = monthCalendarSelection.SelectionEnd;
            if ((CheckInDate > FirstNightAvailable) || (CheckOutDate < LastNightAvailable))
            {
                string message = String.Format("{0} is the latest available check in date and \r" +
                    "{1} is the earliest available check out.\r" +
                    "Please reselect your check in and check out dates.",
                    CheckInDate.ToShortDateString(),
                    CheckOutDate.ToShortDateString());
                MessageBox.Show(message, "Dates Unavailable!",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }

            HotelFilter info = new HotelFilter()
            {
                NumOfGuests = (int)numericUpDownGuests.Value,
                NumOfRooms = (int)numericUpDownRooms.Value,
                FreeBreakfast = checkBoxBreakfast.Checked,
                FreeParking = checkBoxParking.Checked,
                CheckInDate = CheckInDate,
                CheckOutDate = CheckOutDate,
                DistanceUnits = (DistanceUnit)Enum.Parse(typeof(DistanceUnit), comboBoxUnits.Text),
                Distance = (float)numericUpDownSearch.Value,
                MaximumNotifications = (int)numericUpDownMaxNotify.Value
            };
            return info;
        }

        private bool VerifyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Please enter your hotel access key.");
                return false;
            }
            else
            {
                this.key = textBoxKey.Text;
                housingSite = String.Format(housingSite, key);
                return true;
            }
        }

        private async void Button_Start_Click(object sender, EventArgs e)
        {
            housingSite = textBoxHousingSite.Text;
            if (!VerifyKey(textBoxKey.Text)) return;

            ClearForm();

            HotelFilter filter = GetHotelFilter();
            if (filter == null) return;

            try
            {
                vacancies = await Scraper.FindHotels(key, filter);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching hotels: " + ex.Message);
                EnableControls();
                return;
            }

            if (vacancies == null || vacancies.Count == 0)
            {
                MessageBox.Show("No hotels found or the connection failed.");
                return;
            }

            AddHotelsToDisplay(vacancies);

            List<Hotel> results = filter.GetMatchingHotels(vacancies);

            // If there were hotels found that match the search criteria, send notifications.
            if (results != null)
            {
                if (maxCounter < maxNotifications)
                    NotificationManager.SendNotifications(results, checkBoxEmail.Checked, checkBoxPopup.Checked);
                maxCounter += 1;
            }
            else
                maxCounter = 0;

            if (repeat)
            {
                startTime = DateTime.Now;
                endTime = startTime.AddMinutes((double)numericUpDownRepeat.Value);

                Text = title + " - " + GetCountdown(endTime);

                timer1.Start();

                DisableControls();
            }
        }
        private void Button_Stop_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            Text = title;
            EnableControls();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            Text = title + " - " + GetCountdown(endTime);
            if (endTime < DateTime.Now)
            {
                if (repeat)
                {
                    startTime = DateTime.Now;
                    endTime = startTime.AddMinutes((double)numericUpDownRepeat.Value);
                    Text = title + " - " + GetCountdown(endTime.AddSeconds(-1));
                    Button_Start_Click(sender, e);
                }
                else
                {
                    timer1.Stop();
                    Text = title;
                }
            }
        }
        private string GetCountdown(DateTime time)
        {
            TimeSpan diff = time.Subtract(DateTime.Now);
            return diff.ToString(timeFormat);
        }

        private void AddHotelsToDisplay(List<Hotel> hotels)
        {
            // Add the hotels to the form
            foreach (Hotel hotel in hotels)
            {
                // Get the hotel name and add it to the list
                string hotelname = HotelManager.GetPrettyName(hotel.Name);
                ListViewItem hotelItem = listViewHotels.Items.Add(hotelname, hotelname, 0);

                // Add the distance to the list
                hotelItem.SubItems.Add(hotel.DistanceFromEvent.ToString());

                // Add the distance unit to the list
                string distUnit = hotel.DistanceUnit.ToString();
                hotelItem.SubItems.Add(distUnit);

                // Lookup breakfast and parking and add it to the list
                HotelData data = HotelData.data.Find(x => x.Name.Equals(hotel.Name));
                hotelItem.SubItems.Add(data.Breakfast ? "yes" : "no");
                hotelItem.SubItems.Add(data.Parking ? "yes" : "no");

                // Add results to the map
                mapManager.AddHotelToMap(hotel.Name, hotel.Latitude, hotel.Longitude);
            }
        }

        private void Button_Close_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void MyMap_OnMapTypeChanged(GMap.NET.MapProviders.GMapProvider provider) => mapManager.MapTypeChanged(provider);
        private void MyMap_OnMarkerClick(GMap.NET.WindowsForms.GMapMarker marker, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                GMap.NET.PointLatLng position = marker.Position;
                mapManager.SelectHotel(position.Lat, position.Lng);

                float latitude = (float)position.Lat;
                float longitude = (float)position.Lng;
                Hotel foundHotel = vacancies.SingleOrDefault(x => x.Latitude == latitude && x.Longitude == longitude);
                if (foundHotel == null) return;
                ListViewItem[] sel = listViewHotels.Items.Find(foundHotel.Name, false);
                if (sel.Length > 0)
                {
                    sel[0].Selected = true;
                }
            }
        }
        private void MyMap_MouseWheel(object sender, MouseEventArgs e)
        {
            // if past zoom limits, go no further
            if ((myMap.Zoom == myMap.MaxZoom) || (myMap.Zoom == myMap.MinZoom)) return;

            if (sender is GMap.NET.WindowsForms.GMapControl map)
            {
                // if mousewheel is spun within map boundaries
                if (e.X > 0 && 
                    e.Y > 0 && 
                    e.X < map.Width 
                    && e.Y < map.Height)
                {
                    int inc = e.Delta;
                    if (inc > 0)
                        trackBarMap.Value = mapManager.ZoomIn();
                    if (inc < 0)
                        trackBarMap.Value = mapManager.ZoomOut();
                }
            }
        }
        private void Button_ZoomIn_Click(object sender, EventArgs e)
        {
            int zoomValue = mapManager.ZoomIn();
            if (zoomValue >= 0)
                trackBarMap.Value = zoomValue;
        }
        private void Button_ZoomOut_Click(object sender, EventArgs e)
        {
            int zoomValue = mapManager.ZoomOut();
            if (zoomValue >= 0)
                trackBarMap.Value = zoomValue;
        }
        private void TrackBarMap_Scroll(object sender, EventArgs e) => mapManager.Zoom(trackBarMap.Value);
        private void ComboBoxMap_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBoxMap.SelectedItem is GMap.NET.MapProviders.GMapProvider provider)
                mapManager.UpdateMapProvider(provider);
        }

        private void ListViewHotels_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewHotels.SelectedItems.Count == 0) return;

            // get hotel from selection
            string hotelName = listViewHotels.SelectedItems[0].Name;
            Hotel hotel = vacancies?.SingleOrDefault(q => q.Name == hotelName);
            if (hotel == null) return;

            BlockInfo selection = hotel.Blocks[0];
            textBoxOverallRate.Text = selection.Charge.ToString("C2");
            labelRoomType.Text = HotelManager.GetPrettyName(selection.Name);

            Inventory[] nights = selection.Inventory;
            DateTime[] availableNights = new DateTime[nights.Count()];
            int m = 0;
            listViewDates.Items.Clear();
            foreach (Inventory night in nights)
            {
                availableNights[m] = new DateTime(night.Date[0], night.Date[1], night.Date[2]);
                ListViewItem nightItem = listViewDates.Items.Add(availableNights[m].ToShortDateString());
                nightItem.SubItems.Add(night.Available.ToString());
                nightItem.SubItems.Add(night.Rate.ToString("C2"));
                m += 1;
            }

            mapManager.SelectHotel(hotel.Latitude, hotel.Longitude);
        }
        private void ListViewHotels_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine whether the column is the same as the last column clicked.
            if (e.Column != sortedColumnIndex)
            {
                // Set the sort column to the new column.
                sortedColumnIndex = e.Column;
                // Set the sort order to ascending by default.
                listViewHotels.Sorting = SortOrder.Ascending;
            }
            // Determine what the last sort order was and change it.
            else listViewHotels.Sorting = listViewHotels.Sorting == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;

            // Call the sort method to manually sort.
            listViewHotels.Sort();
            // Set the ListViewItemSorter property to a new ListViewItemComparer object.
            listViewHotels.ListViewItemSorter = new ListViewItemComparer(e.Column, listViewHotels.Sorting);
        }

        private void CheckBox_Repeat_Changed(object sender, EventArgs e)
        {
            if (checkBoxRepeat.Checked)
            {
                repeat = true;
                numericUpDownRepeat.Enabled = true;
            }
            else
            {
                repeat = false;
                numericUpDownRepeat.Enabled = false;
            }

        }
        private void CheckBox_Email_Changed(object sender, EventArgs e)
        {
            if (checkBoxEmail.Checked)
                buttonEmailInfo.Enabled = true;
            else
                buttonEmailInfo.Enabled = false;
        }
        private void Button_EmailInfo_Click(object sender, EventArgs e)
        {
            EmailForm emailForm = new EmailForm();
            emailForm.ShowDialog();
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            else
                Activate();
        }
        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (WindowState == FormWindowState.Minimized)
                    WindowState = FormWindowState.Normal;
                else
                    Activate();
            }
        }
        private void ToolStrip_StartSearch_Click(object sender, EventArgs e)
        {
            Button_Start_Click(sender, e);
        }
        private void ToolStrip_StopSearch_Click(object sender, EventArgs e)
        {
            Button_Stop_Click(sender, e);
        }
        private void ToolStrip_Exit_Click(object sender, EventArgs e)
        {
            Button_Close_Click(sender, e);
        }

        private void Key_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new ProcessStartInfo("https://www.gencon.com/housing");
            Process.Start(sInfo);
        }

        private void MaxNotifications_ValueChanged(object sender, EventArgs e)
        {
            maxNotifications = (int)numericUpDownMaxNotify.Value;
        }
    }

    // Implements the manual sorting of items by columns.
    class ListViewItemComparer : IComparer
    {
        private int col;
        private SortOrder order;
        public ListViewItemComparer()
        {
            col = 0;
            order = SortOrder.Ascending;
        }
        public ListViewItemComparer(int column, SortOrder order)
        {
            col = column;
            this.order = order;
        }
        public int Compare(object x, object y)
        {
            int returnVal = -1;
            
            // Determine whether the type being compared is a double.
            try
            {
                double firstNumber = Double.Parse(((ListViewItem)x).SubItems[col].Text);
                double secondNumber = Double.Parse(((ListViewItem)y).SubItems[col].Text);
                returnVal = firstNumber.CompareTo(secondNumber);
            }
            catch (FormatException)
            {
                returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
            }
            catch (IndexOutOfRangeException)
            {
                return ((ListViewItem)x).SubItems.Count.CompareTo(((ListViewItem)y).SubItems.Count);
            }

            // Determine whether the sort order is descending.
            if (order == SortOrder.Descending)
                returnVal *= -1; // Invert the value returned by String.Compare.
            return returnVal;
        }
    }

}
