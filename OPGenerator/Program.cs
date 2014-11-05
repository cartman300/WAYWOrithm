using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OPGenerator {
	class Program {
		static string Input(string Prompt) {
			Console.Write(Prompt);
			return Console.ReadLine();
		}

		static string GenerateLink(string Thread, string HL) {
			string[] Elements = HL.Split(';');

			return Thread + "&page=" + Elements[2] + "#post" + Elements[1] + "   by " + Elements[0];
		}

		static void Main(string[] args) {
			Console.Title = "WAYWO OP Generator";

			string Thread = Input("Thread link: ");
			string[] Highlights = File.ReadAllText(Input("Highlights file: "))
				.Replace("\n", "").Replace("\r", "").Trim().Split(',');
			foreach (var Hl in Highlights)
				Console.WriteLine(GenerateLink(Thread, Hl));
		}
	}
}