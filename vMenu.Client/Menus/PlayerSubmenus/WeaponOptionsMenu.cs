using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;

using ScaleformUI.Menu;

using vMenu.Client.Functions;
using vMenu.Client.Menus.OnlinePlayersSubmenus;
using vMenu.Client.Settings;
using vMenu.Shared.Objects;

namespace vMenu.Client.Menus.PlayerSubmenus
{
    public class WeaponOptionsMenu
    {
        private static UIMenu weaponOptionsMenu = null;

        // Checkbox items
        public bool UnlimitedAmmo { get; private set; } = false;
        public bool NoReload { get; private set; } = false;
        public bool AutoEquipChute { get; private set; } = false;
        public bool UnlimitedParachutes { get; private set; } = false;

        public WeaponOptionsMenu()
        {
            var MenuLanguage = Languages.Menus["WeaponOptionsMenu"];

            // Creating the menu
            weaponOptionsMenu = new Objects.vMenu(MenuLanguage.Subtitle ?? "Weapon Options").Create();

            UIMenuSeparatorItem button = new UIMenuSeparatorItem("Under Construction!", false)
            {
                MainColor = MenuSettings.Colours.Spacers.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Spacers.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Spacers.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Spacers.TextColor
            };
            weaponOptionsMenu.AddItem(button);
            // Menu Items

            UIMenuItem getAllWeapons = new UIMenuItem("Get All Weapons", "Get all weapons.", MenuSettings.Colours.Items.BackgroundColor, MenuSettings.Colours.Items.HighlightColor);
            UIMenuCheckboxItem unlimitedAmmo = new UIMenuCheckboxItem("Unlimited Ammo", UnlimitedAmmo, "Unlimited ammunition supply.")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };

            UIMenuCheckboxItem noReload = new UIMenuCheckboxItem("No Reload", NoReload, "Never reload.")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };
            UIMenuItem setAmmo = new UIMenuItem("Set All Ammo Count", "Set the amount of ammo in all your weapons.", MenuSettings.Colours.Items.BackgroundColor, MenuSettings.Colours.Items.HighlightColor);
            UIMenuItem refillMaxAmmo = new UIMenuItem("Refill All Ammo", "Give all your weapons max ammo.", MenuSettings.Colours.Items.BackgroundColor, MenuSettings.Colours.Items.HighlightColor);
            UIMenuItem spawnByName = new UIMenuItem("Spawn Weapon By Name", "Enter a weapon mode name to spawn.", MenuSettings.Colours.Items.BackgroundColor, MenuSettings.Colours.Items.HighlightColor);

            weaponOptionsMenu.AddItem(getAllWeapons);
            weaponOptionsMenu.AddItem(noReload);
            weaponOptionsMenu.AddItem(setAmmo);
            weaponOptionsMenu.AddItem(refillMaxAmmo);
            weaponOptionsMenu.AddItem(spawnByName);
            // Add-on Weapons Menu

            UIMenuItem addonWeaponsBtn = new UIMenuItem("Addon Weapons", "Equip / remove addon weapons available on this server.", MenuSettings.Colours.Items.BackgroundColor, MenuSettings.Colours.Items.HighlightColor);
            //UIMenu addonWeaponsMenu = new UIMenu("Addon Weapons", "Equip/Remove Addon Weapons");
            addonWeaponsBtn.SetRightLabel("→→→");
            weaponOptionsMenu.AddItem(addonWeaponsBtn);

            addonWeaponsBtn.Activated += (sender, _) => {
                Notify.Alert("This feature isn't currently finished.");
            };

            // Create Weapon Category Submenus
            UIMenuSeparatorItem spacer = new UIMenuSeparatorItem("↓ Weapon Categories ↓", true)
            {
                MainColor = MenuSettings.Colours.Spacers.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Spacers.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Spacers.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Spacers.TextColor
            };
            weaponOptionsMenu.AddItem(spacer);

            //UIMenu handGuns = new UIMenu("Weapons", "Handguns");
            UIMenuItem handGunsBtn = new UIMenuItem("Handguns")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };



            //UIMenu rifles = new UIMenu("Weapons", "Assault Rifles");
            UIMenuItem riflesBtn = new UIMenuItem("Assault Rifles")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };

            //UIMenu shotguns = new UIMenu("Weapons", "Shotguns");
            UIMenuItem shotgunsBtn = new UIMenuItem("Shotguns")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };

            //UIMenu smgs = new UIMenu("Weapons", "Sub-/Light Machine Guns");
            UIMenuItem smgsBtn = new UIMenuItem("Sub-/Light Machine Guns")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };

            //UIMenu throwables = new UIMenu("Weapons", "Throwables");
            UIMenuItem throwablesBtn = new UIMenuItem("Throwables")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };
            //UIMenu melee = new UIMenu("Weapons", "Melee");
            UIMenuItem meleeBtn = new UIMenuItem("Melee")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };
            //UIMenu heavy = new UIMenu("Weapons", "Heavy Weapons");
            UIMenuItem heavyBtn = new UIMenuItem("Heavy Weapons")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };
            //UIMenu snipers = new UIMenu("Weapons", "Sniper Rifles");
            UIMenuItem snipersBtn = new UIMenuItem("Sniper Rifles")
            {
                MainColor = MenuSettings.Colours.Items.BackgroundColor,
                HighlightColor = MenuSettings.Colours.Items.HighlightColor,
                HighlightedTextColor = MenuSettings.Colours.Items.HighlightedTextColor,
                TextColor = MenuSettings.Colours.Items.TextColor
            };

            // Setting up Weapon Category Buttons / Submenus for later configuation.

            handGunsBtn.SetRightLabel("→→→");
            handGunsBtn.Activated += (sender, _) =>
            {
                Notify.Alert("This MenuItem isn't ready for use.");
            };
            weaponOptionsMenu.AddItem(handGunsBtn);

            riflesBtn.SetRightLabel("→→→");
            riflesBtn.Activated += (sender, _) =>
            {
                Notify.Alert("This MenuItem isn't ready for use.");
            };
            weaponOptionsMenu.AddItem(riflesBtn);

            shotgunsBtn.SetRightLabel("→→→");
            shotgunsBtn.Activated += (sender, _) =>
            {
                Notify.Alert("This MenuItem isn't ready for use.");
            };
            weaponOptionsMenu.AddItem(shotgunsBtn);

            smgsBtn.SetRightLabel("→→→");
            smgsBtn.Activated += (sender, _) =>
            {
                Notify.Alert("This MenuItem isn't ready for use.");
            };
            weaponOptionsMenu.AddItem(smgsBtn);

            throwablesBtn.SetRightLabel("→→→");
            throwablesBtn.Activated += (sender, _) =>
            {
                Notify.Alert("This MenuItem isn't ready for use.");
            };
            weaponOptionsMenu.AddItem(throwablesBtn);

            meleeBtn.SetRightLabel("→→→");
            meleeBtn.Activated += (sender, _) =>
            {
                Notify.Alert("This MenuItem isn't ready for use.");
            };
            weaponOptionsMenu.AddItem(meleeBtn);

            heavyBtn.SetRightLabel("→→→");
            heavyBtn.Activated += (sender, _) =>
            {
                Notify.Alert("This MenuItem isn't ready for use.");
            };
            weaponOptionsMenu.AddItem(heavyBtn);

            snipersBtn.SetRightLabel("→→→");
            snipersBtn.Activated += (sender, _) =>
            {
                Notify.Alert("This MenuItem isn't ready for use.");
            };
            weaponOptionsMenu.AddItem(snipersBtn);

            Main.Menus.Add(weaponOptionsMenu);
        }

        public static UIMenu Menu()
        {
            return weaponOptionsMenu;
        }
    }
}
