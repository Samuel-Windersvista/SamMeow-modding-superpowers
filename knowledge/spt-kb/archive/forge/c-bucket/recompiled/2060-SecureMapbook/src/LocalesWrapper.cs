using System.Collections.Generic;

namespace securemapbooke.Models;

public class LocalesWrapper
{
	public Dictionary<string, CustomLocaleDetails> Locales { get; set; } = new Dictionary<string, CustomLocaleDetails>();
}
