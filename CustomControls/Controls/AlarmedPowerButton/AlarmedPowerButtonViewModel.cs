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
using System.Windows.Input;
using NationalInstruments.Design;
using NationalInstruments.Hmi.Core.Commands;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// The view model for the Enable Numeric control. The only thing this does different from the channelnumerictextboxviewmodel is that we return a custom view.
    /// Since the view we have inherits from knob it will be compatible with the existing models and view models.
    /// </summary>
    public class AlarmedPowerButtonViewModel : ChannelBooleanSwitchViewModel
    {
        public interface IMessageService
        {
            void ShowMessage(string message, string caption);
        }

        public class MessageService : IMessageService
        {
            public void ShowMessage(string message, string caption)
            {
                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private AlarmedPowerButtonModel _model;
        public IMessageService messageService;
        private readonly BooleanChannelControlViewModelImplementation<ChannelBooleanSwitchViewModel> _channelControlViewModelImplementation;
        /// <summary>
        /// Constructs a new instance of the AlarmedPowerButtonViewModel class
        /// </summary>
        /// <param name="model">The AlarmedPowerButtonModel assocaited with this view model.</param>
        public AlarmedPowerButtonViewModel(AlarmedPowerButtonModel model) : base(model)
        {
            _channelControlViewModelImplementation = new BooleanChannelControlViewModelImplementation<ChannelBooleanSwitchViewModel>(this);
            _model = model;
            var activeRunTimeServiceProvider = Host.ActiveRunTimeServiceProvider();
            activeRunTimeServiceProvider.RegisterStatusListener(_model);
            _model.ActionFailed += OnActionFailed;
            messageService = new MessageService();
        }

        /// <summary>
        /// Returns a custom view for associated with this view model
        /// </summary>
        /// <returns>Enable Numeric view</returns>
        public override object CreateView()
        {
            var view = new AlarmedPowerButton();
            // numeric controls have a helper which needs to get attached to the view that helps setting values of different types to the control. Since we only have I32
            // for our control type we just hard code that when creating the helper
            //Helper = CreateHelper(typeof(double), this, view);
            view.ValueChanged += valuechanged;
            return view;
        }

        private void OnActionFailed(string message)
        {
            // Show the message to the user using the service
            messageService.ShowMessage(message, "Action Denied");
        }

        public override void CreateCommandContent(ICommandPresentationContext context)
        {

            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                context.Remove(ConfigurationBasedSoftwareControlIndicatorCommands.ControlIndicatorCommand);
                using (context.AddGroup(GatewayAlarmCommands.GatewayConfigurationGroupCommand))
                {
                    context.Add(GatewayAlarmCommands.GatewayCommand, new AddrTextBoxFactory(_model));
                }
            }
        }

        public void valuechanged(object sender, RoutedPropertyChangedEventArgs<bool> args)
        {

            if ((bool)_channelControlViewModelImplementation.ChannelControlModel.ChannelValue != (bool)args.NewValue)
            {
                _model.ChangeValue(args.NewValue);
            }
        }
    }

}
