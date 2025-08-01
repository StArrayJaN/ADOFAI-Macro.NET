﻿using System;

namespace Json.Serialization
{
	/// <summary>
	/// The exception that is thrown when a JSON value cannot be serialized.
	/// </summary>
	/// <remarks>
	/// This exception is only intended to be thrown by LightJson.
	/// </remarks>
	public sealed class JsonSerializationException : Exception
	{
		/// <summary>
		/// Gets the type of error that caused the exception to be thrown.
		/// </summary>
		public ErrorType Type { get; private set; }

		/// <summary>
		/// Initializes a new instance of JsonSerializationException.
		/// </summary>
		public JsonSerializationException()
			: base(GetDefaultMessage(ErrorType.Unknown)) { }

		/// <summary>
		/// Initializes a new instance of JsonSerializationException with the given error type.
		/// </summary>
		/// <param name="type">The error type that describes the cause of the error.</param>
		public JsonSerializationException(ErrorType type)
			: this(GetDefaultMessage(type), type) { }

		/// <summary>
		/// Initializes a new instance of JsonSerializationException with the given message and error type.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="type">The error type that describes the cause of the error.</param>
		public JsonSerializationException(string message, ErrorType type)
			: base(message)
		{
			this.Type = type;
		}

		private static string GetDefaultMessage(ErrorType type)
		{
			switch (type)
			{
				case ErrorType.InvalidNumber:
					return "The value been serialized contains an invalid number value (NAN, infinity).";

				case ErrorType.InvalidValueType:
					return "The value been serialized contains (or is) an invalid JSON type.";

				case ErrorType.CircularReference:
					return "The value been serialized contains circular references.";

				default:
					return "An error occurred during serialization.";
			}
		}

		/// <summary>
		/// Enumerates the types of errors that can occur during serialization.
		/// </summary>
		public enum ErrorType : int
		{
			/// <summary>
			/// Indicates that the cause of the error is unknown.
			/// </summary>
			Unknown = 0,

			/// <summary>
			/// Indicates that the writer encountered an invalid number value (NAN, infinity) during serialization.
			/// </summary>
			InvalidNumber,

			/// <summary>
			/// Indicates that the object been serialized contains an invalid JSON value type.
			/// That is, a value type that is not null, boolean, number, string, object, or array.
			/// </summary>
			InvalidValueType,

			/// <summary>
			/// Indicates that the object been serialized contains a circular reference.
			/// </summary>
			CircularReference,
		}
	}
}
