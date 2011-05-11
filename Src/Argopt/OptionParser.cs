using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

		/// <remarks>adapted from http://blueonionsoftware.com/blog.aspx?p=6091173d-6bdb-498c-9d57-c0da43319839</remarks>
		private static List<string> WordWrap(string text, int margin) {
			if (string.IsNullOrWhiteSpace(text)) {
				return new List<string>();
			}

			int start = 0, end;
			var lines = new List<string>();
			text = Regex.Replace(text, @"\s", " ").Trim();

			while ((end = start + margin) < text.Length) {
				while (text[end] != ' ' && end > start) {
					end -= 1;
				}

				if (end == start) {
					end = start + margin;
				}

				lines.Add(text.Substring(start, end - start));
				start = end + 1;
			}

			if (start < text.Length) {
				lines.Add(text.Substring(start));
			}

			return lines;
		}

		/// <summary>
		/// Gets a formatted description of the contract class defined by <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">The contract class type</typeparam>
		/// <param name="executableName">The name of the executable; if not given, defaults to Process.GetCurrentProcess().MainModule.FileName</param>
		/// <param name="lineLength">The length at which to start wrapping lines</param>
		/// <param name="optionStyle">The command line syntax style, default is <see cref="OptionStyle.Unix"/></param>
		public static string GetDescription<T>(string executableName = null, int lineLength = 100, OptionStyle optionStyle = OptionStyle.Unix) {
			executableName = executableName ?? Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
			var properties = GetProperties<T>();
			var descriptionBuilder = new StringBuilder(properties.Count() * 50);
			const int aliasIndent = 1;
			var aliasPrefix = optionStyle == OptionStyle.Unix ? "-" : "/";
			var prefix = optionStyle == OptionStyle.Unix ? "--" : "/";
			var valuePrefix = optionStyle == OptionStyle.Unix ? "=" : ":";

			//first print the command summary
			
			descriptionBuilder.Append(executableName);
			foreach (var property in properties.Where(p => !p.IsValueProperty).OrderByDescending(p => p.Required).ThenBy(p => p.Name)) {
				var format = property.Required ? " {0}{1}{2}{3}" : " [{0}{1}{2}{3}]";
				descriptionBuilder.Append(string.Format(format, prefix, property.Name, property.ValueName != null ? valuePrefix : "", property.ValueName));
			}
			var valueProperty = properties.FirstOrDefault(p => p.IsValueProperty);

			if (valueProperty != null) {
				descriptionBuilder.Append(" " + valueProperty.ValueName ?? valueProperty.Name);
			}

			var summaryLines = WordWrap(descriptionBuilder.ToString(), lineLength);
			descriptionBuilder.Clear();
			descriptionBuilder.AppendLine();
			descriptionBuilder.AppendLine("USAGE");
			descriptionBuilder.Append(string.Join(Environment.NewLine, summaryLines));

			descriptionBuilder.AppendLine();
			descriptionBuilder.AppendLine();
			
			var descriptionStartColumn = properties.Max(property => {
				if (property.IsValueProperty) {
					return property.ValueName != null ? property.ValueName.Length : property.Name.Length;
				}

				var length = property.Name.Length + prefix.Length;
				if (property.ValueName != null) {
					length += 1 + property.ValueName.Length; //+1 for "=" or ":"
				}

				if (property.IsComplexFlag) {
					length += 5; //appends [+|-]
				}

				if (property.Aliases.Any()) {
					length = Math.Max(length, property.Aliases.Max(alias => alias.Length + aliasPrefix.Length + aliasIndent));
				}

				return length;
			}) + 1; //+1 for padding

			
			var adjustedLineLength = lineLength - descriptionStartColumn;
			
			//first render the value property stuff
			if (valueProperty != null) {
				var name = valueProperty.ValueName ?? valueProperty.Name;
				descriptionBuilder.AppendLine("ARGUMENTS");
				descriptionBuilder.Append(name);
				descriptionBuilder.Append(new string(' ', descriptionStartColumn - name.Length));
				var lines = WordWrap(valueProperty.Description, adjustedLineLength);
				if (lines.Count > 0) {
					var spaceLength = 0;
					foreach (var line in lines) {
						descriptionBuilder.AppendLine(new string(' ', spaceLength) + line);
						spaceLength = descriptionStartColumn;
					}
				} else {
					descriptionBuilder.AppendLine();
				}

				descriptionBuilder.AppendLine();
			}

			descriptionBuilder.AppendLine("OPTIONS");
			foreach (var property in properties.Where(p => !p.IsValueProperty).OrderBy(p => p.Name)) {
				descriptionBuilder.Append(prefix + property.Name);

				var indent = property.Name.Length + prefix.Length;
				if (property.IsComplexFlag) {
					descriptionBuilder.Append("[+|-]");
					indent += 5;
				}

				if (property.ValueName != null) {
					descriptionBuilder.Append(valuePrefix + property.ValueName);
					indent += valuePrefix.Length + property.ValueName.Length;
				}

				descriptionBuilder.Append(new string(' ', descriptionStartColumn - indent));

				var lines = WordWrap(property.Description, adjustedLineLength);
				var aliases = property.Aliases.OrderBy(alias => alias).ToList();
				var i = 1;

				if (lines.Count > 0) {
					var spaceLength = 0;
					for (i = 0; i < lines.Count; i++) {
						if (i > 0) {
							var alias = aliases.ElementAtOrDefault(i - 1);
							if (alias != null) {
								descriptionBuilder.Append(new string(' ', aliasIndent) + aliasPrefix + alias);
								spaceLength = spaceLength - (aliasIndent + alias.Length + aliasPrefix.Length);
							}
						}

						descriptionBuilder.Append(new string(' ', spaceLength));
						descriptionBuilder.AppendLine(lines[i]);
						spaceLength = descriptionStartColumn;
					}
				} else {
					descriptionBuilder.AppendLine();
				}

				for (i = i - 1; i < aliases.Count; i++) {
					descriptionBuilder.AppendLine(new string(' ', aliasIndent) + aliasPrefix + aliases[i]);
				}
			}

			return descriptionBuilder.ToString();
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
					} else {
						continue;
					}
				}
				
				if (arg.Length > name.Length) {
					//the value is given after = or :
					value = arg.Substring(name.Length + 1);
				} else if (property.IsFlag && !property.IsComplexFlag) {
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