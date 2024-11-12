using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NationalInstruments.VeriStand.CustomControls
{
    public class AddrTextBoxFactory : TextBoxFactory
    {
        private char[] _charactersToPrevent;
        private IGatewayModel _model;
        public AddrTextBoxFactory(IGatewayModel model)
        {
            _model = model;
            _charactersToPrevent = new char[] // Characters that cannot exist in a hostname or IP Address
            {
                    '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '+', '=', '{', '}', '[', ']', '|', '\\', ';', '"', '\'', '<', '>', ',', '?', '/', ' ', '`', '~'
            };
        }
        public override void Attach(ICommandEx command, PlatformVisual visual, ICommandAttachContext context)
        {
            ConfigurationPaneLabelContainer configurationPaneLabelContainer = visual.AsType<ConfigurationPaneLabelContainer>();
            base.Attach(command, visual, context);
            ShellTextBox control = (ShellTextBox)GetCommandSourceForVisual(visual);
            control.TextChanged += OnTextChanged;
        }
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            ShellTextBox shellTextBox = (ShellTextBox)sender;
            string text = ((TextBox)(object)shellTextBox).Text;
            if (string.IsNullOrEmpty(text) || text.IndexOfAny(_charactersToPrevent) > 0)
            {
                return;
            }
            // Force the set method of the Symbol
            _model.Gateway = text;
        }
    }
}
