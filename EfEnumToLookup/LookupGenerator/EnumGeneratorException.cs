using System;
using System.Runtime.Serialization;

namespace EfEnumToLookup.LookupGenerator
{
	/// <summary>
	/// Specific exception related to problems during generation of lookup DB objects
	/// </summary>
    [Serializable]
    public class EnumGeneratorException : Exception
    {
	    /// <inheritdoc />
		public EnumGeneratorException() : base() { }

	    /// <inheritdoc />
		public EnumGeneratorException(string message): base(message) { }

	    /// <inheritdoc />
		public EnumGeneratorException(string message, Exception innerException): base(message, innerException) { }

	    /// <inheritdoc />
	    public EnumGeneratorException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
    }
}
