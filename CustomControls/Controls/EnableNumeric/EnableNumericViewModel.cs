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
using NationalInstruments.Hmi.Core.Commands;
using NationalInstruments.PanelCommon.Design;
using NationalInstruments.Shell;
using System.Collections.Generic;
using NationalInstruments.MocCommon.Design;
using NationalInstruments.SourceModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using NationalInstruments.Hmi.Core.Controls.Mapping;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Hmi.Core.SystemReflection;
using System.Diagnostics.Tracing;
using NationalInstruments.ContextualHelp;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// The view model for the Enable Numeric control. The only thing this does different from the channelnumerictextboxviewmodel is that we return a custom view.
    /// Since the view we have inherits from knob it will be compatible with the existing models and view models.
    /// </summary>
    public class EnableNumericViewModel : ChannelNumericTextViewModel, IChannelControlViewValueAccessor
    {
        private EnableNumericModel _model;

        /// <summary>
        /// Constructs a new instance of the EnableNumericViewModel class
        /// </summary>
        /// <param name="model">The EnableNumericModel assocaited with this view model.</param>
        public EnableNumericViewModel(EnableNumericModel model) : base(model)
        {
            var activeRunTimeServiceProvider = Host.ActiveRunTimeServiceProvider();
            activeRunTimeServiceProvider.RegisterStatusListener(model);
            _model = model;
        }

        public override void CreateContextHelpServices()
        {
            base.CreateContextHelpServices();
            SetService<IContextHelpInfoService>(new ElementContextHelpInfoService(this, providesHelp: true));
            SetService<IContextHelpHtmlService>(new DefaultContextHelpHtmlService("Enable Numeric"));
            //SetService<IContextHelpContentProvider>(new DefaultContextHelpContentProvider());
        }

        /// <summary>
        /// Returns a custom view for associated with this view model
        /// </summary>
        /// <returns>Enable Numeric view</returns>
        public override object CreateView()
        {
            var view = new EnableNumeric();
            // numeric controls have a helper which needs to get attached to the view that helps setting values of different types to the control. Since we only have I32
            // for our control type we just hard code that when creating the helper
            Helper = CreateHelper(typeof(double), this, view);
            return view;
        }

        public void OnSelectedEnableNodeChanged(object sender, SelectedNodesEventArgs e)
        {
            IActiveTransaction activeTransaction = this.TransactionManager.BeginTransaction("Set Enable channel", TransactionPurpose.User);
            _model.EnableChannel = e.SelectedNodeNames[0];
            activeTransaction.Commit();
            CeipHelper.LogScreenMapping(Model, e.SelectedNodeNames[0]);
            //UpdateViewValue(OwningViewModel.ProxiedElement, eventArgs.ChannelValue);
        }

        protected override void OnCreateContextMenu(CreateContextMenuRoutedEventArgs args)
        {
            base.OnCreateContextMenu(args);
            //args.Remove("NI.ControlCommands:SetControlToIndicator");
            args.Remove("NI.ControlCommands:SetIndicatorToControl");
        }

        IMappingControl mappingControl;



        public override void CreateCommandContent(ICommandPresentationContext context)
        {

            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                context.Remove(ConfigurationBasedSoftwareControlIndicatorCommands.ControlIndicatorCommand);

                using (context.AddGroup(EnableChannelGroupCommand))
                {
                    MappingControlOptions options = new MappingControlOptions
                    {
                        AllowMultipleSelections = false, // You could also exclude names by setting excludedNodeNames
                        RequiredCapabilities = new[] { HmiCapabilities.Control }
                    };
                    mappingControl = Host.GetSharedExportedValue<IMappingControlProvider>().CreateMappingControl(() => new List<string> { _model.EnableChannel }, options);
                    WeakEventManager<IMappingControl, SelectedNodesEventArgs>.AddHandler(mappingControl, "SelectedNodesChanged", OnSelectedEnableNodeChanged);
                    context.Add(SetEnableChannelCommand, new SingleMappingVisualFactory(mappingControl));
                }
            }
        }

        public static readonly ICommandEx EnableChannelGroupCommand = new ShellSelectionRelayCommand(null, null)
        {
            LabelTitle = "Enable Channel",
            UniqueId = "NI.EnableChannelCommands:EnableChannelGroupCommand",
            Weight = 0.101
        };

        public static readonly ICommandEx SetEnableChannelCommand = new PropertySymbolCommand(EnableNumericModel.EnableChannelSymbol, UIModelPropertySymbolDataModel.GetCreator())
        {
            LabelTitle = "Enable Channel",
            UniqueId = "NI.EnableChannelCommands:EnableChannelCommand"
        };
    }
}
