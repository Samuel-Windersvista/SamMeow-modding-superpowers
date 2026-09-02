using System;

namespace pitTeam.Server.Models;

public class FriendlyTeammateException : Exception
{
	public FriendlyTeammateException(string message)
		: base(message)
	{
	}
}
