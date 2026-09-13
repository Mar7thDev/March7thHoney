using MemoryPack;
using March7thHoney.Enums.MechCraft;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MechCraftDiyMechInstance.json")]
[MemoryPackable]
public partial class MechCraftDiyMechInstanceExcel : ExcelResource
{
    [JsonProperty("FPPNKOEPFIP")] public int DiyMechatronID { get; set; }
    [JsonProperty("BGHKBBMOEKL")] public int PaintColorID { get; set; }
    [JsonProperty("GDBJDAOOCOH")] public int InstanceID { get; set; }

    public override int GetId() => InstanceID;

    public override void Loaded()
    {
        GameData.MechCraftDiyMechInstanceData.Add(this);
    }
}
