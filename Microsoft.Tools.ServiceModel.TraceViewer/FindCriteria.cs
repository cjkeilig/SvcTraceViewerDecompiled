using System.Text.RegularExpressions;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FindCriteria
	{
		private string findingText;

		private FindingScope scope;

		private FindingTarget target;

		private FindingOptions options;

		private FindingToken token;

		private Regex wholeWordRegex;

		public string FindingText
		{
			get
			{
				return findingText;
			}
			set
			{
				findingText = value;
			}
		}

		public FindingScope Scope
		{
			get
			{
				return scope;
			}
			set
			{
				scope = value;
			}
		}

		public FindingTarget Target
		{
			get
			{
				return target;
			}
			set
			{
				target = value;
			}
		}

		public FindingOptions Options
		{
			get
			{
				return options;
			}
			set
			{
				options = value;
			}
		}

		internal FindingToken Token
		{
			get
			{
				return token;
			}
			set
			{
				token = value;
			}
		}

		internal Regex WholeWordRegex
		{
			get
			{
				return wholeWordRegex;
			}
			set
			{
				wholeWordRegex = value;
			}
		}
	}
}
