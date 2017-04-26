namespace CustomBlips
{
    using System;
    using System.Reflection;
    using System.Drawing;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using System.Linq;
    using Rage;
    using Rage.Attributes;

    public static class EntryPoint
    {
        private static Blip lastBlip;
        private static string folder = "plugins/customBlips/";
        private static string ini = $"{folder}customBlips.ini";
        private static string file;
        private static InitializationFile cfg;
#if DEBUG
        [ConsoleCommand]
        private static void Command_Log(string text)
        {
            Game.Console.Print(text);
        }
        [ConsoleCommand]
        private static void Command_Notification(string text)
        {
            Game.DisplayNotification(text);
        }
        [ConsoleCommand]
        private static void Command_HelpNotification(string text)
        {
            Game.DisplayHelp(text);
        }
        [ConsoleCommand]
        private static void Command_Subtitle(string text)
        {
            Game.DisplaySubtitle(text);
        }
#endif
        [ConsoleCommand]
        private static void Command_RevealMap()
        {
            Game.IsFullMapRevealForced = true;
        }
        [ConsoleCommand]
        private static void Command_AddBlip(
            string Name = null, string color = null,
            int Alpha = 0, string RouteColor = null,
            int Number = 0, int Order = 0,
            float Scale = 0, string Sprite = null,
            string IsRouteEnabled = null, string IsFriendly = null,
            int flashInterval = 0, int flashDuration = 0,
            int X = 0, int Y = 0, int Z = 0
        )
        {
            try
            {
                if (X != 0 || Y != 0 || Z != 0)
                    lastBlip = new Blip(new Vector3(X, Y, Z));
                else
                    lastBlip = new Blip(Game.LocalPlayer.Character.Position);
                if (Name != null) { lastBlip.Name = Name; }
                if (color != null) { lastBlip.Color = Color.FromName(color); }
                if (Alpha != 0) { lastBlip.Alpha = Alpha; }
                if (RouteColor != null) { lastBlip.RouteColor = Color.FromName(color); }
                if (Number != 0) { lastBlip.NumberLabel = Number; }
                if (Order != 0) { lastBlip.Order = Order; }
                if (Scale != 0) { lastBlip.Scale = Scale; }
                if (Sprite != null) { lastBlip.Sprite = (BlipSprite)Enum.Parse(typeof(BlipSprite), Sprite); }
                if (IsRouteEnabled != null) { lastBlip.IsRouteEnabled = Convert.ToBoolean(IsRouteEnabled); }
                if (IsFriendly != null) { lastBlip.IsFriendly = Convert.ToBoolean(IsFriendly); }
                if (flashInterval != 0 && flashDuration != 0) { lastBlip.Flash(flashInterval, flashDuration); }
                Log($"Added Blip {Name}");
            }
            catch { Log($"Error in AddBlip Command"); }
        }

        [ConsoleCommand]
        private static void Command_SaveBlip()
        {
            (new FileInfo(file)).Directory.Create();
            if (!File.Exists(file))
            {
                Log($"File {file} doesn't exist, creating new one.");
                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                    xmlWriterSettings.Indent = true;
                    xmlWriterSettings.NewLineOnAttributes = true;
                    using (XmlWriter xmlWriter = XmlWriter.Create(file, xmlWriterSettings))
                    {
                        xmlWriter.WriteStartDocument();
                        xmlWriter.WriteStartElement("CustomBlips");
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndDocument();
                        xmlWriter.Close();
                    }
            }
            XDocument xDocument = XDocument.Load(file);
            XElement root = xDocument.Element("CustomBlips");
            root.Add(BlipToXml(lastBlip));
            xDocument.Save(file);
            Log($"Added Blip to {file}");
        }

        private static void Log(string message)
        {
            Game.Console.Print($"[{DateTime.Now.ToString()}] Custom Blips: {message}");
        }
        private static XElement BlipToXml(Blip blip)
        {
            //var color = new XElement("Color",
            //    new XAttribute("alpha", blip.Color.A),
            //    new XAttribute("red", blip.Color.R),
            //    new XAttribute("green", blip.Color.G),
            //    new XAttribute("blue", blip.Color.B)
            //);
            //var coords = new XElement("Coordinates",
            //    new XAttribute("X", blip.Position.X),
            //    new XAttribute("Y", blip.Position.Y),
            //    new XAttribute("Z", blip.Position.Z),
            //);
            var xml = new XElement("Blip",
               new XAttribute("X", blip.Position.X),
               new XAttribute("Y", blip.Position.Y),
               new XAttribute("Z", blip.Position.Z));
            if (blip.Name != null) { xml.SetValue(blip.Name); }
            if (GetColorName(blip.Color) != "") { xml.Add(new XAttribute("color", GetColorName(blip.Color))); }
            if (blip.Alpha != 1) { xml.Add(new XAttribute("alpha", blip.Alpha)); }
            if (blip.Sprite != BlipSprite.Destination2) { xml.Add(new XAttribute("sprite", blip.Sprite)); }
            //xml.Add(color);xml.Add(coords);
            return xml;
        }

        private static Blip XmlToBlip(XElement xml)
        {
            var x = float.Parse(xml.Attribute("X").Value);
            var y = float.Parse(xml.Attribute("Y").Value);
            var z = float.Parse(xml.Attribute("Z").Value);
            var coords = new Vector3(x, y, z);
            var tmp = new Blip(coords);
            if (xml.Attribute("color") != null)
                tmp.Color = Color.FromName(xml.Attribute("color").Value);
            if (xml.Attribute("alpha") != null)
                tmp.Alpha = float.Parse(xml.Attribute("alpha").Value);
            if (xml.Attribute("sprite") != null)
                tmp.Sprite = (BlipSprite)Enum.Parse(typeof(BlipSprite), xml.Attribute("sprite").Value);
            if (xml.Value != "")
                tmp.Name = xml.Value;
            return tmp;
        }
        private static String GetColorName(Color color)
        {
            var predefined = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static);
            var match = (from p in predefined where ((Color)p.GetValue(null, null)).ToArgb() == color.ToArgb() select (Color)p.GetValue(null, null));
            if (match.Any())
                return match.First().Name;
            return String.Empty;
        }

        public static void Main()
        {
            Log("Loading...");
            Game.TerminateAllScripts("selector");
            (new FileInfo(ini)).Directory.Create();
            cfg = new InitializationFile(ini);
            if (!File.Exists(ini))
            {
                Log($"File {ini} doesn't exist, creating new one.");
                cfg.Write("General", "debug", "false");
                cfg.Write("General", "addBlipsOnStartup", "true");
                cfg.Write("General", "revealMapOnStartup", "false");
                cfg.Write("General", "customBlipsXML", $"{folder}customBlips.xml");
            } else {
            if (!cfg.DoesKeyExist("General", "debug"))
                cfg.Write("General", "debug", "false");
            if (!cfg.DoesKeyExist("General", "addBlipsOnStartup"))
                cfg.Write("General", "addBlipsOnStartup", "true");
            if (!cfg.DoesKeyExist("General", "revealMapOnStartup"))
                cfg.Write("General", "revealMapOnStartup", "false");
            if (cfg.ReadString("General", "customBlipsXML", "") == "")
                cfg.Write("General", "customBlipsXML", $"{folder}customBlips.xml");
            }
            if (cfg.ReadBoolean("General", "revealMapOnStartup", false))
                Log("revealMapOnStartup set, revealing map...");
                Game.IsFullMapRevealForced = true;
            file = cfg.ReadString("General", "customBlipsXML");
            if (bool.Parse(cfg.ReadString("General", "addBlipsOnStartUP")))
            {
                DirectoryInfo d = new DirectoryInfo(folder);
                foreach (var file in d.GetFiles("*.xml"))
                {
                    try
                    {
                        XDocument xDocument = XDocument.Load(file.FullName);
                        XElement root = xDocument.Element("CustomBlips");
                        foreach (XElement el in root.Descendants()) {
                            var blip = XmlToBlip(el);
                            Log($"Added new Blip {blip.Name} from {file.Name} at {blip.Position.X},{blip.Position.Y},{blip.Position.Z}");
                        }
                        Log($"Added all Blips from {file.Name}");
                    }
                    catch
                    {
                        Log($"Cannot load {file.FullName}\nMake sure it exists and check it's validaty with http://codebeautify.org/xmlvalidator");
                    }
                }
            }
            Log("Loaded successfully.");
            GameFiber.Hibernate();
        }
        
        public static void Shutdown()
        {
            Log("Plugin unloaded. If you want to remove the blips created by this plugin use http://www.lcpdfr.com/files/file/10075-clean-all-the-things-blip-cleanup-delete-or-dismiss-vehicles-and-peds-vehicle-repair/");
        }
    }
}