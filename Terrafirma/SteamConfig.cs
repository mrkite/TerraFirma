using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Terrafirma
{
    class SteamConfig
    {
        class Element
        {
            public Dictionary<string, Element> Children { get; set; }
            public string Name { get; set; }
            public string Value;

            private static Regex keyRegex = new Regex("\"[^\"]*\"");

            public Element(Queue<string> lines)
            {
                Children = new Dictionary<string, Element>();
                string line = lines.Dequeue();

                MatchCollection matches = keyRegex.Matches(line);
                if (matches.Count == 0) //corrupt
                    return;
                Name = matches[0].Value.Trim('\"');
                if (matches.Count > 1) //value is a string
                    Value = matches[1].Value.Trim('\"').Replace(@"\\","\\");
                line = lines.Peek();
                if (line.Contains('{'))
                {
                    lines.Dequeue();
                    while (true)
                    {
                        line = lines.Peek();
                        if (line.Contains('}'))
                        {
                            lines.Dequeue();
                            return;
                        }
                        Element e = new Element(lines);
                        Children.Add(e.Name, e);
                    }
                }
            }
            public string Find(string path)
            {
                string[] kv = path.Split(new char[] { '/' }, 2);
                if (kv.Length == 1)
                    return Children[kv[0]].Value;
                return Children[kv[0]].Find(kv[1]);
            }
        }
        Element root = null;
        public SteamConfig()
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\\Valve\\Steam");
            if (key != null)
            {
                string path = key.GetValue("SteamPath") as string;
                path = Path.Combine(path, "config");
                path = Path.Combine(path, "config.vdf");
                if (File.Exists(path))
                    parse(path);
            }
        }
        public bool Ready { get { return root != null; } }

        public string Get(string path)
        {
            return root.Find(path);
        }

        private void parse(string path)
        {
            TextReader read = new StreamReader(path);
            Queue<string> lines = new Queue<string>();
            String line = read.ReadLine();
            while (line != null)
            {
                lines.Enqueue(line);
                line = read.ReadLine();
            }
            root = new Element(lines);
        }

    }
}
