using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Argopt {

	public enum OptionStyle {
		Windows = 1,
		Unix = 2
	}

	public static class OptionParser {
		private static readonly Regex unixRegex = new Regex(@"^--?([^=]+)");
		private static readonly Regex windowsRegex = new Regex(@"^/([^:]+)");

		public static IOptionValues<T> Parse<T>(string[] args, OptionStyle optionStyle = OptionStyle.Unix) where T : new() {
			return Parse(args, new T(), optionStyle);
		}

		public static IOptionValues<T> Parse<T>(string[] args, T contract, OptionStyle optionStyle = OptionStyle.Unix) {
			var properties = contract
				.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Select(p => new OptionProperty(p));

			var nonOptionValues = new List<string>();
			var errors = new List<ParsingError>();
			for (var i = 0; i < args.Length; i++) {
				var arg = args[i];

				var name = (optionStyle == OptionStyle.Windows ? windowsRegex : unixRegex).Match(arg);
				if (!name.Success) {
					nonOptionValues.Add(arg);
					continue;
				}

				var optionName = name.Groups[1].Value;
				string value = null;
				if (optionName.EndsWith("+") || optionName.EndsWith("-")) {
					//handle on/off switches, e.g. /debug+ or /debug-
					value = optionName.Last() == '+' ? bool.TrueString : bool.FalseString;
					optionName = optionName.Substring(0, optionName.Length - 1);
				}

				var property = properties.FirstOrDefault(p => p.NameMatches(optionName));
				if (property == null) {
					//no contract for this option, so it's a value
					nonOptionValues.Add(arg);
					continue;
				}

				if (value != null && property.IsFlag) {
					//this won't throw because the only possible values are bool.TrueString and bool.FalseString
					property.SetValue(contract, value);

					if (property.IsComplexFlag) {
						//complex flags are options with boolean switches, but also take values
						//e.g. /warnaserror+:1400,1456,1680
						property = new OptionProperty(property.ComplexValueProperty);
					}
				}
				
				if (arg.Length > name.Length) {
					//the value is given after = or :
					value = arg.Substring(name.Length + 1);
				} else if (property.IsFlag) {
					value = bool.TrueString;
				} else if (i + 1 < args.Length) {
					//value is the next arg
					value = args[++i];
				}

				try {
					property.SetValue(contract, value);
				} catch (Exception e) {
					errors.Add(new ParsingError { Argument = arg, ThrownException = e });
				}
			}

			return new OptionValues<T>(contract, nonOptionValues, errors);
		}
	}
}