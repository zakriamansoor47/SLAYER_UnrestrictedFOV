using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;
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
    public override string ModuleVersion => "1.1";
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
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.DarkRed}Plugin is Disabled!");
            return;
        }
        var fov = info.ArgByIndex(1);
        if(fov == "")
        {
            player.DesiredFOV = 90; // Set Default FOV
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.Lime}Your FOV has been reset.");
            return;
        }
        if (!IsInt(fov)) 
        {
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.DarkRed}Invalid Value. {ChatColors.White}Please write only integer Value {ChatColors.Lime}!fov <FOV>");
            return;
        }
        if(Convert.ToInt32(fov) < Config.FOVMin)
        {
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.DarkRed}The minimum {ChatColors.Lime}FOV {ChatColors.DarkRed}you can set with {ChatColors.Lime}!fov {ChatColors.DarkRed}is: {ChatColors.Lime}{Config.FOVMin}");
            return;
        }
        if(Convert.ToInt32(fov) > Config.FOVMax)
        {
            player.PrintToChat($" {ChatColors.Lime}[{ChatColors.Gold}FOV{ChatColors.Green}{ChatColors.Lime}] {ChatColors.DarkRed}The maximum {ChatColors.Lime}FOV {ChatColors.DarkRed}you can set with {ChatColors.Lime}!fov {ChatColors.DarkRed}is: {ChatColors.Lime}{Config.FOVMax}");
            return;
        }
        player.DesiredFOV = Convert.ToUInt32(fov);
        Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
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
}
