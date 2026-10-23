using System;
using Newtonsoft.Json;
using Version = Com.AiricLenz.BulkSolutionExporter.Core.Version;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Puts a core solution <see cref="Version"/> on the wire as its dotted
	/// string (<c>"1.0.0.0"</c>), the form the tool's UI shows and sends back.
	/// </summary>
	public sealed class VersionJsonConverter : JsonConverter<Version>
	{

		// ============================================================================
		/// <summary>
		/// Writes the version's dotted string, or <c>null</c>.
		/// </summary>
		public override void WriteJson(
			JsonWriter writer,
			Version value,
			JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			writer.WriteValue(value.ToString());
		}


		// ============================================================================
		/// <summary>
		/// Reads a dotted version string; <c>null</c> reads as no version.
		/// </summary>
		/// <exception cref="JsonSerializationException">The value is not a string.</exception>
		public override Version ReadJson(
			JsonReader reader,
			Type objectType,
			Version existingValue,
			bool hasExistingValue,
			JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}

			if (reader.TokenType != JsonToken.String)
			{
				throw new JsonSerializationException("A solution version must be a dotted string.");
			}

			return new Version((string) reader.Value);
		}

	}
}
