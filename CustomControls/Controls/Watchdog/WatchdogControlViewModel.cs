using NationalInstruments.Hmi.Core.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Composition;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.Hmi.Core.Controls.Models;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using NationalInstruments.VeriStand.ServiceModel;
using NationalInstruments.VeriStand.Shell;
using NationalInstruments.VeriStand.Tools;
using System.Windows;
using System.Windows.Media;
using NationalInstruments.PanelCommon.Design;

namespace NationalInstruments.VeriStand.CustomControls
{
    public class WatchdogControlViewModel : VisualViewModel, IControlContextMenuHelper
    {
        /// <summary>
        /// Constructs a new instance of the WatchdogControlViewModel class
        /// </summary>
        /// <param name="model">The WatchdogControlModel associated with this view model.</param>
        public WatchdogControlViewModel(WatchdogControlModel model)
            : base(model)
        {
            // Register for background colour changes
            model.BackgroundColorChanged += HandleBackgroundColorChanged;
            // Register for channel value change events on the model.  The weak event manager is used here since it helps prevent memory leaks associated
            // with registering for events and lets us be less careful about unregistering for these events at a later time.
            //WeakEventManager<WatchdogControlModel, ChannelValueChangedEventArgs>.AddHandler(model, "DutyCycleChannelValueChangedEvent", DutyCycleValueChangedEventHandler);
            //WeakEventManager<WatchdogControlModel, ChannelValueChangedEventArgs>.AddHandler(model, "FrequencyChannelValueChangedEvent", FrequencyValueChangedEventHandler);
        }

        private SolidColorBrush _backgroundColor;
        public SolidColorBrush BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                _backgroundColor = value;
            }
        }

        private void HandleBackgroundColorChanged(object sender, string e)
        {
            BackgroundColor = BackgroundColor == Brushes.Red ? Brushes.White : Brushes.Red;
            
        }
        public IEnumerable<ShellCommandInstance> CreateContextMenuCommands()
        {
            throw new NotImplementedException();
        }

        public override object CreateView()
        {
            var view = new WatchdogControl(this);
            return view;
        }

        public override void CreateCommandContent(ICommandPresentationContext context)
        {

            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(WatchdogCommands.TargetConfigurationGroupCommand))
                {
                    context.Add(WatchdogCommands.TargetCommand, new TextBoxFactory());
                }
            }
        }

        public static class WatchdogCommands
        {
            public static readonly ICommandEx TargetConfigurationGroupCommand = new ShellRelayCommand
            {
                UniqueId = "NI.WatchdogCommands:GatewayAlarmConfigGroupCommand",
                LabelTitle = "Target Configuration",
                Weight = 0.1
            };
            public static readonly ICommandEx TargetCommand = new PropertySymbolCommand(WatchdogControlModel.TargetSymbol, UIModelPropertySymbolDataModel.GetCreator())
            {
                LabelTitle = "Target Address",
                UniqueId = "NI.WatchdogCommands:TargetCommand"
            };
        }

        public IEnumerable<string> FilterContextMenuCommands()
        {
            throw new NotImplementedException();
        }
    }
}
