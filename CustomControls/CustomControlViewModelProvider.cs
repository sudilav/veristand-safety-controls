using System.ComponentModel.Composition;
using NationalInstruments.Composition;
using NationalInstruments.Shell;
using NationalInstruments.VeriStand.CustomControls;
using NationalInstruments.VeriStand.Design.Screen;

namespace NationalInstruments.VeriStand.CustomControls
{
    /// <summary>
    /// Exports the view models (and associated models) supported by the ScreenSurface.
    /// </summary>
    [PartMetadata(ExportIdentifier.RootContainerKey, "")]
    [ExportProvideViewModels(typeof(ScreenEditControl), supportedModels: "NationalInstruments.VeriStand.CustomControls")]
    public class CustomControlViewModelProvider : ViewModelProvider
    {
        /// <inheritdoc />
        /// <remarks>
        /// This method should use AddSupportedModel to specify the relationship between all models and view models exported
        /// from this assembly.
        /// </remarks>
        protected override void AddSupportedModels()
        {
            AddSupportedModel<WatchdogControlModel>(e => new WatchdogControlViewModel(e));
            AddSupportedModel<AnyAlarmModel>(e => new AnyAlarmViewModel(e));
            AddSupportedModel<EnableNumericModel>(e => new EnableNumericViewModel(e));
            AddSupportedModel<ResetAlarmsModel>(e => new ResetAlarmsViewModel(e));
            AddSupportedModel<AlarmedPowerButtonModel>(e => new AlarmedPowerButtonViewModel(e));
        }
    }
}
