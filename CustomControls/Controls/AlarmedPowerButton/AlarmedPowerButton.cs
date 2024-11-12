using NationalInstruments.Controls;
using System;
using System.Windows;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// This is the custom control for an Alarmed button which only activates if no alarms are active.
    /// </summary>
    public class AlarmedPowerButton : BooleanContentButton
    {
        static AlarmedPowerButton()
        {
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(AlarmedPowerButton), new FrameworkPropertyMetadata(typeof(AlarmedPowerButton)));
            BooleanButton.IsMomentaryProperty.OverrideMetadata(typeof(AlarmedPowerButton), new FrameworkPropertyMetadata(false));
        }
        /// <summary>
        /// Constructs a new instance of the AlarmedPowerButton class
        /// </summary>
        public AlarmedPowerButton()
        {
            // Set the default style key to the type.  The style key is used to apply themes to controls.
            // It lets us set an implicit style for this class in AlarmedPowerButtonTemplate.xaml and have it be applied to this class.
            // If we didn't set this here the theme for the BooleanButton would be used which we aren't currenly importing so we would end up
            // without a visual.
            DefaultStyleKey = typeof(AlarmedPowerButton);
        }
    }
}
