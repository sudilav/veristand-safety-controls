using NationalInstruments.Controls;
using System;
using System.Windows;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// This is the custom control we are styling.  We aren't changing any behaviors in this control, the main point of giving this a new type is so that we can control
    /// which styles are applied to it
    /// </summary>
    public class ResetAlarms : BooleanButton
    {
        /// <summary>
        /// Constructs a new instance of the EnableNumeric class
        /// </summary>
        public ResetAlarms()
        {
            // Set the default style key to the type.  The style key is used to apply themes to controls.  It lets us set an implicit style for this class in EnableNumericTemplate.xaml and have it be applied to this class.  If we didn't set this here the theme for the Knob would be used which we aren't currenly importing so we would end up
            // without a visual.
            DefaultStyleKey = typeof(ResetAlarms);
            this.IsMomentary = true;
            this.ValueChanged += TriggerAction;
        }


        static ResetAlarms()
        {
        }

        private void TriggerAction(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            // Trigger the event that the Model is listening to
            Pressed?.Invoke(this, e);
            this.Value = false;
            this.IsPressed = false;
        }

        // Event triggered when the ViewModel sends data to the Model
        public event EventHandler<EventArgs> Pressed;

        
    }
}
