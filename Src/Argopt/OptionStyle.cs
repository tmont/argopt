namespace Argopt {
	/// <summary>
	/// Represents the syntax of the command line optiond
	/// </summary>
	public enum OptionStyle {
		/// <summary>
		/// Window style command line options: <c>/option:value</c>
		/// </summary>
		Windows = 1,
		/// <summary>
		/// Unix style command line options: <c>--option=value</c>
		/// </summary>
		Unix = 2
	}
}