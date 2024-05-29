using Newtonsoft.Json;

namespace Conway.Library.Configuration
{
    public class BoolToBitJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(bool) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //If we have a 1 in the Json provided, then we will take that as true, anything else will cause a default to false
            return reader.Value.ToString().Equals("1", StringComparison.InvariantCultureIgnoreCase);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //Determine if the value is True (1) or False (0)
            int bitVal = Convert.ToBoolean(value) ? 1 : 0;
            writer.WriteValue(bitVal);
        }
    }
}
