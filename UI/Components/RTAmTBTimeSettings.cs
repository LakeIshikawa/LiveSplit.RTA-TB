﻿using LiveSplit.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public partial class RTAmTBTimeSettings : UserControl
    {
        public int PointsPerFrame { get; set; }
        public double FramesPerSecond { get; set; }
        public int ContinueFrames { get; set; }
        public SortedDictionary<int, int> IGTLookup { get; set; }

        protected const double FPS_NTSCFULL = 59.94;
        protected const double FPS_PALFULL = 50;

        protected const double FPS_NTSCHALF = 29.97;
        protected const double FPS_PALHALF = 25;

        protected const double FPS_PC = 60;
        protected const double FPS_GENSMS = 59.9228;

        private IButtonControl parentDefault;

        public RTAmTBTimeSettings()
        {
            InitializeComponent();

            // initialise settings to Sonic 2
            PointsPerFrame = 100;
            FramesPerSecond = FPS_GENSMS;
            ContinueFrames = 120;

            IGTLookup = new SortedDictionary<int, int>();
            IGTLookup.Add(30000, 62000);
            IGTLookup.Add(45000, 22000);
            IGTLookup.Add(60000, 5000);
            IGTLookup.Add(90000, 4000);
            IGTLookup.Add(120000, 3000);
            IGTLookup.Add(180000, 2000);
            IGTLookup.Add(240000, 1000);
            IGTLookup.Add(300000, 500);

            // only PointsPerFrame and PointsMultiplicationFactor can be automatically dealt with; all other settings have complex requirements
            s_PointsPerFrame.DataBindings.Add("Value", this, "PointsPerFrame", false, DataSourceUpdateMode.OnPropertyChanged);
            s_ContinueFrames.DataBindings.Add("Value", this, "ContinueFrames", false, DataSourceUpdateMode.OnPropertyChanged);
        }

        public void SetSettings(XmlNode node)
        {
            var element = (XmlElement)node;
            PointsPerFrame = SettingsHelper.ParseInt(element["PointsPerFrame"], 100);
            FramesPerSecond = SettingsHelper.ParseDouble(element["FramesPerSecond"], FPS_GENSMS);
            ContinueFrames = SettingsHelper.ParseInt(element["ContinueFrames"], 1);
            var lookupElement = element["IGTLookup"];
            if (lookupElement != null)
            {
                IGTLookup.Clear();
                foreach (var row in lookupElement.ChildNodes)
                {
                    var rowEl = (XmlElement)row;
                    IGTLookup.Add(Int32.Parse(rowEl["MaxTime"].InnerText), Int32.Parse(rowEl["Points"].InnerText));
                }
            }
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            var parent = document.CreateElement("Settings");
            CreateSettingsNode(document, parent);
            return parent;
        }

        public int GetSettingsHashCode()
        {
            return CreateSettingsNode(null, null);
        }

        private int CreateSettingsNode(XmlDocument document, XmlElement parent)
        {
            int hashCode;
            hashCode = SettingsHelper.CreateSetting(document, parent, "Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3)) ^
                SettingsHelper.CreateSetting(document, parent, "PointsPerFrame", PointsPerFrame) ^
                SettingsHelper.CreateSetting(document, parent, "FramesPerSecond", FramesPerSecond) ^
                SettingsHelper.CreateSetting(document, parent, "ContinueFrames", ContinueFrames);

            XmlElement lookupElement = null;
            if (document != null)
            {
                lookupElement = document.CreateElement("IGTLookup");
                parent.AppendChild(lookupElement);
            }

            int count = 1;
            foreach (int ms in IGTLookup.Keys)
            {
                int tmpHash = 0;
                XmlElement row = null;
                if (document != null)
                {
                    row = document.CreateElement("Row");
                    lookupElement.AppendChild(row);
                }
                tmpHash = SettingsHelper.CreateSetting(document, row, "MaxTime", ms) ^
                    SettingsHelper.CreateSetting(document, row, "Points", IGTLookup[ms]);
                hashCode = tmpHash * count;

                count++;
            }
            return hashCode;
        }

        private void linkReadme_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Sophira/LiveSplit.RealTimeMinusBonuses/blob/master/README.md");
        }

        private void RealTimeMinusBonusesSettings_Load(object sender, System.EventArgs e)
        {
            // FPS setting (Points Per Frame is just a normal data binding)
            s_fpsCustomValue.Value = (decimal)FramesPerSecond;
            if (FramesPerSecond == FPS_NTSCFULL)
                s_fpsNTSCFull.Checked = true;
            else if (FramesPerSecond == FPS_PALFULL)
                s_fpsPALFull.Checked = true;
            else if (FramesPerSecond == FPS_NTSCHALF)
                s_fpsNTSCHalf.Checked = true;
            else if (FramesPerSecond == FPS_PALHALF)
                s_fpsPALHalf.Checked = true;
            else if (FramesPerSecond == FPS_PC)
                s_fpsPC.Checked = true;
            else if (FramesPerSecond == FPS_GENSMS)
                s_fpsGenSMS.Checked = true;
            else
                s_fpsCustom.Checked = true;
            UpdateFPS();

            // IGT Lookup table
            var igt = s_LookupTable.Items;
            igt.Clear();
            foreach (int time in IGTLookup.Keys)
            {
                double timesec = time / 1000.0;
                var row = igt.Add(timesec.ToString(), timesec.ToString(), null);
                row.SubItems.Add(IGTLookup[time].ToString());
            }
            s_LookupTable.ListViewItemSorter = new LookupTableSorter(0);

            UpdateLookup();
        }

        private void s_fpsNTSC_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFPS();
        }

        private void s_fpsPAL_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFPS();
        }

        private void s_fpsCustom_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFPS();
        }

        private void UpdateFPS()
        {
            s_fpsCustomValue.Enabled = s_fpsCustom.Checked;

            if (s_fpsNTSCFull.Checked)
                FramesPerSecond = FPS_NTSCFULL;
            else if (s_fpsPALFull.Checked)
                FramesPerSecond = FPS_PALFULL;
            else if (s_fpsNTSCHalf.Checked)
                FramesPerSecond = FPS_NTSCHALF;
            else if (s_fpsPALHalf.Checked)
                FramesPerSecond = FPS_PALHALF;
            else if (s_fpsPC.Checked)
                FramesPerSecond = FPS_PC;
            else if (s_fpsGenSMS.Checked)
                FramesPerSecond = FPS_GENSMS;
            else
                FramesPerSecond = (double)s_fpsCustomValue.Value;
        }

        private void s_fpsCustomValue_ValueChanged(object sender, EventArgs e)
        {
            UpdateFPS();
        }

        private void s_fpsNTSCHalf_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFPS();
        }

        private void s_fpsPALHalf_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFPS();
        }

        private void s_fpsPC_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFPS();
        }

        private void s_fpsGenSMS_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFPS();
        }

        private void IGTRemoveRows_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in s_LookupTable.SelectedItems)
            {
                item.Remove();
            }
            UpdateLookup();
        }

        private void UpdateLookup()
        {
            IGTLookup.Clear();
            foreach (ListViewItem item in s_LookupTable.Items)
            {
                double sec = Double.Parse(item.Text);
                int ms = (int)Math.Round(sec * 1000);

                IGTLookup.Add(ms, Int32.Parse(item.SubItems[1].Text));
            }
        }

        private void ClearIGTTable_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Really clear the lookup table?", "RTA-TB", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                s_LookupTable.Items.Clear();
                UpdateLookup();
            }
        }

        private void lookupAddButton_Click(object sender, EventArgs e)
        {
            double seconds;
            //bool convertedsecs = Double.TryParse(lookupAddMaxIGT.Text, out seconds);
            TimeSpan maxigt;
            try
            {
                maxigt = TimeSpanParser.Parse(lookupAddMaxIGT.Text);
                seconds = maxigt.TotalSeconds;
            }
            catch
            {
                MessageBox.Show("Cannot add - Max IGT is not a valid time.", "RTA-TB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int points;
            bool convertedpoints = Int32.TryParse(lookupAddPoints.Text, out points);

            if (convertedpoints)
            {
                if (points >= 0)   // seconds *can* be negative, and it works, but if anybody finds a use for this, please let me know!
                {
                    int ms = (int)Math.Round(seconds * 1000);
                    double s = ms / 1000.0;   // force seconds to three decimal places

                    if (s_LookupTable.Items.ContainsKey(s.ToString()))
                    {
                        MessageBox.Show("Cannot add - a points value for this time already exists.", "RTA-TB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    var item = s_LookupTable.Items.Add(s.ToString(), s.ToString(), null);
                    item.SubItems.Add(points.ToString());
                    s_LookupTable.Sort();

                    UpdateLookup();

                    lookupAddMaxIGT.Text = "";
                    lookupAddPoints.Text = "";
                    lookupAddMaxIGT.Focus();
                }
                else
                {
                    MessageBox.Show("Cannot add - Points must be a positive number.", "RTA-TB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Cannot add - Points must be a whole number.", "RTA-TB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lookupAddMaxIGT_Enter(object sender, EventArgs e)
        {
            parentDefault = ParentForm.AcceptButton;
            ParentForm.AcceptButton = lookupAddButton;
        }

        private void lookupAddMaxIGT_Leave(object sender, EventArgs e)
        {
            ParentForm.AcceptButton = parentDefault;
        }

        private void lookupAddPoints_Enter(object sender, EventArgs e)
        {
            parentDefault = ParentForm.AcceptButton;
            ParentForm.AcceptButton = lookupAddButton;
        }

        private void lookupAddPoints_Leave(object sender, EventArgs e)
        {
            ParentForm.AcceptButton = parentDefault;
        }
    }

    class LookupTableSorter : IComparer
    {
        private int col;
        public LookupTableSorter()
        {
            col = 0;
        }
        public LookupTableSorter(int column)
        {
            col = column;
        }
        public int Compare(object x, object y)
        {
            return Double.Parse(((ListViewItem)x).SubItems[col].Text).CompareTo(Double.Parse(((ListViewItem)y).SubItems[col].Text));
        }
    }
}
