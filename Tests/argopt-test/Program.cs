using System;
using System.Linq;
using Argopt;

namespace argopt_test {
	public class Contract {
		public Contract() {
			Repeat = 1;
			NonExclamatedNames = new string[0];
			Names = new string[0];
		}

		[Description("The names of the people to greet delimited by \",\"; this option is required", Required = true, ValueName = "name1,name2,name3,...")]
		[Delimited(",")]
		public string[] Names { get; set; }

		[Description("Specifies how many times to repeat, default is 1", ValueName = "times")]
		public int Repeat { get; set; }

		[ComplexFlag("NonExclamatedNames"), Description("Use this option to disable appending an exclamation point for certain names. If no names are specified, the exclamation is disabled for ALL names.", ValueName = "name1,name2,name3...")]
		[Alias("disable", "d")]
		public bool DisableExclamation { get; set; }

		[NotAnOption, Delimited(",")]
		public string[] NonExclamatedNames { get; set; }

		[ValueProperty, Description("The greeting to display for each name", ValueName = "greeting")]
		public string Greeting { get; set; }

		[Description("Show argopt-test.exe usage details")]
		[Name("help"), Alias("?", "h", "usage")]
		public bool ShowUsage { get; set; }

		public override string ToString() {
			return string.Format(
				"Names: {0}\nRepeat: {1}\nDisable exclamation: {2}\nNon exclamated names: {3}\nGreeting: {4}\nShow usage: {5}\n", 
				string.Join(", ", Names),
				Repeat,
				DisableExclamation,
				string.Join(", ", NonExclamatedNames),
				Greeting,
				ShowUsage
			);
		}
	}

	class Program {
		static void PrintUsage() {
			Console.WriteLine(OptionParser.GetDescription<Contract>());
		}

		static void Main(string[] args) {
			var options = OptionParser.Parse<Contract>(args);
			if (!options.IsValid) {
				Console.WriteLine("Some errors occurred");
				foreach (var error in options.Errors) {
					Console.WriteLine(string.Format("Error occurred while parsing argument \"{0}\"", error.Argument));
					Console.WriteLine(error.ThrownException);
				}

				return;
			}

			var contract = options.Contract;
			if (contract.ShowUsage) {
				PrintUsage();
				return;
			}

			if (string.IsNullOrEmpty(contract.Greeting)) {
				PrintUsage();
				return;
			}

			if (contract.Names.Length == 0) {
				PrintUsage();
				return;
			}

			foreach (var name in contract.Names) {
				var doNotAppendExclamationPoint = 
					contract.NonExclamatedNames.Contains(name) 
					|| (contract.DisableExclamation && contract.NonExclamatedNames.Length == 0);

				for (var i = 0; i < contract.Repeat; i++) {
					Console.WriteLine(string.Format(
						"{0}, {1}{2}",
						contract.Greeting,
						name,
						doNotAppendExclamationPoint ? "" : "!"
					));
				}
			}
		}
	}
}