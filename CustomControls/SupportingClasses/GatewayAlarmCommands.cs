using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NationalInstruments.VeriStand.CustomControls
{
    public static class GatewayAlarmCommands
    {
        public static readonly ICommandEx GatewayConfigurationGroupCommand = new ShellRelayCommand
        {
            UniqueId = "NI.GatewayAlarmCommands:GatewayAlarmConfigGroupCommand",
            LabelTitle = "Gateway Alarm Configuration",
            Weight = 0.1
        };
        public static readonly ICommandEx GatewayCommand = new PropertySymbolCommand(AnyAlarmModel.GatewaySymbol, UIModelPropertySymbolDataModel.GetCreator())
        {
            LabelTitle = "Gateway Address",
            UniqueId = "NI.GatewayAlarmCommands:GatewayCommand"
        };
    }
}
