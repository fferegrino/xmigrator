using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Xmigrator
{
    class Program
    {

        static bool verboose = true;
        static bool errors = true;
        static void Main(string[] args)
        {
			if (args.Length < 2)
			{
				Console.WriteLine("Usage:\nXmigrator [source] [destination]");
				return;
			}

			string source = args[0]; // @"C:\Users\anton\Documents\GitHub\MyIssues\Droid\Resources";
			string target = args[1]; // @"C:\Users\anton\Documents\GitHub\questacion\code\myissuesmirror\app\src\main\res";

			var sourceDir = new DirectoryInfo(source);
			var targetDir = new DirectoryInfo(target);

			Console.WriteLine($"Source: {sourceDir.Name}");
			Console.WriteLine($"Target: {targetDir.Name}");

			var xamarinToAndroid = sourceDir.Name.EndsWith("Resources",StringComparison.Ordinal);

            var directories = Directory.GetDirectories(source);
            foreach (var directory in directories)
            {
				ProcessDirectory(directory, target, xamarinToAndroid);
            }
            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        static void ProcessDirectory(string source, string target, bool xamarinToAndroid)
        {
			var directoryName = source.Split(new char []{ '\\', '/'}).Last();
            var realTarget = Path.Combine(target, directoryName);
			Console.WriteLine("Realtarger: " + realTarget);

            if (verboose)
                Console.WriteLine($"Processing {directoryName}:");
            // Clean target:
            if (Directory.Exists(realTarget))
                Directory.Delete(realTarget, true);
            Directory.CreateDirectory(realTarget);

            var files = Directory.GetFiles(source);
            foreach (var file in files)
            {
				ProcessFile(file, realTarget, xamarinToAndroid);
            }
        }


        static List<String> AttributesToLookFor = new List<string>
        {
            "id",
            "src",
            "textCursorDrawable",
            "background",
            "layout_toRightOf",
            "layout_toLeftOf",
            "layout_toStartOf",
            "layout_toEndOf",
            "layout_below",
            "layout_anchor",
            "layout"
    };



        static HashSet<string> SkippableAttributes = new HashSet<string> {
            "@android", "@color", "?attr" };
        static void ProcessFile(string source, string target, bool xamarinToAndroid)
        {
            var stringFileName = Path.GetFileName(source);
            var realTarget = Path.Combine(target, xamarinToAndroid ?
                GetAndroidName(stringFileName) :
                GetXamarinName(stringFileName));

            if (verboose)
                Console.WriteLine($"Processing {stringFileName}:");

            var isXml = Path.GetExtension(stringFileName).Equals(".xml") || Path.GetExtension(stringFileName).Equals(".axml");

            if (isXml)
            {
                try
                {
                    XDocument doc = XDocument.Load(source);
                    var namespaces = doc.Root.Attributes().
                        Where(a => a.IsNamespaceDeclaration).
                        GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName,
                                a => XNamespace.Get(a.Value)).
                        ToDictionary(g => g.Key,
                     g => g.First());

                    Console.WriteLine($"In {stringFileName}");
                    foreach (var node in doc.Descendants())
                    {
                        foreach (var attr in AttributesToLookFor)
                        {
                            var atributo = node.Attribute(attr);
                            if (atributo != null)
                            {
                                atributo.Value = xamarinToAndroid ?
                ProcessXamarinToAndroidAttributeValue(atributo.Value) :
                ProcessAndroidToXamarinAttributeValue(atributo.Value);
                            }
                        }

                        XNamespace androidNamespace;
                        if (namespaces.TryGetValue("android", out androidNamespace))
                        {
                            foreach (var attr in AttributesToLookFor)
                            {
                                var atributo = node.Attribute(androidNamespace + attr);
                                if (atributo != null)
                                {
                                    atributo.Value = xamarinToAndroid ?
                ProcessXamarinToAndroidAttributeValue(atributo.Value) :
                ProcessAndroidToXamarinAttributeValue(atributo.Value);
                                }
                            }

                        }

                        XNamespace appNamespace;
                        if (namespaces.TryGetValue("app", out appNamespace))
                        {
                            foreach (var attr in AttributesToLookFor)
                            {
                                var atributo = node.Attribute(appNamespace + attr);
                                if (atributo != null)
                                {
                                    atributo.Value = xamarinToAndroid ?
                ProcessXamarinToAndroidAttributeValue(atributo.Value) :
                ProcessAndroidToXamarinAttributeValue(atributo.Value);
                                }
                            }

                        }
                    }

					using (var xtw = XmlWriter.Create(Path.ChangeExtension(realTarget, "xml"), new XmlWriterSettings()
					{
						Encoding = Encoding.UTF8,
						NewLineOnAttributes = true,
						Indent = true
					}))
					{
						doc.WriteTo(xtw);
					}
                }
                catch (Exception e)
                {
                    if (errors)
                        Console.Error.WriteLine($"In {stringFileName}: " + e.Message);
                }
            }
            else
            {
                CopyAsIs(source, realTarget);
            }
        }

        static void CopyAsIs(string source, string target)
        {
            if (verboose)
                Console.WriteLine($"Copying {source}");
            File.Copy(source, target, true);
        }

        static string GetAndroidName(string fileName)
        {
            var onlyName = Path.GetFileName(fileName);

            return XamarinToAndroidName(onlyName);

        }

        static string GetXamarinName(string fileName)
        {
            var onlyName = Path.GetFileName(fileName);

            return AndroidToXamarinName(onlyName);

        }


        static string ProcessXamarinToAndroidAttributeValue(string value)
        {
            var start = value.IndexOf('/') + 1;
            if (start > 0)
            {
                return value.Substring(0, start) + XamarinToAndroidName(value.Substring(start));
            }
            return value;
        }

        static string ProcessAndroidToXamarinAttributeValue(string value)
        {
            var start = value.IndexOf('/') + 1;

            if (start > 0)
            {
                var prefix = value.Substring(0, start - 1).Split(':')[0];
                bool skipAttribute = SkippableAttributes.Contains(prefix);
                if (!skipAttribute)
                    return value.Substring(0, start) + AndroidToXamarinName(value.Substring(start));
            }
            return value;
        }

        static string AndroidToXamarinName(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Char.ToUpper(name[0]));
            for (int i = 1; i < name.Length; i++)
            {
                var c = name[i];
                if (c == '_')
                {
                    i++;
                    c = name[i];
                    sb.Append(Char.ToUpper(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        static string XamarinToAndroidName(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Char.ToLower(name[0]));
            for (int i = 1; i < name.Length; i++)
            {
                var c = name[i];
                if (Char.IsUpper(c))
                {
                    sb.Append('_').Append(Char.ToLower(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
