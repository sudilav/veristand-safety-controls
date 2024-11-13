using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.SourceModel;
using NationalInstruments.VeriStand.SourceModel.Screen;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NationalInstruments.Hmi.Core.Services;
using System.Threading;
using System.ComponentModel;
using System.Net.Sockets;
using NationalInstruments.DynamicProperties;
using NationalInstruments.DataTypes;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// This class is an implementation of ICustomVeriStandControl. By specifying it as an Export, we are identifying
    /// it as a plugin to VeriStand.
    /// The interface implementation defines how the control should appear in the palette.
    /// </summary>
    [Export(typeof(ICustomVeriStandControl))]
    public class WatchdogControlModelExporter : ICustomVeriStandControl
    {
        /// <summary>
        /// Mergescript which defines what to drop on the screen from the palette.  Can be used to set default values on the control
        /// </summary>
        public string Target =>
            "<pf:MergeScript xmlns:pf=\"http://www.ni.com/PlatformFramework\">" +
                "<pf:MergeItem>" +
                    "<UIWatchdog xmlns=\"http://www.edward-jones.co.uk/VeriStandExample\" Width=\"[float]52\" Height=\"[float]52\"/>" +
                "</pf:MergeItem>" +
            "</pf:MergeScript>";

        /// <summary>
        /// Name of the control as it will appear in the palette
        /// </summary>
        public string Name => "UI Watchdog";

        /// <summary>
        /// Path to the image to use in the palette
        /// </summary>
        public string ImagePath => "/NationalInstruments.VeriStand.CustomControls;component/Resources/watchdog_32x32.png";

        /// <summary>
        /// Tool tip to display in the palette
        /// </summary>
        public string ToolTip => "UI Watchdog Tool Tip";

        /// <summary>
        /// Unique id for the control. The only requirement is that this doesn't overlap with existing controls or other custom controls.
        /// This is used for serialization and the context help system.
        /// </summary>
        public string UniqueId => "UIWatchdog";

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
    public class WatchdogControlModel :
        VisualModel, INotifyPropertyChanged, 
#if MUTATE2020R4
        IDataEngineStateChangeObserver
#else
        ISubscribeProviderStatusUpdates
#endif
    {
        /// <summary>
        /// The name of the control which will be used in XML.  This name in the mergescript in the Target property of the ICustomVeriStandControl interface must match this
        /// </summary>
        private const string WatchdogName = "UIWatchdog";
        private UdpClient _udpClient;
        /// <summary>
        /// How this model will be represented in XML.
        /// </summary>
        public override XName XmlElementName => XName.Get(WatchdogName, PluginNamespaceSchema.ParsableNamespaceName);

        /// <summary>Creates a <see cref="KeySwitchControlModel"/>.</summary>
        /// <param name="info">An <see cref="IElementCreateInfo"/> instance.</param>
        /// <returns>A <see cref="KeySwitchControlModel"/>.</returns>
        [XmlParserFactoryMethod(WatchdogName, PluginNamespaceSchema.ParsableNamespaceName)]
        public static WatchdogControlModel CreateShifterControlModel(IElementCreateInfo info)
        {
            var model = new WatchdogControlModel();
#if MUTATE2020
            model.Initialize(info);
#else
            model.Init(info);
#endif

            return model;
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
                case "Target":
                    return typeof(string);
                case "Port":
                    return typeof(int);
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
                case "Target":
                    return "localhost";
                case "Port":
                    return 54324;
                default:
                    return base.DefaultValue(identifier);
            }
        }

        public static readonly PropertySymbol TargetSymbol =
            ExposePropertySymbol<WatchdogControlModel>("Target", (string)"localhost");

        public static readonly PropertySymbol PortSymbol =
            ExposePropertySymbol<WatchdogControlModel>("Port", (int)54324);

        /// <summary>
        /// Gets or sets the target address that the watchdog emits the packets to
        /// </summary>
        public string Target
        {
            get { return ImmediateValueOrDefault<string>(TargetSymbol); }
            set
            {
                SetOrReplaceImmediateValue(TargetSymbol, value);
            }
        }

        /// <summary>
        /// Gets or sets the port that the watchdog emits the packets to
        /// </summary>
        public int Port
        {
            get { return ImmediateValueOrDefault<int>(PortSymbol); }
            set
            {
                SetOrReplaceImmediateValue(PortSymbol, (int)value);
            }
        }

        /// <summary>
        /// Provide a xaml generation helper. This is used to help generate xaml for the properties on this control.
        /// </summary>
        public override XamlGenerationHelper XamlGenerationHelper
        {
            get { return new WatchdogControlXamlHelper(); }
        }

        /// <summary>
        /// Private class which helps with xaml generation for this model.  For most custom models this should just need to override the control type from the generic XamlGenerationHelper
        /// </summary>
        private class WatchdogControlXamlHelper : XamlGenerationHelper
        {
            public override Type ControlType => typeof(WatchdogControl);
        }

        private Thread _colorChangingThread;
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
                    _udpClient.Close();
                }

                // Stop the color changing thread
                _isDisposed = true;
                if (_colorChangingThread != null && _colorChangingThread.IsAlive)
                {
                    _colorChangingThread.Join();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public event EventHandler<string> BackgroundColorChanged;

        public void ChangeBackgroundColor()
        {
            // Perform any necessary logic
            OnBackgroundColorChanged();
        }

        protected virtual void OnBackgroundColorChanged()
        {
            BackgroundColorChanged?.Invoke(this, "");
        }

        private void ColorChangingThreadMethod()
        {
            while (!_isDisposed && _isThreadRunning)
            {
                lock (_lockObject)
                {
                    // Change the background color (example: alternating between red and blue)
                    ChangeBackgroundColor();
                }

                // Update UI on the UI thread
                //Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                // Transmit UDP packet
                TransmitUdpPacket(Target, Port, DateTime.Now.ToString());
                TryAlarms();
                // Sleep for 500ms
                Thread.Sleep(500);
            }
        }

        public async void TryAlarms()
        {
            try
            {
                //await faultservice = Host.GetRunTimeService<IAlarmService>();
            }
            catch (Exception ex)
            {

            }
        }

        private void TransmitUdpPacket(string ipAddress, int port, string data)
        {
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(data);
                _udpClient.Send(bytes, bytes.Length, ipAddress, port);
            }
            catch (Exception ex)
            {
                // Handle transmission error
                //Console.WriteLine("Error transmitting UDP packet: " + ex.Message);
            }
        }
        /// <summary>
        /// Called when VeriStand becomes disconnected from the gateway.
        /// </summary>
        /// <returns>awaitable task</returns>
        public Task OnDisconnectedAsync()
        {
            _isThreadRunning = false;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnStartAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnShutdownAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnConnectingAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnConnectedAsync()
        {
            // Initialize UDP client
            _isThreadRunning = true;
            _udpClient = new UdpClient();
            _colorChangingThread = new Thread(ColorChangingThreadMethod);
            _colorChangingThread.IsBackground = true;
            _colorChangingThread.Start();
            return Task.CompletedTask;
        }

        public Task OnDisconnectingAsync()
        {
            return Task.CompletedTask;
        }
    }
}
