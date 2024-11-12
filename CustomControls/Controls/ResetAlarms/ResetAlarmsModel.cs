using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using AlarmState = NationalInstruments.VeriStand.ClientAPI.AlarmState;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// This class is an implementation of ICustomVeriStandControl. By specifying it as an Export, we are identifying
    /// it as a plugin to VeriStand.
    /// The interface implementation defines how the control should appear in the palette.
    /// </summary>
    [Export(typeof(ICustomVeriStandControl))]
    public class ResetAlarmsModelExporter : ICustomVeriStandControl
    {
        /// <summary>
        /// Mergescript which defines what to drop on the screen from the palette.  Can be used to set default values on the control
        /// </summary>
        public string Target =>
            "<pf:MergeScript xmlns:pf=\"http://www.ni.com/PlatformFramework\">" +
                "<pf:MergeItem>" +
                    "<ResetAlarms xmlns=\"http://www.your-company.com/VeriStandExample\" Width=\"[float]57\" Height=\"[float]24\"/>" +
                "</pf:MergeItem>" +
            "</pf:MergeScript>";

        /// <summary>
        /// Name of the control as it will appear in the palette
        /// </summary>
        public string Name => "Reset Alarms";

        /// <summary>
        /// Path to the image to use in the palette
        /// </summary>
        public string ImagePath => "/NationalInstruments.VeriStand.CustomControls;component/Resources/resetalarm_32x32.png";

        /// <summary>
        /// Tool tip to display in the palette
        /// </summary>
        public string ToolTip => "Reset Alarms Tool Tip";

        /// <summary>
        /// Unique id for the control. The only requirement is that this doesn't overlap with existing controls or other custom controls.
        /// This is used for serialization and the context help system.
        /// </summary>
        public string UniqueId => "ResetAlarms";

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
    public class ResetAlarmsModel : BooleanButtonModel, IDataEngineStateChangeObserver
    {
        /// <summary>
        /// The name of the control which will be used in XML.  This name in the mergescript in the Target property of the ICustomVeriStandControl interface must match this
        /// </summary>
        private const string EnNumericName = "ResetAlarms";

        /// <summary>
        /// How this model will be represented in XML.
        /// </summary>
        public override XName XmlElementName => XName.Get(EnNumericName, PluginNamespaceSchema.ParsableNamespaceName);

        /// <summary>Creates a <see cref="ResetAlarmsModel"/>.</summary>
        /// <param name="info">An <see cref="IElementCreateInfo"/> instance.</param>
        /// <returns>A <see cref="ResetAlarmsModel"/>.</returns>
        [XmlParserFactoryMethod(EnNumericName, PluginNamespaceSchema.ParsableNamespaceName)]
        public static ResetAlarmsModel CreateResetAlarmsModel(IElementCreateInfo info)
        {
            var model = new ResetAlarmsModel();
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
            string IDocumentation.Description => "Resets all alarms and also offers the ability to toggle a specified channel to 1 and then 0 for the specified duty cycle time.";

            public string InstanceName { get; set; }

            string IDocumentation.Name => "Reset Alarms Button";
        }

        protected override IDocumentation CreateDocumentation()
        {
            Doc doc_object = new Doc();
            doc_object.InstanceName = this.EventSourceName;
            return doc_object;
        }

        private string[] alarmNames = { };
        public bool connected = false;
        private IAlarmManager2 alarm_manager;

        /// <summary>
        /// Specifies the PropertySymbol for the configuration.  Any custom attribute that needs to serialized so that it is saved needs to be a property symbol.
        /// </summary>
        public static readonly PropertySymbol AlarmServiceTimeoutSymbol =
            ExposePropertySymbol<ResetAlarmsModel>("AlarmServiceTimeout", (int)1000);

        public static readonly PropertySymbol MinimumCycleTimeSymbol =
            ExposePropertySymbol<ResetAlarmsModel>("MinimumCycleTime", (int)50);

        /// <summary>
        /// Gets the type of the specified property.  This must be implemented for any new properties that get added that need to be serialized.
        /// </summary>
        /// <param name="identifier">The property to get the type of.</param>
        /// <returns>The exact runtime type of the specified property.</returns>
        public override Type GetPropertyType(PropertySymbol identifier)
        {
            switch (identifier.Name)
            {
                case "AlarmServiceTimeout":
                    return typeof(int);
                case "MinimumCycleTime":
                    return typeof(int);
                case "ResetChannel":
                    return typeof(string);
                case "Gateway":
                    return typeof(string);
                default:
                    return base.GetPropertyType(identifier);
            }
        }

        /// <summary>
        /// Gets or sets the path to the VeriStand channel associated with this control models frequency
        /// </summary>
        public int AlarmServiceTimeout
        {
            get { return ImmediateValueOrDefault<int>(AlarmServiceTimeoutSymbol); }
            set { SetOrReplaceImmediateValue(AlarmServiceTimeoutSymbol, value); }
        }

        /// <summary>
        /// Gets or sets the path to the VeriStand channel associated with this control models duty cycle
        /// </summary>
        public int MinimumCycleTime
        {
            get { return ImmediateValueOrDefault<int>(MinimumCycleTimeSymbol); }
            set { SetOrReplaceImmediateValue(MinimumCycleTimeSymbol, value); }
        }

        public static readonly PropertySymbol ResetChannelSymbol =
            ExposePropertySymbol<EnableNumericModel>("ResetChannel", "");

        public string ResetChannel
        {
            get
            {
                return ImmediateValueOrDefault<string>(ResetChannelSymbol);
            }
            set
            {
                SetOrReplaceImmediateValue(ResetChannelSymbol, value);
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
                case "AlarmServiceTimeout":
                    return (int)1000;
                case "MinimumCycleTime":
                    return (int)50;
                case "ResetChannel":
                    return "";
                case "Gateway":
                    return "localhost";
                default:
                    return base.DefaultValue(identifier);
            }
        }

        public static readonly PropertySymbol GatewaySymbol =
            ExposePropertySymbol<ResetAlarmsModel>("Gateway", (string)"localhost");

        /// <summary>
        /// Gets or sets the path to the VeriStand channel associated with this control models frequency
        /// </summary>
        public string Gateway
        {
            get { return ImmediateValueOrDefault<string>(GatewaySymbol); }
            set
            {
                SetOrReplaceImmediateValue(GatewaySymbol, value);
                if (connected)
                {
                    try
                    {
                        alarm_manager = factory.GetIAlarmManager2(Gateway);
                        alarm_manager.GetAlarmList(out alarmNames);
                    }
                    catch (Exception ex) when (ShouldExceptionBeCaught(ex))
                    {
                        ReportErrorToModel(ex);
                    }
                }
            }
        }

        public async Task HandleAlarm()
        {
            if (connected)
            {
                alarm_manager = factory.GetIAlarmManager2(Gateway);
                alarm_manager.GetAlarmList(out alarmNames);
                //Oscillate the channel tag
                if (!string.IsNullOrEmpty(ResetChannel))
                {
                    try
                    {
                        await Host.GetRunTimeService<ITagService>().SetTagValueAsync(ResetChannel, TagFactory.CreateTag(1.0));
                    }
                    catch (Exception ex) when (ShouldExceptionBeCaught(ex))
                    {
                        ReportErrorToModel(ex);
                    }
                }
                //await Host.GetRunTimeService<ITagService>().SetTagValueAsync(OutputEnabledChannel, TagFactory.CreateTag(1.0));
                Stopwatch stopwatch = Stopwatch.StartNew();
                AlarmInfo[] alarmInfo; // Declaration without initialization
                // Query Alarm
                Error result = alarm_manager.GetMultipleAlarmsData(alarmNames, (uint)AlarmServiceTimeout, out alarmInfo);
                if (result.Code == 0) // Assuming Error.None indicates success
                {
                    // Now alarmInfo contains the result
                    for (int i = 0; i < Math.Min(alarmNames.Length, alarmInfo.Length); i++)
                    {
                        AlarmInfo alarm = alarmInfo[i];
                        string alarmName = alarmNames[i];
                        if (alarm.State == AlarmState.Tripped)
                        {
                            // Alarm is tripped
                            // Detrip alarm
                            IAlarm alarmobj = factory.GetIAlarm(alarmName);
                            Error err = alarmobj.SetEnabledState(false);
                            Error errset = alarmobj.SetEnabledState(true);
                        }
                    }

                }
                stopwatch.Stop();

                // Check how much time has passed
                long elapsedTimeMs = stopwatch.ElapsedMilliseconds;
                // If the code took less than 50 ms, wait for the remaining time
                if (elapsedTimeMs < MinimumCycleTime && !string.IsNullOrEmpty(ResetChannel))
                {
                    long remainingTimeMs = MinimumCycleTime - elapsedTimeMs;
                    await Task.Delay((int)remainingTimeMs); // Wait for the remaining time
                }
                //Now oscillate it back
                if (!string.IsNullOrEmpty(ResetChannel))
                {
                    try
                    {
                        await Host.GetRunTimeService<ITagService>().SetTagValueAsync(ResetChannel, TagFactory.CreateTag(0.0));
                        await Task.Delay((int)MinimumCycleTime);
                        await Host.GetRunTimeService<ITagService>().SetTagValueAsync(ResetChannel, TagFactory.CreateTag(0.0));
                    }
                    catch (Exception ex) when (ShouldExceptionBeCaught(ex))
                    {
                        ReportErrorToModel(ex);
                    }
                    
                }
            }
        }

        public Task OnConnectedAsync()
        {
            connected = true;
            return Task.CompletedTask;
        }

        public Factory factory;
        public Task OnConnectingAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnDisconnectedAsync()
        {
            connected = false;
            return Task.CompletedTask;
        }

        public Task OnDisconnectingAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnShutdownAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnStartAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// String used to put errors from this control in their own bucket so code from this model doesn't interfere with the rest of the error
        /// reporting behavior in VeriStand
        /// </summary>
        private const string ResetAlarmModelErrorString = "Reset Alarm Errors";

        private void ReportErrorToModel(Exception ex)
        {
            // Clear any existing errors and then report a new error message.  Use Host.BeginInvoke here since this must occur on the UI thread
            Host.BeginInvoke(AsyncTaskPriority.WorkerHigh, () =>
            {
                MessageScope?.AllMessages.ClearMessageByCategoryAndReportingElement(
                    ResetAlarmModelErrorString,
                    this);
#if MUTATE2021
                this.SafeReportError(ResetAlarmModelErrorString, null, MessageDescriptor.Empty, ex);
#else
                this.ReportError(ResetAlarmModelErrorString, null, MessageDescriptor.Empty, ex);
#endif
            });
        }

    }
}
