# VeriStand Safety Controls

This repository is the result of efforts made in developing safer controls at UKAEA for a specific project. I have been granted the privilege of making this available to the public on the basis that we do not actively maintain the code, it does not include any UKAEA IP and there is no expectation upon UKAEA to continue support for this code. It is provided as is, as a benefit to the external VeriStand Community to use or simply see that creating your own VeriStand Controls is indeed possible.

This repo contains new controls that configures and extends the VeriStand Editor. These new controls are:
* Any Alarm: Displays a bounded box, red if any alarm is active and white if none.
* Enabled Power Button: The Power button but only accessible if no alarm is active.
* Enabled Numeric Control: This is a numeric control which behaves as an indicator if a selected channel is zero.
* Watchdog: This is a useful watchdog which sends a UDP Message to a selected IP Address - this is useful in tandem with the Instrument Control and an alarm on the Instrument Controls error code to create watchdog behaviour on a target.
* Reset Alarms: This button resets all alarms in a system simultaneously through the VeriStand Editor.

## Getting Started

### Dependencies

#### To Install:

* VeriStand

#### To Build:

* .NET - .NET 4.6.2 installed to your local machine. VeriStand is built against this version.
* Compiler - Any C# editor and compiler that supports .NET 4.6.2. These examples were created with Visual Studio 2015.
Note: some Visual Studio options can cause the app to crash. If using Visual Studio, it is recommended to turn off options for:
    * Enable XAML Hot Reload
    * Enable UI Debugging Tools for XAML (older version of the above option)

### Using This Repository

#### Installing

You can install this repository of code by building the project into it's DLL and copying from the Build location or taking a release DLL uploaded here and placing this into your VeriStand Custom Controls Directory:

C:\Users\Public\Documents\National Instruments\NI VeriStand 2024\Custom UI Manager Controls\

#### User Documentation

A minimal documentation is [provided here](https://sudilav.github.io/veristand-safety-controls/).

** Note: This repository is probably best used with the minimized feature set demonstrated in the [VeriStand Editor Plugin Example](https://github.com/ni/veristand-editor-plugin-examples) to limit your operators from acting on the system with impunity and is used with this repository in real life practice.**

### Known Issues:

Unfortunately due to the early nature of this feature of extensibility in VeriStand some features do not work as wanted and discussion with National Instruments have shown that this is currently a bug/feature of VeriStand, these are namely:

* Documentation: Context Help loads Documentation on Controls from a complex assortment inside the VeriStand Installation Directory and makes it cumbersome to inject our own documentation here.
* Multiple Mapped Sources and Path: When using a control with multiple source paths set, the second path doesn't display as text in the configuration pane.
* Control/Indicator right click menu: It is impossible to remove the convert to control/indicator button on right click of a control in the Editor.

### Architecture
For more information on the code provided, refer to [VeriStand Editor Plugin Example](https://github.com/ni/veristand-editor-plugin-examples).

## Support

This code is provided as is.

### [Contributing](CONTRIBUTING.md)

### [License](LICENSE)
