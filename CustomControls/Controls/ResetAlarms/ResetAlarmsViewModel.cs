using NationalInstruments.Controls.Design;
using NationalInstruments.Core;
using NationalInstruments.Hmi.Core.Controls.ViewModels;
using System.Windows;
using System;
using NationalInstruments.Controls;
using NationalInstruments.Hmi.Core.Controls.Models;
using System.Threading.Tasks;
using NationalInstruments.VeriStand.SystemDefinitionAPI;
using NationalInstruments.Controls.SourceModel;
using NationalInstruments.Hmi.Core.Services;
using NationalInstruments.Design;
using System.Windows.Controls;
using NationalInstruments.Hmi.Core.Commands;
using NationalInstruments.Shell;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Composition;
using NationalInstruments.SourceModel;
using NationalInstruments.Hmi.Core.Resources;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Hmi.Core.Controls.Mapping;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// The view model for the Enable Numeric control. The only thing this does different from the channelnumerictextboxviewmodel is that we return a custom view.
    /// Since the view we have inherits from knob it will be compatible with the existing models and view models.
    /// </summary>
    /// 
    public class ResetAlarmsViewModel : BooleanButtonViewModel
    {
        /// <summary>
        /// Constructs a new instance of the ResetAlarmsViewModel class
        /// </summary>
        /// <param name="model">The ResetAlarmsModel assocaited with this view model.</param>
        public ResetAlarmsViewModel(ResetAlarmsModel model) : base(model)
        {
            OnPressed += model.HandleAlarm;
            _model = model;
        }
        public virtual bool CanShowLabel => true;
        private ResetAlarmsModel _model;
        /// <summary>
        /// Returns a custom view for associated with this view model
        /// </summary>
        /// <returns>Enable Numeric view</returns>
        public override object CreateView()
        {
            var view = new ResetAlarms();
            view.Pressed += onPressed;
            // numeric controls have a helper which needs to get attached to the view that helps setting values of different types to the control. Since we only have I32
            // for our control type we just hard code that when creating the helper
            //Helper = CreateHelper(typeof(double), this, view);
            return view;
        }

        public event Func<Task> OnPressed;

        public async void onPressed(object sender, EventArgs e)
        {
            foreach (Func<Task> handler in OnPressed.GetInvocationList())
            {
                await handler();
            }
        }

        public void OnSelectedResetNodeChanged(object sender, SelectedNodesEventArgs e)
        {
            IActiveTransaction activeTransaction = this.TransactionManager.BeginTransaction("Set reset channel", TransactionPurpose.User);
            _model.ResetChannel = e.SelectedNodeNames[0];
            activeTransaction.Commit();            
        }

        public override void CreateCommandContent(ICommandPresentationContext context)
        {

            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                context.Remove(ConfigurationBasedSoftwareControlIndicatorCommands.ControlIndicatorCommand);
                context.Remove(BooleanControlCommands.DefaultValueCommandInstance);
                context.Remove(BooleanControlCommands.MechanicalActionCommand);
                // First add the group command which lets us know what top level configuration pane group to put the child commands in
                using (context.AddGroup(ResetAlarmCommands.ResetConfigurationGroupCommand))
                {
                    // add child commands whose visuals will show up in the specified parent group.
                    context.Add(ResetAlarmCommands.TimeoutCommand, new NumericTextBoxFactory(NITypes.Int32, new Range<int>(0, int.MaxValue)));
                    context.Add(ResetAlarmCommands.MinimumCycleTimeCommand, new NumericTextBoxFactory(NITypes.Int32, new Range<int>(0, int.MaxValue)));

                    MappingControlOptions options = new MappingControlOptions();
                    options.AllowMultipleSelections = false;
                    IMappingControl mappingControl = Host.GetSharedExportedValue<IMappingControlProvider>().CreateMappingControl(() => new List<string> { _model.ResetChannel }, options);
                    WeakEventManager<IMappingControl, SelectedNodesEventArgs>.AddHandler(mappingControl, "SelectedNodesChanged", OnSelectedResetNodeChanged);
                    context.Add(ResetAlarmCommands.SetResetChannelCommand, new SingleMappingVisualFactory(mappingControl));
                    context.Add(ResetAlarmCommands.GatewayAddressCommand, new TextBoxFactory());
                }
            }
        }

    }
    public static class ResetAlarmCommands
    {
        public static readonly ICommandEx ResetConfigurationGroupCommand = new ShellRelayCommand
        {
            UniqueId = "NI.ResetAlarmCommands:ResetAlarmConfigGroupCommand",
            LabelTitle = "Reset Alarm Configuration",
            Weight = 0.1 
        }; 
        public static readonly ICommandEx TimeoutCommand = new PropertySymbolCommand(ResetAlarmsModel.AlarmServiceTimeoutSymbol, UIModelPropertySymbolDataModel.GetCreator())
        {
            LabelTitle = "Alarm Service Timeout (ms)",
            UniqueId = "NI.ResetAlarmCommands:TimeoutCommand"
        };
        public static readonly ICommandEx SetResetChannelCommand = new PropertySymbolCommand(ResetAlarmsModel.ResetChannelSymbol, UIModelPropertySymbolDataModel.GetCreator())
        {
            LabelTitle = "Reset Channel",
            UniqueId = "NI.ResetAlarmCommands:ResetChannelCommand"
        };
        public static readonly ICommandEx MinimumCycleTimeCommand = new PropertySymbolCommand(ResetAlarmsModel.MinimumCycleTimeSymbol, UIModelPropertySymbolDataModel.GetCreator())
        {
            LabelTitle = "Reset Channel Duty Cycle (ms)",
            UniqueId = "NI.ResetAlarmCommands:MinimumCycleTimeCommand"
        };
        public static readonly ICommandEx GatewayAddressCommand = new PropertySymbolCommand(ResetAlarmsModel.GatewaySymbol, UIModelPropertySymbolDataModel.GetCreator())
        {
            LabelTitle = "Gateway Address",
            UniqueId = "NI.ResetAlarmCommands:GatewayAddressCommand"
        };

    }

}
