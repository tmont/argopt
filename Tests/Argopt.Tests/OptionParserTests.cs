using System;
using System.Linq;
using NUnit.Framework;

namespace Argopt.Tests {

	[TestFixture]
	public class OptionParserTests {

		[Test]
		public void Should_parse_options_prepended_by_doubledash() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--Lulz", "foo" });
			Assert.That(result.Contract.Lulz, Is.EqualTo("foo"));
		}

		[Test]
		public void Should_parse_options_prepended_by_dash() {
			var result = OptionParser.Parse<OptionContract>(new[] { "-Lulz", "foo" });
			Assert.That(result.Contract.Lulz, Is.EqualTo("foo"));
		}

		[Test]
		public void Should_parse_options_windows_style() {
			var result = OptionParser.Parse<OptionContract>(new[] { "/Lulz:foo" }, OptionStyle.Windows);
			Assert.That(result.Contract.Lulz, Is.EqualTo("foo"));
		}

		[Test]
		public void Should_parse_case_insensitively() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--lUlZ", "foo" });
			Assert.That(result.Contract.Lulz, Is.EqualTo("foo"));
		}

		[Test]
		public void Should_parse_case_sensitively() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--CaseTest", "foo" });
			Assert.That(result.Contract.CaseTest, Is.EqualTo("foo"));

			result = OptionParser.Parse<OptionContract>(new[] { "--casetest", "foo" });
			Assert.That(result.Contract.CaseTest, Is.Null);
		}

		[Test]
		public void Should_parse_enum_value() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--EnumTest", "Bar" });
			Assert.That(result.Contract.EnumTest, Is.EqualTo(MyEnum.Bar));
		}

		[Test]
		public void Should_use_default_enum_value_if_invalid_value_is_given() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--EnumTest", "asdf" });
			Assert.That(result.Contract.EnumTest, Is.EqualTo(MyEnum.Foo));
		}

		[Test]
		public void Should_use_alias() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--alias", "foo" });
			Assert.That(result.Contract.AliasTest, Is.EqualTo("foo"));

			result = OptionParser.Parse<OptionContract>(new[] { "-a", "foo" });
			Assert.That(result.Contract.AliasTest, Is.EqualTo("foo"));

			result = OptionParser.Parse<OptionContract>(new[] { "--AliasTest", "foo" });
			Assert.That(result.Contract.AliasTest, Is.EqualTo("foo"));
		}

		[Test]
		public void Should_parse_value_after_equals_sign() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--Lulz=foo" });
			Assert.That(result.Contract.Lulz, Is.EqualTo("foo"));
		}

		[Test]
		public void Should_parse_option_that_ends_with_equals_sign() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--Lulz=" });
			Assert.That(result.Contract.Lulz, Is.EqualTo(string.Empty));
		}

		[Test]
		public void Should_ignore_option_not_in_the_contract() {
			OptionParser.Parse<OptionContract>(new[] { "--DoesNotExist" });
		}

		[Test]
		public void Should_parse_flag_option() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--FlagTest" });
			Assert.That(result.Contract.FlagTest, Is.True);

			result = OptionParser.Parse<OptionContract>(new[] { "--NotFlagTest" });
			Assert.That(result.Contract.FlagTest, Is.False);
		}

		[Test]
		public void Should_parse_non_option_values() {
			var result = OptionParser.Parse<OptionContract>(new[] { "foo", "bar", "--FlagTest", "baz", "foo=bar", "-dne" });
			Assert.That(result.Values.Count(), Is.EqualTo(5));
			Assert.That(result.Values.ElementAt(0), Is.EqualTo("foo"));
			Assert.That(result.Values.ElementAt(1), Is.EqualTo("bar"));
			Assert.That(result.Values.ElementAt(2), Is.EqualTo("baz"));
			Assert.That(result.Values.ElementAt(3), Is.EqualTo("foo=bar"));
			Assert.That(result.Values.ElementAt(4), Is.EqualTo("-dne"));
		}

		[Test]
		public void Should_parse_named_option() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--NamedTest", "foo" });
			Assert.That(result.Contract.NamedTest, Is.Null);

			result = OptionParser.Parse<OptionContract>(new[] { "--lollersk8", "foo" });
			Assert.That(result.Contract.NamedTest, Is.EqualTo("foo"));
		}

		[Test]
		public void Should_parse_boolean_strings() {
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "true" }).Contract.BoolTest, Is.True);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "True" }).Contract.BoolTest, Is.True);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "tRUe" }).Contract.BoolTest, Is.True);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "1" }).Contract.BoolTest, Is.True);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "yes" }).Contract.BoolTest, Is.True);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "Yes" }).Contract.BoolTest, Is.True);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "YeS" }).Contract.BoolTest, Is.True);

			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "false" }).Contract.BoolTest, Is.False);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "asdf" }).Contract.BoolTest, Is.False);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "0" }).Contract.BoolTest, Is.False);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest", "no" }).Contract.BoolTest, Is.False);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest" }).Contract.BoolTest, Is.False);
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--BoolTest=" }).Contract.BoolTest, Is.False);
		}

		[Test]
		public void Should_parse_numbers() {
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--IntTest", "1" }).Contract.IntTest, Is.EqualTo(1));
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--IntTest", "7" }).Contract.IntTest, Is.EqualTo(7));
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--IntTest", "-20" }).Contract.IntTest, Is.EqualTo(-20));

			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--DoubleTest", "1" }).Contract.DoubleTest, Is.EqualTo(1));
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--DoubleTest", "7.5" }).Contract.DoubleTest, Is.EqualTo(7.5));
			Assert.That(OptionParser.Parse<OptionContract>(new[] { "--DoubleTest", "-20.0" }).Contract.DoubleTest, Is.EqualTo(-20));
		}

		[Test]
		public void Should_parse_complex_flag() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--ComplexFlagTest+", "foo" });
			Assert.That(result.Contract.ComplexFlagTest, Is.True);
			Assert.That(result.Contract.ComplexAuxiliary, Is.EqualTo("foo"));

			result = OptionParser.Parse<OptionContract>(new[] { "-ComplexFlagTest+=foo" });
			Assert.That(result.Contract.ComplexFlagTest, Is.True);
			Assert.That(result.Contract.ComplexAuxiliary, Is.EqualTo("foo"));
		}

		[Test]
		public void Should_parse_array() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--ArrayTest", "foo,bar,baz" });
			Assert.That(result.Contract.ArrayTest, Is.Not.Null);
			Assert.That(result.Contract.ArrayTest, Has.Length.EqualTo(3));
			Assert.That(result.Contract.ArrayTest[0], Is.EqualTo("foo"));
			Assert.That(result.Contract.ArrayTest[1], Is.EqualTo("bar"));
			Assert.That(result.Contract.ArrayTest[2], Is.EqualTo("baz"));
		}

		[Test]
		public void Shoul_parse_non_string_array() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--IntArrayTest", "5|4|-100" });
			Assert.That(result.Contract.IntArrayTest, Is.Not.Null);
			Assert.That(result.Contract.IntArrayTest, Has.Length.EqualTo(3));
			Assert.That(result.Contract.IntArrayTest[0], Is.EqualTo(5));
			Assert.That(result.Contract.IntArrayTest[1], Is.EqualTo(4));
			Assert.That(result.Contract.IntArrayTest[2], Is.EqualTo(-100));
		}

		[Test]
		public void Shoul_parse_enum_array() {
			var result = OptionParser.Parse<OptionContract>(new[] { "-EnumArrayTest=Foo,Bar" });
			Assert.That(result.Contract.EnumArrayTest, Is.Not.Null);
			Assert.That(result.Contract.EnumArrayTest, Has.Length.EqualTo(2));
			Assert.That(result.Contract.EnumArrayTest[0], Is.EqualTo(MyEnum.Foo));
			Assert.That(result.Contract.EnumArrayTest[1], Is.EqualTo(MyEnum.Bar));
		}

		[Test]
		public void Should_catch_error_when_value_cannot_be_parsed() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--IntTest=asdf" });

			Assert.That(result.IsValid, Is.False);

			Assert.That(result.Errors.Count(), Is.EqualTo(1));
			Assert.That(result.Errors.First().Argument, Is.EqualTo("--IntTest=asdf"));
			Assert.That(result.Errors.First().ThrownException, Is.TypeOf<FormatException>());
		}

		[Test]
		public void Should_catch_error_when_value_cannot_be_parsed_in_array() {
			var result = OptionParser.Parse<OptionContract>(new[] { "--IntArrayTest=asdf" });

			Assert.That(result.IsValid, Is.False);

			Assert.That(result.Errors.Count(), Is.EqualTo(1));
			Assert.That(result.Errors.First().Argument, Is.EqualTo("--IntArrayTest=asdf"));
			Assert.That(result.Errors.First().ThrownException, Is.TypeOf<FormatException>());
		}

		#region mocks
		public class OptionContract {
			public string Lulz { get; set; }
			
			[CaseSensitive]
			public string CaseTest { get; set; }

			[Name("lollersk8")]
			public string NamedTest { get; set; }

			[Alias("alias", "a")]
			public string AliasTest { get; set; }

			[Flag]
			public bool FlagTest { get; set; }

			public MyEnum EnumTest { get; set; }

			public bool BoolTest { get; set; }

			public int IntTest { get; set; }
			public double DoubleTest { get; set; }

			[ComplexFlag("ComplexAuxiliary")]
			public bool ComplexFlagTest { get; set; }

			public string ComplexAuxiliary { get; set; }

			[Delimited(",")]
			public string[] ArrayTest { get; set; }

			[Delimited("|")]
			public int[] IntArrayTest { get; set; }

			[Delimited(",")]
			public MyEnum[] EnumArrayTest { get; set; }
		}

		public enum MyEnum {
			Foo,
			Bar,
			Baz
		}
		#endregion

	}
}
