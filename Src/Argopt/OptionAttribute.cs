using System;

namespace Argopt {
	/// <summary>
	/// Base class for contract property attributes
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public abstract class OptionAttribute : Attribute { }

	/// <summary>
	/// Signifies that the name of this option is case sensitive
	/// </summary>
	public sealed class CaseSensitiveAttribute : OptionAttribute { }

	/// <summary>
	/// Can be used to specify one or more aliases for this option
	/// </summary>
	public sealed class AliasAttribute : OptionAttribute {
		/// <param name="aliases">The aliases for this option</param>
		public AliasAttribute(params string[] aliases) {
			if (aliases.Length == 0) {
				throw new ArgumentException("must have at least one alias", "aliases");
			}

			Aliases = aliases;
		}

		/// <summary>
		/// Gets the aliases for this option
		/// </summary>
		public string[] Aliases { get; private set; }
	}

	/// <summary>
	/// Specifies a custom name for this option (the default name is the name of the property)
	/// </summary>
	public sealed class NameAttribute : OptionAttribute {
		/// <param name="name">The custom name for this option</param>
		public NameAttribute(string name) {
			if (string.IsNullOrWhiteSpace(name)) {
				throw new ArgumentException("name must be non-empty", "name");
			}

			Name = name;
		}

		/// <summary>
		/// Gets the custom name for this option
		/// </summary>
		public string Name { get; private set; }
	}

	/// <summary>
	/// Indicates that this option's value is an array, with values delimited by <see cref="Delimiter"/>
	/// </summary>
	public sealed class DelimitedAttribute : OptionAttribute {
		/// <param name="delimiter">The delimiter used to separate individual values</param>
		public DelimitedAttribute(string delimiter) {
			if (string.IsNullOrWhiteSpace(delimiter)) {
				throw new ArgumentException("delimiter cannot be empty", "delimiter");
			}

			Delimiter = delimiter;
		}

		public string Delimiter { get; private set; }
	}

	/// <summary>
	/// Internal attribute used to decorate other option attributes that behave as flags
	/// </summary>
	internal sealed class FlaggableAttribute : Attribute { }

	/// <summary>
	/// Signifies that this option is a boolean flag and should not be passed a value.
	/// It will also accept a "+" or "-" at the end of the option to indicate whether
	/// it's on or off.
	/// </summary>
	[Flaggable]
	public sealed class FlagAttribute : OptionAttribute { }

	/// <summary>
	/// Signifies that this option is a flag that also accepts values. The value of the
	/// flag (a boolean) will be stored in the decorated property, and the values will
	/// be stored in an auxiliary property specified by <see	cref="PropertyName"/>,
	/// e.g. /warnaserror+:1560,1680.
	/// </summary>
	[Flaggable]
	public sealed class ComplexFlagAttribute : OptionAttribute {
		public ComplexFlagAttribute(string propertyName) {
			if (string.IsNullOrWhiteSpace(propertyName)) {
				throw new ArgumentException("propertyName must be non-empty", "propertyName");
			}

			PropertyName = propertyName;
		}

		public string PropertyName { get; private set; }
	}

	/// <summary>
	/// Provides a way of describing an option in a human-readable way
	/// </summary>
	public sealed class DescriptionAttribute : OptionAttribute {
		/// <param name="description">The description of the option</param>
		public DescriptionAttribute(string description = null) {
			Description = description ?? string.Empty;
		}

		/// <summary>
		/// Gets the description
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// Gets or sets whether this option or value is required (default is <c>false</c>)
		/// </summary>
		public bool Required { get; set; }
	}

	/// <summary>
	/// Indicates that this property should not be used by Argopt during parsing
	/// </summary>
	public sealed class NotAnOptionAttribute : OptionAttribute { }

	/// <summary>
	/// Indicates that this property represents the value(s) passed on the command line,
	/// rather than the options (e.g. "/my/file" in "cp -R /my/file")
	/// </summary>
	public sealed class ValuePropertyAttribute : OptionAttribute { }
}