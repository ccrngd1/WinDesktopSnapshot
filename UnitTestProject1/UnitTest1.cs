using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DesktopSnapshot;
using Newtonsoft.Json;

namespace UnitTestProject1
{
	[TestClass]
	public class UnitTest1
	{
		private KnownApps a = new KnownApps();
		private string testFileName;

		public UnitTest1()
		{
			testFileName = "testKnownApps.json";

			a.Add(new BaseApp("test1Class", "random caption", "appA"));
			a.Add(new BaseApp("test2Class", "another caption","appB"));
		}

		[TestMethod]
		public void SimpleJsonSerializationTest()
		{
			var knownAppslist = new List<BaseApp>();

			knownAppslist.Add(new BaseApp("test1Class", "random caption", "appA"));
			knownAppslist.Add(new BaseApp("test2Class", "another caption", "appB"));

			string json = JsonConvert.SerializeObject(knownAppslist);

			Console.WriteLine(json);
		}

		[TestMethod]
		public void SimpleJsonKnownAppsSerializationTest()
		{

			string json = JsonConvert.SerializeObject(a);
		}

		[TestMethod]
		public void SerializeKnwnoAppsToFileTest()
		{
			File.Delete(testFileName);

			using (TextWriter tw = new StreamWriter(new FileStream(testFileName, FileMode.CreateNew)))
			{
				JsonSerializer serializer = new JsonSerializer();
				serializer.Serialize(tw, a);
			}
		}

		[TestMethod]
		public void DeserializeKnownAppsFromFileTest()
		{
			KnownApps app1, app2, app3;
			
			// read file into a string and deserialize JSON to a type
			app1 = JsonConvert.DeserializeObject<KnownApps>(File.ReadAllText(@"KnownApps.json"));

			// deserialize JSON directly from a file
			using (StreamReader file = File.OpenText(@"KnownApps.json"))
			{
				JsonSerializer serializer = new JsonSerializer();
				app2 = (KnownApps)serializer.Deserialize(file, typeof(KnownApps));
			}

			if (File.Exists(testFileName))
			{
				// deserialize JSON directly from a file
				using (StreamReader file = File.OpenText(testFileName))
				{
					JsonSerializer serializer = new JsonSerializer();
					app3 = (KnownApps)serializer.Deserialize(file, typeof(KnownApps));
				}
			}
		}
	}
}
