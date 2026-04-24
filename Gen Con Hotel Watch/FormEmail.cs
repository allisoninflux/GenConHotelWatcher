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
using Gen_Con_Hotel_Watch.Notifications;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Gen_Con_Hotel_Watch
{
    public partial class EmailForm : Form
    {
        int lastNumber;
        int shift = 26;
        int initialHeight;

        public EmailForm()
        {
            InitializeComponent();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Dispose();
        }
        private void ButtonSubmit_Click(object sender, EventArgs e)
        {
            List<Email> emails = GetEmailInfo();
            NotificationManager.emails = emails;
            Dispose();
        }
        private void ButtonTest_Click(object sender, EventArgs e)
        {
            List<Email> emails = GetEmailInfo();

            string subject = "Test Email from Gen Con Hotel Watch";
            string body = "<html><body><ul>";
            body += "<li><b>Hotel 1</b><ul><li>3 blocks away<br></li></ul></li>";
            body += "<li><b>Hotel 2</b><ul><li>2 blocks away<br></li></ul></li>";
            body += "</ul></body></html>";

            NotificationManager.SendTestEmail(emails, subject, body);
        }

        private List<Email> GetEmailInfo()
        {
            List<Email> emails = new List<Email>();
            List<Control> comboBoxSMTPArray = FindControlsByName("comboBoxSMTP");
            List<Control> textBoxEmailFromArray = FindControlsByName("textBoxEmailFrom");
            List<Control> maskedTextBoxPasswordArray = FindControlsByName("maskedTextBoxPassword");
            List<Control> textBoxEmailToArray = FindControlsByName("textBoxEmailTo");

            for (int i = 0; i < comboBoxSMTPArray.Count; i++)
            {
                Email email = new Email
                {
                    Smtp = comboBoxSMTPArray[i].Text,
                    From = textBoxEmailFromArray[i].Text,
                    Pword = maskedTextBoxPasswordArray[i].Text,
                    To = textBoxEmailToArray[i].Text
                };
                emails.Add(email);
            }

            return emails;
        }

        private void AddLabel(string name)
        {
            // find previous label
            Control previousLabel = FindLatest(name);
            // get number of previous label
            int.TryParse(previousLabel.Name.Substring(previousLabel.Name.Length - 1), out lastNumber);
            // create new label
            Label newLabel = new Label
            {
                Name = name + (lastNumber + 1).ToString(),
                Size = new Size(previousLabel.Width, previousLabel.Height),
                Text = previousLabel.Text,
                Top = previousLabel.Top + shift,
                Left = previousLabel.Left
            };

            Controls.Add(newLabel);
        }
        private void AddTextBox(string name, Boolean isMasked)
        {
            // find previous textBox
            Control textBoxPrevious = FindLatest(name);
            // get the number of previous textBox
            int.TryParse(textBoxPrevious.Name.Substring(textBoxPrevious.Name.Length - 1), out lastNumber);
            // create new textBox
            if(isMasked)
            {
                MaskedTextBox maskedTextBox = new MaskedTextBox
                {
                    Name = name + (lastNumber + 1).ToString(),
                    Size = new Size(textBoxPrevious.Width, textBoxPrevious.Height),
                    Top = textBoxPrevious.Top + shift,
                    Left = textBoxPrevious.Left,
                    PasswordChar = '*'
                };
                Controls.Add(maskedTextBox);

            } else
            {
                TextBox textBox = new TextBox
                {
                    Name = name + (lastNumber + 1).ToString(),
                    Size = new Size(textBoxPrevious.Width, textBoxPrevious.Height),
                    Top = textBoxPrevious.Top + shift,
                    Left = textBoxPrevious.Left
                };
                Controls.Add(textBox);
            }
        }
        private void AddComboBox(string name)
        {
            // get info from previous combo box
            Control comboBoxPrevious = FindLatest(name);
            int.TryParse(comboBoxPrevious.Name.Substring(comboBoxPrevious.Name.Length - 1), out lastNumber);
            object[] contents = new object[((ComboBox)comboBoxPrevious).Items.Count];
            ((ComboBox)comboBoxPrevious).Items.CopyTo(contents, 0);
            
            ComboBox comboBox = new ComboBox
            {
                Name = name + (lastNumber + 1).ToString(),
                Size = new Size(comboBoxPrevious.Width, comboBoxPrevious.Height),
                Top = comboBoxPrevious.Top + shift,
                Left = comboBoxPrevious.Left,
                Text = comboBoxPrevious.Text
            };
            comboBox.Items.AddRange(contents);

            Controls.Add(comboBox);
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            if (Height < initialHeight + shift * 9)
            {
                if (Height == initialHeight)
                    buttonRemove.Enabled = true;

                Height = Height + shift;

                if (Height == initialHeight + shift * 9)
                    buttonAdd.Enabled = false;

            }
            else return;

            AddLabel("labelEmailSMTP");
            AddComboBox("comboBoxSMTP");

            AddLabel("labelEmailFrom");
            AddTextBox("textBoxEmailFrom", false);

            AddLabel("labelEmailPassword");
            AddTextBox("maskedTextBoxPassword", true);

            AddLabel("labelEmailTo");
            AddTextBox("textBoxEmailTo", false);

        }
        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            if (!(Height == initialHeight))
            {
                if (Height == initialHeight + shift)
                    buttonRemove.Enabled = false;
                if (Height == initialHeight + shift * 9)
                    buttonAdd.Enabled = true;
                Height = Height - shift;
            }
            else return;

            FindLatest("labelEmailSMTP").Dispose();
            FindLatest("comboBoxSMTP").Dispose();
            FindLatest("labelEmailFrom").Dispose();
            FindLatest("textBoxEmailFrom").Dispose();
            FindLatest("labelEmailPassword").Dispose();
            FindLatest("maskedTextBoxPassword").Dispose();
            FindLatest("labelEmailTo").Dispose();
            FindLatest("textBoxEmailTo").Dispose();
        }

        private void FormEmail_Load(object sender, EventArgs e)
        {
            initialHeight = Height;
            List<Email> emails = NotificationManager.emails;
            if (emails != null)
            {
                foreach(Email email in emails)
                {
                    Control comboBoxSMTPlast = FindLatest("comboBoxSMTP");
                    Control textBoxEmailFromlast = FindLatest("textBoxEmailFrom");
                    Control maskedTextBoxPasswordlast = FindLatest("maskedTextBoxPassword");
                    Control textBoxEmailTolast = FindLatest("textBoxEmailTo");

                    comboBoxSMTPlast.Text = email.Smtp;
                    textBoxEmailFromlast.Text = email.From;
                    maskedTextBoxPasswordlast.Text = email.Pword;
                    textBoxEmailTolast.Text = email.To;
                }
            }
        }

        private List<Control> FindControlsByName(string name)
        {
            List<Control> found = new List<Control>();
            foreach (Control item in Controls)
            {
                if (item.Name.Contains(name))
                    found.Add(item);
            }
            return found;
        }

        private Control FindLatest(string name)
        {
            List<Control> matches = FindControlsByName(name);

            Control result = matches[0];

            if (matches.Count == 1) return result;

            foreach(Control c in matches)
            {
                string name1 = result.Name;
                string name2 = c.Name;

                if (!(int.TryParse(name1.Substring(name1.Length - 1), out int Pos1)))
                    return result;
                if (!(int.TryParse(name2.Substring(name2.Length - 1), out int Pos2)))
                    return result;

                if (Pos1 < Pos2)
                    result = c;
            }

            return result;
        }
    }
}
