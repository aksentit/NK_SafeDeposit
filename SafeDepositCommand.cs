using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;
using Steamworks;
using Rocket.Unturned.Chat;
using Rocket.Unturned;
using Newtonsoft.Json;


namespace NK_SafeDeposit {
    public class NK_SafeDepositBoxes {
        
    } 

    public class NK_SafeDepositCommand : IRocketCommand {
        internal static Dictionary<CSteamID, Transform> PlayerStorage = new Dictionary<CSteamID, Transform>();
        internal static Dictionary<CSteamID, InteractableStorage> interactableStorage = new Dictionary<CSteamID, InteractableStorage>();

        

        public List<string> Aliases {
            get {
                return new List<string>();
            }
        }

        public AllowedCaller AllowedCaller {
            get {
                return AllowedCaller.Player;
            }
        }

        public string Help {
            get {
                return "Opens a Safety Deposit Box.";
            }
        }

        public string Name {
            get {
                return "safedeposit";
            }
        }

        public List<string> Permissions {
            get {
                return new List<string>() { "SDB" };
            }
        }

        public string Syntax {
            get {
                return "safedeposit";
            }
        }

        public void Execute(IRocketPlayer caller, string[] command) {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            Barricade barricade = new Barricade(366);
            if (!PlayerStorage.ContainsKey(player.CSteamID)) {
                PlayerStorage.Add(player.CSteamID, BarricadeManager.dropBarricade(barricade, null, new Vector3(player.Position.x, player.Position.y - 400, player.Position.z), 0, 0, 0, 0ul, 0ul));
            }

            byte x, y;
            ushort plant, index;
            BarricadeRegion regionBarricade;
            
            if (BarricadeManager.tryGetInfo(PlayerStorage[player.CSteamID], out x, out y, out plant, out index, out regionBarricade)) {
                if (!interactableStorage.ContainsKey(player.CSteamID)) {
                    interactableStorage.Add(player.CSteamID, regionBarricade.drops[index].interactable as InteractableStorage);
                    interactableStorage[player.CSteamID].isOpen = true;
                    interactableStorage[player.CSteamID].opener = player.Player;
                    interactableStorage[player.CSteamID].name = "{SafeDepositBox}" + player.CharacterName;
                    interactableStorage[player.CSteamID].items.clear();
                    interactableStorage[player.CSteamID].items.resize((byte)6, (byte)8);
                    player.Inventory.isStoring = true;
                    player.Inventory.storage = interactableStorage[player.CSteamID];
                    player.Inventory.updateItems(PlayerInventory.STORAGE, interactableStorage[player.CSteamID].items);
                    player.Inventory.sendStorage();
                } else {
                    interactableStorage[player.CSteamID].isOpen = true;
                    interactableStorage[player.CSteamID].opener = player.Player;
                    interactableStorage[player.CSteamID].name = "{SafeDepositBox}" + player.CharacterName;
                    //interactableStorage[player.CSteamID].items.clear();
                    interactableStorage[player.CSteamID].items.resize((byte)6, (byte)8);
                    player.Inventory.isStoring = true;
                    player.Inventory.storage = interactableStorage[player.CSteamID];
                    player.Inventory.updateItems(PlayerInventory.STORAGE, interactableStorage[player.CSteamID].items);
                    player.Inventory.sendStorage();

                    

                    

                    //Rocket.Core.Logging.Logger.Log(player.Inventory.storage.items.getItemCount().ToString());

                }
            }


        }

    }
}
