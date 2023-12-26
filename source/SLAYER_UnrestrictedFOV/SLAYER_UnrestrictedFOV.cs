using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Text.Json.Serialization;

namespace SLAYER_UnrestrictedFOV;

public class ConfigSpecials : BasePluginConfig
{
    [JsonPropertyName("PluginEnabled")] public bool PluginEnabled { get; set; } = true;
    [JsonPropertyName("FOVMin")] public int FOVMin { get; set; } = 20;
    [JsonPropertyName("FOVMax")] public int FOVMax { get; set; } = 130;
}
public class SLAYER_UnrestrictedFOV : BasePlugin, IPluginConfig<ConfigSpecials>
{
    public override string ModuleName => "SLAYER_UnrestrictedFOV";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "SLAYER";
    public override string ModuleDescription => "Allows players to choose their own FOV settings";

    public required ConfigSpecials Config {get; set;}
    public void OnConfigParsed(ConfigSpecials config)
    {
        Config = config;
    }
    

    public override void Load(bool hotReload)
    {
        AddCommand("css_fov", "Enabled/Disabled Scope", cmd_fov);
    }
    private void cmd_fov(CCSPlayerController? player, CommandInfo info)
    {
        
        if(player == null) // if player is server then return
        {
            info.ReplyToCommand("[FOV] Cannot use command from RCON");
            return;
        }
        if(Config.PluginEnabled == false)
        {
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.Darkred}Plugin is Disabled!");
            return;
        }
        var fov = info.ArgByIndex(1);
        if(fov == "")
        {
            player.DesiredFOV = 60;
            SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.Lime}Your FOV has been reset.");
            return;
        }
        if (!IsInt(fov)) 
        {
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.Darkred}Invalid Value. {ChatColors.White}Please write only integer Value {ChatColors.Lime}!fov <FOV>");
            return;
        }
        if(Convert.ToInt32(fov) < Config.FOVMin)
        {
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.Darkred}The minimum {ChatColors.Lime}FOV {ChatColors.Darkred}you can set with {ChatColors.Lime}!fov {ChatColors.Darkred}is: {ChatColors.Lime}{Config.FOVMin}");
            return;
        }
        if(Convert.ToInt32(fov) > Config.FOVMax)
        {
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.Darkred}The maximum {ChatColors.Lime}FOV {ChatColors.Darkred}you can set with {ChatColors.Lime}!fov {ChatColors.Darkred}is: {ChatColors.Lime}{Config.FOVMax}");
            return;
        }
        player.DesiredFOV = Convert.ToUInt32(fov);
        SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
        player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.White}Your FOV has been set to {ChatColors.Lime}{fov} {ChatColors.White}on this server.");
    }
    private bool IsInt(string sVal)
    {
        foreach (char c in sVal)
        {
            int iN = (int)c;
            if ((iN > 57) || (iN < 48))
                return false;
        }
        return true;
    }
    // Thanks "yarukon (59 61 72 75 6B 6F 6E)" Discord member
    private static MemoryFunctionVoid<nint, nint, int, short, short> _StateChanged = new(@"\x55\x48\x89\xE5\x41\x57\x41\x56\x41\x55\x41\x54\x53\x89\xD3");
    private static MemoryFunctionVoid<nint, int, long> _NetworkStateChanged = new(@"\x4C\x8B\x07\x4D\x85\xC0\x74\x2A\x49\x8B\x40\x10");
    public int FindSchemaChain(string classname) => Schema.GetSchemaOffset(classname, "__m_pChainEntity");

    public void SetStateChanged(CBaseEntity entity, string classname, string fieldname, int extraOffset = 0)
    {
        int offset = Schema.GetSchemaOffset(classname, fieldname);
        int chainOffset = FindSchemaChain(classname);

        if (chainOffset != 0)
        {
            _NetworkStateChanged.Invoke(entity.Handle + chainOffset, offset, 0xFFFFFFFF);
            return; // No need to execute the rest of the things
        }

        _StateChanged.Invoke(entity.NetworkTransmitComponent.Handle, entity.Handle, offset + extraOffset, -1, -1);

        entity.LastNetworkChange = Server.CurrentTime;
        entity.IsSteadyState.Clear();
    }
}
