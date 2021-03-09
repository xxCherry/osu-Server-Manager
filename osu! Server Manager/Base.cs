using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Newtonsoft.Json;
using Terminal.Gui;

namespace osu__Server_Manager
{
    [SupportedOSPlatform("windows")]
    public class Base
    {
        private static string _osuPath;
        private static string _userConfigPath;
        
        public static void Main(string[] args)
        {
            var consoleHandle = NativeMethods.GetConsoleWindow();
            var systemMenu = NativeMethods.GetSystemMenu(consoleHandle, false);

            NativeMethods.DeleteMenu(systemMenu, NativeMethods.SC_MINIMIZE, NativeMethods.MF_BYCOMMAND);
            NativeMethods.DeleteMenu(systemMenu, NativeMethods.SC_MAXIMIZE, NativeMethods.MF_BYCOMMAND);
            NativeMethods.DeleteMenu(systemMenu, NativeMethods.SC_SIZE, NativeMethods.MF_BYCOMMAND);

            Console.Title = "osu!ServerManager";

            Console.SetWindowSize(60, 10);

            Config.TryCreate();
            Config.LoadConfig();
            Initialize();
        }

        public static void Initialize()
        {
            _osuPath = Path.GetDirectoryName(ReadOsuPath());
            _userConfigPath = Path.Combine(_osuPath, $"osu!.{Environment.UserName}.cfg");

            DrawGUI();
        }

        public static void DrawGUI()
        {
            Application.Init();

            var top = Application.Top;

            var win = new Window("osu!manager")
            {
                X = 0,
                Y = 0,

                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var serverItems = new List<MenuItem>();

            if (Config.Servers.Count == 0)
            {
                Config.Servers.Add(new ServerInfo("Astellia", "astellia.club"));
                Config.Servers.Add(new ServerInfo("Kurikku", "kurikku.pw"));
                Config.Servers.Add(new ServerInfo("Gatari", "osu.gatari.pw"));
                Config.Servers.Add(new ServerInfo("Ainu", "ainu.pw"));
                Config.Servers.Add(new ServerInfo("Ripple", "ripple.moe"));

                Config.SetValue("Servers", JsonConvert.SerializeObject(Config.Servers));
            }

            // Populate server list
            foreach (var server in Config.Servers)
            {
                serverItems.Add(new (server.Name, "", () =>
                {
                    SetCredentialServer(server.EndPoint);
                    Process.Start(Path.Combine(_osuPath, "osu!.exe"), $"-devserver {server.EndPoint}");
                }));
            }

            var menu = new MenuBar(new MenuBarItem[] 
            {
                new MenuBarItem ("Servers", serverItems.ToArray())
                {
                    Action = () =>
                    {
                        var menuItemsLength = Config.Servers.Count;
                        var newHeight = 10 + menuItemsLength;

                        Console.SetWindowSize(60, 10 + newHeight);
                    }
                }
            });

            // Set height of frame
            var menuItems = menu.Menus[0].Children;
            var menuItemsLength = menuItems.Length;
            var newHeight = 20 + menuItemsLength;

            win.Height = newHeight;

            top.Add(win);

            var serverName = new Label("Server Name: ") { X = 2, Y = 0 };
            var serverEndPoint = new Label("Server Endpoint: ")
            {
                X = Pos.Left(serverName),
                Y = Pos.Top(serverName) + 2
            };
            var serverNameText = new TextField("")
            {
                X = Pos.Right(serverEndPoint),
                Y = Pos.Top(serverName),
                Width = 40
            };
            var serverEndPointText = new TextField("")
            {
                X = Pos.Left(serverNameText),
                Y = Pos.Top(serverEndPoint),
                Width = Dim.Width(serverNameText)
            };

            var createButton = new Button(15, 4, "Create!");

            createButton.Clicked += () =>
            {
                var name = serverNameText.Text;
                var endPoint = serverEndPointText.Text;

                if (name.IsEmpty || endPoint.IsEmpty)
                {
                    MessageBox.Query("Error", "Name or endpoint is empty.", "Ok");
                    return;
                }

                if (Config.Servers.Any(x => x.Name == name))
                {
                    MessageBox.Query("Error", "Server already exist.", "Ok");
                    return;
                }

                Config.Servers.Add(new ServerInfo(name.ToString(), endPoint.ToString()));

                menu.Menus[0].Children = menu.Menus[0].Children.Append(new(name, "", () =>
                {
                    SetCredentialServer(endPoint.ToString());
                    Process.Start(Path.Combine(_osuPath, "osu!.exe"), $"-devserver {endPoint}");
                })).ToArray();

                Config.SetValue("Servers", JsonConvert.SerializeObject(Config.Servers));

                // Update height of frame
                var menuItems = menu.Menus[0].Children;
                var menuItemsLength = menuItems.Length;
                var newHeight = 20 + menuItemsLength;

                win.Height = newHeight;
            };

            var frame = new FrameView(new Rect(1, 1, 50, 7), "Server creation")
            {
                { 
                    serverName, serverEndPoint, serverNameText, serverEndPointText, createButton 
                }
            };

            win.Add(frame);

            top.Add(menu);

            menu.KeyPress += (m) => {
                var menuItems = menu.Menus[0].Children;
                var menuItemsLength = menuItems.Length;
                var newHeight = 10 + menuItemsLength;

                Console.SetWindowSize(60, 10);
            };

            Application.Run();
        }

        public static string ReadOsuPath()
        {
            using var registry = Registry.ClassesRoot.OpenSubKey("osu\\DefaultIcon");

            if (registry != null)
            {
                var osuPath = registry.GetValue(null).ToString();

                return osuPath[1..^3];
            }

            throw new Exception("Can't find osu! path, make sure you run osu! at least once.");
        }

        /// <summary>
        /// Sets the osu!'s credential server, so login credentials won't be flushed
        /// </summary>
        /// <param name="endpoint"></param>
        public static void SetCredentialServer(string endpoint)
        {
            var contents = File.ReadAllText(_userConfigPath);
            var lines = contents.Split('\n');

            foreach (var line in lines)
            {
                var kvp = line.Split("=");

                if (kvp[0] == "CredentialEndpoint")
                {
                    contents = contents.Replace("CredintialEndpoint=" + kvp[1], "CredintialEndpoint=" + endpoint);
                    break;
                }
            }

            File.WriteAllText(_userConfigPath, contents);
        }
    }
}
