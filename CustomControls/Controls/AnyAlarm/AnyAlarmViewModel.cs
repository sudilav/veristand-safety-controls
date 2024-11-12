using NationalInstruments.Hmi.Core.Controls.ViewModels;
using NationalInstruments.Design;
using NationalInstruments.Controls.Design;
using NationalInstruments.Hmi.Core.Screen;
using System.Collections.Generic;
using System;
using NationalInstruments.Composition;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.Hmi.Core.Controls.Models;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using NationalInstruments.VeriStand.ServiceModel;
using NationalInstruments.VeriStand.Shell;
using NationalInstruments.VeriStand.Tools;
using System.Windows;
using System.Windows.Media;
using NationalInstruments.PanelCommon.Design;
using System.ComponentModel;
using NationalInstruments.Controls;
using NationalInstruments.DataTypes;
using NationalInstruments.Hmi.Core.Controls.Mapping;
using System.Windows.Controls;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using NationalInstruments.VeriStand.SystemDefinitionAPI;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// The view model for the keyswitch control. The only thing this does different from the channelknobviewmodel is that we return a custom view.
    /// Since the view we have inherits from knob it will be compatible with the existing models and view models.
    /// </summary>
    public class AnyAlarmViewModel : VisualViewModel, INotifyPropertyChanged
    {
        private AnyAlarmModel _model;
        /// <summary>
        /// Constructs a new instance of the KeySwitchControlViewModel class
        /// </summary>
        /// <param name="model">The KeySwitchControlModel assocaited with this view model.</param>
        public AnyAlarmViewModel(AnyAlarmModel model) : base(model)
        {
            // Register for background colour changes
            _model = model;
            _model.PropertyChanged += Model_PropertyChanged;
        }

        // This is the property the UI will bind to
        public Brush EllipseFill => _model.IsRed ? Brushes.Red : Brushes.White;

        // Handle model changes and notify the UI
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_model.IsRed))
            {
                OnPropertyChanged(nameof(EllipseFill)); // Notify that EllipseFill has changed
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnCreateContextMenu(CreateContextMenuRoutedEventArgs args)
        {
        }

        public override void CreateCommandContent(ICommandPresentationContext context)
        {

            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(GatewayAlarmCommands.GatewayConfigurationGroupCommand))
                {
                    context.Add(GatewayAlarmCommands.GatewayCommand, new AddrTextBoxFactory(_model));
                }
            }
        }

        /// <summary>
        /// Creates the view associated with this view model by initializing a new instance of our custom control class PWMControl
        /// This is an opportunity to provide callbacks to the view and to hook up event handlers.  In this case we add a value changed event handler so we can
        /// react when the view changes value.
        /// </summary>
        /// <returns>pwmcontrol view</returns>
        public override object CreateView()
        {
            var view = new AnyAlarm(this);
            return view;
        }

        public IEnumerable<string> FilterContextMenuCommands()
        {
            throw new System.NotImplementedException();
        }
    }
}
