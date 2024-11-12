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
using NationalInstruments.Hmi.Core.Screen;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// This class is an implementation of ICustomVeriStandControl. By specifying it as an Export, we are identifying
    /// it as a plugin to VeriStand.
    /// The interface implementation defines how the control should appear in the palette.
    /// </summary>
    [Export(typeof(ICustomVeriStandControl))]
    public class EnableNumericModelExporter : ICustomVeriStandControl
    {
        /// <summary>
        /// Mergescript which defines what to drop on the screen from the palette.  Can be used to set default values on the control
        /// </summary>
        public string Target =>
            "<pf:MergeScript xmlns:pf=\"http://www.ni.com/PlatformFramework\">" +
                "<pf:MergeItem>" +
                    "<EnableNumeric xmlns=\"http://www.your-company.com/VeriStandExample\" Width=\"[float]57\" Height=\"[float]24\"/>" +
                "</pf:MergeItem>" +
            "</pf:MergeScript>";

        /// <summary>
        /// Name of the control as it will appear in the palette
        /// </summary>
        public string Name => "Enable Numeric";

        /// <summary>
        /// Path to the image to use in the palette
        /// </summary>
        public string ImagePath => "/NationalInstruments.VeriStand.CustomControls;component/Resources/enablenumeric_32x32.png";

        /// <summary>
        /// Tool tip to display in the palette
        /// </summary>
        public string ToolTip => "Enable numeric Tool Tip";

        /// <summary>
        /// Unique id for the control. The only requirement is that this doesn't overlap with existing controls or other custom controls.
        /// This is used for serialization and the context help system.
        /// </summary>
        public string UniqueId => "EnableNumeric";

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
    public class EnableNumericModel : ChannelNumericTextBoxModel, IDataEngineStateChangeObserver, ICommonConfigurationPaneControl
    {
        /// <summary>
        /// The name of the control which will be used in XML.  This name in the mergescript in the Target property of the ICustomVeriStandControl interface must match this
        /// </summary>
        private const string EnNumericName = "EnableNumeric";

        /// <summary>
        /// How this model will be represented in XML.
        /// </summary>
        public override XName XmlElementName => XName.Get(EnNumericName, PluginNamespaceSchema.ParsableNamespaceName);

        /// <summary>Creates a <see cref="EnableNumericModel"/>.</summary>
        /// <param name="info">An <see cref="IElementCreateInfo"/> instance.</param>
        /// <returns>A <see cref="EnableNumericModel"/>.</returns>
        [XmlParserFactoryMethod(EnNumericName, PluginNamespaceSchema.ParsableNamespaceName)]
        public static EnableNumericModel CreateEnableNumericModel(IElementCreateInfo info)
        {
            var model = new EnableNumericModel();
#if MUTATE2020
            model.Initialize(info);
#else
            model.Init(info);
#endif
            model.IsReadOnly = true;

            return model;
        }

        public class Doc : IDocumentation
        {
            string IDocumentation.Description => "This Numeric Channel only allows you to write to the specified channel if a selected enable channel is a non-zero value (i.e. not false).";

            public string InstanceName { get; set; }

            string IDocumentation.Name => "Enabled Numeric";
        }

        protected override IDocumentation CreateDocumentation()
        {
            Doc doc_object = new Doc();
            doc_object.InstanceName = this.EventSourceName;
            return doc_object;
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
                case "EnableChannel":
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
                case "EnableChannel":
                    return "";
                default:
                    return base.DefaultValue(identifier);
            }
        }
        /// <summary>
        /// Gets the channel value
        /// </summary>
        public double EnabledChannelValue { get; protected set; }

        public string EnableChannelDisplayName
        {
            get { return ImmediateValueOrDefault<string>(EnableChannelSymbol); }
        }

        public static readonly PropertySymbol EnableChannelSymbol =
            ExposePropertySymbol<EnableNumericModel>("EnableChannel", "");
        /// <summary>
        /// Gets or sets the path to the VeriStand channel associated with this control model
        /// </summary>
        public string EnableChannel
        {
            get
            {
                return ImmediateValueOrDefault<string>(EnableChannelSymbol);
            }
            set
            {

                // If we're connected, unregister the tag
                if (IsConnected)
                {
                    if (ImmediateValueOrDefault<string>(EnableChannelSymbol) != "")
                    {
                        Host.GetRunTimeService<ITagService>().UnregisterTagAsync(value, OnEnableChannelValueChange);
                    }
                }
                SetOrReplaceImmediateValue(EnableChannelSymbol, value);
                IsReadOnly = true;
                // Re-register
                if (IsConnected)
                {
                    Host.BeginInvoke(AsyncTaskPriority.WorkerHigh, () =>
                    {
                        MessageScope?.AllMessages.ClearMessageByCategoryAndReportingElement(EnableNumericModelErrorString, this);
                    });
                    if (!string.IsNullOrEmpty(value))
                    {
                        try
                        {
                            Host.GetRunTimeService<ITagService>().RegisterTagAsync(value, OnEnableChannelValueChange);
                        }
                        catch (Exception ex) when (ShouldExceptionBeCaught(ex))
                        {
                            ReportErrorToModel(ex);
                        }
                    }else
                    {
                        ReportErrorToModel(new Exception("No Enable Channel selected for Enable Numeric - it will remain a indicator until one is selected."));
                    }
                }
            }
        }

        public bool IsConnected = false;

        /// <summary>
        /// String used to put errors from this control in their own bucket so code from this model doesn't interfere with the rest of the error
        /// reporting behavior in VeriStand
        /// </summary>
        private const string EnableNumericModelErrorString = "EnableNumericModelErrors";


        public async Task OnConnectedAsync()
        {
            IsConnected = true;
            Host.BeginInvoke(AsyncTaskPriority.WorkerHigh, () =>
            {
                MessageScope?.AllMessages.ClearMessageByCategoryAndReportingElement(EnableNumericModelErrorString, this);
            });
            if (!string.IsNullOrEmpty(EnableChannel))
            {
                try
                {
                    await Host.GetRunTimeService<ITagService>().RegisterTagAsync(EnableChannel, OnEnableChannelValueChange);
                    ITagValue value = await Host.GetRunTimeService<ITagService>().GetTagValueAsync(EnableChannel);
                    if((double)value.Value > 0)
                    {
                        IActiveTransaction activeTransaction = this.TransactionManager.BeginTransaction("Set enable channel direction", TransactionPurpose.User);
                        this.IsReadOnly = false;
                        activeTransaction.Commit();
                    }
                }
                catch (Exception ex) when (ShouldExceptionBeCaught(ex))
                {
                    ReportErrorToModel(ex);
                }
            }
            else
            {
                ReportErrorToModel(new Exception("No Enable Channel selected for Enable Numeric - it will remain a indicator until one is selected."));
            }
        }


        /// <summary>
        /// Acquires a read lock on the entire model that the associated element is a part of.
        /// the model includes all elements which share a transaction manager.
        /// </summary>
        /// <returns>Disposable to dispose of to release the read lock.</returns>
        protected IDisposable AcquireReadLock()
        {
            return AcquireModelReadLock();
        }

        /// <summary>
        /// These objects are used as owners for the gateway collator.  The gateway collators only stores one action per owner at a time and sends it to/receives from  the gateway periodically.   This
        /// is limit the rate at which things are sent/received from the gateway to avoid flooding the WCF pipe or falling behind in time.  Since this control has two buckets of things to collate against each other (frequency updates,
        /// and duty cycle updates, we need two owners to keep one controls updates from overwriting the others updates in the collator
        /// </summary>
        private readonly object _enabledNumericCollatorOwner = new object();

        /// <summary>
        /// Fired when output channel value changes
        /// </summary>
        /// <param name="value">new tag value</param>
        private void OnEnableChannelValueChange(ITagValue value)
        {
            double newChannelValue = (double)value.Value;
            using (AcquireReadLock())
            {
                // The visual parent is null if the item is deleted, this is not null for models that are within a container
                // and are not directly the children of the screen surface.
                if (VisualParent == null)
                {
                    return;
                }
                ScreenModel screenModel = ScreenModel.GetScreen(this);
                // add an action to the collator.  the collator will limit the number of actions coming from the gateway and only
                // process the most recent action. This keeps us from falling behind in time if we can't process the gateway updates as fast as they are received.
                screenModel.FromGatewayActionCollator.AddAction(
                    _enabledNumericCollatorOwner,
                    () =>
                    {
                        using (AcquireReadLock())
                        {
                            // The item could get deleted after the action has been dispatched.
                            if (VisualParent != null)
                            {
                                if (!Equals(EnabledChannelValue, newChannelValue))
                                {
                                    EnabledChannelValue = newChannelValue;
                                    if (newChannelValue > 0.0)
                                    {
                                            IActiveTransaction activeTransaction = this.TransactionManager.BeginTransaction("Set enable channel direction", TransactionPurpose.User);
                                            this.IsReadOnly = false;
                                            activeTransaction.Commit();
                                        
                                    }
                                    else
                                    {
                                        IActiveTransaction activeTransaction = this.TransactionManager.BeginTransaction("Set enable channel direction", TransactionPurpose.User);
                                        this.IsReadOnly = true;
                                        activeTransaction.Commit();
                                    }
                                }
                            }
                        }
                    });
            }
        }

        private void ReportErrorToModel(Exception ex)
        {
            // Clear any existing errors and then report a new error message.  Use Host.BeginInvoke here since this must occur on the UI thread
            Host.BeginInvoke(AsyncTaskPriority.WorkerHigh, () =>
            {
                MessageScope?.AllMessages.ClearMessageByCategoryAndReportingElement(
                    EnableNumericModelErrorString,
                    this);
#if MUTATE2021
                this.SafeReportError(EnableNumericModelErrorString, null, MessageDescriptor.Empty, ex);
#else
                this.ReportError(EnableNumericModelErrorString, null, MessageDescriptor.Empty, ex);
#endif
            });
        }

        public async Task OnDisconnectingAsync()
        {
            IsConnected = false;
            // use Host.BeginInvoke to clear error messages when connecting to the gateway.  The error message collection must be interacted with by the UI thread
            // which is why we must use BeginInvoke since OnConnectToGateway is not guaranteed to be called by the UI thread
            Host.BeginInvoke(
                AsyncTaskPriority.WorkerHigh,
                () =>
                    MessageScope?.AllMessages.ClearMessageByCategoryAndReportingElement(
                        EnableNumericModelErrorString,
                        this));
            if (!string.IsNullOrEmpty(EnableChannel))
            {
                try
                {
                    await Host.GetRunTimeService<ITagService>().UnregisterTagAsync(EnableChannel, OnEnableChannelValueChange);
                }
                catch (Exception ex) when (ShouldExceptionBeCaught(ex))
                {
                    ReportErrorToModel(ex);
                }
            }
            IActiveTransaction activeTransaction = this.TransactionManager.BeginTransaction("Set enable channel direction", TransactionPurpose.User);
            this.IsReadOnly = true;
            activeTransaction.Commit();
        }

        public Task OnConnectingAsync()
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task OnDisconnectedAsync()
        {
            IActiveTransaction activeTransaction = this.TransactionManager.BeginTransaction("Set enable channel direction", TransactionPurpose.User);
            this.IsReadOnly = true;
            activeTransaction.Commit();
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
