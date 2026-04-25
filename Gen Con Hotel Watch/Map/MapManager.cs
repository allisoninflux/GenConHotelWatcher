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
 using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Gen_Con_Hotel_Watch.Map
{
    class MapManager
    {
        private ComboBox comboBox;
        private TrackBar trackBar;
        private GMapControl map;
        private List<PointLatLng> conventionCenterPolygonPoints = new List<PointLatLng>
            {
                new PointLatLng(39.765818, -86.167050),
                new PointLatLng(39.765719, -86.161772),
                new PointLatLng(39.762684, -86.161900),
                new PointLatLng(39.763707, -86.166578),
                new PointLatLng(39.764672, -86.167116)
            };

        private GMapOverlay hotelMarkers = new GMapOverlay("hotels");

        private string selectedToolTip = "selected";

        public MapManager(ComboBox comboBox, GMapControl map, TrackBar trackBar)
        {
            this.comboBox = comboBox;
            this.trackBar = trackBar;
            this.map = map;

            // Specifiying the map provider and center location
            map.MapProvider = GMap.NET.MapProviders.BingMapProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            map.Position = new PointLatLng(39.764386, -86.164050); // Center map on Indianapolis
            map.ShowCenter = false;

            // add the convention center polygon
            GMapOverlay iccOverlay = new GMapOverlay("ICC");
            
            GMapPolygon polygon = new GMapPolygon(conventionCenterPolygonPoints, "ICC")
            {
                Fill = new SolidBrush(Color.FromArgb(100, Color.DarkBlue)),
                Stroke = new Pen(Color.DarkBlue, 1)
            };
            iccOverlay.Polygons.Add(polygon);

            // add the overlays to the map
            map.Overlays.Add(hotelMarkers);
            map.Overlays.Add(iccOverlay);

            // set the drag button
            map.DragButton = MouseButtons.Left;
            map.Select();

            // initialize the combo bo
            comboBox.Items.Add(BingMapProvider.Instance);
            comboBox.Items.Add(BingSatelliteMapProvider.Instance);
            comboBox.SelectedText = map.MapProvider.Name;
        }

        public void MapTypeChanged(GMapProvider provider)
        {
            if (map.MapProvider.MaxZoom != null)
                map.MaxZoom = (int)map.MapProvider.MaxZoom;
            if (map.MapProvider.MaxZoom > 5)
                map.MinZoom = (int)map.MapProvider.MinZoom;
            int zoomDelta = map.MaxZoom - map.MinZoom;
            trackBar.Maximum = zoomDelta;
            trackBar.Value = (int)map.Zoom - map.MinZoom;
        }

        public void AddHotelToMap(string name, double latitude, double longitude)
        {
            PointLatLng hotelPoint = new PointLatLng(latitude, longitude);
            GMarkerGoogle marker = new GMarkerGoogle(hotelPoint, GMarkerGoogleType.red)
            {
                ToolTipText = name
            };
            hotelMarkers.Markers.Add(marker);
            map.Update();
        }

        public void ClearMarkers() => hotelMarkers.Clear();

        public void ClearSelectedMarkers()
        {
            // remove any previously selected markers
            List<GMapMarker> previousMarkers = hotelMarkers.Markers.Where(
                x => !x.ToolTip.Equals(selectedToolTip))
                .ToList();
            ClearMarkers();
            previousMarkers.ForEach(m => hotelMarkers.Markers.Add(m));
        }

        public void SelectHotel(double latitude, double longitude)
        {
            ClearSelectedMarkers();

            PointLatLng position = new PointLatLng(latitude, longitude);
            GMarkerGoogle newMarker = new GMarkerGoogle(position, GMarkerGoogleType.green)
            {
                ToolTipText = selectedToolTip
            };
            hotelMarkers.Markers.Add(newMarker);

            //GMarkerGoogle oldMarker = (GMarkerGoogle)hotelMarkers.Markers.SingleOrDefault(n => n.ToolTipText == null);
            //hotelMarkers.Markers.Remove(oldMarker);
            //PointLatLng newPos = sender.Position;
            //GMarkerGoogle newMarker = new GMarkerGoogle(newPos, GMarkerGoogleType.green);
            //hotelMarkers.Markers.Add(newMarker);
        }

        public int ZoomIn()
        {
            int zoomValue = -1;
            if (map.Zoom != map.MaxZoom)
            {
                double prevZoom = map.Zoom;
                map.Zoom = prevZoom + 1;
                zoomValue = (int)map.Zoom - map.MinZoom;
            }
            return zoomValue;
        }
        public int ZoomOut()
        {
            int zoomValue = -1;
            if (map.Zoom != map.MinZoom)
            {
                double prevZoom = map.Zoom;
                map.Zoom = prevZoom - 1;
                zoomValue = (int)map.Zoom - map.MinZoom;
            }
            return zoomValue;
        }
        public void Zoom(int zoomLevel) => map.Zoom = zoomLevel + map.MinZoom;

        public void UpdateMapProvider(GMapProvider provider)
        {
            map.MapProvider = provider;
            map.Update();
        }
    }
}
