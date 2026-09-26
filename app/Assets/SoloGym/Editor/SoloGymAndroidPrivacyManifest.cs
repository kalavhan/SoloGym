using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace SoloGym.Editor
{
    /// <summary>Apply privacy policy in the highest-priority app manifest, after library generation.</summary>
    public sealed class SoloGymAndroidPrivacyManifest : IPostGenerateGradleAndroidProject
    {
        const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        const string ToolsNamespace = "http://schemas.android.com/tools";
        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            string gradleRoot = Directory.GetParent(unityLibraryPath).FullName;
            string manifestPath = Path.Combine(gradleRoot, "launcher/src/main/AndroidManifest.xml");
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("SoloGym requires the generated launcher manifest to apply its permission policy.", manifestPath);

            var document = new XmlDocument { PreserveWhitespace = true };
            document.Load(manifestPath);
            XmlElement manifest = document.DocumentElement;
            manifest.SetAttribute("xmlns:tools", ToolsNamespace);

            // These arrive from Auth's transitive Analytics libraries, which SoloGym does not use.
            foreach (string permission in new[]
            {
                "com.google.android.gms.permission.AD_ID",
                "android.permission.ACCESS_ADSERVICES_ATTRIBUTION",
                "android.permission.ACCESS_ADSERVICES_AD_ID",
                "com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE"
            })
            {
                XmlElement element = FindNamedElement(manifest, "uses-permission", permission);
                if (element == null)
                {
                    element = document.CreateElement("uses-permission");
                    manifest.AppendChild(element);
                    SetAttribute(element, "android", "name", AndroidNamespace, permission);
                }
                SetAttribute(element, "tools", "node", ToolsNamespace, "remove");
            }

            var application = (XmlElement)manifest.SelectSingleNode("application");
            SetMetadata(application, "firebase_analytics_collection_deactivated", "true");
            SetMetadata(application, "firebase_data_collection_default_enabled", "false");
            document.Save(manifestPath);
        }

        static void SetMetadata(XmlElement application, string name, string value)
        {
            XmlElement element = FindNamedElement(application, "meta-data", name);
            if (element == null)
            {
                element = application.OwnerDocument.CreateElement("meta-data");
                application.AppendChild(element);
                SetAttribute(element, "android", "name", AndroidNamespace, name);
            }
            SetAttribute(element, "android", "value", AndroidNamespace, value);
            SetAttribute(element, "tools", "replace", ToolsNamespace, "android:value");
        }

        static XmlElement FindNamedElement(XmlElement parent, string tag, string name)
        {
            foreach (XmlNode child in parent.ChildNodes)
                if (child is XmlElement element && element.Name == tag
                    && element.GetAttribute("name", AndroidNamespace) == name) return element;
            return null;
        }

        static void SetAttribute(XmlElement element, string prefix, string name, string ns, string value)
        {
            XmlAttribute attribute = element.OwnerDocument.CreateAttribute(prefix, name, ns);
            attribute.Value = value;
            element.Attributes.SetNamedItem(attribute);
        }
    }
}
