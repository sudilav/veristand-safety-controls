using NationalInstruments.VeriStand.CustomControls;
using System.Reflection;
using NationalInstruments.SourceModel;
using System.Xml.Linq;
using NationalInstruments.Search;
using Castle.Core.Internal;

namespace UIControlsTest
{
    [TestClass]
    public class AnyAlarmTests
    {
        [TestMethod]
        public void AttributesAnyAlarmTest()
        {
            // Test the Exporter attributes
            var exporter = new AnyAlarmModelExporter();
            Assert.AreEqual("Any Alarm", exporter.Name);
            Assert.AreEqual("/NationalInstruments.VeriStand.CustomControls;component/Resources/any_alarm_32x32.png", exporter.ImagePath);
            Assert.AreEqual("Any Alarm Tool Tip", exporter.ToolTip);
            Assert.AreEqual("AnyAlarm", exporter.UniqueId);

            // Test the model attribute
            var model = new AnyAlarmModel();
            var privateObject = new PrivateObject(model);
            XName name = (XName)privateObject.GetValue("XmlElementName");
            Assert.AreEqual("AnyAlarm", name.LocalName);
        }
        [TestMethod]
        public void checkDocumentation()
        {
            var model = new AnyAlarmModel();
            var privateObject = new PrivateObject(model);
            // Access private field
            IDocumentation privateFieldValue = (IDocumentation)privateObject.Invoke("CreateDocumentation");
            Assert.AreEqual("Any Alarm Notifier", privateFieldValue.Name);
        }

        [TestMethod]
        public void SettingRed_ShouldRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new AnyAlarmModel();
            bool propertyChangedRaised = false;

            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(AnyAlarmModel.IsRed))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.IsRed = true;

            // Assert
            Assert.IsTrue(propertyChangedRaised);
        }
    }
}