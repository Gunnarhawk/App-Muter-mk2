using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace App_Muter_mk2
{
    [XmlRoot("ApplicationRoot")]
    public class RootElement
    {
        [XmlElement("Settings")]
        public SettingsElement Settings { get; set; }
    }

    public class SettingsElement
    {
        [XmlElement("GrenadeBind")]
        public string GrenadeBind { get; set; }

        [XmlElement("AppMute")]
        public string AppMute { get; set; }

        [XmlElement("TargetVolume")]
        public float TargetVolume { get; set; }

        [XmlElement("UpMask")]
        public int UpMask { get; set; }

        [XmlElement("DownMask")]
        public int DownMask { get; set; }
    }

    // <GrenadeBind></GrenadeBind>
    // <AppMute></AppMute>

    // mouse bind ex: Mouse Button 4
    // key bind ex: A
    // app mute ex: FireFox.exe

    public class SettingsHandler
    {
        private string default_path = "settings.xml";

        public string bind = "";
        public string app = "";
        public float t_vol = 0.0f;
        public int u_mask = 0;
        public int d_mask = 0;

        public SettingsHandler()
        {
            // append the current application path
            default_path = AppDomain.CurrentDomain.BaseDirectory + default_path;

            // check if the file exists at the default path, create it if it does not exist
            CheckExists();

            // init values that were saved in the xml file
            (bind, app, t_vol, u_mask, d_mask) = ReadSettings();
        }

        public void CheckExists()
        {
            if (!File.Exists(default_path))
            {
                GenerateDefaultSettings();
            }
        }

        // called when program is first ran
        public (string, string, float, int, int) ReadSettings()
        {
            // file should already exist at this point, but i would rather not have it throw an error
            CheckExists();

            string gb = string.Empty, am = string.Empty;
            int um = 0, dm = 0;
            float tv = 0.0f;
            XmlSerializer s = new XmlSerializer(typeof(RootElement));
            using (FileStream fs = new FileStream(default_path, FileMode.Open))
            {
                RootElement root = (RootElement)s.Deserialize(fs);
                (gb, am, tv, um, dm) = (root.Settings.GrenadeBind, root.Settings.AppMute, root.Settings.TargetVolume, root.Settings.UpMask, root.Settings.DownMask);
            }

            return (gb, am, tv, um, dm);
        }

        private void GenerateDefaultSettings()
        {
            RootElement root = new RootElement
            {
                Settings = new SettingsElement
                {
                    GrenadeBind = "",
                    AppMute = "",
                    TargetVolume = 0.0f,
                    UpMask = 0,
                    DownMask = 0
                }
            };

            XmlSerializer s = new XmlSerializer(typeof(RootElement));
            using (FileStream fs = new FileStream(default_path, FileMode.Create))
            {
                s.Serialize(fs, root);
            }
        }

        public void UpdateSettings(string new_gb_value = "", string new_am_value = "", float new_target_volume = -1f, int new_up_mask = 0, int new_down_mask = 0, ApplicationHandler hApplication = null)
        {
            // file should already exist at this point, but i would rather not have it throw an error
            CheckExists();

            XDocument document = XDocument.Load(default_path);
            XElement settings = document.Root.Element("Settings");
            if (settings != null)
            {
                if (!string.IsNullOrWhiteSpace(new_gb_value))
                {
                    settings.Element("GrenadeBind").SetValue(new_gb_value);
                    bind = new_gb_value;
                }

                if (!string.IsNullOrWhiteSpace(new_am_value))
                {
                    settings.Element("AppMute").SetValue(new_am_value);
                    app = new_am_value;

                    if (hApplication != null)
                    {
                        hApplication.GetProcessID(new_am_value);
                        hApplication.GetApplicationVolume();
                    }
                }

                if (new_target_volume != -1f)
                {
                    settings.Element("TargetVolume").SetValue(new_target_volume);
                    t_vol = new_target_volume;

                    if (hApplication != null)
                    {
                        hApplication.target_volume = new_target_volume;
                    }
                }

                if (new_up_mask != 0)
                {
                    settings.Element("UpMask").SetValue(new_up_mask);
                    u_mask = new_up_mask;
                }

                if (new_down_mask != 0)
                {
                    settings.Element("DownMask").SetValue(new_down_mask);
                    d_mask = new_down_mask;
                }

                document.Save(default_path);
            }
        }
    }
}
