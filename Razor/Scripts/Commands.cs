﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assistant.Core;
using UOSteam;

namespace Assistant.Scripts
{
    public class Commands
    {
        public static void Register()
        {
            // Commands based on Actions.cs
            Interpreter.RegisterCommandHandler("cast", Cast); //BookcastAction, etc

            // Dress
            Interpreter.RegisterCommandHandler("dress", DressCommand); //DressAction
            Interpreter.RegisterCommandHandler("undress", UnDressCommand); //UndressAction
            Interpreter.RegisterCommandHandler("dressconfig", DressConfig);

            // Targets
            Interpreter.RegisterCommandHandler("target", Target); //Absolute Target
            Interpreter.RegisterCommandHandler("targettype", TargetType); //TargetTypeAction
            Interpreter.RegisterCommandHandler("targetrelloc", TargetRelLoc); //TargetRelLocAction

            Interpreter.RegisterCommandHandler("waitfortarget", WaitForTarget); //WaitForTargetAction
            Interpreter.RegisterCommandHandler("wft", WaitForTarget); //WaitForTargetAction

            // Using stuff
            Interpreter.RegisterCommandHandler("dclicktype", UseType); // DoubleClickTypeAction
            Interpreter.RegisterCommandHandler("dclick", UseObject); //DoubleClickAction
            Interpreter.RegisterCommandHandler("dclickvar", DummyCommand); //DoubleClickVariableAction -- this needed?

            Interpreter.RegisterCommandHandler("usetype", UseType); // DoubleClickTypeAction
            Interpreter.RegisterCommandHandler("useobject", UseObject); //DoubleClickAction

            // Moving stuff
            Interpreter.RegisterCommandHandler("drop", DummyCommand); //DropAction
            Interpreter.RegisterCommandHandler("droprelloc", DummyCommand); //DropAction
            Interpreter.RegisterCommandHandler("lift", DummyCommand); //LiftAction
            Interpreter.RegisterCommandHandler("lifttype", DummyCommand); //LiftTypeAction

            // Gump
            Interpreter.RegisterCommandHandler("waitforgump", WaitForGump); // WaitForGumpAction
            Interpreter.RegisterCommandHandler("waitformenu", DummyCommand); // WaitForMenuAction
            Interpreter.RegisterCommandHandler("gumpresponse", DummyCommand); // GumpResponseAction
            Interpreter.RegisterCommandHandler("replygump", DummyCommand); // GumpResponseAction
            Interpreter.RegisterCommandHandler("closegump", DummyCommand);

            // Menu
            Interpreter.RegisterCommandHandler("contextmenu", DummyCommand); //ContextMenuAction
            Interpreter.RegisterCommandHandler("menuresponse", DummyCommand); //MenuResponseAction
            Interpreter.RegisterCommandHandler("waitformenu", DummyCommand); //WaitForMenuAction

            // Prompt
            Interpreter.RegisterCommandHandler("promptresponse", DummyCommand); //PromptAction
            Interpreter.RegisterCommandHandler("waitforprompt", DummyCommand); //WaitForPromptAction

            // Hotkey execution
            Interpreter.RegisterCommandHandler("hotkey", Hotkey); //HotKeyAction

            // Messages
            Interpreter.RegisterCommandHandler("say", Msg); //SpeechAction
            Interpreter.RegisterCommandHandler("msg", Msg); //SpeechAction
            Interpreter.RegisterCommandHandler("overhead", HeadMsg); //OverheadMessageAction
            Interpreter.RegisterCommandHandler("sysmsg", SysMsg); //SystemMessageAction

            // General Waits/Pauses
            Interpreter.RegisterCommandHandler("wait", Pause); //PauseAction
            Interpreter.RegisterCommandHandler("pause", Pause); //PauseAction
            Interpreter.RegisterCommandHandler("waitforstat", Pause); //WaitForStatAction

            // Misc
            Interpreter.RegisterCommandHandler("setability", SetAbility); //SetAbilityAction
            Interpreter.RegisterCommandHandler("setlasttarget", DummyCommand); //SetLastTargetAction
            Interpreter.RegisterCommandHandler("lasttarget", DummyCommand); //LastTargetAction
            Interpreter.RegisterCommandHandler("setvar", DummyCommand); //SetMacroVariableTargetAction
            Interpreter.RegisterCommandHandler("skill", UseSkill); //SkillAction
            Interpreter.RegisterCommandHandler("walk", Walk); //Move/WalkAction
        }

        private static bool DummyCommand(string command, Argument[] args, bool quiet, bool force)
        {
            ScriptManager.Error($"Unimplemented command: {command}");

            return true;
        }

        private static bool UseItem(Item cont, ushort find)
        {
            for (int i = 0; i < cont.Contains.Count; i++)
            {
                Item item = cont.Contains[i];

                if (item.ItemID == find)
                {
                    PlayerData.DoubleClick(item);
                    return true;
                }
                else if (item.Contains != null && item.Contains.Count > 0)
                {
                    if (UseItem(item, find))
                        return true;
                }
            }

            return false;
        }

        private static bool Target(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1 || !abilities.Contains(args[0].AsString()))
            {
                ScriptManager.Error("Usage: target 'serial'");
                return true;
            }

            Serial serial = args[0].AsSerial();

            if (serial != Serial.Zero) // Target a specific item or mobile
            {
                Item item = World.FindItem(serial);

                if (item != null)
                {
                    Targeting.Target(item);
                    return true;
                }

                Mobile mobile = World.FindMobile(serial);

                if (mobile != null)
                {
                    Targeting.Target(mobile);
                }
            }

            return true;
        }

        private static bool TargetType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Error("Usage: targettype (isMobile) (graphic) ");
                return true;
            }

            bool isMobile = bool.Parse(args[0].ToString());
            ushort gfx = Utility.ToUInt16(args[1].ToString(), 0);

            if (Targeting.FromGrabHotKey)
                return false;

            ArrayList list = new ArrayList();
            if (isMobile)
            {
                foreach (Mobile find in World.MobilesInRange())
                {
                    if (find.Body == gfx)
                    {
                        if (Config.GetBool("RangeCheckTargetByType"))
                        {
                            if (Utility.InRange(World.Player.Position, find.Position, 2))
                            {
                                list.Add(find);
                            }
                        }
                        else
                        {
                            list.Add(find);
                        }
                    }
                }
            }
            else
            {
                foreach (Item i in World.Items.Values)
                {
                    if (i.ItemID == gfx && !i.IsInBank)
                    {
                        if (Config.GetBool("RangeCheckTargetByType"))
                        {
                            if (Utility.InRange(World.Player.Position, i.Position, 2))
                            {
                                list.Add(i);
                            }
                        }
                        else
                        {
                            list.Add(i);
                        }
                    }
                }
            }

            if (list.Count > 0)
            {
                /*if (Config.GetBool("DiffTargetByType") && list.Count > 1)
                {
                    object currentObject = list[Utility.Random(list.Count)];

                    while (_previousObject != null && _previousObject == currentObject)
                    {
                        currentObject = list[Utility.Random(list.Count)];
                    }

                    Targeting.Target(currentObject);

                    _previousObject = currentObject;
                }
                else
                {
                    Targeting.Target(list[Utility.Random(list.Count)]);
                }*/

                Targeting.Target(list[Utility.Random(list.Count)]);
            }
            else
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.NoItemOfType,
                    isMobile ? $"Character [{gfx}]" : ((ItemID) gfx).ToString());
            }

            return true;
        }

        private static bool TargetRelLoc(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Error("Usage: targetrelloc (x-offset) (y-offset) ");
                return true;
            }

            int xoffset = Utility.ToInt32(args[0].ToString(), 0);
            int yoffset = Utility.ToInt32(args[1].ToString(), 0);

            ushort x = (ushort) (World.Player.Position.X + xoffset);
            ushort y = (ushort) (World.Player.Position.Y + yoffset);
            short z = (short) World.Player.Position.Z;

            try
            {
                Ultima.HuedTile tile = Map.GetTileNear(World.Player.Map, x, y, z);
                Targeting.Target(new Point3D(x, y, tile.Z), tile.ID);
            }
            catch (Exception e)
            {
                ScriptManager.Error($"Error Executing TargetRelLoc: {e.Message}");
            }

            return true;
        }

        private static bool WaitForTarget(string command, Argument[] args, bool quiet, bool force)
        {
            return Targeting.HasTarget;
        }

        private static bool Hotkey(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Error("Usage: hotkey ('name of hotkey'/'hotkeyID')");
                return true;
            }

            string query = args[0].AsString();

            KeyData hk = HotKey.GetByNameOrId(query);

            if (hk == null)
            {
                ScriptManager.Error($"Hotkey '{query}' not found");
                return true;
            }

            hk.Callback();

            return true;
        }

        private static bool WaitForGump(string command, Argument[] args, bool quiet, bool force)
        {
            uint gumpId = 0;
            bool strict = false;

            // Look for a specific gump
            if (args.Length == 1)
            {
                gumpId = Utility.ToUInt32(args[0].ToString(), 0);

                if (gumpId > 0)
                    strict = true;
            }

            return !((World.Player.HasGump || World.Player.HasCompressedGump) &&
                     (World.Player.CurrentGumpI == gumpId || !strict || gumpId == 0));
        }

        private static string[] abilities = new string[4] {"primary", "secondary", "stun", "disarm"};

        private static bool SetAbility(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1 || !abilities.Contains(args[0].AsString()))
            {
                ScriptManager.Error("Usage: setability ('primary'/'secondary'/'stun'/'disarm') ['on'/'off']");
                return true;
            }

            if (args.Length == 2 && args[1].AsString() == "on" || args.Length == 1)
            {
                switch (args[0].AsString())
                {
                    case "primary":
                        SpecialMoves.SetPrimaryAbility();
                        break;
                    case "secondary":
                        SpecialMoves.SetSecondaryAbility();
                        break;
                    case "stun":
                        Client.Instance.SendToServer(new StunRequest());
                        break;
                    case "disarm":
                        Client.Instance.SendToServer(new DisarmRequest());
                        break;
                    default:
                        break;
                }
            }
            else if (args.Length == 2 && args[1].AsString() == "off")
            {
                Client.Instance.SendToServer(new UseAbility(AOSAbility.Clear));
                Client.Instance.SendToClient(ClearAbility.Instance);
            }

            return true;
        }

        private static bool Attack(string command, Argument[] args, bool quiet, bool force)
        {
            return true;
        }

        private static string[] hands = new string[3] {"left", "right", "both"};

        private static bool ClearHands(string command, Argument[] args, bool quiet, bool force)
        {
            // expect one STRING node

            if (args.Length == 0 || !hands.Contains(args[0].AsString()))
            {
                ScriptManager.Error("Usage: clearhands ('left'/'right'/'both')");
                return true;
            }

            switch (args[0].AsString())
            {
                case "left":
                    Dress.Unequip(Layer.LeftHand);
                    break;
                case "right":
                    Dress.Unequip(Layer.RightHand);
                    break;
                default:
                    Dress.Unequip(Layer.LeftHand);
                    Dress.Unequip(Layer.RightHand);
                    break;
            }

            return true;
        }

        private static bool UseType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: dclicktype|usetype ('graphic/name of item')");
                return true;
            }

            string gfxStr = args[0].AsString();
            ushort gfx = Utility.ToUInt16(gfxStr, 0);

            Serial click = Serial.Zero;
            bool isItem = false;
            Item item = null;

            // No graphic id, maybe searching by name?
            if (gfx == 0)
            {
                item = World.FindItemByName(gfxStr);

                if (item == null)
                {
                    ScriptManager.Error($"Script Error: Couldn't find '{gfxStr}'");
                    return true;
                }
            }
            else // Check backpack first
            {
                item = World.Player.Backpack != null ? World.Player.Backpack.FindItemByID(gfx) : null;
            }

            // Not in backpack? Lets check the world
            if (item == null)
            {
                List<Item> list = new List<Item>();
                foreach (Item i in World.Items.Values)
                {
                    if (i.ItemID == gfx && i.RootContainer == null)
                    {
                        isItem = true;

                        if (Config.GetBool("RangeCheckDoubleClick"))
                        {
                            if (Utility.InRange(World.Player.Position, i.Position, 2))
                            {
                                list.Add(i);
                            }
                        }
                        else
                        {
                            list.Add(i);
                        }
                    }
                }

                if (list.Count == 0)
                {
                    foreach (Item i in World.Items.Values)
                    {
                        if (i.ItemID == gfx && !i.IsInBank)
                        {
                            isItem = true;

                            if (Config.GetBool("RangeCheckDoubleClick"))
                            {
                                if (Utility.InRange(World.Player.Position, i.Position, 2))
                                {
                                    list.Add(i);
                                }
                            }
                            else
                            {
                                list.Add(i);
                            }
                        }
                    }
                }

                if (list.Count > 0)
                    click = list[Utility.Random(list.Count)].Serial;
            }
            else
            {
                isItem = true;
                click = item.Serial;
            }

            // Still no item? Mobile check!
            if (item == null)
            {
                List<Mobile> list = new List<Mobile>();
                foreach (Mobile m in World.MobilesInRange())
                {
                    if (m.Body == gfx)
                    {
                        if (Config.GetBool("RangeCheckDoubleClick"))
                        {
                            if (Utility.InRange(World.Player.Position, m.Position, 2))
                            {
                                list.Add(m);
                            }
                        }
                        else
                        {
                            list.Add(m);
                        }
                    }
                }

                if (list.Count > 0)
                    click = list[Utility.Random(list.Count)].Serial;
            }

            if (click != Serial.Zero)
                PlayerData.DoubleClick(click);
            else
                World.Player.SendMessage(MsgLevel.Force, LocString.NoItemOfType,
                    isItem ? ((ItemID) gfx).ToString() : $"(Character) 0x{gfx:X}");

            return true;
        }

        private static bool UseObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: useobject (serial)");
                return true;
            }

            Serial serial = args[0].AsSerial();

            if (!serial.IsValid)
            {
                ScriptManager.Error("useobject - invalid serial");
                return true;
            }

            Client.Instance.SendToServer(new DoubleClick(serial));

            return true;
        }

        private static bool UseOnce(string command, Argument[] args, bool quiet, bool force)
        {
            return true;
        }

        private static bool MoveItem(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Error("Usage: moveitem (serial) (destination) [(x, y, z)] [amount]");
                return true;
            }

            uint serial = args[0].AsSerial();
            uint destination = args[1].AsSerial();
            if (args.Length == 2)
                DragDropManager.DragDrop(World.FindItem((uint) serial), World.FindItem((uint) destination));
            else if (args.Length == 5)
                return true;
            else if (args.Length == 6)
                return true;

            return true;
        }

        private static bool Walk(string command, Argument[] args, bool quiet, bool force)
        {
            return true;
        }

        private static Dictionary<string, int> UsableSkills = new Dictionary<string, int>()
        {
            {"anatomy", 1}, // anatomy
            {"animallore", 2}, // animal lore
            {"itemidentification", 3}, // item identification
            {"armslore", 4}, // arms lore
            {"begging", 6}, // begging
            {"peacemaking", 9}, // peacemaking
            {"cartography", 12}, // cartography
            {"detectinghidden", 14}, // detect hidden
            {"discordance", 15}, // Discordance
            {"evaluatingintelligence", 16}, // evaluate intelligence
            {"forensicevaluation", 19}, // forensic evaluation
            {"hiding", 21}, // hiding
            {"provocation", 22}, // provocation
            {"inscription", 23}, // inscription
            {"poisoning", 30}, // poisoning
            {"spiritspeak", 32}, // spirit speak
            {"stealing", 33}, // stealing
            {"taming", 35}, // taming
            {"tasteidentification", 36}, // taste id
            {"tracking", 38}, // tracking
            {"meditation", 46}, // Meditation
            {"stealth", 47}, // Stealth
            {"removetrap", 48} // RemoveTrap
        };

        private static bool UseSkill(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: useskill ('skill name'/'last')");
                return true;
            }

            if (args[0].AsString() == "last")
                Client.Instance.SendToServer(new UseSkill(World.Player.LastSkill));
            else if (UsableSkills.TryGetValue(args[0].AsString(), out int skillId))
                Client.Instance.SendToServer(new UseSkill(skillId));

            return true;
        }

        private static bool SetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Error("Usage: setalias ('name') [serial]");
                return true;
            }

            Interpreter.SetAlias(args[0].AsString(), args[1].AsSerial());

            return true;
        }

        private static bool UnsetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: unsetalias (string)");
                return true;
            }

            Interpreter.SetAlias(args[0].AsString(), 0);

            return true;
        }

        public static bool EquipItem(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Error("Usage: equipitem (serial) (layer)");
                return true;
            }

            Item equip = World.FindItem(args[0].AsSerial());
            byte layer = (byte) Utility.ToInt32(args[1].AsString(), 0);
            if (equip != null && (Layer) layer != Layer.Invalid)
                Dress.Equip(equip, (Layer) layer);

            return true;
        }

        private static bool Pause(string command, Argument[] args, bool quiet, bool force)
        {
            return true;
        }

        public static bool Msg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: msg ('text') [color]");
                return true;
            }

            if (!Client.Instance.ClientRunning)
                return true;

            if (args.Length == 1)
                World.Player.Say(Config.GetInt("SysColor"), args[0].AsString());
            else
                World.Player.Say(Utility.ToInt32(args[1].AsString(), 0), args[0].AsString());

            return true;
        }

        public static bool Cast(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: cast 'spell' [serial]");
                return true;
            }

            if (!Client.Instance.ClientRunning)
                return true;

            Spell spell;

            if (int.TryParse(args[0].AsString(), out int spellnum))
                spell = Spell.Get(spellnum);
            else
                spell = Spell.GetByName(args[0].AsString());
            if (spell != null)
            {
                spell.OnCast(new CastSpellFromMacro((ushort) spell.GetID()));

                if (args.Length > 1)
                {
                    Serial s = args[1].AsSerial();
                    if (force)
                        Targeting.ClearQueue();
                    if (s > Serial.Zero && s != Serial.MinusOne)
                    {
                        Targeting.Target(s);
                    }
                    else if (!quiet)
                        ScriptManager.Error("cast - invalid serial or alias");
                }
            }
            else if (!quiet)
                ScriptManager.Error("cast - spell name or number not valid");

            return true;
        }

        public static bool HeadMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: headmsg ('text') [color] [serial]");
                return true;
            }

            if (!Client.Instance.ClientRunning)
                return true;

            if (args.Length == 1)
                World.Player.OverheadMessage(Config.GetInt("SysColor"), args[0].AsString());
            else
            {
                int hue = Utility.ToInt32(args[1].AsString(), 0);

                if (args.Length == 3)
                {
                    uint serial = args[2].AsSerial();
                    Mobile m = World.FindMobile((uint) serial);

                    if (m != null)
                        m.OverheadMessage(hue, args[0].AsString());
                }
                else
                    World.Player.OverheadMessage(hue, args[0].AsString());
            }

            return true;
        }

        public static bool SysMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Error("Usage: sysmsg ('text') [color]");
                return true;
            }

            if (!Client.Instance.ClientRunning)
                return true;

            if (args.Length == 1)
                World.Player.SendMessage(Config.GetInt("SysColor"), args[0].AsString());
            else if (args.Length == 2)
                World.Player.SendMessage(Utility.ToInt32(args[1].AsString(), 0), args[0].AsString());

            return true;
        }

        public static bool DressCommand(string command, Argument[] args, bool quiet, bool force)
        {
            if (!Client.Instance.ClientRunning)
                return true;

            //we're using a named dresslist or a temporary dresslist?
            if (args.Length == 0)
            {
                if (DressList._Temporary != null)
                    DressList._Temporary.Dress();
                else if (!quiet)
                    ScriptManager.Error(
                        "No dresslist specified and no temporary dressconfig present - usage: dress ['dresslist']");
            }
            else
            {
                var d = DressList.Find(args[0].AsString());
                if (d != null)
                    d.Dress();
                else if (!quiet)
                    ScriptManager.Error($"dresslist {args[0].AsString()} not found");
            }

            return true;
        }

        public static bool UnDressCommand(string command, Argument[] args, bool quiet, bool force)
        {
            if (!Client.Instance.ClientRunning)
                return true;
            //we're using a named dresslist or a temporary dresslist?
            if (args.Length == 0)
            {
                if (DressList._Temporary != null)
                    DressList._Temporary.Undress();
                else if (!quiet)
                    ScriptManager.Error(
                        "No dresslist specified and no temporary dressconfig present - usage: undress ['dresslist']");
            }
            else
            {
                var d = DressList.Find(args[0].AsString());
                if (d != null)
                    d.Undress();
                else if (!quiet)
                    ScriptManager.Error($"dresslist {args[0].AsString()} not found");
            }

            return true;
        }

        public static bool DressConfig(string command, Argument[] args, bool quiet, bool force)
        {
            if (!Client.Instance.ClientRunning)
                return true;

            if (DressList._Temporary == null)
                DressList._Temporary = new DressList("dressconfig");

            DressList._Temporary.Items.Clear();
            for (int i = 0; i < World.Player.Contains.Count; i++)
            {
                Item item = World.Player.Contains[i];
                if (item.Layer <= Layer.LastUserValid && item.Layer != Layer.Backpack && item.Layer != Layer.Hair &&
                    item.Layer != Layer.FacialHair)
                    DressList._Temporary.Items.Add(item.Serial);
            }

            return true;
        }
    }
}