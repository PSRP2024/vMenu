using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using NativeUI;
using static vMenuShared.ConfigManager;

namespace vMenuClient
{
    /// <summary>
    /// This class manages all things that need to be done every tick based on
    /// checkboxes/things changing in any of the (sub) menus.
    /// </summary>
    class FunctionsController : BaseScript
    {
        // Variables
        private CommonFunctions cf = MainMenu.Cf;

        private int LastVehicle = 0;
        private bool SwitchedVehicle = false;
        private Dictionary<int, string> playerList = new Dictionary<int, string>();
        private List<int> deadPlayers = new List<int>();
        private UIMenu lastOpenMenu = null;
        private float cameraRotationHeading = 0f;

        // show location variables
        private Vector3 currentPos = Game.PlayerPed.Position;
        private Vector3 nodePos = Game.PlayerPed.Position;
        private bool node = false;
        private float heading = 0f;
        private float safeZoneSizeX = (1 / GetSafeZoneSize() / 3.0f) - 0.358f;
        private float safeZoneSizeY = (1 / GetSafeZoneSize() / 3.6f) - 0.27f;
        private uint crossing = 1;
        private string crossingName = "";
        private string suffix = "";
        private bool wasMenuJustOpen = false;
        private PlayerList blipsPlayerList = new PlayerList();
        private List<int> waypointPlayerIdsToRemove = new List<int>();
        private int voiceTimer = 0;
        private int voiceCycle = 1;
        private const float voiceIndicatorWidth = 0.02f;
        private const float voiceIndicatorHeight = 0.041f;
        private const float voiceIndicatorMutedWidth = voiceIndicatorWidth + 0.0021f;
        private const string clothingAnimationDecor = "clothing_animation_type";
        private bool clothingAnimationReverse = false;
        private float clothingOpacity = 1f;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FunctionsController()
        {
            // Load the initial playerlist.
            foreach (Player p in new PlayerList())
            {
                playerList.Add(p.Handle, p.Name);
            }

            // Add all tick events.
            Tick += GeneralTasks;
            Tick += PlayerOptions;
            Tick += VehicleOptions;
            Tick += MoreVehicleOptions;
            Tick += VoiceChat;
            Tick += TimeOptions;
            Tick += _WeatherOptions;
            Tick += WeaponOptions;
            Tick += OnlinePlayersTasks;

            Tick += MiscSettings;
            Tick += DeathNotifications;
            Tick += JoinQuitNotifications;
            Tick += UpdateLocation;
            Tick += ManagePlayerAppearanceCamera;
            Tick += PlayerBlipsControl;
            Tick += RestorePlayerAfterBeingDead;
            Tick += PlayerClothingAnimationsController;
        }

        /// Task related
        #region General Tasks
        /// <summary>
        /// All general tasks that run every game tick (and are not (sub)menu specific).
        /// </summary>
        /// <returns></returns>
        private async Task GeneralTasks()
        {
            //Debug.Write("Size: " + playerList.Count + "\n");
            // CommonFunctions is required, if it doesn't exist then we won't execute the checks.
            if (cf != null)
            {
                // Check if the player has switched to a new vehicle.
                if (IsPedInAnyVehicle(PlayerPedId(), true)) // added this for improved performance.
                {
                    var tmpVehicle = cf.GetVehicle();
                    if (DoesEntityExist(tmpVehicle) && tmpVehicle != LastVehicle)
                    {
                        // Set the last vehicle to the new vehicle entity.
                        LastVehicle = tmpVehicle;
                        SwitchedVehicle = true;
                    }
                }

                if (!MainMenu.DontOpenMenus && MainMenu.Mp.IsAnyMenuOpen())
                {
                    lastOpenMenu = cf.GetOpenMenu();
                }
                // If any on-screen keyboard is visible, close any open menus and disable any menu from opening.
                if (UpdateOnscreenKeyboard() == 0 && (MainMenu.Mp.IsAnyMenuOpen() || wasMenuJustOpen)) // still editing aka the input box is visible.
                {
                    MainMenu.DontOpenMenus = true;
                    MainMenu.DisableControls = true;
                    wasMenuJustOpen = true; // added for extra check to make sure only vMenu gets re-opened if vMenu was already open.
                }
                // Otherwise, check if the "DontOpenMenus" option is (still) true.
                else
                {
                    if (MainMenu.DontOpenMenus)
                    {
                        // Allow menus from being displayed.
                        MainMenu.DontOpenMenus = false;

                        // Check if the previous menu isn't null.
                        if (lastOpenMenu != null && wasMenuJustOpen)
                        {
                            // Re-open the last menu.
                            lastOpenMenu.Visible = true;
                            // Set the last menu to null.
                            lastOpenMenu = null;
                            wasMenuJustOpen = false; // reset the justOpen state.
                        }

                        // Wait 5 ticks before allowing the menu to be controlled, to prevent accidental interactions when the menu JUST re-appeared.
                        await Delay(5);
                        MainMenu.DisableControls = false;


                    }
                }
                await Delay(0);
            }
            else
            {
                await Delay(0);
            }
        }
        #endregion
        #region Player Options Tasks
        /// <summary>
        /// Run all tasks for the Player Options menu.
        /// </summary>
        /// <returns></returns>
        private async Task PlayerOptions()
        {
            // perms
            bool ignorePlayerAllowed = cf.IsAllowed(Permission.POIgnored);
            bool godmodeAllowed = cf.IsAllowed(Permission.POGod);
            bool playerInvisible = cf.IsAllowed(Permission.POInvisible);
            bool noRagdollAllowed = cf.IsAllowed(Permission.PONoRagdoll);
            bool vehicleGodModeAllowed = cf.IsAllowed(Permission.VOGod);
            bool playerFrozenAllowed = cf.IsAllowed(Permission.POFreeze);




            // Player options. Only run player options if the player options menu has actually been created.
            if (MainMenu.PlayerOptionsMenu != null && cf.IsAllowed(Permission.POMenu))
            {
                // Manage Player God Mode
                SetEntityInvincible(PlayerPedId(), MainMenu.PlayerOptionsMenu.PlayerGodMode && godmodeAllowed);

                // Manage invisibility.
                SetEntityVisible(PlayerPedId(), (!MainMenu.PlayerOptionsMenu.PlayerInvisible && playerInvisible) ||
                    (!playerInvisible), false);

                // Manage Stamina
                if (MainMenu.PlayerOptionsMenu.PlayerStamina && cf.IsAllowed(Permission.POUnlimitedStamina))
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 100, true);
                }
                else
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 0, true);
                }
                // Manage other stats.
                StatSetInt((uint)GetHashKey("MP0_STRENGTH"), 100, true);
                StatSetInt((uint)GetHashKey("MP0_LUNG_CAPACITY"), 80, true); // reduced because it was over powered
                StatSetInt((uint)GetHashKey("MP0_WHEELIE_ABILITY"), 100, true);
                StatSetInt((uint)GetHashKey("MP0_FLYING_ABILITY"), 100, true);
                StatSetInt((uint)GetHashKey("MP0_SHOOTING_ABILITY"), 50, true); // reduced because it was over powered
                StatSetInt((uint)GetHashKey("MP0_STEALTH_ABILITY"), 100, true);

                // Manage Super jump.
                if (MainMenu.PlayerOptionsMenu.PlayerSuperJump && cf.IsAllowed(Permission.POSuperjump))
                {
                    SetSuperJumpThisFrame(PlayerId());
                }

                // Manage PlayerNoRagdoll
                SetPedCanRagdoll(PlayerPedId(), (!MainMenu.PlayerOptionsMenu.PlayerNoRagdoll && noRagdollAllowed) ||
                    (!noRagdollAllowed));


                // Fall off bike / dragged out of car.
                if (MainMenu.VehicleOptionsMenu != null)
                {
                    SetPedCanBeKnockedOffVehicle(PlayerPedId(), (((MainMenu.PlayerOptionsMenu.PlayerNoRagdoll && noRagdollAllowed)
                        || (MainMenu.VehicleOptionsMenu.VehicleGodMode) && vehicleGodModeAllowed) ? 1 : 0));

                    SetPedCanBeDraggedOut(PlayerPedId(), ((MainMenu.PlayerOptionsMenu.PlayerIsIgnored && ignorePlayerAllowed) ||
                        (MainMenu.VehicleOptionsMenu.VehicleGodMode && vehicleGodModeAllowed) ||
                        (MainMenu.PlayerOptionsMenu.PlayerGodMode && godmodeAllowed)));

                    SetPedCanBeShotInVehicle(PlayerPedId(), !((MainMenu.PlayerOptionsMenu.PlayerGodMode && godmodeAllowed) ||
                        (MainMenu.VehicleOptionsMenu.VehicleGodMode && vehicleGodModeAllowed)));
                }
                else
                {
                    SetPedCanBeKnockedOffVehicle(PlayerPedId(), ((MainMenu.PlayerOptionsMenu.PlayerNoRagdoll && noRagdollAllowed) ? 1 : 0));
                    SetPedCanBeDraggedOut(PlayerPedId(), (MainMenu.PlayerOptionsMenu.PlayerIsIgnored && ignorePlayerAllowed));
                    SetPedCanBeShotInVehicle(PlayerPedId(), !(MainMenu.PlayerOptionsMenu.PlayerGodMode && godmodeAllowed));
                }

                // Manage never wanted.
                if (MainMenu.PlayerOptionsMenu.PlayerNeverWanted && GetPlayerWantedLevel(PlayerId()) > 0 && cf.IsAllowed(Permission.PONeverWanted))
                {
                    ClearPlayerWantedLevel(PlayerId());
                }

                // Manage player is ignored by everyone.
                SetEveryoneIgnorePlayer(PlayerId(), MainMenu.PlayerOptionsMenu.PlayerIsIgnored && ignorePlayerAllowed);

                SetPoliceIgnorePlayer(PlayerId(), MainMenu.PlayerOptionsMenu.PlayerIsIgnored && ignorePlayerAllowed);

                SetPlayerCanBeHassledByGangs(PlayerId(), !((MainMenu.PlayerOptionsMenu.PlayerIsIgnored && ignorePlayerAllowed) ||
                    (MainMenu.PlayerOptionsMenu.PlayerGodMode && godmodeAllowed)));

                if (MainMenu.NoClipMenu != null)
                {
                    if (!MainMenu.NoClipEnabled)
                    {
                        // Manage player frozen.
                        if (MainMenu.PlayerOptionsMenu.PlayerFrozen && playerFrozenAllowed)
                            FreezeEntityPosition(PlayerPedId(), true);
                    }
                }
                else
                {
                    // Manage player frozen.
                    if (MainMenu.PlayerOptionsMenu.PlayerFrozen && playerFrozenAllowed)
                        FreezeEntityPosition(PlayerPedId(), true);
                }


                if (MainMenu.Cf.driveToWpTaskActive && !Game.IsWaypointActive)
                {
                    ClearPedTasks(PlayerPedId());
                    Notify.Custom("Destination reached, the car will now stop driving!");
                    MainMenu.Cf.driveToWpTaskActive = false;
                }
            }
            else
            {
                await Delay(0);
            }
        }
        #endregion
        #region Vehicle Options Tasks
        /// <summary>
        /// Manage all vehicle related tasks.
        /// </summary>
        /// <returns></returns>
        private async Task VehicleOptions()
        {
            // Vehicle options. Only run vehicle options if the vehicle options menu has actually been created.
            if (MainMenu.VehicleOptionsMenu != null && cf.IsAllowed(Permission.VOMenu))
            {
                // When the player is in a valid vehicle:
                if (IsPedInAnyVehicle(PlayerPedId(), true))
                {
                    int veh = cf.GetVehicle();
                    if (DoesEntityExist(veh))
                    {
                        // Create a new vehicle object.
                        Vehicle vehicle = new Vehicle(cf.GetVehicle());

                        // God mode
                        bool god = MainMenu.VehicleOptionsMenu.VehicleGodMode && cf.IsAllowed(Permission.VOGod);
                        vehicle.CanBeVisiblyDamaged = !god;
                        vehicle.CanEngineDegrade = !god;
                        vehicle.CanTiresBurst = !god;
                        vehicle.CanWheelsBreak = !god;
                        vehicle.IsAxlesStrong = god;
                        vehicle.IsBulletProof = god;
                        vehicle.IsCollisionProof = god;
                        vehicle.IsExplosionProof = god;
                        vehicle.IsFireProof = god;
                        vehicle.IsInvincible = god;
                        vehicle.IsMeleeProof = god;
                        foreach (VehicleDoor vd in vehicle.Doors.GetAll())
                        {
                            vd.CanBeBroken = !god;
                        }
                        bool specialgod = MainMenu.VehicleOptionsMenu.VehicleSpecialGodMode && cf.IsAllowed(Permission.VOSpecialGod);
                        if (specialgod && vehicle.EngineHealth < 1000)
                        {
                            vehicle.Repair(); // repair vehicle if special god mode is on and the vehicle is not full health.
                        }

                        // Freeze Vehicle Position (if enabled).
                        if (MainMenu.VehicleOptionsMenu.VehicleFrozen && cf.IsAllowed(Permission.VOFreeze))
                        {
                            FreezeEntityPosition(vehicle.Handle, true);
                        }

                        await Delay(0);
                        // If the torque multiplier is enabled and the player is allowed to use it.
                        if (MainMenu.VehicleOptionsMenu.VehicleTorqueMultiplier && cf.IsAllowed(Permission.VOTorqueMultiplier))
                        {
                            // Set the torque multiplier to the selected value by the player.
                            // no need for an "else" to reset this value, because when it's not called every frame, nothing happens.
                            SetVehicleEngineTorqueMultiplier(vehicle.Handle, MainMenu.VehicleOptionsMenu.VehicleTorqueMultiplierAmount);
                        }
                        // If the player has switched to a new vehicle, and the vehicle engine power multiplier is turned on. Set the new value.
                        if (SwitchedVehicle)
                        {
                            // Only needs to be set once.
                            SwitchedVehicle = false;

                            // Vehicle engine power multiplier. Enable it once the player switched vehicles.
                            // Only do this if the option is enabled AND the player has permissions for it.
                            if (MainMenu.VehicleOptionsMenu.VehiclePowerMultiplier && cf.IsAllowed(Permission.VOPowerMultiplier))
                            {
                                SetVehicleEnginePowerMultiplier(vehicle.Handle, MainMenu.VehicleOptionsMenu.VehiclePowerMultiplierAmount);
                            }
                            // If the player switched vehicles and the option is turned off or the player has no permissions for it
                            // Then reset the power multiplier ONCE.
                            else
                            {
                                SetVehicleEnginePowerMultiplier(vehicle.Handle, 1f);
                            }

                            // disable this if els compatibility is turned on.
                            if (!GetSettingsBool(Setting.vmenu_use_els_compatibility_mode))
                            {
                                // No Siren Toggle
                                vehicle.IsSirenSilent = MainMenu.VehicleOptionsMenu.VehicleNoSiren && cf.IsAllowed(Permission.VONoSiren);
                            }

                        }

                        // Manage "no helmet"
                        var ped = new Ped(PlayerPedId());
                        // If the no helmet feature is turned on, disalbe "ped can wear helmet"
                        if (MainMenu.VehicleOptionsMenu.VehicleNoBikeHelemet && cf.IsAllowed(Permission.VONoHelmet))
                        {
                            ped.CanWearHelmet = false;
                        }
                        // otherwise, allow helmets.
                        else if (!MainMenu.VehicleOptionsMenu.VehicleNoBikeHelemet || !cf.IsAllowed(Permission.VONoHelmet))
                        {
                            ped.CanWearHelmet = true;
                        }
                        // If the player is still wearing a helmet, even if the option is set to: no helmet, then remove the helmet.
                        if (ped.IsWearingHelmet && MainMenu.VehicleOptionsMenu.VehicleNoBikeHelemet && cf.IsAllowed(Permission.VONoHelmet))
                        {
                            ped.RemoveHelmet(true);
                        }

                        await Delay(0);
                    }
                }
                // When the player is not inside a vehicle:
                else
                {
                    UIMenu[] vehicleSubmenus = new UIMenu[6];
                    vehicleSubmenus[0] = MainMenu.VehicleOptionsMenu.VehicleModMenu;
                    vehicleSubmenus[1] = MainMenu.VehicleOptionsMenu.VehicleLiveriesMenu;
                    vehicleSubmenus[2] = MainMenu.VehicleOptionsMenu.VehicleColorsMenu;
                    vehicleSubmenus[3] = MainMenu.VehicleOptionsMenu.VehicleDoorsMenu;
                    vehicleSubmenus[4] = MainMenu.VehicleOptionsMenu.VehicleWindowsMenu;
                    vehicleSubmenus[5] = MainMenu.VehicleOptionsMenu.VehicleComponentsMenu;
                    foreach (UIMenu m in vehicleSubmenus)
                    {
                        if (m.Visible)
                        {
                            MainMenu.VehicleOptionsMenu.GetMenu().Visible = true;
                            m.Visible = false;
                            Notify.Error(CommonErrors.NoVehicle, placeholderValue: "to access this menu");
                        }
                    }
                }

                await Delay(1);

                // Manage vehicle engine always on.
                if ((MainMenu.VehicleOptionsMenu.VehicleEngineAlwaysOn && DoesEntityExist(cf.GetVehicle(lastVehicle: true)) &&
                    !IsPedInAnyVehicle(PlayerPedId(), false)) && (cf.IsAllowed(Permission.VOEngineAlwaysOn)))
                {
                    await Delay(100);
                    SetVehicleEngineOn(cf.GetVehicle(lastVehicle: true), true, true, true);
                }

            }
            else
            {
                await Delay(1);
            }
        }
        private async Task MoreVehicleOptions()
        {
            if (MainMenu.VehicleOptionsMenu != null && IsPedInAnyVehicle(PlayerPedId(), true) && MainMenu.VehicleOptionsMenu.FlashHighbeamsOnHonk && cf.IsAllowed(Permission.VOFlashHighbeamsOnHonk))
            {
                var veh = cf.GetVehicle();
                if (DoesEntityExist(veh))
                {
                    Vehicle vehicle = new Vehicle(veh);
                    if (vehicle.Driver == Game.PlayerPed && vehicle.IsEngineRunning && !IsPauseMenuActive())
                    {
                        // turn on high beams when honking.
                        if (Game.IsControlPressed(0, Control.VehicleHorn))
                        {
                            vehicle.AreHighBeamsOn = true;
                        }
                        // turn high beams back off when just stopped honking.
                        if (Game.IsControlJustReleased(0, Control.VehicleHorn))
                        {
                            vehicle.AreHighBeamsOn = false;
                        }
                    }
                    else
                    {
                        await Delay(1);
                    }
                }
                else
                {
                    await Delay(1);
                }
            }
            else
            {
                await Delay(1);
            }

        }
        #endregion
        #region Weather Options
        private async Task _WeatherOptions()
        {
            await Delay(1000);
            if (MainMenu.WeatherOptionsMenu != null && cf.IsAllowed(Permission.WOMenu) && GetSettingsBool(Setting.vmenu_enable_weather_sync))
            {
                if (MainMenu.WeatherOptionsMenu.GetMenu().Visible)
                {
                    MainMenu.WeatherOptionsMenu.GetMenu().MenuItems.ForEach(mi => { if (mi.GetType() != typeof(UIMenuCheckboxItem)) mi.SetRightBadge(UIMenuItem.BadgeStyle.None); });
                    var item = WeatherOptions.weatherHashMenuIndex[GetNextWeatherTypeHashName().ToString()];
                    item.SetRightBadge(UIMenuItem.BadgeStyle.Tick);
                    if (cf.IsAllowed(Permission.WODynamic))
                    {
                        UIMenuCheckboxItem dynWeatherTmp = (UIMenuCheckboxItem)MainMenu.WeatherOptionsMenu.GetMenu().MenuItems[0];
                        dynWeatherTmp.Checked = EventManager.dynamicWeather;
                        if (cf.IsAllowed(Permission.WOBlackout))
                        {
                            UIMenuCheckboxItem blackoutTmp = (UIMenuCheckboxItem)MainMenu.WeatherOptionsMenu.GetMenu().MenuItems[1];
                            blackoutTmp.Checked = EventManager.blackoutMode;
                        }
                    }
                    else if (cf.IsAllowed(Permission.WOBlackout))
                    {
                        UIMenuCheckboxItem blackoutTmp = (UIMenuCheckboxItem)MainMenu.WeatherOptionsMenu.GetMenu().MenuItems[0];
                        blackoutTmp.Checked = EventManager.blackoutMode;
                    }


                }
            }
        }
        #endregion
        #region Misc Settings Menu Tasks
        /// <summary>
        /// Run all tasks that need to be handeled for the Misc Settings Menu.
        /// </summary>
        /// <returns></returns>
        private async Task MiscSettings()
        {
            if (MainMenu.MiscSettingsMenu != null /*&& cf.IsAllowed(Permission.MSMenu)*/)
            {
                #region Misc Settings
                // Show speedometer km/h
                if (MainMenu.MiscSettingsMenu.ShowSpeedoKmh)
                {
                    ShowSpeedKmh();
                }

                // Show speedometer mph
                if (MainMenu.MiscSettingsMenu.ShowSpeedoMph)
                {
                    ShowSpeedMph();
                }

                // Show coordinates.
                if (MainMenu.MiscSettingsMenu.ShowCoordinates && cf.IsAllowed(Permission.MSShowCoordinates))
                {
                    var pos = GetEntityCoords(PlayerPedId(), true);
                    cf.DrawTextOnScreen($"~r~X~t~: {Math.Round(pos.X, 2)}" +
                        $"~n~~r~Y~t~: {Math.Round(pos.Y, 2)}" +
                        $"~n~~r~Z~t~: {Math.Round(pos.Z, 2)}" +
                        $"~n~~r~Heading~t~: {Math.Round(GetEntityHeading(PlayerPedId()), 1)}", 0.45f, 0f, 0.38f, Alignment.Left, (int)Font.ChaletLondon);
                }

                //// Hide hud.
                //if (MainMenu.MiscSettingsMenu.HideHud)
                //{
                //    //HideHudAndRadarThisFrame();
                //    DisplayHud(false);
                //}

                // Hide radar.
                if (MainMenu.MiscSettingsMenu.HideRadar)
                {
                    DisplayRadar(false);
                }
                // Show radar (or hide it if the user disabled it in pausemenu > settings > display > show radar.
                else if (!IsRadarHidden()) // this should allow other resources to still disable it
                {
                    DisplayRadar(IsRadarPreferenceSwitchedOn());
                }
                #endregion

                #region Show Location
                // Show location & time.
                if (MainMenu.MiscSettingsMenu.ShowLocation && cf.IsAllowed(Permission.MSShowLocation))
                {
                    ShowLocation();
                }
                #endregion

                #region camera angle locking
                if (MainMenu.MiscSettingsMenu.LockCameraY)
                {
                    SetGameplayCamRelativePitch(0f, 0f);
                }
                if (MainMenu.MiscSettingsMenu.LockCameraX)
                {
                    if (Game.IsControlPressed(0, Control.LookLeftOnly))
                    {
                        cameraRotationHeading++;
                    }
                    else if (Game.IsControlPressed(0, Control.LookRightOnly))
                    {
                        cameraRotationHeading--;
                    }
                    SetGameplayCamRelativeHeading(cameraRotationHeading);
                }
                #endregion
            }
            else
            {
                await Delay(0);
            }
        }

        #region Join / Quit notifications
        /// <summary>
        /// Runs join/quit notification checks.
        /// </summary>
        /// <returns></returns>
        private async Task JoinQuitNotifications()
        {
            if (MainMenu.MiscSettingsMenu != null)
            {
                // Join/Quit notifications
                if (MainMenu.MiscSettingsMenu.JoinQuitNotifications && cf.IsAllowed(Permission.MSJoinQuitNotifs))
                {
                    PlayerList plist = new PlayerList();
                    Dictionary<int, string> pl = new Dictionary<int, string>();
                    foreach (Player p in plist)
                    {
                        pl.Add(p.Handle, p.Name);
                    }
                    await Delay(0);
                    // new player joined.
                    if (pl.Count > playerList.Count)
                    {
                        foreach (KeyValuePair<int, string> player in pl)
                        {
                            if (!playerList.Contains(player))
                            {
                                Notify.Custom($"~g~<C>{player.Value}</C>~s~ joined the server.");
                                await Delay(0);
                            }
                        }
                    }
                    // player left.
                    else if (pl.Count < playerList.Count)
                    {
                        foreach (KeyValuePair<int, string> player in playerList)
                        {
                            if (!pl.Contains(player))
                            {
                                Notify.Custom($"~r~<C>{player.Value}</C>~s~ left the server.");
                                await Delay(0);
                            }
                        }
                    }
                    playerList = pl;
                    await Delay(100);
                }
            }
        }
        #endregion

        #region Death Notifications
        /// <summary>
        /// Runs death notification checks.
        /// </summary>
        /// <returns></returns>
        private async Task DeathNotifications()
        {
            if (MainMenu.MiscSettingsMenu != null)
            {
                // Death notifications
                if (MainMenu.MiscSettingsMenu.DeathNotifications && cf.IsAllowed(Permission.MSDeathNotifs))
                {
                    PlayerList pl = new PlayerList();
                    var tmpiterator = 0;
                    foreach (Player p in pl)
                    {
                        tmpiterator++;
                        //if (tmpiterator % 5 == 0)
                        //{
                        await Delay(0);
                        //}
                        if (p.IsDead)
                        {
                            if (deadPlayers.Contains(p.Handle)) { return; }
                            var killer = p.Character.GetKiller();
                            if (killer != null)
                            {
                                if (killer.Handle != p.Character.Handle)
                                {
                                    if (killer.Exists())
                                    {
                                        foreach (Player playerKiller in pl)
                                        {
                                            if (playerKiller.Character.Handle == killer.Handle)
                                            {
                                                Notify.Custom($"~o~<C>{p.Name}</C> ~s~has been murdered by ~y~<C>{playerKiller.Name}</C>~s~.");
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Notify.Custom($"~o~<C>{p.Name}</C> ~s~has been murdered.");
                                    }
                                }
                                else
                                {
                                    Notify.Custom($"~o~<C>{p.Name}</C> ~s~committed suicide.");
                                }
                            }
                            else
                            {
                                Notify.Custom($"~o~<C>{p.Name}</C> ~s~died.");
                            }
                            deadPlayers.Add(p.Handle);
                        }
                        else
                        {
                            if (deadPlayers.Contains(p.Handle))
                            {
                                deadPlayers.Remove(p.Handle);
                            }
                        }
                    }
                    await Delay(50);
                }
            }
        }
        #endregion

        #region Update Location for location display
        /// <summary>
        /// Updates the location for location display.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateLocation()
        {
            if (MainMenu.MiscSettingsMenu != null)
            {
                if (MainMenu.MiscSettingsMenu.ShowLocation && cf.IsAllowed(Permission.MSShowLocation))
                {
                    // Get the current location.
                    currentPos = GetEntityCoords(PlayerPedId(), true);

                    // Get the nearest vehicle node.
                    nodePos = currentPos;
                    node = GetNthClosestVehicleNode(currentPos.X, currentPos.Y, currentPos.Z, 0, ref nodePos, 0, 0, 0);
                    heading = Game.PlayerPed.Heading;

                    // Get the safezone size for x and y to be able to move with the minimap.
                    safeZoneSizeX = (1 / GetSafeZoneSize() / 3.0f) - 0.358f;
                    safeZoneSizeY = (1 / GetSafeZoneSize() / 3.6f) - 0.27f;

                    // Get the cross road.
                    var p1 = (uint)1; // unused
                    crossing = (uint)1;
                    GetStreetNameAtCoord(currentPos.X, currentPos.Y, currentPos.Z, ref p1, ref crossing);
                    crossingName = GetStreetNameFromHashKey(crossing);

                    // Set the suffix for the road name to the corssing name, or to an empty string if there's no crossing.
                    suffix = (crossingName != "" && crossingName != "NULL" && crossingName != null) ? "~t~ / " + crossingName : "";

                    await Delay(200);
                }
                else
                {
                    await Delay(100);
                }
            }
        }
        #endregion

        #region ShowLocation
        /// <summary>
        /// Show location function to show the player's location.
        /// </summary>
        private void ShowLocation()
        {
            if (!IsPauseMenuActive() && !IsPauseMenuRestarting() && !IsPlayerSwitchInProgress() && IsScreenFadedIn() && !IsWarningMessageActive() && !IsHudHidden())
            {
                // Create the default prefix.
                var prefix = "~s~";

                // If the vehicle node is further away than 1400f, then the player is not near a valid road.
                // So we set the prefix to "Near " (<streetname>).
                if (Vdist2(currentPos.X, currentPos.Y, currentPos.Z, nodePos.X, nodePos.Y, nodePos.Z) > 1400f)
                {
                    prefix = "~m~Near ~s~";
                }

                string headingCharacter = "";

                // Heading Facing North
                if (heading > 320 || heading < 45)
                {
                    headingCharacter = "N";
                }
                // Heading Facing West
                else if (heading >= 45 && heading <= 135)
                {
                    headingCharacter = "W";
                }
                // Heading Facing South
                else if (heading > 135 && heading < 225)
                {
                    headingCharacter = "S";
                }
                // Heading Facing East
                else
                {
                    headingCharacter = "E";
                }

                // Draw the street name + crossing.
                cf.DrawTextOnScreen(prefix + World.GetStreetName(currentPos) + suffix, 0.234f + safeZoneSizeX, 0.925f - safeZoneSizeY, 0.48f);
                // Draw the zone name.
                cf.DrawTextOnScreen(World.GetZoneLocalizedName(currentPos), 0.234f + safeZoneSizeX, 0.9485f - safeZoneSizeY, 0.45f);

                // Draw the left border for the heading character.
                cf.DrawTextOnScreen("~t~|", 0.188f + safeZoneSizeX, 0.915f - safeZoneSizeY, 1.2f, Alignment.Left);
                // Draw the heading character.
                cf.DrawTextOnScreen(headingCharacter, 0.208f + safeZoneSizeX, 0.915f - safeZoneSizeY, 1.2f, Alignment.Center);
                // Draw the right border for the heading character.
                cf.DrawTextOnScreen("~t~|", 0.228f + safeZoneSizeX, 0.915f - safeZoneSizeY, 1.2f, Alignment.Right);

                // Get and draw the time.
                var tth = GetClockHours();
                var ttm = GetClockMinutes();
                var th = (tth < 10) ? $"0{tth.ToString()}" : tth.ToString();
                var tm = (ttm < 10) ? $"0{ttm.ToString()}" : ttm.ToString();
                cf.DrawTextOnScreen($"~c~{th}:{tm}", 0.208f + safeZoneSizeX, 0.9748f - safeZoneSizeY, 0.40f, Alignment.Center);
            }
        }
        #endregion
        #endregion
        #region Voice Chat Tasks
        /// <summary>
        /// Run all voice chat options tasks
        /// </summary>
        /// <returns></returns>
        private async Task VoiceChat()
        {
            if (MainMenu.VoiceChatSettingsMenu != null && cf.IsAllowed(Permission.VCMenu))
            {
                if (MainMenu.VoiceChatSettingsMenu.EnableVoicechat && cf.IsAllowed(Permission.VCEnable))
                {
                    NetworkSetVoiceActive(true);
                    NetworkSetTalkerProximity(MainMenu.VoiceChatSettingsMenu.currentProximity);
                    int channel = MainMenu.VoiceChatSettingsMenu.channels.IndexOf(MainMenu.VoiceChatSettingsMenu.currentChannel);
                    if (channel < 1)
                    {
                        NetworkClearVoiceChannel();
                    }
                    else
                    {
                        NetworkSetVoiceChannel(channel);
                    }
                    if (MainMenu.VoiceChatSettingsMenu.ShowCurrentSpeaker && cf.IsAllowed(Permission.VCShowSpeaker))
                    {
                        PlayerList pl = new PlayerList();
                        var i = 1;
                        var currentlyTalking = false;
                        // cf.DrawTextOnScreen($"~b~Debugging", 0.5f, 0.00f + (i * 0.03f), 0.5f, Alignment.Center, 6);
                        // i++;
                        foreach (Player p in pl)
                        {
                            if (NetworkIsPlayerTalking(p.Handle))
                            {
                                if (!currentlyTalking)
                                {
                                    cf.DrawTextOnScreen("~s~Currently Talking", 0.5f, 0.00f, 0.5f, Alignment.Center, 6);
                                    currentlyTalking = true;
                                }
                                cf.DrawTextOnScreen($"~b~{p.Name}", 0.5f, 0.00f + (i * 0.03f), 0.5f, Alignment.Center, 6);
                                i++;
                            }
                        }
                    }
                    if (MainMenu.VoiceChatSettingsMenu.ShowVoiceStatus)
                    {

                        if (GetGameTimer() - voiceTimer > 150)
                        {
                            voiceTimer = GetGameTimer();
                            voiceCycle++;
                            if (voiceCycle > 3)
                            {
                                voiceCycle = 1;
                            }
                        }
                        if (!HasStreamedTextureDictLoaded("mpleaderboard"))
                        {
                            RequestStreamedTextureDict("mpleaderboard", false);
                            while (!HasStreamedTextureDictLoaded("mpleaderboard"))
                            {
                                await Delay(0);
                            }
                        }
                        if (NetworkIsPlayerTalking(PlayerId()))
                        {
                            DrawSprite("mpleaderboard", $"leaderboard_audio_{voiceCycle}", 0.008f, 0.985f, voiceIndicatorWidth, voiceIndicatorHeight, 0f, 255, 55, 0, 255);
                        }
                        else
                        {
                            DrawSprite("mpleaderboard", "leaderboard_audio_mute", 0.008f, 0.985f, voiceIndicatorMutedWidth, voiceIndicatorHeight, 0f, 255, 55, 0, 255);
                        }

                    }
                    else
                    {
                        if (HasStreamedTextureDictLoaded("mpleaderboard"))
                        {
                            SetStreamedTextureDictAsNoLongerNeeded("mpleaderboard");
                        }
                    }
                }
                else
                {
                    NetworkSetVoiceActive(false);
                    NetworkClearVoiceChannel();
                }
            }
            else
            {
                await Delay(0);
            }
        }
        #endregion
        #region Update Time Options Menu (current time display)
        /// <summary>
        /// Update the current time display in the time options menu.
        /// </summary>
        /// <returns></returns>
        private async Task TimeOptions()
        {
            if (MainMenu.TimeOptionsMenu != null && cf.IsAllowed(Permission.TOMenu) && GetSettingsBool(Setting.vmenu_enable_time_sync))
            {
                if ((MainMenu.TimeOptionsMenu.freezeTimeToggle != null && MainMenu.TimeOptionsMenu.GetMenu().Visible) && cf.IsAllowed(Permission.TOFreezeTime))
                {
                    // Update the current time displayed in the Time Options menu (only when the menu is actually visible).
                    var hours = GetClockHours();
                    var minutes = GetClockMinutes();
                    var hoursString = hours < 10 ? "0" + hours.ToString() : hours.ToString();
                    var minutesString = minutes < 10 ? "0" + minutes.ToString() : minutes.ToString();
                    MainMenu.TimeOptionsMenu.freezeTimeToggle.SetRightLabel($"(Current Time {hoursString}:{minutesString})");
                }
            }
            // This only needs to be updated once every 2 seconds so we can delay it.
            await Delay(2000);
        }
        #endregion
        #region Weapon Options Tasks
        /// <summary>
        /// Manage all weapon options that need to be handeled every tick.
        /// </summary>
        /// <returns></returns>
        private async Task WeaponOptions()
        {
            if (MainMenu.WeaponOptionsMenu != null && cf.IsAllowed(Permission.WPMenu))
            {
                // If no reload is enabled.
                if (MainMenu.WeaponOptionsMenu.NoReload && Game.PlayerPed.Weapons.Current.Hash != WeaponHash.Minigun && cf.IsAllowed(Permission.WPNoReload))
                {
                    // Disable reloading.
                    PedSkipNextReloading(PlayerPedId());
                }

                // Enable/disable infinite ammo.
                //SetPedInfiniteAmmoClip(PlayerPedId(), MainMenu.WeaponOptionsMenu.UnlimitedAmmo && cf.IsAllowed(Permission.WPUnlimitedAmmo));
                if (Game.PlayerPed.Weapons.Current != null && Game.PlayerPed.Weapons.Current.Hash != WeaponHash.Unarmed)
                {
                    Game.PlayerPed.Weapons.Current.InfiniteAmmo = MainMenu.WeaponOptionsMenu.UnlimitedAmmo && cf.IsAllowed(Permission.WPUnlimitedAmmo);
                }


                /// THIS SOLUTION IS BUGGED AND CAUSES CRASHES
                //// workaround for mk2 weapons (the infinite ammo doesn't seem to work all the time for mk2 weapons)
                //if (MainMenu.WeaponOptionsMenu.UnlimitedAmmo && cf.IsAllowed(Permission.WPUnlimitedAmmo) && Game.PlayerPed.Weapons.Current.IsMk2 &&
                //    Game.PlayerPed.Weapons.Current.Ammo != Game.PlayerPed.Weapons.Current.MaxAmmo)
                //{
                //    Game.PlayerPed.Weapons.Current.Ammo = Game.PlayerPed.Weapons.Current.MaxAmmo;
                //}

                if (MainMenu.WeaponOptionsMenu.AutoEquipChute)
                {
                    if ((IsPedInAnyHeli(PlayerPedId()) || IsPedInAnyPlane(PlayerPedId())) && !HasPedGotWeapon(PlayerPedId(), (uint)WeaponHash.Parachute, false))
                    {
                        GiveWeaponToPed(PlayerPedId(), (uint)WeaponHash.Parachute, 1, false, true);
                        SetPlayerHasReserveParachute(PlayerId());
                        SetPlayerCanLeaveParachuteSmokeTrail(PlayerPedId(), true);
                    }
                }
            }
            else
            {
                await Delay(0);
            }
        }
        #endregion
        #region Spectate Handling Tasks
        /// <summary>
        /// OnTick runs every game tick.
        /// Used here for the spectating feature.
        /// </summary>
        /// <returns></returns>
        private async Task SpectateHandling()
        {
            if (MainMenu.OnlinePlayersMenu != null && cf.IsAllowed(Permission.OPMenu) && cf.IsAllowed(Permission.OPSpectate))
            {
                // When the player dies while spectating, cancel the spectating to prevent an infinite black loading screen.
                if (GetEntityHealth(PlayerPedId()) < 1 && NetworkIsInSpectatorMode())
                {
                    DoScreenFadeOut(50);
                    await Delay(50);
                    NetworkSetInSpectatorMode(true, PlayerPedId());
                    NetworkSetInSpectatorMode(false, PlayerPedId());

                    await Delay(50);
                    DoScreenFadeIn(50);
                    while (GetEntityHealth(PlayerPedId()) < 1)
                    {
                        await Delay(0);
                    }
                }
            }
            else
            {
                await Delay(0);
            }
        }
        #endregion
        #region Player Appearance
        private async Task ManagePlayerAppearanceCamera()
        {
            if (MainMenu.PlayerAppearanceMenu != null && MainMenu.PlayerAppearanceMenu.mpCharMenu != null)
            {
                //foreach (UIMenu m in )
                bool open = MainMenu.PlayerAppearanceMenu.mpCharMenus.Any(m => (m.Visible));
                if (open)
                {
                    int cam = CreateCam("DEFAULT_SCRIPTED_CAMERA", true);
                    Camera camera = new Camera(cam);

                    Game.PlayerPed.Task.ClearAllImmediately();
                    while (open)
                    {
                        await Delay(0);
                        open = MainMenu.PlayerAppearanceMenu.mpCharMenus.Any(m => (m.Visible));

                        //SetFacialIdleAnimOverride(Game.PlayerPed.Handle, "mood_Happy_1", null);
                        SetFacialIdleAnimOverride(Game.PlayerPed.Handle, "mood_normal_1", null);

                        RenderScriptCams(true, false, 0, true, false);

                        FreezeEntityPosition(PlayerPedId(), true);

                        cf.DisableMovementControlsThisFrame(true, true);

                        camera.PointAt(Game.PlayerPed.Position + new Vector3(0f, 0f, 0.7f));
                        camera.Position = Game.PlayerPed.GetOffsetPosition(new Vector3(0f, 0.8f, 0.7f));
                        Game.PlayerPed.Task.ClearAll();
                        if (Game.IsDisabledControlPressed(0, Control.MoveRight))
                        {
                            Game.PlayerPed.Task.LookAt(Game.PlayerPed.GetOffsetPosition(new Vector3(-2f, 0.05f, 0.7f)));
                        }
                        else if (Game.IsDisabledControlPressed(0, Control.MoveLeftOnly))
                        {
                            Game.PlayerPed.Task.LookAt(Game.PlayerPed.GetOffsetPosition(new Vector3(2f, 0.05f, 0.7f)));
                        }
                        else
                        {
                            float input = Game.GetDisabledControlNormal(0, Control.LookLeftRight);

                            if (input > 0.5f)
                            {
                                Game.PlayerPed.Task.LookAt(Game.PlayerPed.GetOffsetPosition(new Vector3(-2f, 0.05f, 0.7f)));
                            }
                            else if (input < -0.5f)
                            {
                                Game.PlayerPed.Task.LookAt(Game.PlayerPed.GetOffsetPosition(new Vector3(2f, 0.05f, 0.7f)));
                            }
                            else
                            {
                                Game.PlayerPed.Task.LookAt(camera.Position);
                            }
                        }

                    }
                    RenderScriptCams(false, false, 0, false, false);
                    camera.Delete();
                    DestroyAllCams(true);
                    Game.PlayerPed.Task.ClearLookAt();
                    FreezeEntityPosition(PlayerPedId(), false);
                }
            }
        }
        #endregion
        #region Restore player skin & weapons after respawning.
        private async Task RestorePlayerAfterBeingDead()
        {
            if (Game.PlayerPed.IsDead)
            {
                if (MainMenu.MiscSettingsMenu != null)
                {
                    if (MainMenu.MiscSettingsMenu.RestorePlayerAppearance && cf.IsAllowed(Permission.MSRestoreAppearance))
                    {
                        cf.SavePed("vMenu_tmp_saved_ped");
                    }

                    if (MainMenu.MiscSettingsMenu.RestorePlayerWeapons && cf.IsAllowed(Permission.MSRestoreWeapons))
                    {
                        await cf.SaveWeaponLoadout();
                    }

                    while (Game.PlayerPed.IsDead || IsScreenFadedOut() || IsScreenFadingOut() || IsScreenFadingIn())
                    {
                        await Delay(0);
                    }

                    if (cf.GetPedInfoFromBeforeDeath() && MainMenu.MiscSettingsMenu.RestorePlayerAppearance && cf.IsAllowed(Permission.MSRestoreAppearance))
                    {
                        cf.LoadSavedPed("vMenu_tmp_saved_ped", false);
                    }
                    if (MainMenu.MiscSettingsMenu.RestorePlayerWeapons && cf.IsAllowed(Permission.MSRestoreWeapons))
                    {
                        cf.RestoreWeaponLoadout();
                    }
                }

            }
        }
        #endregion
        #region Player clothing animations controller.
        private async Task PlayerClothingAnimationsController()
        {
            if (!DecorIsRegisteredAsType(clothingAnimationDecor, 3))
            {
                DecorRegister(clothingAnimationDecor, 3);
            }
            else
            {
                DecorSetInt(PlayerPedId(), clothingAnimationDecor, PlayerAppearance.ClothingAnimationType);
            }

            foreach (Player player in new PlayerList())
            {
                Ped p = player.Character;
                if (p != null && p.Exists() && !p.IsDead)
                {
                    if (DecorExistOn(p.Handle, clothingAnimationDecor))
                    {
                        int decorVal = DecorGetInt(p.Handle, clothingAnimationDecor);
                        if (decorVal == 0) // on solid/no animation.
                        {
                            this.SetPedIlluminatedClothingGlowIntensity(p.Handle, 1f);
                        }
                        else if (decorVal == 1) // off.
                        {
                            this.SetPedIlluminatedClothingGlowIntensity(p.Handle, 0f);
                        }
                        else if (decorVal == 2) // fade.
                        {
                            this.SetPedIlluminatedClothingGlowIntensity(p.Handle, clothingOpacity);
                        }
                        else if (decorVal == 3) // flash.
                        {
                            float result = 0f;
                            if (clothingAnimationReverse)
                            {
                                if ((clothingOpacity >= 0f && clothingOpacity <= 0.25f) || (clothingOpacity >= 0.5f && clothingOpacity <= 0.75f))
                                {
                                    result = 1f;
                                }
                            }
                            else
                            {
                                if ((clothingOpacity >= 0.25f && clothingOpacity <= 0.5f) || (clothingOpacity >= 0.75f && clothingOpacity <= 1.0f))
                                {
                                    result = 1f;
                                }
                            }

                            this.SetPedIlluminatedClothingGlowIntensity(p.Handle, result);
                        }
                    }
                }
            }




            if (clothingAnimationReverse)
            {
                clothingOpacity -= 0.05f;
                if (clothingOpacity < 0f)
                {
                    clothingOpacity = 0f;
                    clothingAnimationReverse = false;
                }
            }
            else
            {
                clothingOpacity += 0.05f;
                if (clothingOpacity > 1f)
                {
                    clothingOpacity = 1f;
                    clothingAnimationReverse = true;
                }
            }
            await Delay(100);
        }
        private void SetPedIlluminatedClothingGlowIntensity(int ped, float intensity)
        {
            CitizenFX.Core.Native.Function.Call((CitizenFX.Core.Native.Hash)0x4E90D746056E273D, ped, intensity);
        }

        #endregion

        #region player blips tasks
        private async Task PlayerBlipsControl()
        {
            if (MainMenu.MiscSettingsMenu != null)
            {
                bool enabled = MainMenu.MiscSettingsMenu.ShowPlayerBlips && cf.IsAllowed(Permission.MSPlayerBlips);

                blipsPlayerList = new PlayerList();
                foreach (Player p in blipsPlayerList)
                {
                    if (p != null && NetworkIsPlayerActive(p.Handle) && p.Character != null && p.Character.Exists())
                    {
                        if (enabled)
                        {
                            if (p != Game.Player)
                            {
                                int ped = p.Character.Handle;
                                int blip = GetBlipFromEntity(ped);

                                if (blip == 0 || blip == -1)
                                {
                                    cf.Log("New Player blip created (1/2).");
                                    blip = AddBlipForEntity(ped);
                                    cf.Log("New Player blip attached to player (2/2).");
                                }

                                SetBlipColour(blip, 0);

                                if (!IsPedInAnyVehicle(ped, false))
                                {
                                    ShowHeadingIndicatorOnBlip(blip, true);
                                }
                                else
                                {
                                    ShowHeadingIndicatorOnBlip(blip, false);
                                }

                                cf.SetCorrectBlipSprite(ped, blip);
                                SetBlipNameToPlayerName(blip, p.Handle);

                                // thanks lambda menu for hiding this great feature in their source code!
                                // sets the blip category to 7, which makes the blips group under "Other Players:"
                                SetBlipCategory(blip, 7);

                                if (p.Character.IsInVehicle() && !p.Character.IsInHeli)
                                {
                                    SetBlipRotation(blip, (int)GetEntityHeading(ped));
                                }
                            }
                        }
                        else
                        {
                            if (!(p.Character.AttachedBlip == null || !p.Character.AttachedBlip.Exists()) && MainMenu.OnlinePlayersMenu != null && !MainMenu.OnlinePlayersMenu.PlayersWaypointList.Contains(p.Handle))
                            {
                                p.Character.AttachedBlip.Delete();
                            }
                        }
                    }
                }
                await Delay(1); // wait 1 tick before doing the next loop.
            }
            else
            {
                await Delay(1000);
            }
        }

        #endregion

        #region Online Player Options Tasks
        private async Task OnlinePlayersTasks()
        {
            await Delay(500);
            if (MainMenu.OnlinePlayersMenu != null && MainMenu.OnlinePlayersMenu.PlayersWaypointList.Count > 0)
            {
                foreach (int playerId in MainMenu.OnlinePlayersMenu.PlayersWaypointList)
                {
                    if (!NetworkIsPlayerActive(playerId))
                    {
                        waypointPlayerIdsToRemove.Add(playerId);
                    }
                    else
                    {
                        Vector3 pos1 = GetEntityCoords(GetPlayerPed(playerId), true);
                        Vector3 pos2 = Game.PlayerPed.Position;
                        if (Vdist2(pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z) < 20f)
                        {
                            int blip = GetBlipFromEntity(GetPlayerPed(playerId));
                            if (DoesBlipExist(blip))
                            {
                                SetBlipRoute(blip, false);
                                RemoveBlip(ref blip);
                                waypointPlayerIdsToRemove.Add(playerId);
                                Notify.Custom($"~g~You've reached ~s~<C>{GetPlayerName(playerId)}</C>'s~g~ location, disabling GPS route.");
                            }
                        }
                    }
                    await Delay(10);
                }
                if (waypointPlayerIdsToRemove.Count > 0)
                {
                    foreach (int id in waypointPlayerIdsToRemove)
                    {
                        MainMenu.OnlinePlayersMenu.PlayersWaypointList.Remove(id);
                    }
                    await Delay(10);
                }
                waypointPlayerIdsToRemove.Clear();
            }
        }
        #endregion

        /// Not task related
        #region Private ShowSpeed Functions
        /// <summary>
        /// Shows the current speed in km/h.
        /// Must be in a vehicle.
        /// </summary>
        private void ShowSpeedKmh()
        {
            if (IsPedInAnyVehicle(PlayerPedId(), false))
            {
                int speed = int.Parse(Math.Round(GetEntitySpeed(cf.GetVehicle()) * 3.6f).ToString());
                cf.DrawTextOnScreen($"{speed} KM/h", 0.995f, 0.955f, 0.7f, Alignment.Right, 4);
            }
        }

        /// <summary>
        /// Shows the current speed in mph.
        /// Must be in a vehicle.
        /// </summary>
        private void ShowSpeedMph()
        {
            if (IsPedInAnyVehicle(PlayerPedId(), false))
            {
                int speed = int.Parse(Math.Round(GetEntitySpeed(cf.GetVehicle()) * 2.23694f).ToString());

                if (MainMenu.MiscSettingsMenu.ShowSpeedoKmh)
                {
                    cf.DrawTextOnScreen($"{speed} MPH", 0.995f, 0.925f, 0.7f, Alignment.Right, 4);
                    HideHudComponentThisFrame((int)HudComponent.StreetName);
                }
                else
                {
                    cf.DrawTextOnScreen($"{speed} MPH", 0.995f, 0.955f, 0.7f, Alignment.Right, 4);
                }
            }
        }
        #endregion
    }
}
