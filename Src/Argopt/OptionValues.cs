using System.Collections.Generic;
using System.Linq;

namespace Argopt {
	public interface IOptionValues<T> {
		T Contract { get; }
		IEnumerable<string> Values { get; }
		IEnumerable<ParsingError> Errors { get; }
		bool IsValid { get; }
	}

	internal class OptionValues<T> : IOptionValues<T> {
		public OptionValues(T contract, IEnumerable<string> values, IEnumerable<ParsingError> errors) {
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