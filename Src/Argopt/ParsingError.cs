using System;

namespace Argopt {
	public struct ParsingError {
		public Exception ThrownException { get; set; }
		public string Argument { get; set; }
	}
}