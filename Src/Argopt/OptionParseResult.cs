using System.Collections.Generic;
using System.Linq;

namespace Argopt {
	/// <summary>
	/// Represents the result of parsing the command line options
	/// </summary>
	/// <typeparam name="T">The option contract type</typeparam>
	public interface IOptionParseResult<out T> {
		/// <summary>
		/// Gets the option contract instance with values parsed from the command line arguments injected
		/// </summary>
		T Contract { get; }

		/// <summary>
		/// Gets an enumeration of command line values that were not part of an option 
		/// (e.g. "/my/file" in "cp -R /my/file")
		/// </summary>
		IEnumerable<string> Values { get; }

		/// <summary>
		/// Gets an enumeration of all errors that occurred during parsing
		/// </summary>
		IEnumerable<ParsingError> Errors { get; }

		/// <summary>
		/// Gets whether any errors occurred during parsing
		/// </summary>
		bool IsValid { get; }
	}

	internal sealed class OptionParseResult<T> : IOptionParseResult<T> {
		public OptionParseResult(T contract, IEnumerable<string> values, IEnumerable<ParsingError> errors) {
			Contract = contract;
			Values = values;
			Errors = errors;
		}

		public T Contract { get; private set; }
		public IEnumerable<string> Values { get; private set; }
		public IEnumerable<ParsingError> Errors { get; private set; }
		public bool IsValid { get { return !Errors.Any(); } }
	}
}