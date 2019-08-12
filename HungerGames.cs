using System;
using Colecciones= System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Rust;
using Oxide;
using Oxide.Core;
using Facepunch;
using Facepunch.MeshBatch;
using Oxide.Core.Plugins;
using System.Reflection;
namespace Oxide.Plugins
{
    [Info("HungerGames", "Daniel25A", 0.2)]
    [Description("Juegos del Hambre")]

    class HungerGames : RustLegacyPlugin
    {
        class DataStore
        {
            public Colecciones::List<object> CajasdeArmas = new Colecciones.List<object>();
            public DataStore()
            {

            }
        }
        DataStore storeData;
        enum OptionFroze
        {
            PlayerFroze,
            PlayerunFroze
        }
        public class PlayerStatus : global::UnityEngine.MonoBehaviour
        {
            public PlayerClient Player;
            public int Kills = 0;
            public bool InGame = false;
            public bool Whiner = false;
            void Start()
            {
                this.Player = GetComponent<PlayerClient>();
            }
        }

        StructureComponent paredMadera;
         Collider AtributeCollider;
         MeshBatchInstance AtributeInstance;
         RaycastHit HitRayCastCharacter;
         Ray AtributeRayCharacter;
         bool AtributeBolean;
         RustServerManagement ServerController;
        static Vector3? cachedLocationEvent=null;
        static FieldInfo StructureComponents;
        static bool EventoIniciado = false;
        static bool ForceStartEvent = false;
        static string SysName = "HungerGames";
        static bool AgregarCajas = false;
        static NetUser PlayerExecuteCommand = null;
        static int MaximoPlayers = 20; //Poner el Maximo de Players Aqui
        static float TiempodeInicio = 60f; //Poner el Tiempo de Inicio una vez forzado o alcanzado el Maximo de Players
        static float RadiationDistance = 50f;
        static string Blue = "[color #0099FF]",
                 Red = "[color #FF0000]",
                 Pink = "[color #CC66FF]",
                 Teal = "[color #00FFFF]",
                 Green = "[color #009900]",
                 Purple = "[color #6600CC]",
                 White = "[color #FFFFFF]",
                 Yellow = "[color #FFFF00]";
        static Colecciones::Dictionary<string, object> Mensajes = new Colecciones.Dictionary<string, object>();
        static Colecciones::List<NetUser> JugadoresenJuego = new Colecciones.List<NetUser>();
        static Colecciones::Dictionary<int, Colecciones::Dictionary<int, string>> RamdomItems = new Colecciones.Dictionary<int, Colecciones::Dictionary<int, string>>() { 
        {0, new Colecciones::Dictionary<int,string>(){{1,"Pipe Shotgun"}}},
        {1,new Colecciones::Dictionary<int,string>(){{1,"Revolver"}}},
        {2,new Colecciones::Dictionary<int,string>(){{2,"Large Medkit"}}},
        {3,new Colecciones::Dictionary<int,string>(){{1,"Pick Axe"}}},
        {4,new Colecciones::Dictionary<int,string>(){{7,"Handmade Shells"}}},
        {5,new Colecciones::Dictionary<int,string>(){{15,"9mm Ammo"}}}
        };
        static Colecciones::Dictionary<ulong, object> Inventarios = new Colecciones.Dictionary<ulong, object>();
        void Init()
        {
            Mensajes.Add("EntroNormal", Red + "HungerGames:" + White + "{0}/{1}" + Green + "players are waiting. " + White + "/hg join");
            Mensajes.Add("Titulo", Red + "---------------HungerGames---------------");
            Mensajes.Add("EventoActivadoInfo", Red + "-----------HungerGames is Active---------");
            Mensajes.Add("Footer", Red + "-----------------------------------------");
            Mensajes.Add("InicioForzado", Green + "HungerGames force started!");
            Mensajes.Add("EntroForzado", Red + "EVENT STARTING" + Green + " Players: {0}" + White + "/hg join");
            Mensajes.Add("PlayerFroze", Red + "You froze!");
            Mensajes.Add("PlayerunFroze", Red + "You are now free");
            Mensajes.Add("PlayerenJuego", White + "You joined the game!");
            Mensajes.Add("EventoActivado", Green + "You can join the EVENT using " + Blue + "/hg join");
            Mensajes.Add("Informacion", Green + "Type /hg to know more!");
        }
        void RecordInventoryPlayer(NetUser Player){
            var Ropas = new Colecciones::List<object>();
            var Armas = new Colecciones::List<object>();
            var CosasInventarios = new Colecciones::List<object>();
            var InfoInventario = new Colecciones::Dictionary<string, object>();
            IInventoryItem Item;
            Inventory PlayerInventory = Player.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            try
            {
                for (int i = 0; i < 40; i++)
                {
                    if (PlayerInventory.GetItem(i, out Item)) {
                        if (i >= 0 && i < 30) {
                            CosasInventarios.Add(new Colecciones::Dictionary<string, int>() { { Item.datablock.name, Item.datablock._splittable ? (int)Item.uses : 1 } });
                            continue;
                        }
                        if (i >= 30 && i < 36) {
                            Armas.Add(new Colecciones::Dictionary<string, int>() { { Item.datablock.name, Item.datablock._splittable ? (int)Item.uses : 1 } });
                            continue;
                        }
                        Ropas.Add(new Colecciones::Dictionary<string, int>() { { Item.datablock.name, Item.datablock._splittable ? (int)Item.uses : 1 } });
                    }
                }
                InfoInventario.Add("Ropas", Ropas);
                InfoInventario.Add("Armas", Armas);
                InfoInventario.Add("CosasInventario", CosasInventarios);
                Inventarios.Add(Player.userID, InfoInventario);
                rust.Notice(Player, "Your Inventory Save, if you inventory dont return type /hg Inventory");
                PlayerInventory.Clear();
            }
            catch (Exception ex)
            {

                Debug.LogError("Error the record Player Inventory in Plugin HungerGames " + ex.Message);
            }
        }
        void ReturnPlayerInventory(NetUser Player) {
            if (!Inventarios.ContainsKey(Player.userID)) return;
            var InfoKeyValueInventory = Inventarios[Player.userID] as Colecciones::Dictionary<string, object>;
            Inventory PlayerInventory = Player.playerClient.rootControllable.idMain.GetComponent<Inventory>();
            Inventory.Slot.Preference SlopPreference;
            ItemDataBlock InventoryItem;
            try
            {
                var Ropa = InfoKeyValueInventory["Ropas"] as Colecciones::List<object>;
                var Armas = InfoKeyValueInventory["Armas"] as Colecciones::List<object>;
                var CosasInventario = InfoKeyValueInventory["CosasInventario"] as Colecciones::List<object>;
                PlayerInventory.Clear();
                if (Ropa.Count > 0)
                {
                    SlopPreference = Inventory.Slot.Preference.Define(Inventory.Slot.KindFlags.Armor, false, Inventory.Slot.KindFlags.Belt);
                    foreach (var x in Ropa)
                    {
                        var KeyValueItem = x as Colecciones::Dictionary<string, int>;
                        if (KeyValueItem != null) {
                            InventoryItem = DatablockDictionary.GetByName(KeyValueItem.First().Key);
                            PlayerInventory.AddItemAmount(InventoryItem, KeyValueItem.First().Value,SlopPreference);
                        }
                    }
                }
                if (Armas.Count > 0)
                {
                    SlopPreference = Inventory.Slot.Preference.Define(Inventory.Slot.KindFlags.Belt, false, Inventory.Slot.KindFlags.Belt);
                    foreach (var x in Armas)
                    {
                        var KeyValueItem = x as Colecciones::Dictionary<string, int>;
                        if (KeyValueItem != null)
                        {
                            InventoryItem = DatablockDictionary.GetByName(KeyValueItem.First().Key);
                            PlayerInventory.AddItemAmount(InventoryItem, KeyValueItem.First().Value, SlopPreference);
                        }
                    }
                }
                if (CosasInventario.Count > 0)
                {
                    SlopPreference = Inventory.Slot.Preference.Define(Inventory.Slot.KindFlags.Default, false, Inventory.Slot.KindFlags.Belt);
                    foreach (var x in CosasInventario)
                    {
                        var KeyValueItem = x as Colecciones::Dictionary<string, int>;
                        if (KeyValueItem != null)
                        {
                            InventoryItem = DatablockDictionary.GetByName(KeyValueItem.First().Key);
                            PlayerInventory.AddItemAmount(InventoryItem, KeyValueItem.First().Value, SlopPreference);
                        }
                    }
                }
                rust.Notice(Player, "Your Inventory Retorned");
            }
            catch (Exception ex)
            {

                Debug.LogError("Error the return Player Inventory in Plugin HungerGames " + ex.Message);
            }
        }
        void OnServerInitialized()
        {
            StructureComponents = typeof(StructureMaster).GetField("_structureComponents", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            ServerController = RustServerManagement.Get();
        }
        void Loaded()
        {
            if (storeData == null) { storeData = new DataStore(); Debug.Log("Instance of DataStore Create"); }
        }
        void ResetEvent(NetUser Player)
        {
            if (Player.admin == false)
                return;
            PlayerExecuteCommand = null;
            EventoIniciado = false;
            ForceStartEvent = false;
            AgregarCajas = false;
            paredMadera = null;
            AtributeBolean = false;
            AtributeCollider = null;
            AtributeInstance = null;
            AtributeRayCharacter.direction = new Vector3(float.NaN, float.NaN, float.NaN);
            foreach (NetUser UserGame in JugadoresenJuego.Where(x => x.playerClient.GetComponent<PlayerStatus>() != null))
                GameObject.Destroy(UserGame.playerClient.GetComponent<PlayerStatus>());
            JugadoresenJuego.Clear();
            storeData.CajasdeArmas.Clear();
        }
        void PlayerFreezer(NetUser Player, OptionFroze Opcion, bool SendMensaje)
        {
            if (Player == null)
                return;
            if (Opcion == OptionFroze.PlayerFroze) { 
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Up 7 None");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Down 7 None");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Left 7 None");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Right 7 None");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Sprint 7 None");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Duck 7 None");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Jump 7 None");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Fire 7 None");
                if (SendMensaje)
                    rust.SendChatMessage(Player, SysName, Mensajes["PlayerFroze"].ToString());
            }
            else if (Opcion == OptionFroze.PlayerunFroze) {
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Up W UpArrow");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Down S DownArrow");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Right D RightArrow");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Sprint LeftShift RightShift");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Left A LeftArrow");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Duck LeftControl RightControl");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Jump Space None");
                ConsoleNetworker.SendClientCommand(Player.networkPlayer, "input.bind Fire Mouse0 None");
                if (SendMensaje)
                    rust.SendChatMessage(Player, SysName, Mensajes["PlayerunFroze"].ToString());
            }
        }
        void EntraralJuego(NetUser Player)
        {
            if (JugadoresenJuego.Count >= MaximoPlayers) {
                rust.SendChatMessage(Player, SysName, Green + "HungerGames have the max of " + Red + "players " + White + "please wait that the finally of current event");
                return;
            }
            if (EventoIniciado) {
                rust.SendChatMessage(Player, SysName, Green + "HungerGames already started " + Red + "please wait that the event finally");
                return;
            }
            if (Player.playerClient.GetComponent<PlayerStatus>() == null)
                Player.playerClient.gameObject.AddComponent<PlayerStatus>();
            if (Player.playerClient.GetComponent<PlayerStatus>().InGame)
                return;
            JugadoresenJuego.Add(Player);
            ServerController.TeleportPlayerToWorld(Player.networkPlayer, cachedLocationEvent ?? new Vector3(Player.playerClient.transform.localPosition.x, Player.playerClient.transform.localPosition.y, Player.playerClient.transform.localPosition.z));
            rust.BroadcastChat(SysName, string.Format(Mensajes["EntroNormal"].ToString(), JugadoresenJuego.Count, MaximoPlayers));
           timer.Once(1f, () =>
           {
                PlayerFreezer(Player, OptionFroze.PlayerFroze, true);
           });
           timer.Once(3f, () =>
           {
               PlayerFreezer(Player, OptionFroze.PlayerunFroze, true);
           });

        }
        void ForceStart(NetUser Player)
        {
            if (Player.admin == false) { rust.Notice(Player, "No Puedes usar este comando"); return; }
            if (!EventoIniciado) { rust.SendChatMessage(Player, SysName, "Hunger Games no Esta Iniciado"); return; }
            for (int i = 0; i < storeData.CajasdeArmas.Count; i++)
            {

                (storeData.CajasdeArmas[i] as GameObject).GetComponent<Inventory>().AddItemAmount(DatablockDictionary.GetByName("P250"), 1);
                (storeData.CajasdeArmas[i] as GameObject).GetComponent<Inventory>().AddItemAmount(DatablockDictionary.GetByName("9mm Ammo"), 50);
                (storeData.CajasdeArmas[i] as GameObject).GetComponent<Inventory>().AddItemAmount(DatablockDictionary.GetByName("Large Medkit"), 2);
                rust.SendChatMessage(Player, SysName, Green + i.ToString() + Red + " Cajas Cargadas");
            }
        }
        void IniciarEvento(NetUser Player)
        {
            if (!Player.admin)
                return;
            if (EventoIniciado){
                rust.SendChatMessage(Player, SysName, Green + "HungerGames already started " + Red + "wait Please");
                return;
            }
            if (cachedLocationEvent == null) {
                rust.SendChatMessage(Player, SysName, Green + "Please Insert the Location where start the event" + Yellow + " /config setlocation");
                return;
            }
            EventoIniciado = true;
            rust.BroadcastChat(SysName, Mensajes["EventoActivadoInfo"].ToString());
            rust.BroadcastChat(SysName, Mensajes["EventoActivado"].ToString());
            rust.BroadcastChat(SysName, Mensajes["Informacion"].ToString());
            rust.BroadcastChat(SysName, Mensajes["Footer"].ToString());
        }
        void BuscarParedes(NetUser Player)
        {
            if (!JugadoresenJuego.Contains(Player) && Player!=PlayerExecuteCommand) return;
            AtributeRayCharacter = Player.playerClient.rootControllable.idMain.GetComponent<Character>().eyesRay;
            if (MeshBatchPhysics.Raycast(AtributeRayCharacter, out HitRayCastCharacter, out AtributeBolean, out AtributeInstance))
            {
                AtributeCollider = AtributeInstance.physicalColliderReferenceOnly;

                if (AtributeCollider.GetComponent<StructureComponent>() != null)
                {
                    this.paredMadera = AtributeCollider.GetComponent<StructureComponent>();
                    if (paredMadera.IsWallType())
                    {
                        TakeDamage.KillSelf(this.paredMadera);
                    }
                }
            }
            else
            {
                rust.Notice(Player, "You dont see a WAll Structure ¿?");
            }
        }
        void OnItemDeployedByPlayer(DeployableObject component, IDeployableItem item)
        {
            if (item.character.playerClient!=null)
            {
                if (PlayerExecuteCommand != null)
                {
                    if (item.character.playerClient.netUser == PlayerExecuteCommand)
                    {
                        storeData.CajasdeArmas.Add(component.gameObject);
                        rust.SendChatMessage(PlayerExecuteCommand, SysName, Green + "New Box Add in " + Yellow + component.transform.localPosition.x.ToString());
                    }
                }
            }
        }
        [ChatCommand("hg")]
        void HungerGamesCommands(NetUser netUser, string command, string[] args)
        {
           
        }
        [ChatCommand("config")]
        void FindWallCommand(NetUser Player, string command, string[] args)
        {
           
        }
    }
}
