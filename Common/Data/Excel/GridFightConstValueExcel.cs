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

// accepts the baked {Type, Value} wrapper, the flat {IntValue} spelling and (4.6.52+ exports) the bare JSON value
public class GridFightDynamicValueConverter : JsonConverter<GridFightDynamicValue>
{
    private static readonly HashSet<string> FlatKeys =
        ["IntValue", "DoubleValue", "BoolValue", "StringValue", "ArrayValue", "MapValue"];

    public override bool CanWrite => false;

    public override GridFightDynamicValue ReadJson(JsonReader reader, Type objectType,
        GridFightDynamicValue? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return Read(JToken.ReadFrom(reader));
    }

    public override void WriteJson(JsonWriter writer, GridFightDynamicValue? value, JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }

    private static GridFightDynamicValue Read(JToken token)
    {
        return token switch
        {
            JObject obj => Read(obj),
            JArray array => new GridFightDynamicValue { ArrayValue = array.Select(Read).ToList() },
            { Type: JTokenType.Integer } => new GridFightDynamicValue { IntValue = token.Value<long>() },
            { Type: JTokenType.Float } => new GridFightDynamicValue { DoubleValue = token.Value<double>() },
            { Type: JTokenType.Boolean } => new GridFightDynamicValue { BoolValue = token.Value<bool>() },
            { Type: JTokenType.String } => new GridFightDynamicValue { StringValue = token.Value<string>() },
            _ => new GridFightDynamicValue(),
        };
    }

    private static GridFightDynamicValue Read(JObject obj)
    {
        if (obj["Type"]?.Value<string>() is { } type && obj.TryGetValue("Value", out var payload))
            return ReadWrapped(type, payload);
        // An empty object is an omitted default in the older exports; any key outside the flat spelling makes it a
        // bare map ({"ExtraHealAddedRatio": 0.1}).
        if (obj.Count == 0 || obj.Properties().All(property => FlatKeys.Contains(property.Name)))
            return ReadFlat(obj);
        return new GridFightDynamicValue
        {
            MapValue = obj.Properties().ToDictionary(property => property.Name, property => Read(property.Value)),
        };
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

[ResourceEntity("GridFightConstCommon.json,GridFightConstValueCommonV2.json,GridFightTraitConstValueS2.json", true,
    OptionalFileNames = "GridFightTraitConstValueS2.json")]
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
