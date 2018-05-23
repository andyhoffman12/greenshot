#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.DesktopWindowsManager;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;
using Greenshot.Components;
using Greenshot.Configuration;
using Greenshot.Destinations;
using Greenshot.Helpers;

#endregion

namespace Greenshot.Forms
{
	/// <summary>
	///     Description of SettingsForm.
	/// </summary>
	public partial class SettingsForm : BaseForm
	{
	    private readonly DestinationHolder _destinationHolder;
	    private static readonly LogSource Log = new LogSource();
		private readonly ToolTip _toolTip = new ToolTip();
		private int _daysbetweencheckPreviousValue;
		private bool _inHotkey;

        public SettingsForm(DestinationHolder destinationHolder)
        {
            _destinationHolder = destinationHolder;
            // Make sure the store isn't called to early, that's why we do it manually
			ManualStoreFields = true;
        }

	    public void Initialize()
	    {
	        InitializeComponent();
	    }

        protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Fix for Vista/XP differences
			trackBarJpegQuality.BackColor = WindowsVersion.IsWindowsVistaOrLater ? SystemColors.Window : SystemColors.Control;

			// This makes it possible to still capture the settings screen
			fullscreen_hotkeyControl.Enter += EnterHotkeyControl;
			fullscreen_hotkeyControl.Leave += LeaveHotkeyControl;
			window_hotkeyControl.Enter += EnterHotkeyControl;
			window_hotkeyControl.Leave += LeaveHotkeyControl;
			region_hotkeyControl.Enter += EnterHotkeyControl;
			region_hotkeyControl.Leave += LeaveHotkeyControl;
			ie_hotkeyControl.Enter += EnterHotkeyControl;
			ie_hotkeyControl.Leave += LeaveHotkeyControl;
			lastregion_hotkeyControl.Enter += EnterHotkeyControl;
			lastregion_hotkeyControl.Leave += LeaveHotkeyControl;
			// Changes for BUG-2077
			numericUpDown_daysbetweencheck.ValueChanged += NumericUpDownDaysbetweencheckOnValueChanged;

			_daysbetweencheckPreviousValue = (int) numericUpDown_daysbetweencheck.Value;
			UpdateUi();
			ExpertSettingsEnableState(false);
			DisplaySettings();
			CheckSettings();
		}

		/// <summary>
		///     This makes sure the check cannot be set to 1-6
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="eventArgs">EventArgs</param>
		private void NumericUpDownDaysbetweencheckOnValueChanged(object sender, EventArgs eventArgs)
		{
			var currentValue = (int) numericUpDown_daysbetweencheck.Value;

			// Check if we can into the forbidden range
			if (currentValue > 0 && currentValue < 7)
			{
				if (_daysbetweencheckPreviousValue <= currentValue)
				{
					numericUpDown_daysbetweencheck.Value = 7;
				}
				else
				{
					numericUpDown_daysbetweencheck.Value = 0;
				}
			}
			if ((int) numericUpDown_daysbetweencheck.Value < 0)
			{
				numericUpDown_daysbetweencheck.Value = 0;
			}
			if ((int) numericUpDown_daysbetweencheck.Value > 365)
			{
				numericUpDown_daysbetweencheck.Value = 365;
			}
			_daysbetweencheckPreviousValue = (int) numericUpDown_daysbetweencheck.Value;
		}

		private void EnterHotkeyControl(object sender, EventArgs e)
		{
			HotkeyControl.UnregisterHotkeys();
			_inHotkey = true;
		}

		private void LeaveHotkeyControl(object sender, EventArgs e)
		{
		    HotkeyHandler.RegisterHotkeys();
			_inHotkey = false;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Escape:
					if (!_inHotkey)
					{
						DialogResult = DialogResult.Cancel;
					}
					else
					{
						return base.ProcessCmdKey(ref msg, keyData);
					}
					break;
				default:
					return base.ProcessCmdKey(ref msg, keyData);
			}
			return true;
		}

		/// <summary>
		///     This is a method to populate the ComboBox
		///     with the items from the enumeration
		/// </summary>
		/// <param name="comboBox">ComboBox to populate</param>
		/// <param name="availableValues"></param>
		/// <param name="selectedValue"></param>
		private void PopulateComboBox<TEnum>(ComboBox comboBox, TEnum[] availableValues, TEnum selectedValue) where TEnum : struct
		{
			comboBox.Items.Clear();
			foreach (var enumValue in availableValues)
			{
				comboBox.Items.Add(Language.Translate(enumValue));
			}
			comboBox.SelectedItem = Language.Translate(selectedValue);
		}


		/// <summary>
		///     Get the selected enum value from the combobox, uses generics
		/// </summary>
		/// <param name="comboBox">Combobox to get the value from</param>
		/// <returns>The generics value of the combobox</returns>
		private TEnum GetSelected<TEnum>(ComboBox comboBox)
		{
			var enumTypeName = typeof(TEnum).Name;
			var selectedValue = comboBox.SelectedItem as string;
			var availableValues = (TEnum[]) Enum.GetValues(typeof(TEnum));
			var returnValue = availableValues[0];
			foreach (var enumValue in availableValues)
			{
				var translation = Language.GetString(enumTypeName + "." + enumValue);
				if (translation.Equals(selectedValue))
				{
					returnValue = enumValue;
					break;
				}
			}
			return returnValue;
		}

		private void SetWindowCaptureMode(WindowCaptureModes selectedWindowCaptureMode)
		{
			WindowCaptureModes[] availableModes;
			if (!Dwm.IsDwmEnabled)
			{
				// Remove DWM from configuration, as DWM is disabled!
				if (coreConfiguration.WindowCaptureMode == WindowCaptureModes.Aero || coreConfiguration.WindowCaptureMode == WindowCaptureModes.AeroTransparent)
				{
					coreConfiguration.WindowCaptureMode = WindowCaptureModes.GDI;
				}
				availableModes = new[] {WindowCaptureModes.Auto, WindowCaptureModes.Screen, WindowCaptureModes.GDI};
			}
			else
			{
				availableModes = new[] {WindowCaptureModes.Auto, WindowCaptureModes.Screen, WindowCaptureModes.GDI, WindowCaptureModes.Aero, WindowCaptureModes.AeroTransparent};
			}
			PopulateComboBox(combobox_window_capture_mode, availableModes, selectedWindowCaptureMode);
		}
        
	    /// <summary>
	    /// Add plugins to the Listview
	    /// </summary>
	    /// <param name="plugins"></param>
	    /// <param name="listview"></param>
	    private void FillListview(IEnumerable<Assembly> plugins, ListView listview)
	    {
	        foreach (var pluginAssembly in plugins)
	        {
	            var item = new ListViewItem(pluginAssembly.GetName().Name)
	            {
	                Tag = pluginAssembly
	            };
	            item.SubItems.Add(pluginAssembly.GetName().Version.ToString());
	            item.SubItems.Add(pluginAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false).Cast<AssemblyCompanyAttribute>().FirstOrDefault()?.Company);
	            item.SubItems.Add(pluginAssembly.Location);
	            listview.Items.Add(item);
	        }
	    }

        /// <summary>
        ///     Update the UI to reflect the language and other text settings
        /// </summary>
        private void UpdateUi()
		{
			if (coreConfiguration.HideExpertSettings)
			{
				tabcontrol.Controls.Remove(tab_expert);
			}
			_toolTip.SetToolTip(label_language, Language.GetString(LangKey.settings_tooltip_language));
			_toolTip.SetToolTip(label_storagelocation, Language.GetString(LangKey.settings_tooltip_storagelocation));
			_toolTip.SetToolTip(label_screenshotname, Language.GetString(LangKey.settings_tooltip_filenamepattern));
			_toolTip.SetToolTip(label_primaryimageformat, Language.GetString(LangKey.settings_tooltip_primaryimageformat));

			// Removing, otherwise we keep getting the event multiple times!
			combobox_language.SelectedIndexChanged -= Combobox_languageSelectedIndexChanged;

			// Initialize the Language ComboBox
			combobox_language.DisplayMember = "Description";
			combobox_language.ValueMember = "Ietf";
			// Set datasource last to prevent problems
			// See: http://www.codeproject.com/KB/database/scomlistcontrolbinding.aspx?fid=111644
			combobox_language.DataSource = Language.SupportedLanguages;
			if (Language.CurrentLanguage != null)
			{
				combobox_language.SelectedValue = Language.CurrentLanguage;
			}

			// Delaying the SelectedIndexChanged events untill all is initiated
			combobox_language.SelectedIndexChanged += Combobox_languageSelectedIndexChanged;
			UpdateDestinationDescriptions();
			UpdateClipboardFormatDescriptions();
		}

		// Check the settings and somehow visibly mark when something is incorrect
		private bool CheckSettings()
		{
			return CheckFilenamePattern() && CheckStorageLocationPath();
		}

		private bool CheckFilenamePattern()
		{
			var filename = FilenameHelper.GetFilenameFromPattern(textbox_screenshotname.Text, coreConfiguration.OutputFileFormat, null);
			// we allow dynamically created subfolders, need to check for them, too
			var pathParts = filename.Split(Path.DirectorySeparatorChar);

			var filenamePart = pathParts[pathParts.Length - 1];
			var settingsOk = FilenameHelper.IsFilenameValid(filenamePart);

			for (var i = 0; settingsOk && i < pathParts.Length - 1; i++)
			{
				settingsOk = FilenameHelper.IsDirectoryNameValid(pathParts[i]);
			}

			DisplayTextBoxValidity(textbox_screenshotname, settingsOk);

			return settingsOk;
		}

		private bool CheckStorageLocationPath()
		{
			var settingsOk = Directory.Exists(FilenameHelper.FillVariables(textbox_storagelocation.Text, false));
			DisplayTextBoxValidity(textbox_storagelocation, settingsOk);
			return settingsOk;
		}

		private void DisplayTextBoxValidity(GreenshotTextBox textbox, bool valid)
		{
			if (valid)
			{
				// "Added" feature #3547158
				textbox.BackColor = WindowsVersion.IsWindowsVistaOrLater ? SystemColors.Window : SystemColors.Control;
			}
			else
			{
				textbox.BackColor = Color.Red;
			}
		}

		private void FilenamePatternChanged(object sender, EventArgs e)
		{
			CheckFilenamePattern();
		}

		private void StorageLocationChanged(object sender, EventArgs e)
		{
			CheckStorageLocationPath();
		}

		/// <summary>
		///     Show all destination descriptions in the current language
		/// </summary>
		private void UpdateDestinationDescriptions()
		{
			foreach (ListViewItem item in listview_destinations.Items)
			{
			    if (item.Tag is IDestination destinationFromTag)
				{
					item.Text = destinationFromTag.Description;
				}
			}
		}

		/// <summary>
		///     Show all clipboard format descriptions in the current language
		/// </summary>
		private void UpdateClipboardFormatDescriptions()
		{
			foreach (ListViewItem item in listview_clipboardformats.Items)
			{
				var cf = (ClipboardFormats) item.Tag;
				item.Text = Language.Translate(cf);
			}
		}

		/// <summary>
		///     Build the view with all the destinations
		/// </summary>
		private void DisplayDestinations()
		{
		    var destinationsEnabled = !coreConfiguration.IsWriteProtected("Destinations");
            checkbox_picker.Checked = false;

			listview_destinations.Items.Clear();
			var imageList = new ImageList();
			listview_destinations.SmallImageList = imageList;
			var imageNr = -1;
			foreach (var currentDestination in _destinationHolder.SortedActiveDestinations)
			{
				var destinationImage = currentDestination.GetDisplayIcon(DpiHandler.Dpi);
				if (destinationImage != null)
				{
					imageList.Images.Add(destinationImage);
					imageNr++;
				}
				if (typeof(PickerDestination).GetDesignation().Equals(currentDestination.Designation))
				{
					checkbox_picker.Checked = coreConfiguration.OutputDestinations.Contains(currentDestination.Designation);
					checkbox_picker.Text = currentDestination.Description;
				}
				else
				{
					ListViewItem item;
					if (destinationImage != null)
					{
						item = listview_destinations.Items.Add(currentDestination.Description, imageNr);
					}
					else
					{
						item = listview_destinations.Items.Add(currentDestination.Description);
					}
					item.Tag = currentDestination;
					item.Checked = coreConfiguration.OutputDestinations.Contains(currentDestination.Designation);
				}
			}
			if (checkbox_picker.Checked)
			{
				listview_destinations.Enabled = false;
				foreach (int index in listview_destinations.CheckedIndices)
				{
					var item = listview_destinations.Items[index];
					item.Checked = false;
				}
			}
			checkbox_picker.Enabled = destinationsEnabled;
			listview_destinations.Enabled = destinationsEnabled;
		}

		private void DisplaySettings()
		{
			colorButton_window_background.SelectedColor = coreConfiguration.DWMBackgroundColor;

			// Expert mode, the clipboard formats
			foreach (ClipboardFormats clipboardFormat in Enum.GetValues(typeof(ClipboardFormats)))
			{
				var item = listview_clipboardformats.Items.Add(Language.Translate(clipboardFormat));
				item.Tag = clipboardFormat;
				item.Checked = coreConfiguration.ClipboardFormats.Contains(clipboardFormat);
			}

			if (Language.CurrentLanguage != null)
			{
				combobox_language.SelectedValue = Language.CurrentLanguage;
			}
            // Disable editing when the value is fixed
            combobox_language.Enabled = !coreConfiguration.IsWriteProtected("Language");

            textbox_storagelocation.Text = FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false);
			// Disable editing when the value is fixed
			textbox_storagelocation.Enabled = !coreConfiguration.IsWriteProtected("OutputFilePath");

			SetWindowCaptureMode(coreConfiguration.WindowCaptureMode);
			// Disable editing when the value is fixed
			combobox_window_capture_mode.Enabled = !coreConfiguration.CaptureWindowsInteractive && !coreConfiguration.IsWriteProtected("WindowCaptureMode");
			radiobuttonWindowCapture.Checked = !coreConfiguration.CaptureWindowsInteractive;

			trackBarJpegQuality.Value = coreConfiguration.OutputFileJpegQuality;
			trackBarJpegQuality.Enabled = !coreConfiguration.IsWriteProtected("OutputFileJpegQuality");
			textBoxJpegQuality.Text = $"{coreConfiguration.OutputFileJpegQuality}%";

			DisplayDestinations();

			numericUpDownWaitTime.Value = coreConfiguration.CaptureDelay >= 0 ? coreConfiguration.CaptureDelay : 0;
			numericUpDownWaitTime.Enabled = !coreConfiguration.IsWriteProtected("CaptureDelay");
			if (coreConfiguration.IsPortable)
			{
				checkbox_autostartshortcut.Visible = false;
				checkbox_autostartshortcut.Checked = false;
			}
			else
			{
				// Autostart checkbox logic.
				if (StartupHelper.HasRunAll())
				{
					// Remove runUser if we already have a run under all
					StartupHelper.DeleteRunUser();
					checkbox_autostartshortcut.Enabled = StartupHelper.CanWriteRunAll();
					checkbox_autostartshortcut.Checked = true; // We already checked this
				}
				else if (StartupHelper.IsInStartupFolder())
				{
					checkbox_autostartshortcut.Enabled = false;
					checkbox_autostartshortcut.Checked = true; // We already checked this
				}
				else
				{
					// No run for all, enable the checkbox and set it to true if the current user has a key
					checkbox_autostartshortcut.Enabled = StartupHelper.CanWriteRunUser();
					checkbox_autostartshortcut.Checked = StartupHelper.HasRunUser();
				}
			}

			numericUpDown_daysbetweencheck.Value = coreConfiguration.UpdateCheckInterval;
			numericUpDown_daysbetweencheck.Enabled = !coreConfiguration.IsWriteProtected("UpdateCheckInterval");
			numericUpdownIconSize.Value = coreConfiguration.IconSize.Width / 16 * 16;
			CheckDestinationSettings();
		}

		private void SaveSettings()
		{
			if (combobox_language.SelectedItem != null)
			{
				var newLang = combobox_language.SelectedValue.ToString();
				if (!string.IsNullOrEmpty(newLang))
				{
					coreConfiguration.Language = combobox_language.SelectedValue.ToString();
				}
			}

			// retrieve the set clipboard formats
			var clipboardFormats = new List<ClipboardFormats>();
			foreach (int index in listview_clipboardformats.CheckedIndices)
			{
				var item = listview_clipboardformats.Items[index];
				if (item.Checked)
				{
					clipboardFormats.Add((ClipboardFormats) item.Tag);
				}
			}
			coreConfiguration.ClipboardFormats = clipboardFormats;

			coreConfiguration.WindowCaptureMode = GetSelected<WindowCaptureModes>(combobox_window_capture_mode);
			if (!FilenameHelper.FillVariables(coreConfiguration.OutputFilePath, false).Equals(textbox_storagelocation.Text))
			{
				coreConfiguration.OutputFilePath = textbox_storagelocation.Text;
			}
			coreConfiguration.OutputFileJpegQuality = trackBarJpegQuality.Value;

			var destinations = new List<string>();
			if (checkbox_picker.Checked)
			{
				destinations.Add(typeof(PickerDestination).GetDesignation());
			}
			foreach (int index in listview_destinations.CheckedIndices)
			{
				var item = listview_destinations.Items[index];

				var destinationFromTag = item.Tag as IDestination;
				if (item.Checked && destinationFromTag != null)
				{
					destinations.Add(destinationFromTag.Designation);
				}
			}
			coreConfiguration.OutputDestinations = destinations;
			coreConfiguration.CaptureDelay = (int) numericUpDownWaitTime.Value;
			coreConfiguration.DWMBackgroundColor = colorButton_window_background.SelectedColor;
			coreConfiguration.UpdateCheckInterval = (int) numericUpDown_daysbetweencheck.Value;

			coreConfiguration.IconSize = new Size((int) numericUpdownIconSize.Value, (int) numericUpdownIconSize.Value);

			try
			{
				if (checkbox_autostartshortcut.Checked)
				{
					// It's checked, so we set the RunUser if the RunAll isn't set.
					// Do this every time, so the executable is correct.
					if (!StartupHelper.HasRunAll())
					{
						StartupHelper.SetRunUser();
					}
				}
				else
				{
					// Delete both settings if it's unchecked
					if (StartupHelper.HasRunAll())
					{
						StartupHelper.DeleteRunAll();
					}
					if (StartupHelper.HasRunUser())
					{
						StartupHelper.DeleteRunUser();
					}
				}
			}
			catch (Exception e)
			{
				Log.Warn().WriteLine(e, "Problem checking registry, ignoring for now: ");
			}
		}

		private void Settings_cancelClick(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void Settings_okayClick(object sender, EventArgs e)
		{
			if (CheckSettings())
			{
				HotkeyControl.UnregisterHotkeys();
				SaveSettings();
				StoreFields();
				HotkeyHandler.RegisterHotkeys();

				// Make sure the current language & settings are reflected in the Main-context menu
				MainForm.Instance.UpdateUi();
				DialogResult = DialogResult.OK;
			}
			else
			{
				tabcontrol.SelectTab(tab_output);
			}
		}

		private void BrowseClick(object sender, EventArgs e)
		{
			// Get the storage location and replace the environment variables
			folderBrowserDialog1.SelectedPath = FilenameHelper.FillVariables(textbox_storagelocation.Text, false);
			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
			{
				// Only change if there is a change, otherwise we might overwrite the environment variables
				if (folderBrowserDialog1.SelectedPath != null && !folderBrowserDialog1.SelectedPath.Equals(FilenameHelper.FillVariables(textbox_storagelocation.Text, false)))
				{
					textbox_storagelocation.Text = folderBrowserDialog1.SelectedPath;
				}
			}
		}

		private void TrackBarJpegQualityScroll(object sender, EventArgs e)
		{
			textBoxJpegQuality.Text = trackBarJpegQuality.Value.ToString(CultureInfo.InvariantCulture);
		}


		private void BtnPatternHelpClick(object sender, EventArgs e)
		{
			var filenamepatternText = Language.GetString(LangKey.settings_message_filenamepattern);
			// Convert %NUM% to ${NUM} for old language files!
			filenamepatternText = Regex.Replace(filenamepatternText, "%([a-zA-Z_0-9]+)%", @"${$1}");
			MessageBox.Show(filenamepatternText, Language.GetString(LangKey.settings_filenamepattern));
		}

		private void Listview_pluginsSelectedIndexChanged(object sender, EventArgs e)
		{
		    // TODO: Configure
		    //button_pluginconfigure.Enabled = PluginHelper.Instance.IsSelectedItemConfigurable(listview_plugins);
        }

        private void Button_pluginconfigureClick(object sender, EventArgs e)
		{
            // TODO: Configure
			//PluginHelper.Instance.ConfigureSelectedItem(listview_plugins);
		}

		private void Combobox_languageSelectedIndexChanged(object sender, EventArgs e)
		{
			// Get the combobox values BEFORE changing the language
			//EmailFormat selectedEmailFormat = GetSelected<EmailFormat>(combobox_emailformat);
			var selectedWindowCaptureMode = GetSelected<WindowCaptureModes>(combobox_window_capture_mode);
			if (combobox_language.SelectedItem != null)
			{
				Log.Debug().WriteLine("Setting language to: " + (string) combobox_language.SelectedValue);
				Language.CurrentLanguage = (string) combobox_language.SelectedValue;
			}
			// Reflect language changes to the settings form
			UpdateUi();

			// Reflect Language changes form
			ApplyLanguage();

			// Update the email & windows capture mode
			//SetEmailFormat(selectedEmailFormat);
			SetWindowCaptureMode(selectedWindowCaptureMode);
		}

		private void Combobox_window_capture_modeSelectedIndexChanged(object sender, EventArgs e)
		{
			var mode = GetSelected<WindowCaptureModes>(combobox_window_capture_mode);
			if (WindowsVersion.IsWindowsVistaOrLater)
			{
				switch (mode)
				{
					case WindowCaptureModes.Aero:
						colorButton_window_background.Visible = true;
						return;
				}
			}
			colorButton_window_background.Visible = false;
		}

		/// <summary>
		///     Check the destination settings
		/// </summary>
		private void CheckDestinationSettings()
		{
			var clipboardDestinationChecked = false;
			var pickerSelected = checkbox_picker.Checked;
			var destinationsEnabled = !coreConfiguration.IsWriteProtected("Destinations");
			listview_destinations.Enabled = destinationsEnabled;

			foreach (int index in listview_destinations.CheckedIndices)
			{
				var item = listview_destinations.Items[index];
			    if (!(item.Tag is IDestination destinationFromTag) ||
			        !destinationFromTag.Designation.Equals(typeof(ClipboardDestination).GetDesignation()))
			    {
			        continue;
			    }

			    clipboardDestinationChecked = true;
			    break;
			}

			if (pickerSelected)
			{
				listview_destinations.Enabled = false;
				foreach (int index in listview_destinations.CheckedIndices)
				{
					var item = listview_destinations.Items[index];
					item.Checked = false;
				}
			}
			else
			{
				// Prevent multiple clipboard settings at once, see bug #3435056
				if (clipboardDestinationChecked)
				{
					checkbox_copypathtoclipboard.Checked = false;
					checkbox_copypathtoclipboard.Enabled = false;
				}
				else
				{
					checkbox_copypathtoclipboard.Enabled = true;
				}
			}
		}

		private void DestinationsCheckStateChanged(object sender, EventArgs e)
		{
			CheckDestinationSettings();
		}

		protected override void OnFieldsFilled()
		{
			// the color radio button is not actually bound to a setting, but checked when monochrome/grayscale are not checked
			if (!radioBtnGrayScale.Checked && !radioBtnMonochrome.Checked)
			{
				radioBtnColorPrint.Checked = true;
			}
		}

		/// <summary>
		///     Set the enable state of the expert settings
		/// </summary>
		/// <param name="state"></param>
		private void ExpertSettingsEnableState(bool state)
		{
			listview_clipboardformats.Enabled = state;
			checkbox_autoreducecolors.Enabled = state;
			checkbox_optimizeforrdp.Enabled = state;
			checkbox_thumbnailpreview.Enabled = state;
			textbox_footerpattern.Enabled = state;
			textbox_counter.Enabled = state;
			checkbox_suppresssavedialogatclose.Enabled = state;
			checkbox_checkunstableupdates.Enabled = state;
			checkbox_minimizememoryfootprint.Enabled = state;
			checkbox_reuseeditor.Enabled = state;
		}

		/// <summary>
		///     Called if the "I know what I am doing" on the settings form is changed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CheckboxEnableExpertCheckedChanged(object sender, EventArgs e)
		{
		    if (sender is CheckBox checkBox)
			{
				ExpertSettingsEnableState(checkBox.Checked);
			}
		}

		private void Radiobutton_CheckedChanged(object sender, EventArgs e)
		{
			combobox_window_capture_mode.Enabled = radiobuttonWindowCapture.Checked;
		}
	}
}