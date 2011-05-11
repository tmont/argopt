using System;
using System.Linq;
using System.Reflection;

namespace Argopt {

	internal class OptionProperty {
		private readonly PropertyInfo property;

		public OptionProperty(PropertyInfo property) {
			this.property = property;

			var attributes = (Attribute[])property.GetCustomAttributes(typeof(OptionAttribute), true);

			CaseSensitive = attributes.Any(a => a.GetType() == typeof(CaseSensitiveAttribute));

			var aliasAttribute = attributes.OfType<AliasAttribute>().FirstOrDefault();
			Aliases = aliasAttribute != null ? aliasAttribute.Aliases : new string[0];

			var nameAttribute = attributes.OfType<NameAttribute>().FirstOrDefault();
			Name = nameAttribute != null ? nameAttribute.Name : property.Name;

			IsFlag = attributes.Any(a => a.GetType().GetCustomAttributes(typeof(FlaggableAttribute), true).Length > 0);
			IsComplexFlag = attributes.Any(a => a.GetType() == typeof(ComplexFlagAttribute));
			if (IsComplexFlag) {
				ComplexValueProperty = property.DeclaringType.GetProperty(attributes.OfType<ComplexFlagAttribute>().First().PropertyName);
			}

			var delimitedAttribute = attributes.OfType<DelimitedAttribute>().FirstOrDefault();
			if (delimitedAttribute != null) {
				Delimiter = delimitedAttribute.Delimiter;
			}

			var descriptionAttribute = attributes.OfType<DescriptionAttribute>().FirstOrDefault();
			if (descriptionAttribute != null) {
				Description = descriptionAttribute.Description;
				ValueName = descriptionAttribute.ValueName;
				Required = descriptionAttribute.Required;
			}

			IsValueProperty = attributes.Any(a => a.GetType() == typeof(ValuePropertyAttribute));
		}

		private static object ConvertValue(string value, Type type) {
			object convertedValue;
			if (type == typeof(bool)) {
				if (string.IsNullOrWhiteSpace(value)) {
					convertedValue = false;
				} else {
					switch (value.ToUpperInvariant()) {
						case "1":
						case "TRUE":
						case "YES":
							convertedValue = true;
							break;
						default:
							convertedValue = false;
							break;
					}
				}
			} else if (type.IsEnum) {
				if (!Enum.IsDefined(type, value)) {
					return null;
				}

				convertedValue = Enum.Parse(type, value, true);
			} else {
				convertedValue = Convert.ChangeType(value, type);
			}

			return convertedValue;
		}

		private static Array ConvertToArray(string[] values, Type elementType) {
			if (elementType == typeof(string)) {
				return values;
			}

			var array = Array.CreateInstance(elementType, values.Length);
			var i = 0;
			foreach (var arrayValue in values.Select(splitValue => ConvertValue(splitValue, elementType))) {
				array.SetValue(arrayValue, i++);
			}

			return array;
		}

		public void SetValue(object contract, string value) {
			object convertedValue;

			if (Delimiter != null && Type.IsArray) {
				convertedValue = ConvertToArray(value.Split(new[] { Delimiter }, StringSplitOptions.None), Type.GetElementType());
			} else {
				convertedValue = ConvertValue(value, Type);
			}

			property.SetValue(contract, convertedValue, new object[0]);
		}

		public void SetNonOptionValues(object contract, string[] values) {
			if (!IsValueProperty) {
				throw new InvalidOperationException("This property is not annotated with ValueProperty");
			}

			if (Type.IsArray) {
				property.SetValue(contract, ConvertToArray(values, Type.GetElementType()), new object[0]);
			} else {
				SetValue(contract, values.FirstOrDefault());
			}
		}

		public bool NameMatches(string name) {
			if (IsValueProperty) {
				return false;
			}

			var upperName = name.ToUpperInvariant();
			if (Name == name || (!CaseSensitive && upperName == Name.ToUpperInvariant())) {
				return true;
			}

			return !CaseSensitive
				? Aliases.Any(alias => alias.ToUpper() == upperName)
				: Aliases.Any(alias => alias == name);
		}

		public Type Type { get { return property.PropertyType; } }
		public bool CaseSensitive { get; private set; }
		public string[] Aliases { get; private set; }
		public string Name { get; private set; }
		public bool IsFlag { get; private set; }
		public bool IsComplexFlag { get; private set; }
		public PropertyInfo ComplexValueProperty { get; private set; }
		public string Delimiter { get; private set; }
		public string Description { get; private set; }
		public bool IsValueProperty { get; private set; }
		public string ValueName{ get; private set; }
		public bool Required{ get; private set; }
	}
}