using MemoryPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace March7thHoney.Data.Excel;

[MemoryPackable]
[JsonConverter(typeof(GridFightDynamicValueConverter))]
public partial class GridFightDynamicValue
{
    public long? IntValue { get; set; }
    public double? DoubleValue { get; set; }
    public bool? BoolValue { get; set; }
    public string? StringValue { get; set; }
    public List<GridFightDynamicValue>? ArrayValue { get; set; }
    public Dictionary<string, GridFightDynamicValue>? MapValue { get; set; }

    public uint AsUInt32(uint fallback = 0)
    {
        return IntValue is >= 0 and <= uint.MaxValue ? (uint)IntValue.Value : fallback;
    }

    public IReadOnlyList<uint> AsUInt32List()
    {
        return ArrayValue?.Select(value => value.AsUInt32()).ToList() ?? [];
    }
}

// accepts both the baked {Type, Value} wrapper and the flat {IntValue} spelling
public class GridFightDynamicValueConverter : JsonConverter<GridFightDynamicValue>
{
    public override bool CanWrite => false;

    public override GridFightDynamicValue ReadJson(JsonReader reader, Type objectType,
        GridFightDynamicValue? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return JToken.ReadFrom(reader) is JObject obj ? Read(obj) : new GridFightDynamicValue();
    }

    public override void WriteJson(JsonWriter writer, GridFightDynamicValue? value, JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }

    private static GridFightDynamicValue Read(JObject obj)
    {
        return obj["Type"]?.Value<string>() is { } type && obj.TryGetValue("Value", out var payload)
            ? ReadWrapped(type, payload)
            : ReadFlat(obj);
    }

    private static GridFightDynamicValue ReadWrapped(string type, JToken payload)
    {
        var value = new GridFightDynamicValue();
        switch (type)
        {
            case "Array":
                value.ArrayValue = payload.OfType<JObject>().Select(Read).ToList();
                break;
            case "Map":
                // a wrapped map is a list of {Key, Value} pairs rather than a JSON object
                value.MapValue = payload.OfType<JObject>()
                    .Select(pair => (Key: pair["Key"]?["Value"]?.Value<string>(), Value: pair["Value"] as JObject))
                    .Where(pair => pair.Key != null && pair.Value != null)
                    .ToDictionary(pair => pair.Key!, pair => Read(pair.Value!));
                break;
            case "String":
                value.StringValue = payload.Value<string>();
                break;
            case "Bool":
            case "Boolean":
                value.BoolValue = payload.Value<bool>();
                break;
            case "Float":
            case "Double":
                value.DoubleValue = payload.Value<double>();
                break;
            default:
                value.IntValue = payload.Value<long?>();
                break;
        }

        return value;
    }

    private static GridFightDynamicValue ReadFlat(JObject obj)
    {
        var value = new GridFightDynamicValue
        {
            IntValue = obj["IntValue"]?.Value<long?>(),
            DoubleValue = obj["DoubleValue"]?.Value<double?>(),
            BoolValue = obj["BoolValue"]?.Value<bool?>(),
            StringValue = obj["StringValue"]?.Value<string>()
        };

        if (obj["ArrayValue"] is JArray array)
            value.ArrayValue = array.OfType<JObject>().Select(Read).ToList();

        if (obj["MapValue"] is JObject map)
            value.MapValue = map.Properties()
                .Where(property => property.Value is JObject)
                .ToDictionary(property => property.Name, property => Read((JObject)property.Value));

        return value;
    }
}

[ResourceEntity("GridFightConstCommon.json,GridFightConstValueCommonV2.json", true)]
[MemoryPackable]
public partial class GridFightConstValueExcel : ExcelResource
{
    public string ConstValueName { get; set; } = string.Empty;
    public GridFightDynamicValue Value { get; set; } = new();

    public override int GetId()
    {
        return StringComparer.Ordinal.GetHashCode(ConstValueName);
    }

    public override void Loaded()
    {
        GameData.GridFightConstValueData[ConstValueName] = Value;
    }
}
