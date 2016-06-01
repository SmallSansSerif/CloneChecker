/*
 * Original plugin by DaGamesta.
*/

namespace MoreAdminCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Terraria;
    using TerrariaApi.Server;
    using TShockAPI;

    [ApiVersion(1, 23)]
    public class MoreAdminCommands : TerrariaPlugin
    {
        public static bool informOnConnect = true;
        public static Dictionary<string, List<string>> IPuserNameList = new Dictionary<string, List<string>>();

        public MoreAdminCommands(Main game) : base(game)
        {
            Order = -1;
        }

        public void add(string IP, string userName)
        {
            if (IPuserNameList.Keys.Contains<string>(IP))
            {
                if (!IPuserNameList[IP].Contains(userName))
                {
                    IPuserNameList[IP].Add(userName);
                    this.save();
                }
            }
            else
            {
                List<string> list = new List<string> {
                    userName
                };
                IPuserNameList.Add(IP, list);
                this.save();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            }
            base.Dispose(disposing);
        }

        private static char GetEscape(char c)
        {
            switch (c)
            {
                case '"':
                    return '"';

                case '\\':
                    return '\\';

                case 't':
                    return '\t';
            }
            return c;
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);

        }

        private static bool IsWhiteSpace(char c)
        {
            if ((c != ' ') && (c != '\t'))
            {
                return (c == '\n');
            }
            return true;
        }

        public void load()
        {
            if (File.Exists(@"tshock\CloneChecker.log"))
            {
                string[] source = File.ReadAllLines(@"tshock\CloneChecker.log");
                IPuserNameList = new Dictionary<string, List<string>>();
                for (int i = 0; i < source.Count<string>(); i++)
                {
                    if (source[i] != "")
                    {
                        string key = source[i];
                        List<string> list = new List<string>();
                        i++;
                        while ((i < source.Count<string>()) && (source[i] != ""))
                        {
                            list.Add(source[i]);
                            i++;
                        }
                        IPuserNameList.Add(key, list);
                    }
                }
            }
        }

        public void loadConfig()
        {
            if (File.Exists(@"tshock\CloneChecker.config"))
            {
                string[] source = File.ReadAllLines(@"tshock\CloneChecker.config");
                for (int i = 0; i < source.Count<string>(); i++)
                {
                    if (source[i].ToLower().StartsWith("showaliasesonjoinbydefault:"))
                    {
                        if (source[i].ToLower().Contains("true"))
                        {
                            informOnConnect = true;
                        }
                        else
                        {
                            informOnConnect = false;
                        }
                    }
                }
            }
            else
            {
                string[] contents = new string[] { "showAliasesOnJoinByDefault:True" };
                File.WriteAllLines(@"tshock\CloneChecker.config", contents);
            }
        }

        public void OnInitialize(EventArgs args)
        {
            List<string> permissions = new List<string> { "clonecheck", "clonechecknotify" };
            TShock.Groups.AddPermissions("trustedadmin", permissions);
            Commands.ChatCommands.Add(new Command("clonecheck", WhoIs, "whois"));
            Commands.ChatCommands.Add(new Command("clonechecknotify", ToggleCloneNotify, "toggleclonenotify"));
            this.loadConfig();
            this.load();
        }

        public void OnJoin(JoinEventArgs e)
        {
            this.add(TShock.Players[e.Who].IP, TShock.Players[e.Who].Name);
            string msg = TShock.Players[e.Who].Name + " also goes by: ";
            if (informOnConnect && IPuserNameList.Keys.Contains<string>(TShock.Players[e.Who].IP))
            {
                if ((IPuserNameList[TShock.Players[e.Who].IP].Count > 1) || ((IPuserNameList[TShock.Players[e.Who].IP].Count > 0) && (IPuserNameList[TShock.Players[e.Who].IP][0] != TShock.Players[e.Who].Name)))
                {
                    foreach (string str2 in IPuserNameList[TShock.Players[e.Who].IP])
                    {
                        if (str2 != TShock.Players[e.Who].Name)
                        {
                            msg = msg + str2 + ", ";
                        }
                    }
                    if (msg.EndsWith(", "))
                    {
                        msg.Remove(msg.Length - 2);
                    }
                }
                else
                {
                    msg = TShock.Players[e.Who].Name + " is not a clone.";
                }
                foreach (TSPlayer player in TShock.Players)
                {
                    try
                    {
                        if (((player != null) && player.Active) && player.Group.HasPermission("clonechecknotify"))
                        {
                            player.SendMessage(msg, Color.Yellow);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static List<string> parseParameters(string str)
        {
            List<string> list = new List<string>();
            string item = "";
            bool flag = false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (flag)
                {
                    switch (c)
                    {
                        case '\\':
                            if ((i + 1) >= str.Length)
                            {
                                goto Label_00D9;
                            }
                            c = GetEscape(str[++i]);
                            break;

                        case '"':
                        {
                            list.Add(item);
                            item = "";
                            flag = false;
                            continue;
                        }
                    }
                    item = item + c;
                }
                else if (IsWhiteSpace(c))
                {
                    if (item.Length > 0)
                    {
                        list.Add(item.ToString());
                        item = "";
                    }
                }
                else if (c == '"')
                {
                    if (item.Length > 0)
                    {
                        list.Add(item.ToString());
                        item = "";
                    }
                    flag = true;
                }
                else
                {
                    item = item + c;
                }
            }
        Label_00D9:
            if (item.Length > 0)
            {
                list.Add(item.ToString());
            }
            return list;
        }

        public void save()
        {
            List<string> list = new List<string>();
            foreach (KeyValuePair<string, List<string>> pair in IPuserNameList)
            {
                list.Add(pair.Key);
                foreach (string str in pair.Value)
                {
                    list.Add(str);
                }
                list.Add("");
            }
            File.WriteAllLines(@"tshock\CloneChecker.log", list.ToArray());
        }

        public void ToggleCloneNotify(CommandArgs args)
        {
            informOnConnect = !informOnConnect;
            args.Player.SendSuccessMessage("Alias notifications on join have been turned " + (informOnConnect ? "on." : "off."));
        }

        public void WhoIs(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                if (IPuserNameList.Keys.Contains<string>(args.Parameters[0]))
                {
                    string msg = "Has gone by: ";
                    for (int i = 0; i < IPuserNameList[args.Parameters[0]].Count; i++)
                    {
                        if (i < (IPuserNameList[args.Parameters[0]].Count - 1))
                        {
                            msg = msg + IPuserNameList[args.Parameters[0]][i] + ", ";
                        }
                        else
                        {
                            msg = msg + IPuserNameList[args.Parameters[0]][i] + ".";
                        }
                    }
                    if (IPuserNameList[args.Parameters[0]].Count > 0)
                    {
                        args.Player.SendMessage(msg, Color.Yellow);
                    }
                    else
                    {
                        args.Player.SendErrorMessage("No usernames logged under that IP address");
                    }
                }
                else
                {
                    List<TSPlayer> list = TShock.Utils.FindPlayer(args.Parameters[0]);
                    if (list.Count <= 0)
                    {
                        args.Player.SendErrorMessage("No players matched that name.");
                    }
                    else if (list.Count > 1)
                    {
                        args.Player.SendErrorMessage(list.Count.ToString() + " players matched.");
                    }
                    else
                    {
                        string str2 = "Has gone by: ";
                        string iP = list[0].IP;
                        if (IPuserNameList.Keys.Contains<string>(iP))
                        {
                            for (int j = 0; j < IPuserNameList[iP].Count; j++)
                            {
                                if (j < (IPuserNameList[iP].Count - 1))
                                {
                                    str2 = str2 + IPuserNameList[iP][j] + ", ";
                                }
                                else
                                {
                                    str2 = str2 + IPuserNameList[iP][j] + ".";
                                }
                            }
                            args.Player.SendMessage(str2, Color.Yellow);
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Error occured, player not found in the database, attempting to add.");
                            this.add(list[0].IP, list[0].Name);
                        }
                    }
                }
            }
            else
            {
                args.Player.SendErrorMessage("No arguments were given.  Proper syntax: /whois IP/Username");
            }
        }

        public override string Author
        {
            get
            {
                return "Zaicon";
            }
        }

        public override string Description
        {
            get
            {
                return "Tracks each player's character names.";
            }
        }

        public override string Name
        {
            get
            {
                return "CloneChecker";
            }
        }

        public override System.Version Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }
    }
}

