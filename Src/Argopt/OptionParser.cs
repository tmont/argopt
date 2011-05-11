using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Argopt {
	/// <summary>
	/// Provides static methods for parsing command line arguments
	/// </summary>
	public static class OptionParser {
		private static readonly Regex unixRegex = new Regex(@"^--?([^=]+)");
		private static readonly Regex windowsRegex = new Regex(@"^/([^:]+)");

		private static IEnumerable<OptionProperty> GetProperties<T>() {
			return typeof(T)
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => !p.GetCustomAttributes(true).Any(a => a.GetType() == typeof(NotAnOptionAttribute)))
				.Select(p => new OptionProperty(p));
		}

		public static string GetDescription<T>() {
			var properties = GetProperties<T>();
			return "";
		}

		/// <summary>
		/// Parses command line options, injecting values into a new instance of the option contract
		/// defined in <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">The type of the option contract</typeparam>
		/// <param name="args">The command line arguments to parse</param>
		/// <param name="optionStyle">The command line syntax style, default is <see cref="OptionStyle.Unix"/></param>
		/// <seealso cref="Parse{T}(string[],T,OptionStyle)"/>
		public static IOptionParseResult<T> Parse<T>(string[] args, OptionStyle optionStyle = OptionStyle.Unix) where T : new() {
			return Parse(args, new T(), optionStyle);
		}

		/// <summary>
		/// Parses command line options, injecting values into the given instance of the option contract
		/// </summary>
		/// <typeparam name="T">The type of the option contract</typeparam>
		/// <param name="args">The command line arguments to parse</param>
		/// <param name="contract">An instance of the option contract</param>
		/// <param name="optionStyle">The command line syntax style, default is <see cref="OptionStyle.Unix"/></param>
		/// <seealso cref="Parse{T}(string[],OptionStyle)"/>
		public static IOptionParseResult<T> Parse<T>(string[] args, T contract, OptionStyle optionStyle = OptionStyle.Unix) {
			var properties = GetProperties<T>();

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

			//inject values into ValueProperty property, if given
			var valueProperty = properties.FirstOrDefault(p => p.IsValueProperty);
			if (valueProperty != null) {
				valueProperty.SetNonOptionValues(contract, nonOptionValues.ToArray());
			}

			return new OptionParseResult<T>(contract, errors);
		}
	}
}