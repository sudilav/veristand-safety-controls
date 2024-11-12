﻿using System.Windows;
using System.Windows.Controls;
using NationalInstruments.Core;
using NationalInstruments.Shell;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// The control for a preference page
    /// </summary>
    public partial class UKAEAPreferencePage : UserControl
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public UKAEAPreferencePage()
        {
            InitializeComponent();
            HideEditor = UKAEAPreferencePageController.HideEditor;
        }

        /// <summary>
        /// DependencyProperty for HideEditor
        /// </summary>
        public static readonly DependencyProperty HideEditorProperty = DependencyProperty.Register(
            nameof(HideEditor),
            typeof(bool),
            typeof(UKAEAPreferencePage),
            new PropertyMetadata(false));

        /// <summary>
        /// Option to hide menus and tool windows
        /// </summary>
        public bool HideEditor
        {
            get => (bool)GetValue(HideEditorProperty);
            set => SetValue(HideEditorProperty, value);
        }
    }

    /// <summary>
    /// Plugin export defining our user preference page
    /// </summary>
    [ExportUserPreferences(Weight = -0.1)]
    public class VeriStandGeneralPreferencePageProvider : UserPreferencesProvider
    {
        /// <inheritdoc/>
        public override string PreferencesPageName => "UKAEA ELSA";

        /// <inheritdoc/>
        public override UserPreferencesPage CreatePage() => new UKAEAPreferencePageController();
    }

    /// <summary>
    /// Controller class for preference page
    /// </summary>
    public class UKAEAPreferencePageController : UserPreferencesPage
    {
        private readonly UKAEAPreferencePage _visual = new UKAEAPreferencePage();
        private const string HideEditorPreferenceName = "NationalInstruments.VeriStand.UKAEA_CustomControls.HideEditor";

        /// <summary>
        /// Underlying UserPreference for whether we should hide menus and tool windows. This is saved in the preferences file in %temp%\..\National Instruments\VeriStand {year}
        /// </summary>
        public static bool HideEditor
        {
            get { return PreferencesHelper.GetPreference(null, HideEditorPreferenceName, false); }
            set { PreferencesHelper.SetPreference(null, HideEditorPreferenceName, value); }
        }

        /// <summary>
        /// The actual visual
        /// </summary>
        public override PlatformVisual Visual => _visual;

        /// <summary>
        /// Apply user changes to visual to preference
        /// </summary>
        /// <returns>Whether we need to restart</returns>
        public override UserPreferencesChangeResults CommitChanges()
        {
            if (HideEditor != _visual.HideEditor)
            {
                HideEditor = _visual.HideEditor;
                return UserPreferencesChangeResults.ChangesRequireRestart;
            }
            return base.CommitChanges();
        }
    }
}
