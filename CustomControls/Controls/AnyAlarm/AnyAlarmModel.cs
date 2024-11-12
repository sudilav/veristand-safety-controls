using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Xml.Linq;
using NationalInstruments.Controls;
using NationalInstruments.Controls.Primitives;
using NationalInstruments.Controls.SourceModel;
using NationalInstruments.Hmi.Core.Controls.Models;
using NationalInstruments.Hmi.Core.Services;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VeriStand.SourceModel.Screen;
using NationalInstruments.VeriStand.ClientAPI;
using System.Threading;
using System.Net.Sockets;
using System.ComponentModel;
using System.Windows.Media;
using NationalInstruments.DynamicProperties;
using System.Linq.Expressions;
using NationalInstruments.CommonModel;
using NationalInstruments.Core;
using NationalInstruments.VeriStand.SourceModel;
using System.Net;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// This class is an implementation of ICustomVeriStandControl. By specifying it as an Export, we are identifying
    /// it as a plugin to VeriStand.
    /// The interface implementation defines how the control should appear in the palette.
    /// </summary>
    [Export(typeof(ICustomVeriStandControl))]
    public class AnyAlarmModelExporter : ICustomVeriStandControl
    {
        /// <summary>
        /// Mergescript which defines what to drop on the screen from the palette.  Can be used to set default values on the control
        /// </summary>
        public string Target =>
            "<pf:MergeScript xmlns:pf=\"http://www.ni.com/PlatformFramework\">" +
                "<pf:MergeItem>" +
                    "<AnyAlarm xmlns=\"http://www.edward-jones.co.uk/VeriStandExample\" Width=\"[float]52\" Height=\"[float]52\"/>" +
                "</pf:MergeItem>" +
            "</pf:MergeScript>";

        /// <summary>
        /// Name of the control as it will appear in the palette
        /// </summary>
        public string Name => "Any Alarm";

        /// <summary>
        /// Path to the image to use in the palette
        /// </summary>
        public string ImagePath => "/NationalInstruments.VeriStand.CustomControls;component/Resources/any_alarm_32x32.png";

        /// <summary>
        /// Tool tip to display in the palette
        /// </summary>
        public string ToolTip => "Any Alarm Tool Tip";

        /// <summary>
        /// Unique id for the control. The only requirement is that this doesn't overlap with existing controls or other custom controls.
        /// This is used for serialization and the context help system.
        /// </summary>
        public string UniqueId => "AnyAlarm";

        /// <summary>
        /// If a folder hierarchy is desired it can be returned here.  If multiple controls should show up in the same subfolder either use the same PaletteElementCategory object in their
        /// hierarchy list or use a category with the same unique id.  Unique IDs cannot be duplicated at different hierarchy levels.
        /// </summary>
        public IList<PaletteElementCategory> PaletteHierarchy =>
            new List<PaletteElementCategory>() { new PaletteElementCategory("Safety", ImagePath, "Safety", .1) };
    }

    /// <summary>
    /// This model has the business logic for the AnyAlarm control.  Mostly it handles all of that in the base class ChannelKnobModel.
    /// </summary>
    public class AnyAlarmModel :
        VisualModel, INotifyPropertyChanged, IGatewayModel,
#if MUTATE2020R4
        IDataEngineStateChangeObserver
#else
        ISubscribeProviderStatusUpdates
#endif
    {
        /// <summary>
        /// The name of the control which will be used in XML.  This name in the mergescript in the Target property of the ICustomVeriStandControl interface must match this
        /// </summary>
        private const string AnyAlarmName = "AnyAlarm";
        private IAlarmManager2 alarm_manager;
        /// <summary>
        /// How this model will be represented in XML.
        /// </summary>
        public override XName XmlElementName => XName.Get(AnyAlarmName, PluginNamespaceSchema.ParsableNamespaceName);

        /// <summary>Creates a <see cref="AnyAlarmControlModel"/>.</summary>
        /// <param name="info">An <see cref="IElementCreateInfo"/> instance.</param>
        /// <returns>A <see cref="AnyAlarmControlModel"/>.</returns>
        [XmlParserFactoryMethod(AnyAlarmName, PluginNamespaceSchema.ParsableNamespaceName)]
        /// <summary>
        /// Factory method for creating a new PWMControlModel
        /// </summary>
        /// <param name="info">Information required to create the model, such as the parser.</param>
        /// <returns>A constructed and initialized PulseWidthModulationControlModel instance.</returns>
        [XmlParserFactoryMethod(AnyAlarmName, PluginNamespaceSchema.ParsableNamespaceName)]
        public static AnyAlarmModel Create(IElementCreateInfo info)
        {
            var model = new AnyAlarmModel();
#if MUTATE2020
            model.Initialize(info);
#else
            model.Init(info);
#endif
            return model;
        }

        public class Doc : IDocumentation
        {
            string IDocumentation.Description => "This bounding box item is red when any alarm in the system is tripped, white when not and gives an overall status of the system.";

            public string InstanceName { get; set; }

            string IDocumentation.Name => "Any Alarm Notifier";
        }

        protected override IDocumentation CreateDocumentation()
        {
            Doc doc_object = new Doc();
            //doc_object.InstanceName = this.EventSourceName;
            return doc_object;
        }

        private Thread _alarmThread;
        private bool _isDisposed = false;
        private bool _isThreadRunning = false; // Flag to control the thread
        private readonly object _lockObject = new object();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    
                }

                // Stop the color changing thread
                _isDisposed = true;
                if (_alarmThread != null && _alarmThread.IsAlive)
                {
                    _alarmThread.Join();
                }
            }
        }

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
            ExposePropertySymbol<AnyAlarmModel>("Gateway", (string)"localhost");

        /// <summary>
        /// Gets or sets the path to the VeriStand channel associated with this control models frequency
        /// </summary>
        public string Gateway
        {
            get { return ImmediateValueOrDefault<string>(GatewaySymbol); }
            set {
                IActiveTransaction activeTransaction = this.TransactionManager.BeginTransaction("Gateway Address Changed", TransactionPurpose.User);
                SetOrReplaceImmediateValue(GatewaySymbol, value);
                // Check whether the alarm is active
                if (_isThreadRunning)
                {
                    _isThreadRunning = false;
                    _alarmThread.Join();
                }
                if (isConnected && HostnameIpValidator.IsValidHostnameOrIp(value))
                {
                    // Supposedly our thread is now dead and we restart it all back up
                    Thread _startalarmThread = new Thread(startThread);
                    _startalarmThread.IsBackground = true;
                    _startalarmThread.Start();
                }
                if (!HostnameIpValidator.IsValidHostnameOrIp(value))
                {
                    ReportErrorToModel(new Exception("Invalid Gateway Address Set: Address is not a valid hostname or ip address."));
                }
                activeTransaction.Commit();
            }
        }

        private bool isConnected = false;
        private bool _isRed;

        public bool IsRed
        {
            get => _isRed;
            set
            {
                _isRed = value;
                OnPropertyChanged(nameof(IsRed)); // Notify ViewModel of changes
            }
        }

        private string[] alarmNames = {};
        private uint timeout = 1000;
        private void AlarmCheckThreadMethod()
        {
            while (!_isDisposed && _isThreadRunning)
            {
                lock (_lockObject)
                {
                    // Change the background color (example: alternating between red and blue)
                    // This is where we would change the colors
                }
                
                AlarmInfo[] alarmInfo; // Declaration without initialization
                bool anytripped = false;
                // Query Alarm
                Error result = alarm_manager.GetMultipleAlarmsData(alarmNames, timeout, out alarmInfo);
                if (result.Code == 0) // Assuming Error.None indicates success
                {
                    // Now alarmInfo contains the result
                    foreach (var alarm in alarmInfo)
                    {
                        if(alarm.State == AlarmState.Tripped){
                            // Alarm is tripped
                            anytripped = true;
                        }
                    }
                    // Only update if needed
                    if (IsRed != anytripped) { IsRed = anytripped; }
                    
                }
                // Sleep for 500ms
                Thread.Sleep(500);
            }
            _alarmThread = null;
        }

        public Task OnConnectingAsync()
        {
            return Task.CompletedTask;
        }


        public Task OnDisconnectingAsync()
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

        public void startThread()
        {
            try {
                Factory factory = new Factory();
                alarm_manager = factory.GetIAlarmManager2(Gateway);
                alarm_manager.GetAlarmList(out alarmNames);
                // Next start a thread which monitors the alarms status
                _alarmThread = new Thread(AlarmCheckThreadMethod);
                _alarmThread.IsBackground = true;
                _isThreadRunning = true;
                _alarmThread.Start();
            } catch {
                ReportErrorToModel(new Exception("Invalid Gateway Address Set and could not retrieve Alarm Manager. Check: Ports are allowed by firewall, port is correct or 2039, address is correct. This has been compiled for the right VS Version (2024)"));
            }
        }

        /// <summary>
        /// String used to put errors from this control in their own bucket so code from this model doesn't interfere with the rest of the error
        /// reporting behavior in VeriStand
        /// </summary>
        private const string AnyAlarmModelErrorString = "AnyAlarmModelErrors";

        private void ReportErrorToModel(Exception ex)
        {
            // Clear any existing errors and then report a new error message.  Use Host.BeginInvoke here since this must occur on the UI thread
            Host.BeginInvoke(AsyncTaskPriority.WorkerHigh, () =>
            {
                MessageScope?.AllMessages.ClearMessageByCategoryAndReportingElement(
                    AnyAlarmModelErrorString,
                    this);
#if MUTATE2021
                this.SafeReportError(AnyAlarmModelErrorString, null, MessageDescriptor.Empty, ex);
#else
                this.ReportError(AnyAlarmModelErrorString, null, MessageDescriptor.Empty, ex);
#endif
            });
        }

        public Task OnConnectedAsync()
        {
            isConnected = true;
            startThread();
            return Task.CompletedTask;
        }

        public Task OnDisconnectedAsync()
        {
            isConnected = false;
            _isThreadRunning = false;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override XamlGenerationHelper XamlGenerationHelper
        {
            get { return new AnyAlarmXamlHelper(); }
        }

        private class AnyAlarmXamlHelper : XamlGenerationHelper
        {
            public override Type ControlType => typeof(AnyAlarm);
        }
    }
}
