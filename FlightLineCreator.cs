using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Nyoko.FlightLineCreator.Extensors;
using UnityEngine;
using System.Drawing;
using static Mono.Security.X509.X520;

namespace Nyoko.FlightLineCreator
{
    public class FlightLineCreatorMod : LoadingExtensionBase, IUserMod
    {
        private static bool _localizationInitialized;
        private GameObject _planeGameObject;

        public string Name => "Flight Line Creator";
        public string Description => "Provides the ability to create airplane lines in the city.";

        public static string Version => $"{majorVersion}.{typeof(FlightLineCreatorMod).Assembly.GetName().Version.Build} r{typeof(FlightLineCreatorMod).Assembly.GetName().Version.Revision}";
        public static string majorVersion => $"{typeof(FlightLineCreatorMod).Assembly.GetName().Version.Major}.{typeof(FlightLineCreatorMod).Assembly.GetName().Version.Minor}";
        private UITextureAtlas m_atlas;
        public override void OnLevelLoaded(LoadMode mode)
        {
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame) return;

            InstallLocalization();
            _planeGameObject = CreateTransportLineGo("Airplane", "PublicTransportPlane");
        }

        public override void OnLevelUnloading()
        {
            if (_planeGameObject != null)
            {
                GameObject.Destroy(_planeGameObject);
            }
        }

        private static T GetPrivate<T>(object o, string fieldName)
        {
            var fields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field = null;

            foreach (var f in fields)
            {
                if (f.Name == fieldName)
                {
                    field = f;
                    break;
                }
            }

            return (T)field.GetValue(o);
        }

        private void InstallLocalization()
        {
            if (_localizationInitialized) return;

            Redirector.doLog("Updating Localization.");

            try
            {
                var locale = GetPrivate<Locale>(LocaleManager.instance, "m_Locale");
                Locale.Key k;

                // Airplane locale
                k = new Locale.Key()
                {
                    m_Identifier = "TRANSPORT_LINE",
                    m_Key = "Airplane"
                };
                locale.AddLocalizedString(k, "Plane");

                k = new Locale.Key()
                {
                    m_Identifier = "Airplane Line",
                    m_Key = "Airplane"
                };
                locale.AddLocalizedString(k, "Air Line {0}");

                k = new Locale.Key()
                {
                    m_Identifier = "Flight Line",
                    m_Key = "Airplane"
                };
                locale.AddLocalizedString(k, "Air Line Tool");

                k = new Locale.Key()
                {
                    m_Identifier = "Draw air lines by clicking on airports. Draw the line from the departure airport to the terminal airport and back as an air line needs to be circular.",
                    m_Key = "Airplane"
                };
                locale.AddLocalizedString(k, "Draw air lines by clicking on airports. Draw the line from the departure airport to the terminal airport and back as an air line needs to be circular.");

                _localizationInitialized = true;
                Redirector.doLog("Localization successfully updated.");
            }
            catch (ArgumentException e)
            {
                Redirector.doLog($"Unexpected {e.GetType().Name} updating localization: {e.Message}{Environment.NewLine}{e.StackTrace}{Environment.NewLine}");
            }
        }

        private GameObject CreateTransportLineGo(string transportInfoName, string category)
        {
            GameObject result = null;
            try
            {
                var busTransportInfo = PrefabCollection<TransportInfo>.FindLoaded("Metro");

                var planeLinePrefab = PrefabCollection<TransportInfo>.FindLoaded(transportInfoName);
                planeLinePrefab.m_lineMaterial2 = GameObject.Instantiate(busTransportInfo.m_lineMaterial2);
                planeLinePrefab.m_lineMaterial2.shader = planeLinePrefab.m_pathMaterial2.shader;
                planeLinePrefab.m_lineMaterial = GameObject.Instantiate(busTransportInfo.m_lineMaterial);
                planeLinePrefab.m_lineMaterial.shader = planeLinePrefab.m_pathMaterial.shader;
                planeLinePrefab.m_prefabDataLayer = 0;

                // Workaround for button/panel bug when you return to main menu and then load a map again.
                Transform scrollPanel = null;
                PublicTransportPanel transportPanel = null;
                var items = GameObject.FindObjectsOfType<PublicTransportPanel>();
                foreach (var item in items)
                {
                    if (item.category == category)
                    {
                        scrollPanel = item.transform.Find("ScrollablePanel");
                        transportPanel = item;
                        break;
                    }
                }


                NetInfo netInfo = PrefabCollection<NetInfo>.FindLoaded("Metro");
                // This creates the button and adds the functionality.
                netInfo.m_availableIn = ItemClass.Availability.All;
                netInfo.m_placementStyle = ItemClass.Placement.Manual;
                typeof(NetInfo).GetField("m_UICategory", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(netInfo, "PublicTransportPlane");
                string name = "Metro";
                string desc = "Create flight line";
                string prefixIcon = ".png";
                netInfo.m_Atlas = m_atlas;
                netInfo.m_Thumbnail = prefixIcon;

                // Adding missing locale
                // Get the Locale instance
                Locale locale = (Locale)typeof(LocaleManager).GetField("m_Locale", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SingletonLite<LocaleManager>.instance);

                // Add localized strings for the network title and description
                Locale.Key titleKey = new Locale.Key() { m_Identifier = "NET_TITLE", m_Key = name };
                if (!locale.Exists(titleKey))
                    locale.AddLocalizedString(titleKey, name);

                Locale.Key descKey = new Locale.Key() { m_Identifier = "NET_DESC", m_Key = name };
                if (!locale.Exists(descKey))
                    locale.AddLocalizedString(descKey, desc);

                // Get the GeneratedScrollPanel instance and invoke the CreateAssetItem method
                var generatedScrollPanelField = this.GetType().GetField("panel", BindingFlags.NonPublic | BindingFlags.Instance);
                var generatedScrollPanel = generatedScrollPanelField.GetValue(this);
                var createAssetItemMethod = generatedScrollPanel.GetType().GetMethod("CreateAssetItem", BindingFlags.NonPublic | BindingFlags.Instance);
                createAssetItemMethod.Invoke(generatedScrollPanel, new object[] { netInfo });
                Bitmap customIcon = new Bitmap("path/to/custom/icon.png");

                // add the custom icon as a sprite to an existing UITextureAtlas objec

                // assign the new UITextureAtlas to the netInfo object
                netInfo.m_Atlas = m_atlas;
                netInfo.m_Thumbnail = prefixIcon;







                // Find the newly created button and assign it to the return value so we can destroy it on level unload.
                result = scrollPanel.Find(transportInfoName).gameObject;

                Redirector.doLog(transportInfoName + " line button successfully created.");
            }
            catch (Exception e)
            {
                Redirector.doLog("Couldn't create " + transportInfoName + " line button. " + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine);
            }
            return result;
        }
    }
}
