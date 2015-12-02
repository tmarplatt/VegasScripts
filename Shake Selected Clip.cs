/**

	Simple camera shake effect for Sony Vegas.
	Generates horizontal and vertical sinusoid motion.
	Uses Pan+Crop keyframes to achieve the effect
	all along the duration of the clip.
	
	By Tommy Marplatt - December 2015

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Sony.Vegas;

public class EntryPoint
{
	//Rate of shake multiplier
	Double SkewIn = 12;
	//Synchronicity factor between horizontal and vertical movements
	Double SkewXY = 1.5;
	//Number of pixels to displace
	Single SkewOut = 4;
	//Affect displacement horizontally as a proportion of vertical displacement
	Single XToYRatio = 2.5f;
	//Reset Pan on first frame
	bool ShouldResetPan = false;
	//Start all over?
	bool ShouldClearFrames = true;
	
	void Shout(string shout)
	{
		MessageBox.Show(shout);
	}

	public void FromVegas(Vegas vegas)
	{
		if (DialogResult.OK != DoDialog(vegas))
		{
			return;
		}
		ShakeClip(vegas);
	}
	
	DialogResult DoDialog(Vegas vegas)
	{
		Form form = new Form();
		form.SuspendLayout();
		
		form.StartPosition = FormStartPosition.CenterParent;
		form.FormBorderStyle = FormBorderStyle.FixedDialog;
		form.MaximizeBox = false;
		form.MinimizeBox = false;
		form.HelpButton = false;
		form.ShowInTaskbar = false;
		form.AutoSize = true;
		form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		form.Text = "Camera Shake Settings";
		
		TableLayoutPanel l = new TableLayoutPanel();
		l.AutoSize = true;
		l.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		l.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
		l.ColumnCount = 2;
		l.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
		l.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
		form.Controls.Add(l);
		
		ToolTip tt = new ToolTip();
		
		Label label = new Label();
		label.Margin = new Padding(4, 4, 4, 4);
		label.Text = "Shake speed";
		l.Controls.Add(label);
		
		TextBox SkewInBox = new TextBox();
		SkewInBox.Text = String.Format("{0}", SkewIn);
		l.Controls.Add(SkewInBox);
		
		tt.SetToolTip(label, "Lower values for slower camera movement.");
		tt.SetToolTip(SkewInBox, "Lower values for slower camera movement.");
		
		label = new Label();
		label.Margin = new Padding(4, 4, 4, 4);
		label.Text = "H/V synchronicity";
		l.Controls.Add(label);
		
		TextBox SkewXYBox = new TextBox();
		SkewXYBox.Text = String.Format("{0}", SkewXY);
		l.Controls.Add(SkewXYBox);
		
		tt.SetToolTip(label, "Changes the relative vertical speed. Set to 1 to move in a circle.");
		tt.SetToolTip(SkewXYBox, "Changes the relative vertical speed. Set to 1 to move in a circle.");
		
		label = new Label();
		label.Margin = new Padding(4, 4, 4, 4);
		label.Text = "Pixels to displace";
		l.Controls.Add(label);
		
		TextBox SkewOutBox = new TextBox();
		SkewOutBox.Text = String.Format("{0}", SkewOut);
		l.Controls.Add(SkewOutBox);
		
		tt.SetToolTip(label, "Number of pixels the camera will shift away from the center. It is also the margin of zoom-in.");
		tt.SetToolTip(SkewOutBox, "Number of pixels the camera will shift away from the center. It is also the margin of zoom-in.");
		
		label = new Label();
		label.Margin = new Padding(4, 4, 4, 4);
		label.AutoSize = false;
		label.TextAlign = ContentAlignment.MiddleLeft;
		label.Text = "H/V ratio of displacement";
		label.Anchor = AnchorStyles.Left|AnchorStyles.Right;
		l.Controls.Add(label);
		
		TextBox XToYBox = new TextBox();
		XToYBox.Text = String.Format("{0}", XToYRatio);
		l.Controls.Add(XToYBox);
		
		tt.SetToolTip(label, "Multiply horizontal distance. Values above 1 will produce a greater zoom-in.");
		tt.SetToolTip(XToYBox, "Multiply horizontal distance. Values above 1 will produce a greater zoom-in.");
		
		CheckBox SRP = new CheckBox();
		SRP.Text = "Reset Pan/Crop on first frame";
		SRP.Checked = ShouldResetPan;
		SRP.AutoSize = false;
		SRP.Margin = new Padding(4,4,4,4);
		SRP.Anchor = AnchorStyles.Left|AnchorStyles.Right;
		l.Controls.Add(SRP);
		l.SetColumnSpan(SRP, 2);
		
		tt.SetToolTip(SRP, "Leave unchecked to shake within the current video zoom.");
		
		CheckBox SCF = new CheckBox();
		SCF.Text = "Reset all frames before shaking.";
		SCF.Checked = ShouldClearFrames;
		SCF.AutoSize = false;
		SCF.Margin = new Padding(4,4,4,4);
		SCF.Anchor = AnchorStyles.Left|AnchorStyles.Right;
		l.Controls.Add(SCF);
		l.SetColumnSpan(SCF, 2);
		
		tt.SetToolTip(SCF, "Leave unchecked to multiply the new shake effect with a previous shake effect.");
		
		FlowLayoutPanel panel = new FlowLayoutPanel();
		panel.FlowDirection = FlowDirection.RightToLeft;
		panel.Size = Size.Empty;
		panel.AutoSize = true;
		panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		panel.Anchor = AnchorStyles.Top|AnchorStyles.Right;
		l.Controls.Add(panel);
		l.SetColumnSpan(panel, 2);
		
		Button cancel = new Button();
		cancel.Text = "Cancel";
		cancel.DialogResult = DialogResult.Cancel;
		panel.Controls.Add(cancel);
		form.CancelButton = cancel;
		
		Button ok = new Button();
		ok.Text = "Ok";
		ok.DialogResult = DialogResult.OK;
		panel.Controls.Add(ok);
		form.AcceptButton = ok;
		
		form.ResumeLayout();
		
		DialogResult result = form.ShowDialog(vegas.MainWindow);
		if (DialogResult.OK == result)
		{
			try {
				SkewIn = Double.Parse(SkewInBox.Text);
				SkewXY = Double.Parse(SkewXYBox.Text);
				SkewOut = Single.Parse(SkewOutBox.Text);
				XToYRatio = Single.Parse(XToYBox.Text);
				ShouldResetPan = SRP.Checked;
				ShouldClearFrames = SCF.Checked;
			}
			catch (FormatException e)
			{
				Shout("Invalid parameter! " + e.Message);
				return DialogResult.Cancel;
			}
		}
		return result;
	}
	
	void ShakeClip(Vegas vegas)
	{
		VideoEvent ve = FindSelected(vegas.Project);
		if (! ve.IsValid())
		{
			Shout("No video event selected!");
			return;
		}
		if (ShouldClearFrames)
		{
			// Remove all pan/crop keyframes
			ve.VideoMotion.Keyframes.Clear();
			// Restore zoom to default
			if (ShouldResetPan)
			{
				ResetPan(ve);
			}
			// Populate with keyframes: one per frame
			Populate(ve);
		}
		//Shake that camera
		PanShake(ve);
	}
	
	void PanShake(VideoEvent ve)
	{
		int i = 0;
		
		foreach (VideoMotionKeyframe key in ve.VideoMotion.Keyframes)
		{
			SafelyScaleVideo(key);

			VideoMotionVertex movertex = MoveDelta(i);
			key.MoveBy(movertex);
			
			i++;
		}
	}
	
	void ResetPan(VideoEvent ve)
	{
		VideoStream v = ve.ActiveTake.Media.GetVideoStreamByIndex(0);
		ve.VideoMotion.Keyframes[0].Bounds = new VideoMotionBounds( new VideoMotionVertex(0,0), new VideoMotionVertex(v.Width, 0), new VideoMotionVertex(v.Width, v.Height), new VideoMotionVertex(0,v.Height));
	}
	
	void SafelyScaleVideo(VideoMotionKeyframe key)
	{
		VideoMotionVertex delta = new VideoMotionVertex(0,0);
		
		//Obtain the scaling ratio for each axis
		delta.X = ( - (2 * SkewOut * XToYRatio) + (key.TopRight.X - key.TopLeft.X) ) / (key.TopRight.X - key.TopLeft.X);
		delta.Y = ( - (2 * SkewOut) + (key.BottomLeft.Y - key.TopLeft.Y) ) / (key.BottomLeft.Y - key.TopLeft.Y);
		
		key.ScaleBy(delta);
	}
	
	VideoMotionVertex MoveDelta(int n)
	{
		VideoMotionVertex delta = new VideoMotionVertex(0,0);

		delta.X = (Single) Math.Sin(ToRads(n * SkewIn)) * SkewOut * XToYRatio;
		delta.Y = (Single) Math.Cos(ToRads(n * SkewIn * SkewXY)) * SkewOut;

		return delta;
	}
	
	Double ToRads(Double deg)
	{
		return deg * (Math.PI / 180);
	}
	
	void Populate(VideoEvent ve)
	{
		for (int i=0; i < ve.Length.FrameCount; i++)
		{
			VideoMotionKeyframe key = new VideoMotionKeyframe(Timecode.FromFrames(i));
			ve.VideoMotion.Keyframes.Add(key);
		}
	}

	VideoEvent FindSelected(Project project)
	{
		VideoEvent casket = new VideoEvent();
		foreach (Track track in project.Tracks) {
			if (track.IsVideo()) {
					foreach (VideoEvent videoEvent in track.Events)
					{
						if (videoEvent.Selected)
						{
							return videoEvent;
						}
					}
			  }
		}
		return casket;
	}
}
