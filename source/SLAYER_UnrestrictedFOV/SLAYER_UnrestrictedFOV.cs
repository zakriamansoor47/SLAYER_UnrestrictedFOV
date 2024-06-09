using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace SLAYER_UnrestrictedFOV;

public class ConfigSpecials : BasePluginConfig
{
    [JsonPropertyName("PluginEnabled")] public bool PluginEnabled { get; set; } = true;
    [JsonPropertyName("ForceFOVOnSpawn")] public bool ForceFOVOnSpawn { get; set; } = false;
    [JsonPropertyName("FOVMin")] public int FOVMin { get; set; } = 20;
    [JsonPropertyName("FOVMax")] public int FOVMax { get; set; } = 130;
}
public class SLAYER_UnrestrictedFOV : BasePlugin, IPluginConfig<ConfigSpecials>
{
    public override string ModuleName => "SLAYER_UnrestrictedFOV";
    public override string ModuleVersion => "1.3";
    public override string ModuleAuthor => "SLAYER";
    public override string ModuleDescription => "Allows players to choose their own FOV settings";

    public required ConfigSpecials Config {get; set;}
    private int[] PlayerFov = new int[64];
    public void OnConfigParsed(ConfigSpecials config)
    {
        Config = config;
    }
    
    private SqliteConnection _connection = null!;
    public override void Load(bool hotReload)
    {
        AddCommand("css_fov", "Command to Set FOV", cmd_fov);

        _connection = new SqliteConnection($"Data Source={Path.Join(ModuleDirectory, "Database/SLAYER_UnrestrictedFOV.db")}");
        _connection.Open();
        Task.Run(async () =>
        {
            await _connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `SLAYER_UnrestrictedFOV` (
	                `steamid` UNSIGNED BIG INT NOT NULL,
	                `fov` INT NOT NULL DEFAULT 90,
	                PRIMARY KEY (`steamid`));");
        });
        
        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            if(!Config.PluginEnabled || @event.Userid == null || !@event.Userid.IsValid)return HookResult.Continue;

            var steamId = @event.Userid.AuthorizedSteamID?.SteamId64;
            var player = @event.Userid;
            // Run in a separate thread to avoid blocking the main thread
            Task.Run(async () =>
            {
                try
                {
                    var result = await _connection.QueryFirstOrDefaultAsync(@"SELECT `fov` FROM `SLAYER_UnrestrictedFOV` WHERE `steamid` = @SteamId;",
                    new
                    {
                        SteamId = steamId
                    });

                    // Print the result to the player's chat. Note that this needs to be run on the game thread.
                    // So we use `Server.NextFrame` to run it on the next game tick.
                    Server.NextFrame(() => 
                    {
                        player.DesiredFOV = Convert.ToUInt32($"{result?.fov ?? 0}");
                        Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
                        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Set", result?.fov ?? 90]}");
                        PlayerFov[player.Slot] = Convert.ToInt32($"{result?.fov ?? 0}");
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SLAYER_UnrestrictedFOV] Error on PlayerConnectFull while retrieving player FOV: {ex.Message}");
                    Logger.LogError($"[SLAYER_UnrestrictedFOV] Error on PlayerConnectFull while retrieving player FOV: {ex.Message}");
                }
                
                
            });
            return HookResult.Continue;
        });
        
        RegisterEventHandler<EventPlayerSpawn>((@event, @info) => 
        {
            if(Config.PluginEnabled && Config.ForceFOVOnSpawn && @event.Userid != null && @event.Userid.IsValid)
            {
                var player = @event.Userid;
                player.DesiredFOV = Convert.ToUInt32(PlayerFov[player.Slot]);
                Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
            }
            return HookResult.Continue;
        });
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
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {ChatColors.DarkRed}Plugin is Disabled!");
            return;
        }
        var fov = info.ArgByIndex(1);
        if(fov == "")
        {
            player.DesiredFOV = 90; // Set Default FOV
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Reset"]}");
            return;
        }
        if (!IsInt(fov)) 
        {
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Invalid"]}");
            return;
        }
        if(Convert.ToInt32(fov) < Config.FOVMin)
        {
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Min", Config.FOVMin]}");
            return;
        }
        if(Convert.ToInt32(fov) > Config.FOVMax)
        {
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Max", Config.FOVMax]}");
            return;
        }
        player.DesiredFOV = Convert.ToUInt32(fov);
        Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.Set", fov]}");
        PlayerFov[player.Slot] = Convert.ToInt32(fov);

        var steamId = player.AuthorizedSteamID?.SteamId64; // Get Player Steam ID
        if (steamId == null) return; // Steam ID shouldn't be Null
        
        Task.Run(async () => // Run in a separate thread to avoid blocking the main thread
        {
            try
            {
                // insert or update the player's fov
                await _connection.ExecuteAsync(@"
                    INSERT INTO `SLAYER_UnrestrictedFOV` (`steamid`, `fov`) VALUES (@SteamId, @Fov)
                    ON CONFLICT(`steamid`) DO UPDATE SET `fov` = @Fov;",
                    new
                    {
                        SteamId = steamId,
                        Fov = fov
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SLAYER_UnrestrictedFOV] Error while saving player FOV: {ex.Message}");
                Logger.LogError($"[SLAYER_UnrestrictedFOV] Error while saving player FOV: {ex.Message}");
            }
        });
        
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
