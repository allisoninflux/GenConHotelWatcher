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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;

namespace Gen_Con_Hotel_Watch.Notifications
{
    public class NotificationManager
    {
        public static List<Email> emails;

        public static void SendTestEmail(List<Email> emailAddresses, string subject, string body) => SendEmail(emailAddresses, subject, body);

        private static void SendEmail(List<Email> emailAddresses, string subject, string body)
        {
            foreach (Email email in emailAddresses)
            {
                SmtpClient client = new SmtpClient(email.Smtp)
                {
                    EnableSsl = true,
                    Port = 587,
                    Credentials = new NetworkCredential(email.From, email.Pword)
                };
                client.SendCompleted += new SendCompletedEventHandler(EmailSentCallback);

                MailMessage message = new MailMessage()
                {
                    From = new MailAddress(email.From),
                    IsBodyHtml = true,
                    Subject = subject,
                    Body = body
                };
                message.To.Add(email.To);

                client.SendAsync(message, "Gen Con Hotel Watch Notification");
            }
        }

        private static void EmailSentCallback(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
                MessageBox.Show(String.Format("[{0}] {1}", e.UserState, e.Error.ToString()), "Email Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void SendNotifications(List<Hotel> hotels, bool sendEmail, bool showPopup)
        {
            if (hotels == null || hotels.Count == 0) return;

            string popup = "";
            string emailBody = "";
            foreach (Hotel hotel in hotels)
            {
                string distUnit = hotel.DistanceUnit.ToString();
                string distFromEvent = hotel.DistanceFromEvent.ToString();
                emailBody += String.Format("<li><b>{0}</b><ul><li>{1} {2} away</li></ul></li>",
                    hotel.Name, distFromEvent, distUnit);
                popup += String.Format("{0}\r\t{1} {2}",
                    hotel.Name, distFromEvent, distUnit) + "\r\r";
            }
            string email = "<html><body><ul>" + emailBody +
                String.Format("</ul><a href=\"{0}\">Go to Housing page.</a></body></html>", MainForm.housingSite);

            // Send email if it is not empty
            if (sendEmail && (emails != null))
                SendEmail(emails, "Vacant rooms found!", email);

            if (showPopup)
                MessageBox.Show(new Form { TopMost = true }, popup, "Vacant rooms found!", MessageBoxButtons.OK);
        }
    }



}
