﻿using LiveSplit.ManualGameTime.UI.Components;
using LiveSplit.Model;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class RTAmTBTimeComponent : LogicComponent
    {
        public RTAmTBTimeSettings Settings { get; set; }

        public GraphicsCache Cache { get; set; }
        protected LiveSplitState CurrentState { get; set; }
        public Form GameTimeForm { get; set; }
        protected Point PreviousLocation { get; set; } 

        public override string ComponentName => "RTA-TB Game Time";

        public RTAmTBTimeComponent(LiveSplitState state)
        {
            Settings = new RTAmTBTimeSettings();
            GameTimeForm = new ShitSplitter(state, Settings);
            state.OnStart += state_OnStart;
            state.OnReset += state_OnReset;
            state.OnUndoSplit += State_OnUndoSplit;
            CurrentState = state;
        }

        private void State_OnUndoSplit(object sender, EventArgs e)
        {
            var curIndex = CurrentState.CurrentSplitIndex;
            CurrentState.SetGameTime(curIndex > 0 ? CurrentState.Run[curIndex - 1].SplitTime.GameTime : TimeSpan.Zero);
        }

        void state_OnReset(object sender, TimerPhase e)
        {
            GameTimeForm.Close();
            PreviousLocation = GameTimeForm.Location;
        }

        void state_OnStart(object sender, EventArgs e)
        {
            GameTimeForm = new ShitSplitter(CurrentState, Settings);
            CurrentState.Form.Invoke(new Action(() => GameTimeForm.Show(CurrentState.Form)));
            if (!PreviousLocation.IsEmpty)
                GameTimeForm.Location = PreviousLocation;
            CurrentState.IsGameTimePaused = true;
            CurrentState.SetGameTime(TimeSpan.Zero);
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return Settings;
        }

        public override System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public override void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            var gt = (ShitSplitter)GameTimeForm;
            if (gt.PauseInProgress && (state.PauseTime >= gt.PauseDuration))
            {
                gt.PauseInProgress = false;
                gt.Model.Pause();
            }
        }

        public override void Dispose()
        {
            if (GameTimeForm != null && !GameTimeForm.IsDisposed)
                GameTimeForm.Close();
            CurrentState.OnStart -= state_OnStart;
            CurrentState.OnReset -= state_OnReset;
            CurrentState.OnUndoSplit -= State_OnUndoSplit;
        }

        public int GetSettingsHashCode()
        {
            return Settings.GetSettingsHashCode();
        }
    }
}
