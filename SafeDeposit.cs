using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using SDG.Unturned;
//using UnityEngine;
using Steamworks;
using Rocket.Unturned.Chat;
using Rocket.Unturned;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Unturned.Events;
using Newtonsoft.Json;


namespace NK_SafeDeposit {

    class SafeDeposit : Rocket.Core.Plugins.RocketPlugin<SafeDepositConfig> {

        //class players {

        //    public CSteamID steamid;
        //    public UnturnedPlayer player;
        //    public Item[] playerItems;

        //    public players(CSteamID sid, UnturnedPlayer p, Item[] items) {
        //        steamid = sid;
        //        player = p;
        //    }

        //    class items {

        //    }

        //}

        class player {
            [JsonProperty("SteamID")]
            public string SteamId { get; set; }

            [JsonProperty("items")]
            public playerItems[] playerItems { get; set; }
        }

        class playerItems {
            [JsonProperty("itemid")]
            public string itemid { get; set; }

            [JsonProperty("amount")]
            public string amount { get; set; }

            [JsonProperty("durability")]
            public string durability { get; set; }

            [JsonProperty("metadata")]
            public string metadata { get; set; }

            [JsonProperty("quality")]
            public string quality { get; set; }

            [JsonProperty("hasItem")]
            public bool hasItem { get; set; }
        }

        class SDResult {
            [JsonProperty("player")]
            public player player { get; set; }

            //public SDResult() {
            //    player.SteamId = "";
            //    for (int i = 0; i < player.playerItems.Length; i++) {
            //        player.playerItems[i].itemid = "";
            //        player.playerItems[i].amount = "";
            //        player.playerItems[i].durability = "";
            //        player.playerItems[i].quality = "";
            //        player.playerItems[i].metadata = "";
            //    }
            //}
        }

        public static SafeDeposit Instance;

        internal static Dictionary<CSteamID, List<string>[]> dataArray = new Dictionary<CSteamID, List<string>[]>();
        internal static Dictionary<CSteamID, List<Item>> PlayerAddedItems = new Dictionary<CSteamID, List<Item>>();
        internal static List<CSteamID> PlayersToSave = new List<CSteamID>();
        internal static Dictionary<CSteamID, DateTime> cCooldown = new Dictionary<CSteamID, DateTime>();

        List<CSteamID> allPlayers = new List<CSteamID>();
        //List<Item> playerItems = new List<Item>();
        Dictionary<List<CSteamID>, List<Item>> playerItemMix = new Dictionary<List<CSteamID>, List<Item>>();

        SDResult[] tempData = new SDResult[24];


        protected override void Load() {
            Instance = this;
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;

            //tell that log thing that the program loaded and set all event handlers
            Rocket.Core.Logging.Logger.Log("safedeposit loaded");
            UnturnedPlayerEvents.OnPlayerInventoryResized += UnturnedPlayerEvents_OnPlayerInventoryResized;
            UnturnedPlayerEvents.OnPlayerInventoryAdded += UnturnedPlayerEvents_OnPlayerInventoryAdded;
            UnturnedPlayerEvents.OnPlayerInventoryUpdated += UnturnedPlayerEvents_OnPlayerInventoryUpdated;
            UnturnedPlayerEvents.OnPlayerInventoryRemoved += UnturnedPlayerEvents_OnPlayerInventoryRemoved;
            UnturnedPlayerEvents.OnPlayerDeath += UnturnedPlayerEvents_OnPlayerDeath;
            U.Events.OnPlayerConnected += Events_OnPlayerConnected;

            //instantiate every single MOTHER FUCKING THING in the array because c# is a dick and only reserves the memory for the array, not the actual instance of the array -.-
            for (int i = 0; i < tempData.Length; i++) {

                Logger.Log(tempData.Length.ToString());
                tempData[i] = new SDResult();
                tempData[i].player = new player {
                    SteamId = "",
                    playerItems = new playerItems[30]
                };

                //arrayception. this is the array in the array that is in charge of keeping the player's items to be shipped off to the management system in JSon
                for (int j = 0; j < tempData[i].player.playerItems.Length; j++) {
                    tempData[i].player.playerItems[j] = new playerItems() {
                        itemid = "",
                        amount = "",
                        durability = "",
                        quality = "",
                        metadata = "",
                        hasItem = false
                    };
                }
            }
        }

        //same thing as added. just updates the state of the array and nested array. 
        private void UnturnedPlayerEvents_OnPlayerInventoryRemoved(UnturnedPlayer player, Rocket.Unturned.Enumerations.InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P) {
            try {
                //if the storage is open...
                if (player.Inventory.storage.isOpen == true) {
                    //make sure that the dynamically generated interactable storage is the one that's open.
                    if (player.Inventory.storage.name.StartsWith("{SafeDepositBox}")) {
                        //substring the entire storage name, {SafeDepositBox}Username. cut out the first sixteen characters and check ownership. if so, do things
                        if (player.Inventory.storage.name.Substring(16) == player.CharacterName) {

                            //thing 1: set up a loop to go through the temp data array
                            for (int i = 0; i < tempData.Length; i++) {
                                //thing 2: if the player in the array's steamid isn't blank, and it matches the player that triggered the event, do thing three, else do thing 4
                                if (tempData[i].player.SteamId != "" && tempData[i].player.SteamId == player.CSteamID.ToString()) {
                                    //thing 3: get the number of items in the safe deposit box, cycle through the nested items array in temp data, add each item into array.
                                    for (int j = 0; j < player.Inventory.storage.items.getItemCount(); j++) {
                                        tempData[i].player.playerItems[j].itemid = player.Inventory.storage.items.getItem((byte)j).item.id.ToString();
                                        tempData[i].player.playerItems[j].amount = player.Inventory.storage.items.getItem((byte)j).item.amount.ToString();
                                        tempData[i].player.playerItems[j].durability = player.Inventory.storage.items.getItem((byte)j).item.durability.ToString();
                                        tempData[i].player.playerItems[j].quality = player.Inventory.storage.items.getItem((byte)j).item.quality.ToString();
                                        tempData[i].player.playerItems[j].metadata = player.Inventory.storage.items.getItem((byte)j).item.amount.ToString();
                                    }


                                } else { //thing 4: if the player does not have his info in tempData, add his steamid to the array as well as the items in the nested array.
                                    tempData[i].player.SteamId = player.CSteamID.ToString();
                                    for (int j = 0; j < player.Inventory.storage.items.getItemCount(); j++) {
                                        tempData[i].player.playerItems[j].itemid = player.Inventory.storage.items.getItem((byte)j).item.id.ToString();
                                        tempData[i].player.playerItems[j].amount = player.Inventory.storage.items.getItem((byte)j).item.amount.ToString();
                                        tempData[i].player.playerItems[j].durability = player.Inventory.storage.items.getItem((byte)j).item.durability.ToString();
                                        tempData[i].player.playerItems[j].quality = player.Inventory.storage.items.getItem((byte)j).item.quality.ToString();
                                        tempData[i].player.playerItems[j].metadata = player.Inventory.storage.items.getItem((byte)j).item.amount.ToString();

                                    }
                                }


                                //Logger.Log(JsonConvert.SerializeObject(tempData[i]));
                            }

                            postJson(tempData);
                            //tempData[plil].player.SteamId = 



                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Log("god fucking damn it");
                Logger.Log(ex.Message + " ------ stack trace ------" + ex.StackTrace);
            }

        }

        //also does dick-all
        private void Events_OnPlayerConnected(UnturnedPlayer player) {
            Logger.Log("player connected");
            allPlayers.Add(player.CSteamID);




        }

        //this voids job is literally to pitch a fucking fit.
        private void UnturnedPlayerEvents_OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer) {
            throw new NotImplementedException();
        }

        //that good shit.
        private void UnturnedPlayerEvents_OnPlayerInventoryAdded(UnturnedPlayer player, Rocket.Unturned.Enumerations.InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P) {
            //throw it all in a try catch = error handling.
            try {
                //if the storage is open...
                if (player.Inventory.storage.isOpen == true) {
                    //make sure that the dynamically generated interactable storage is the one that's open.
                    if (player.Inventory.storage.name.StartsWith("{SafeDepositBox}")) {
                        //substring the entire storage name, {SafeDepositBox}Username. cut out the first sixteen characters and check ownership. if so, do things
                        if (player.Inventory.storage.name.Substring(16) == player.CharacterName) {

                            //thing 1: set up a loop to go through the temp data array
                            for (int i = 0; i < tempData.Length; i++) {
                                //thing 2: if the player in the array's steamid isn't blank, and it matches the player that triggered the event, do thing three, else do thing 4
                                if (tempData[i].player.SteamId != "" && tempData[i].player.SteamId == player.CSteamID.ToString()) {
                                    //thing 3: get the number of items in the safe deposit box, cycle through the nested items array in temp data, add each item into array.
                                    for (int j = 0; j < player.Inventory.storage.items.getItemCount(); j++) {
                                        
                                        tempData[i].player.playerItems[j].itemid = player.Inventory.storage.items.getItem((byte)j).item.id.ToString();
                                        tempData[i].player.playerItems[j].amount = player.Inventory.storage.items.getItem((byte)j).item.amount.ToString();
                                        tempData[i].player.playerItems[j].durability = player.Inventory.storage.items.getItem((byte)j).item.durability.ToString();
                                        tempData[i].player.playerItems[j].quality = player.Inventory.storage.items.getItem((byte)j).item.quality.ToString();
                                        tempData[i].player.playerItems[j].metadata = player.Inventory.storage.items.getItem((byte)j).item.amount.ToString();
                                        tempData[i].player.playerItems[j].hasItem = true;
                                    }


                                } else { //thing 4: if the player does not have his info in tempData, add his steamid to the array as well as the items in the nested array.
                                    tempData[i].player.SteamId = player.CSteamID.ToString();
                                    for (int j = 0; j < player.Inventory.storage.items.getItemCount(); j++) {
                                        tempData[i].player.playerItems[j].itemid = player.Inventory.storage.items.getItem((byte)j).item.id.ToString();
                                        tempData[i].player.playerItems[j].amount = player.Inventory.storage.items.getItem((byte)j).item.amount.ToString();
                                        tempData[i].player.playerItems[j].durability = player.Inventory.storage.items.getItem((byte)j).item.durability.ToString();
                                        tempData[i].player.playerItems[j].quality = player.Inventory.storage.items.getItem((byte)j).item.quality.ToString();
                                        tempData[i].player.playerItems[j].metadata = player.Inventory.storage.items.getItem((byte)j).item.amount.ToString();
                                        tempData[i].player.playerItems[j].hasItem = true;
                                    }
                                    postJson(tempData);
                                }


                                //Logger.Log(JsonConvert.SerializeObject(tempData[i]));
                                
                            }


                            //tempData[plil].player.SteamId = 



                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Log("god fucking damn it");
                Logger.Log(ex.Message + " ------ stack trace ------" + ex.StackTrace);
            }

        }

        private void UnturnedPlayerEvents_OnPlayerInventoryResized(UnturnedPlayer player, Rocket.Unturned.Enumerations.InventoryGroup inventoryGroup, byte O, byte U) {
            Logger.Log("player inventory resized");
        }

        private void UnturnedPlayerEvents_OnPlayerInventoryUpdated(UnturnedPlayer player, Rocket.Unturned.Enumerations.InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P) {
            Logger.Log("player inventory updated");
        }

        protected override void Unload() {
            Rocket.Core.Logging.Logger.Log("safedeposit unloaded");
        }

        private void postJson(SDResult[] obj) {
            //define the rocket HTTP Client for use with post, and set the headers for JSon transmission
            Rocket.Core.Utils.RocketWebClient fuckboi = new Rocket.Core.Utils.RocketWebClient();

            fuckboi.Headers["Content-Type"] = "application/json; charset=UTF-8";
            fuckboi.Headers["Accept"] = "application/json";

            if (checkIfEmpty(obj) == true) {
                fuckboi.UploadStringAsync(new System.Uri(""), "[]");
            } else {
                SDResult[] postInfo = new SDResult[24];
                for (int i = 0; i < obj.Length; i++) {
                    postInfo[i] = new SDResult();
                    postInfo[i].player = new player {
                        SteamId = "",
                        playerItems = new playerItems[30]
                };
                    for (int j = 0; j < tempData[i].player.playerItems.Length; j++) {
                        tempData[i].player.playerItems[j] = new playerItems() {
                            itemid = "",
                            amount = "",
                            durability = "",
                            quality = "",
                            metadata = "",
                            hasItem = false
                        };
                    }
                }
                int x = 0;
                for (int i = 0; i < obj.Length; i++) {
                    if (obj[i].player.SteamId != "" && obj[i].player.SteamId == "ass") {
                        postInfo[x].player.SteamId = obj[i].player.SteamId;
                        int y = 0;
                        for (int j = 0; j < obj[i].player.playerItems.Length; j++) {
                            if (obj[i].player.playerItems[j].hasItem == true) {
                                postInfo[x].player.playerItems[y].itemid = obj[i].player.playerItems[j].itemid;
                                postInfo[x].player.playerItems[y].amount = obj[i].player.playerItems[j].metadata;
                                postInfo[x].player.playerItems[y].durability = obj[i].player.playerItems[j].durability;
                                postInfo[x].player.playerItems[y].quality = obj[i].player.playerItems[j].quality;
                                postInfo[x].player.playerItems[y].metadata = obj[i].player.playerItems[j].metadata;
                            }
                            y++;
                        }
                        x++;
                    }
                }
                Logger.Log(JsonConvert.SerializeObject(postInfo));
            }


            //check to see if the server is empty, if it is, send this, if not, do the things.
            //if (checkIfEmpty(obj) == true) {
                //fuckboi.UploadString(Configuration.Instance.EndPoint, "[]");
            //} else {
                //set up the post object
                //playInfo[] postInfo = new playInfo[24];

                //populate the postInfo array records so it doesn't chuck a fucking wobbly
                //for (int i = 0; i < obj.Length; i++) {
                    //postInfo[i] = new playInfo {
                    //    steam_ID = "",
                    //    steam_Name = "",
                    //    char_Name = "",
                    //    ping = "",
                    //    position = "",
                    //    rotation = "",
                    //    dead = "",
                    //    vehicle = "",
                    //    connected = false
                    //};
                //}


                //set a count variable, x, to record every time a connected player is found.
                //when a connected player is found, it will record his info into the Post object, postInfo,
                //at the free spot declared by x, and set x to the next empty spot in the array.
                //int x = 0;
                //for (int i = 0; i < postInfo.Length; i++) {

                    //if (obj[i].connected == true) {
                    //    postInfo[x] = new playInfo {
                    //        steam_ID = obj[i].steam_ID,
                    //        steam_Name = obj[i].steam_Name,
                    //        char_Name = obj[i].char_Name,
                    //        ping = obj[i].ping,
                    //        position = obj[i].position,
                    //        rotation = obj[i].rotation,
                    //        dead = obj[i].dead,
                    //        vehicle = obj[i].vehicle,
                    //        connected = obj[i].connected
                    //    };
                    //    x++;
                    //}
                //}

                //post the newly formatted playInfo, sent as the object postInfo.
                //fuckboi.UploadString(Configuration.Instance.EndPoint, JsonConvert.SerializeObject(postInfo).ToString());

                
            //}

        }
        private bool checkIfEmpty(SDResult[] obj) {
            for (int i = 0; i < obj.Length; i++) {
                if (i == 23 && obj[i].player.SteamId == "") {
                    return true;
                }else if (obj[i].player.SteamId != "") {
                    return false;
                }

                //if (i == 23 && obj[i].player == false) {
                //    return true;
                //} else if (obj[i].connected == true) {
                //    return false;

                //}
            }
            return false;
        }
    }

    public class SafeDepositConfig : IRocketPluginConfiguration {
        public string EndPoint;
        public int timerInterval;
        public bool timerEnabled;

        public void LoadDefaults() {
            EndPoint = "false";
            timerInterval = 60;
            timerEnabled = false;
        }
    }

}
