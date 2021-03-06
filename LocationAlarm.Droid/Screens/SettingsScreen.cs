﻿using Android.App;
using Android.Media;
using Android.Preferences;
using SimpleLocationAlarm.Droid.Services;
using System;

namespace SimpleLocationAlarm.Droid.Screens
{
	[Activity (
		Label = "@string/action_settings",
		Icon = "@drawable/map_white",
		Theme = "@style/SettingsTheme",
		ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class SettingsScreen : PreferenceActivity
	{
		public const string VibrateSettingKey = "pref_vibrate";
		public const bool VibrateSettingDefaultValue = true;
		public const string PlaySoundSettingKey = "pref_play_sound";
		public const bool PlaySoundSettingDefaultValue = true;
		public const string AddMarkerOnLongTap = "pref_long_tap";
		public const bool AddMarkerOnLongTapDefaultValue = false;
		public const string SoundSettingKey = "pref_sound";
		public const string DefaultRadiusKey = "pref_radius";
		public const int DefaultRadiusValue = 200;
		public const string StartsCount = "StartsCount";
		public const string ShouldAskForRating = "ShouldAskForRating";
		public const string AdsSettingKey = "pref_ads";

		Preference _soundPref, _adsPref;

		InAppBillingManager _billingManager;

		protected GoogleAnalyticsManager GoogleAnalyticsManager = new GoogleAnalyticsManager ();

		protected override void OnCreate (Android.OS.Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			GoogleAnalyticsManager.ReportScreenEnter (GetType ().FullName);

			AddPreferencesFromResource (Resource.Xml.preferences);

			_soundPref = FindPreference (SoundSettingKey);
			var soundPrefValue = PreferenceManager.GetDefaultSharedPreferences (this).GetString (SoundSettingKey, "");
			_soundPref.Summary = string.IsNullOrEmpty (soundPrefValue) ? GetString (Resource.String.settings_sound_sum) :
                RingtoneManager.GetRingtone (this, Android.Net.Uri.Parse (soundPrefValue)).GetTitle (this);
			_soundPref.PreferenceClick += SoundPreferenceClick;     

			_adsPref = FindPreference (AdsSettingKey);
			_adsPref.PreferenceClick += AdsPreferenceClick;
		}

		const int _pickSoundRequestId = 45;

		void SoundPreferenceClick (object sender, Preference.PreferenceClickEventArgs e)
		{
			var intent = _soundPref.Intent;
			intent.SetAction (RingtoneManager.ActionRingtonePicker);
			StartActivityForResult (intent, _pickSoundRequestId);
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Android.Content.Intent data)
		{
			switch (requestCode) {
			case _pickSoundRequestId:
				if (resultCode == Result.Ok) {
					var uri = data.GetParcelableExtra (RingtoneManager.ExtraRingtonePickedUri);
					var ringtone = RingtoneManager.GetRingtone (this, Android.Net.Uri.Parse (uri.ToString ()));

					_soundPref.Summary = ringtone.GetTitle (this);
					PreferenceManager.GetDefaultSharedPreferences (this).Edit ().PutString (SoundSettingKey, uri.ToString ()).Commit ();

					GoogleAnalyticsManager.ReportEvent (GACategory.SettingsScreen, GAAction.Click, "new ringtone is set");
				}
				break;
			default:
				if (_billingManager != null)
					_billingManager.OnActivityResult (requestCode, resultCode, data);

				base.OnActivityResult (requestCode, resultCode, data);
				break;
			}
		}

		public override bool OnOptionsItemSelected (Android.Views.IMenuItem item)
		{
			switch (item.ItemId) {
			case Android.Resource.Id.Home:
				OnBackPressed ();
				return true;
			default:
				return base.OnOptionsItemSelected (item);
			}
		}

		void AdsPreferenceClick (object sender, Preference.PreferenceClickEventArgs e)
		{
			if (_billingManager == null) {
				_billingManager = new InAppBillingManager (this);
				_billingManager.Connected += HandleConnected;
			} else
				HandleConnected (null, EventArgs.Empty);
		}

		void HandleConnected (object sender, EventArgs e)
		{
			_billingManager.Connected -= HandleConnected;

			_billingManager.Pay ();
		}
	}
}