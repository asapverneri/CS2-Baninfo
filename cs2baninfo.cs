using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Diagnostics.Eventing.Reader;

namespace CS2baninfo;

public partial class CS2baninfo : BasePlugin, IPluginConfig<CS2baninfoConfig>
{
    public CS2baninfoConfig Config { get; set; } = new();

    public override string ModuleName => "CS2 Baninfo";
    public override string ModuleDescription => "Prints info about connected players in console";
    public override string ModuleAuthor => "verneri";
    public override string ModuleVersion => "1.0.3";

    public void OnConfigParsed(CS2baninfoConfig config)
	{
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        Logger.LogInformation($"[{ModuleName}] Loaded (version {ModuleVersion})");
    }

    private string GetConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = Config.DatabaseHost, 
            Database = Config.DatabaseName, 
            UserID = Config.DatabaseUser,    
            Password = Config.DatabasePassword,
            Port = (uint)Config.DatabasePort
        };
        return builder.ConnectionString;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;
        var player = @event.Userid;
        var Name = player.PlayerName;
        var steamid = @event.Userid.SteamID;
        var admins = Utilities.GetPlayers().Where(player => AdminManager.PlayerHasPermissions(player, Config.Adminflag));
        bool printToConsole = Config.PrintInfoToConsole;
        bool printToChat = Config.PrintInfoToChat;
        bool printToCenter = Config.PrintInfoToCenter;

        if (player.IsValid)
        {
            int bansCount = 0;
            int mutesCount = 0;

            try
            {
                using (var connection = new MySqlConnection(GetConnectionString()))
                {
                    connection.Open();


                    string checkbansQuery = "SELECT COUNT(*) FROM sa_bans WHERE player_steamid = @SteamID";
                    bansCount = connection.ExecuteScalar<int>(checkbansQuery, new { SteamID = steamid });


                    string checkmutesQuery = "SELECT COUNT(*) FROM sa_mutes WHERE player_steamid = @SteamID";
                    mutesCount = connection.ExecuteScalar<int>(checkmutesQuery, new { SteamID = steamid });

                }
            }
            catch (Exception ex)
            {
                player.PrintToConsole($"[ERROR] An unexpected error occurred: {ex.Message}");
                return HookResult.Continue;
            }


            var Historyalert = $"{Localizer["center.top"]}<br>" +
                               $"{Localizer["center.name"]} {Name}<br>" +
                               $"{Localizer["center.steamid"]} {steamid}<br>" +
                               $"{Localizer["center.bans"]} {bansCount}<br>" +
                               $"{Localizer["center.mutes"]} {mutesCount}<br>" +
                               $"{Localizer["center.bottom"]}";


            foreach (var admin in admins)
            {
                if (printToConsole) {
                //notify admins to check console
                admin.PrintToChat($"{Localizer["playerinfoavailable", Name]}");
                //Console info
                admin.PrintToConsole($"{Localizer["console.top"]}");
                admin.PrintToConsole($"{Localizer["console.name"]} {Name}");
                admin.PrintToConsole($"{Localizer["console.steamid"]} {steamid}");
                admin.PrintToConsole($"{Localizer["console.bans"]} {bansCount}");
                admin.PrintToConsole($"{Localizer["console.mutes"]} {mutesCount}");
                admin.PrintToConsole($"{Localizer["console.bottom"]}");
                } 
                else if (printToChat)
                {
                    admin.PrintToChat($"{Localizer["chat.top"]}");
                    admin.PrintToChat($"{Localizer["chat.name"]} {Name}");
                    admin.PrintToChat($"{Localizer["chat.steamid"]} {steamid}");
                    admin.PrintToChat($"{Localizer["chat.bans"]} {bansCount}");
                    admin.PrintToChat($"{Localizer["chat.mutes"]} {mutesCount}");
                    admin.PrintToChat($"{Localizer["chat.bottom"]}");

                }
                else if (printToCenter)
                {
                    AddTimer(2.0f, () =>
                    {
                        admin.PrintToCenterHtml(Historyalert);
                    });
                }
            }

        }
        return HookResult.Continue;
    }
}