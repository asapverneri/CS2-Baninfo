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
    public override string ModuleVersion => "1.0.5";

    public void OnConfigParsed(CS2baninfoConfig config)
	{
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        Logger.LogInformation($"Loaded (version {ModuleVersion})");

        if (Config.AdminPlugin != 1 && Config.AdminPlugin != 2 && Config.AdminPlugin != 3)
        {

            Logger.LogError($"AdminPluginType is invalid!");
            Logger.LogError($"Correct values: 1 = SimpleAdmin, 2 = cs2-admin, 3 = iks_admin");
        }
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
            int gagsCount = 0;

            try
            {
                using (var connection = new MySqlConnection(GetConnectionString()))
                {
                    connection.Open();

                    if (Config.AdminPlugin == 1) //SimplAadmin
                    {
                        string checkbansQuery = "SELECT COUNT(*) FROM sa_bans WHERE player_steamid = @SteamID";
                        bansCount = connection.ExecuteScalar<int>(checkbansQuery, new { SteamID = steamid });

                        string checkmutesQuery = "SELECT COUNT(*) FROM sa_mutes WHERE player_steamid = @SteamID";
                        mutesCount = connection.ExecuteScalar<int>(checkmutesQuery, new { SteamID = steamid });
                    }
                    if (Config.AdminPlugin == 2) //cs2-admin
                    {
                        string checkbansQuery = "SELECT COUNT(*) FROM baseban WHERE steamid = @SteamID";
                        bansCount = connection.ExecuteScalar<int>(checkbansQuery, new { SteamID = steamid });
                    }
                    if (Config.AdminPlugin == 3) //Iks_admin
                    {
                        string checkbansQuery = "SELECT COUNT(*) FROM iks_bans WHERE sid = @SteamID";
                        bansCount = connection.ExecuteScalar<int>(checkbansQuery, new { SteamID = steamid });

                        string checkmutesQuery = "SELECT COUNT(*) FROM iks_mutes WHERE sid = @SteamID";
                        mutesCount = connection.ExecuteScalar<int>(checkmutesQuery, new { SteamID = steamid });

                        string checkgagsQuery = "SELECT COUNT(*) FROM iks_gags WHERE sid = @SteamID";
                        gagsCount = connection.ExecuteScalar<int>(checkgagsQuery, new { SteamID = steamid });
                    }

                }
            }
            catch (Exception ex)
            {
                player.PrintToConsole($"[ERROR] An unexpected error occurred: {ex.Message}");
                return HookResult.Continue;
            }

           var Historyalertsimple = $"{Localizer["center.top"]}<br>" +
                   $"{Localizer["center.name"]} {Name}<br>" +
                   $"{Localizer["center.steamid"]} {steamid}<br>" +
                   $"{Localizer["center.bans"]} {bansCount}<br>" +
                   $"{Localizer["center.mutes"]} {mutesCount}<br>" +
                   $"{Localizer["center.bottom"]}";

            var Historyalertcs2admin = $"{Localizer["center.top"]}<br>" +
                   $"{Localizer["center.name"]} {Name}<br>" +
                   $"{Localizer["center.steamid"]} {steamid}<br>" +
                   $"{Localizer["center.bans"]} {bansCount}<br>" +
                   $"{Localizer["center.bottom"]}";

            var Historyalertiks = $"{Localizer["center.top"]}<br>" +
                   $"{Localizer["center.name"]} {Name}<br>" +
                   $"{Localizer["center.steamid"]} {steamid}<br>" +
                   $"{Localizer["center.bans"]} {bansCount}<br>" +
                   $"{Localizer["center.mutes"]} {mutesCount}<br>" +
                   $"{Localizer["center.gags"]} {gagsCount}<br>" +
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
                    if(Config.AdminPlugin == 1 || Config.AdminPlugin == 3)
                    {
                        admin.PrintToConsole($"{Localizer["console.mutes"]} {mutesCount}");
                    }
                    if(Config.AdminPlugin == 3)
                    {
                        admin.PrintToConsole($"{Localizer["console.gags"]} {gagsCount}");
                    }
                admin.PrintToConsole($"{Localizer["console.bottom"]}");
                } 
                else if (printToChat)
                {
                    admin.PrintToChat($"{Localizer["chat.top"]}");
                    admin.PrintToChat($"{Localizer["chat.name"]} {Name}");
                    admin.PrintToChat($"{Localizer["chat.steamid"]} {steamid}");
                    admin.PrintToChat($"{Localizer["chat.bans"]} {bansCount}");
                    if (Config.AdminPlugin == 1 || Config.AdminPlugin == 3)
                    {
                        admin.PrintToConsole($"{Localizer["chat.mutes"]} {mutesCount}");
                    }
                    if (Config.AdminPlugin == 3)
                    {
                        admin.PrintToConsole($"{Localizer["chat.gags"]} {gagsCount}");
                    }
                    admin.PrintToChat($"{Localizer["chat.bottom"]}");

                }
                else if (printToCenter)
                {
                    AddTimer(2.0f, () =>
                    {
                        if(Config.AdminPlugin == 1)
                        {
                            admin.PrintToCenterHtml(Historyalertsimple);
                        }
                        else if(Config.AdminPlugin == 2)
                        {
                            admin.PrintToCenterHtml(Historyalertcs2admin);
                        }
                        else if (Config.AdminPlugin == 3)
                        {
                            admin.PrintToCenterHtml(Historyalertiks);
                        }
                        else
                        {
                            Logger.LogError($"AdminPluginType is invalid!");
                        }
                        
                    });
                }
            }

        }
        return HookResult.Continue;
    }
}