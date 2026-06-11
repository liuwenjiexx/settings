using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SettingsManagement.Tests
{
    public class VariantTest : SettingsTestsBase
    {
        private static Setting<string> variantString = new(Settings, nameof(VariantString), null, SettingsScope.EditorUser);

        public static string debugVariant => ExampleSettings.debugVariant;
        public static string demoVariant => ExampleSettings.demoVariant;
        public static string demoDebugVariant => ExampleSettings.demoDebugVariant;

        [SetUp]
        public void SetUp()
        {
            variantString.Delete(PlatformNames.Default, null);
            variantString.Delete(PlatformNames.Default, debugVariant);
            variantString.Delete(PlatformNames.Default, demoVariant);
            variantString.Delete(PlatformNames.Default, demoDebugVariant);

            Settings.SetVariant(null);
        }

        [TearDown]
        public void TearDown()
        {
            variantString.Delete();
            Settings.SetVariant(null);
        }

        [Test]
        public void DeleteVariant()
        {
            variantString.SetValue(PlatformNames.Default, null, "abc", true);
            variantString.SetValue(PlatformNames.Default, debugVariant, "abc.debug", true);

            variantString.Delete(PlatformNames.Default, debugVariant, true);

            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, null));
            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, debugVariant));
        }

        [Test]
        public void VariantString()
        {
            Assert.AreEqual(string.Empty, variantString.GetValue(PlatformNames.Default, null));
            Assert.AreEqual(string.Empty, variantString.GetValue(PlatformNames.Default, debugVariant));

            variantString.SetValue(PlatformNames.Default, null, "abc", true);
            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, null));
            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, debugVariant));

            variantString.Delete(PlatformNames.Default, null, true);
            Assert.AreEqual(string.Empty, variantString.GetValue(PlatformNames.Default, null));
            Assert.AreEqual(string.Empty, variantString.GetValue(PlatformNames.Default, debugVariant));

            Settings.SetVariant(debugVariant);

            variantString.SetValue(PlatformNames.Default, debugVariant, "abc.debug", true);
            Assert.AreEqual(string.Empty, variantString.GetValue(PlatformNames.Default, null));
            Assert.AreEqual("abc.debug", variantString.GetValue(PlatformNames.Default, debugVariant));

        }


        [Test]
        public void NoVariant()
        {
            variantString.SetValue(PlatformNames.Default, null, "abc", true);
            variantString.SetValue(PlatformNames.Default, debugVariant, "abc.debug", true);

            Settings.SetVariant(null);

            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, null));
            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, debugVariant));

            Assert.AreEqual("abc", variantString.Value);
        }

        [Test]
        public void Debug()
        {
            variantString.SetValue(PlatformNames.Default, null, "abc", true);
            variantString.SetValue(PlatformNames.Default, debugVariant, "abc.debug", true);
            variantString.SetValue(PlatformNames.Default, demoVariant, "abc.demo", true);
            variantString.SetValue(PlatformNames.Default, demoDebugVariant, "abc.demo_debug", true);

            Settings.SetVariant(debugVariant);

            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, null));
            Assert.AreEqual("abc.debug", variantString.GetValue(PlatformNames.Default, debugVariant));

            Assert.AreEqual("abc.debug", variantString.Value);
        }

        [Test]
        public void Demo()
        {
            Settings.SetVariant(demoVariant);

            variantString.SetValue(PlatformNames.Default, null, "abc", true);
            variantString.SetValue(PlatformNames.Default, debugVariant, "abc.debug", true);
            variantString.SetValue(PlatformNames.Default, demoVariant, "abc.demo", true);
            variantString.SetValue(PlatformNames.Default, demoDebugVariant, "abc.demo_debug", true);


            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, null));
            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, debugVariant));
            Assert.AreEqual("abc.demo", variantString.GetValue(PlatformNames.Default, demoVariant));
            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, demoDebugVariant));

            Assert.AreEqual("abc.demo", variantString.Value);
        }

        [Test]
        public void DemoDebug()
        {
            Settings.SetVariant(demoDebugVariant);

            variantString.SetValue(PlatformNames.Default, null, "abc");
            variantString.SetValue(PlatformNames.Default, debugVariant, "abc.debug");
            variantString.SetValue(PlatformNames.Default, demoVariant, "abc.demo");
            variantString.SetValue(PlatformNames.Default, demoDebugVariant, "abc.demo_debug");

            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, null));
            Assert.AreEqual("abc", variantString.GetValue(PlatformNames.Default, debugVariant));
            Assert.AreEqual("abc.demo", variantString.GetValue(PlatformNames.Default, demoVariant));
            Assert.AreEqual("abc.demo_debug", variantString.GetValue(PlatformNames.Default, demoDebugVariant));


            Assert.AreEqual("abc.demo_debug", variantString.Value);
        }

        [Test]
        public void NoDemoDebug()
        {
            Settings.SetVariant(demoDebugVariant);

            variantString.SetValue(PlatformNames.Default, null, "abc");
            variantString.SetValue(PlatformNames.Default, demoVariant, "abc.demo");

            Assert.AreEqual("abc.demo", variantString.Value);
        }

        [Test]
        public void NoDemo()
        {
            Settings.SetVariant(demoDebugVariant);

            variantString.SetValue(PlatformNames.Default, null, "abc");

            Assert.AreEqual("abc", variantString.Value);
        }
    }
}