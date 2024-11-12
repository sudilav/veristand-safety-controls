using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NationalInstruments.Controls;
using NationalInstruments.VeriStand.CustomControls;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// Interaction logic for WatchdogControl.xaml
    /// </summary>
    public partial class AnyAlarm
    {
        private readonly AnyAlarmViewModel _viewModel;

        /// <summary>
        /// Constructor for WatchdogControl
        /// </summary>
        /// <param name="viewModel">view model associated with this control</param>
        public AnyAlarm(AnyAlarmViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel; // Set the DataContext to the ViewModel
        }
        /// <summary>
        /// Event that is fired when the value on the control changes
        /// </summary>
        //public event EventHandler<CustomChannelValueChangedEventArgs> ValueChanged;

        ///// <summary>
        ///// Raises the ChannelValueChanged event. Invoked when the channel value changes.
        ///// </summary>
        ///// <param name="channelValue">New channel value</param>
        ///// <param name="channelName">Name of the channel that changed</param>
        //protected virtual void OnValueChanged(double channelValue, string channelName)
        //{
        //    var channelValueChangedSubscribers = ValueChanged;
        //    if (channelValueChangedSubscribers != null)
        //    {
        //        channelValueChangedSubscribers(this, new CustomChannelValueChangedEventArgs(channelValue, channelName));
        //    }
        //}

    }
}