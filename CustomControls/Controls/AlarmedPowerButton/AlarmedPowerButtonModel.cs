using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.Hmi.Core.Controls.Models;
using NationalInstruments.Hmi.Core.Services;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VeriStand.ClientAPI;
using NationalInstruments.VeriStand.SourceModel.Screen;
using NationalInstruments.CommonModel;
using NationalInstruments.VeriStand.SourceModel;
using static NationalInstruments.Core.ExceptionHelper;
using NationalInstruments.Controls.SourceModel;
using NationalInstruments.DynamicProperties;
using NationalInstruments.VeriStand.ServiceModel;
using NationalInstruments.VeriStand.SystemDefinitionAPI;
using NationalInstruments.PanelCommon.SourceModel;
using NationalInstruments.Controls;
using NationalInstruments.Hmi.Core.Controls.ViewModels;
using AlarmState = NationalInstruments.VeriStand.ClientAPI.AlarmState;
using static NationalInstruments.VeriStand.CustomControls.AnyAlarmModel;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// This class is an implementation of ICustomVeriStandControl. By specifying it as an Export, we are identifying
    /// it as a plugin to VeriStand.
    /// The interface implementation defines how the control should appear in the palette.
    /// </summary>
    [Export(typeof(ICustomVeriStandControl))]
    public class AlarmedPowerButtonModelExporter : ICustomVeriStandControl
    {
        /// <summary>
        /// Mergescript which defines what to drop on the screen from the palette.  Can be used to set default values on the control
        /// </summary>
        public string Target =>
            "<pf:MergeScript xmlns:pf=\"http://www.ni.com/PlatformFramework\">" +
                "<pf:MergeItem>" +
                    "<AlarmedPowerButton xmlns=\"http://www.your-company.com/VeriStandExample\" Width=\"[float]57\" Height=\"[float]24\"/>" +
                "</pf:MergeItem>" +
            "</pf:MergeScript>";

        /// <summary>
        /// Name of the control as it will appear in the palette
        /// </summary>
        public string Name => "Alarmed Power Button";

        /// <summary>
        /// Path to the image to use in the palette
        /// </summary>
        public string ImagePath => "/NationalInstruments.VeriStand.CustomControls;component/Resources/alarmedpower_32x32.png";

        /// <summary>
        /// Tool tip to display in the palette
        /// </summary>
        public string ToolTip => "Alarmed Power Button Tool Tip";

        /// <summary>
        /// Unique id for the control. The only requirement is that this doesn't overlap with existing controls or other custom controls.
        /// This is used for serialization and the context help system.
        /// </summary>
        public string UniqueId => "AlarmedPowerButton";

        /// <summary>
        /// If a folder hierarchy is desired it can be returned here.  If multiple controls should show up in the same subfolder either use the same PaletteElementCategory object in their
        /// hierarchy list or use a category with the same unique id.  Unique IDs cannot be duplicated at different hierarchy levels.
        /// </summary>
        public IList<PaletteElementCategory> PaletteHierarchy =>
            new List<PaletteElementCategory>() { new PaletteElementCategory("Safety", ImagePath, "Safety", .1) };
    }

    /// <summary>
    /// This model has the business logic for the keyswitch control.  Mostly it handles all of that in the base class ChannelKnobModel.
    /// </summary>
    public class AlarmedPowerButtonModel : ChannelBooleanSwitchModel, IGatewayModel,
#if MUTATE2020R4
        IDataEngineStateChangeObserver
#else
        ISubscribeProviderStatusUpdates
#endif
    {
        private readonly BooleanChannelControlModelImplementation<ChannelBooleanSwitchModel> _channelControlModelImplementation;

        protected AlarmedPowerButtonModel()
        {
            _channelControlModelImplementation = new BooleanChannelControlModelImplementation<ChannelBooleanSwitchModel>(this);
        }
        /// <summary>
        /// The name of the control which will be used in XML.  This name in the mergescript in the Target property of the ICustomVeriStandControl interface must match this
        /// </summary>
        private const string AlarmedPowerButtonName = "AlarmedPowerButton";
        /// <summary>
        /// How this model will be represented in XML.
        /// </summary>
        public override XName XmlElementName => XName.Get(AlarmedPowerButtonName, PluginNamespaceSchema.ParsableNamespaceName);

        /// <summary>Creates a <see cref="AlarmedPowerButtonModel"/>.</summary>
        /// <param name="info">An <see cref="IElementCreateInfo"/> instance.</param>
        /// <returns>A <see cref="AlarmedPowerButtonModel"/>.</returns>
        [XmlParserFactoryMethod(AlarmedPowerButtonName, PluginNamespaceSchema.ParsableNamespaceName)]
        public static AlarmedPowerButtonModel CreateAlarmedPowerButtonModel(IElementCreateInfo info)
        {
            var model = new AlarmedPowerButtonModel();
#if MUTATE2020
            model.Initialize(info);
#else
            model.Init(info);
#endif
            model.factory = new Factory();
            return model;
        }

        public class Doc : IDocumentation
        {
            string IDocumentation.Description => "This Implementation of the power button will not transition to enabled if any alarm is tripped and will block any transitions to non-zero from elsewhere while an alarm is tripped. Note: It will not move to zero if an alarm trips, it is expected that you do this via the alarm's procedure.";

            public string InstanceName { get; set; }

            string IDocumentation.Name => "Alarmed Power Button";
        }

        protected override IDocumentation CreateDocumentation()
        {
            Doc doc_object = new Doc();
            doc_object.InstanceName = this.EventSourceName;
            return doc_object;
        }

        public event Action<string> ActionFailed;
        private bool IsConnected = false;
        public void ChangeValue(bool NewValue)
        {
            bool anytripped = false;
            if (IsConnected)
            {
                AlarmInfo[] alarmInfo; // Declaration without initialization
                
                // Query Alarm
                Error result = alarm_manager.GetMultipleAlarmsData(alarmNames, timeout, out alarmInfo);
                if (result.Code == 0) // Assuming Error.None indicates success
                {
                    // Now alarmInfo contains the result
                    foreach (var alarm in alarmInfo)
                    {
                        if (alarm.State == AlarmState.Tripped)
                        {
                            // Alarm is tripped
                            anytripped = true;
                        }
                    }

                }
            }
            if (anytripped && NewValue)
            {
                ActionFailed?.Invoke("You can't perform this action while an alarm is active.");
                _channelControlModelImplementation.SetChannelValue(false);
            }
            else
            {
                _channelControlModelImplementation.SetChannelValue(NewValue);
            }
        }

        private string[] alarmNames = { };
        private uint timeout = 1000;
        private IAlarmManager2 alarm_manager;
        private Factory factory;

        /// <summary>
        /// Gets the type of the specified property.  This must be implemented for any new properties that get added that need to be serialized.
        /// </summary>
        /// <param name="identifier">The property to get the type of.</param>
        /// <returns>The exact runtime type of the specified property.</returns>
        public override Type GetPropertyType(PropertySymbol identifier)
        {
            switch (identifier.Name)
            {
                case "Gateway":
                    return typeof(string);
                default:
                    return base.GetPropertyType(identifier);
            }
        }

        /// <summary>
        /// Gets the default value of the specified property.  This must be implemented for any new properties that get added that need to be serialized.
        /// </summary>
        /// <param name="identifier">The property to get the default value of.</param>
        /// <returns>The default value of the specified property.</returns>
        public override object DefaultValue(PropertySymbol identifier)
        {
            switch (identifier.Name)
            {
                case "Gateway":
                    return "localhost";
                default:
                    return base.DefaultValue(identifier);
            }
        }

        public static readonly PropertySymbol GatewaySymbol =
            ExposePropertySymbol<AlarmedPowerButtonModel>("Gateway", (string)"localhost");

        /// <summary>
        /// Gets or sets the path to the VeriStand channel associated with this control models frequency
        /// </summary>
        public string Gateway
        {
            get { return ImmediateValueOrDefault<string>(GatewaySymbol); }
            set
            {
                IActiveTransaction activeTransaction = this.TransactionManager.BeginTransaction("Gateway Address Changed", TransactionPurpose.User);
                SetOrReplaceImmediateValue(GatewaySymbol, value);
                // Check whether the alarm is active
                if (IsConnected && HostnameIpValidator.IsValidHostnameOrIp(value))
                {
                    // Supposedly our thread is now dead and we restart it all back up
                    alarm_manager = factory.GetIAlarmManager2(Gateway);
                    alarm_manager.GetAlarmList(out alarmNames);
                }
                if (!HostnameIpValidator.IsValidHostnameOrIp(value))
                {
                    ReportErrorToModel(new Exception("Invalid Gateway Address Set: Address is not a valid hostname or ip address."));
                }
                activeTransaction.Commit();
            }
        }

        public Task OnConnectedAsync()
        {
            _channelControlModelImplementation.SetChannelValue(0.0);
            alarm_manager = factory.GetIAlarmManager2(Gateway);
            alarm_manager.GetAlarmList(out alarmNames);
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task OnConnectingAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnDisconnectingAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        private const string AlarmedPowerButtonModelErrorString = "AlarmedPowerButtonModelErrors";

        private void ReportErrorToModel(Exception ex)
        {
            // Clear any existing errors and then report a new error message.  Use Host.BeginInvoke here since this must occur on the UI thread
            Host.BeginInvoke(AsyncTaskPriority.WorkerHigh, () =>
            {
                MessageScope?.AllMessages.ClearMessageByCategoryAndReportingElement(
                    AlarmedPowerButtonModelErrorString,
                    this);
#if MUTATE2021
                this.SafeReportError(AlarmedPowerButtonModelErrorString, null, MessageDescriptor.Empty, ex);
#else
                this.ReportError(AlarmedPowerButtonModelErrorString, null, MessageDescriptor.Empty, ex);
#endif
            });
        }


        public Task OnDisconnectedAsync()
        {            
            return Task.CompletedTask;
        }

        public Task OnStartAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnShutdownAsync()
        {
            return Task.CompletedTask;
        }
    }
    }
