﻿using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

namespace Tutorial5_Classes
{
    public class Tutorial5 : WildfrostMod
    {
        public override string GUID => "fg.wildfrost.asonefrost";

        public override string[] Depends => new string[0];

        public override string Title => "AsOneFrost";

        public override string Description => "New Tribes to play.";

        internal static Tutorial5 instance;

        

        public static void AddComponent(string name)
        {
            Campaign.instance.gameObject.AddComponentByName(name);
        }

        public Tutorial5(string baseDirectory) : base(baseDirectory) { }

        public static List<object> assets = new List<object>();

        public bool preLoaded = false;

        internal CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);
        internal CardData.TraitStacks TStack(string name, int amount) => new CardData.TraitStacks(TryGet<TraitData>(name), amount);

        internal T TryGet<T>(string name) where T : DataFile
        {
            T data;
            if (typeof(StatusEffectData).IsAssignableFrom(typeof(T)))
                data = base.Get<StatusEffectData>(name) as T;
            else if (typeof(KeywordData).IsAssignableFrom(typeof(T)))
                data = base.Get<KeywordData>(name.ToLower()) as T;
            else
                data = base.Get<T>(name);

            if (data == null)
                throw new Exception($"TryGet Error: Could not find a [{typeof(T).Name}] with the name [{name}] or [{Extensions.PrefixGUID(name, this)}]");

            return data;
        }

        private CardDataBuilder CardCopy(string oldName, string newName) => DataCopy<CardData, CardDataBuilder>(oldName, newName);
        private ClassDataBuilder TribeCopy(string oldName, string newName) => DataCopy<ClassData,ClassDataBuilder>(oldName, newName);
        private T DataCopy<Y, T>(string oldName, string newName) where Y : DataFile where T : DataFileBuilder<Y, T>, new()
        {
            Y data = Get<Y>(oldName).InstantiateKeepName();
            data.name = GUID + "." + newName;
            T builder = data.Edit<Y, T>();
            builder.Mod = this;
            return builder;
        }

        private T[] DataList<T>(params string[] names) where T : DataFile => names.Select((s) => TryGet<T>(s)).ToArray();

        internal CardScript GiveUpgrade(string name = "Crown")
        {
            CardScriptGiveUpgrade script = ScriptableObject.CreateInstance<CardScriptGiveUpgrade>();
            script.name = $"Give {name}";
            script.upgradeData = TryGet<CardUpgradeData>(name);
            return script;
        }

        internal CardScript AddRandomHealth(int min, int max)
        {
            CardScriptAddRandomHealth health = ScriptableObject.CreateInstance<CardScriptAddRandomHealth>();
            health.name = "Random Health";
            health.healthRange = new Vector2Int(min,max);
            return health;
        }

        internal CardScript AddRandomDamage(int min, int max)
        {
            CardScriptAddRandomDamage damage = ScriptableObject.CreateInstance<CardScriptAddRandomDamage>();
            damage.name = "Give Damage";
            damage.damageRange = new Vector2Int(min, max);
            return damage;
        }

        internal CardScript AddRandomCounter(int min, int max)
        {
            CardScriptAddRandomCounter counter = ScriptableObject.CreateInstance<CardScriptAddRandomCounter>();
            counter.name = "Give Counter";
            counter.counterRange = new Vector2Int(min, max);
            return counter;
        }

        private RewardPool CreateRewardPool(string name, string type, DataFile[] list)
        {
            RewardPool pool = ScriptableObject.CreateInstance<RewardPool>();
            pool.name = name;
            pool.type = type;
            pool.list = list.ToList();
            return pool;
        }

        private void CreateModAssets()
        {
            assets.Add(CardCopy("Ruckus", "needleLeader")
                .WithCardType("Leader")
                .FreeModify(
                (data) =>
                {
                    data.createScripts = new CardScript[] 
                    { 
                        GiveUpgrade(),
                        AddRandomHealth(-2,2),
                        AddRandomDamage(-1,1),
                        AddRandomCounter(-1,1)
                    };
                })
            );

            assets.Add(CardCopy("TrueFinalBoss6", "muncherLeader")
                .WithCardType("Leader")
                .SetStats(8,5,5)
                .FreeModify(
                (data) =>
                {
                    data.traits.Add(TStack("Draw", 1));

                    data.createScripts = new CardScript[] 
                    {
                        GiveUpgrade(),
                        AddRandomHealth(-1,3),
                        AddRandomDamage(0,2),
                        AddRandomCounter(-1,1)
                    };
                })
            );

            assets.Add(CardCopy("Wrenchy", "superMuncher")
                .WithTitle("Super Muncher")
                .FreeModify(
                (data) =>
                {
                    data.traits.Add(TStack("Draw", 2));
                })
            );

            assets.Add(new CardUpgradeDataBuilder(this)
                .Create("CardUpgradeSuperDraw")
                .WithTitle("Quickdraw Charm")
                .WithText($"Gain <keyword=draw> <2> and <keyword=zoomlin>")
                .WithType(CardUpgradeData.Type.Charm)
                .WithImage("blueDraw.png")
                .SetTraits(TStack("Draw",2), TStack("Consume",1))
                .FreeModify(
                (data) =>
                {
                    TargetConstraintIsItem item = ScriptableObject.CreateInstance<TargetConstraintIsItem>();
                    item.name = "Is Item";
                    TargetConstraintHasTrait consume = ScriptableObject.CreateInstance<TargetConstraintHasTrait>();
                    consume.name = "Does Not Have Consume";
                    consume.trait = TryGet<TraitData>("Consume");
                    consume.not = true;
                    data.targetConstraints = new TargetConstraint[] {item, consume};
                })
            );

            assets.Add(CardCopy("Pimento", "Pimento2")
                .WithCardType("Leader")
                .WithTitle("Pimento")
                .FreeModify(
                (data) =>
                {
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade()
                    };
                })
            );

            assets.Add(CardCopy("ShellWitch", "ShellWitch2")
                .WithCardType("Leader")
                .WithTitle("Shell Witch")
                .FreeModify(
                (data) =>
                {
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade()
                    };
                })
            );

            assets.Add(CardCopy("Tusk", "Tusk2")
                .WithCardType("Leader")
                .WithTitle("Tusk")
                .FreeModify(
                (data) =>
                {
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade()
                    };
                })
            );

            assets.Add(CardCopy("Numskull", "Numskull2")
                .WithCardType("Leader")
                .WithTitle("Numskull")
                .SetStats(6,5,5)
                .FreeModify(
                (data) =>
                {
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade(),
                    };
                })
            );

            assets.Add(CardCopy("Witch", "Witch2")
                .WithCardType("Leader")
                .WithTitle("Biji")
                .SetStats(8,0,4)
                .FreeModify(
                (data) =>
                {
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade(),
                    };
                })
            );

            assets.Add(CardCopy("Frosty", "Frosty2")
                .WithCardType("Leader")
                .WithTitle("The Ringer")
                .SetStats(8,3,5)
                .SetTraits(TStack("Barrage",1))
                .FreeModify(
                (data) =>
                {
                    data.createScripts = new CardScript[]
                    {
                        GiveUpgrade(),
                    };
                })
            );

            //.SetStats(8, 5, 5)
            //Scrapped GameModifier Code. Maybe added later in a later tutorial...
            /* 
            assets.Add(new GameModifierDataBuilder(this)
                .Create("BlessingCycler")
                .WithTitle("Sun Bell of Cycling")
                .WithDescription("Reduce hand size by <2>, but draw to hand size each turn")
                .WithBellSprite("Images/cycleBell.png")
                .WithDingerSprite("Images/cycleDinger.png")
                .WithRingSfxEvent(Get<GameModifierData>("DoubleBlingsFromCombos").ringSfxEvent)
                .WithSystemsToAdd("DrawToAmountModifierSystem")
                .FreeModify(
                (data) =>
                {
                    Texture2D texture = data.bellSprite.texture;
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 1f), 314);
                    data.bellSprite = sprite;

                    texture = data.dingerSprite.texture;
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 1.7f), 314);
                    data.dingerSprite = sprite;

                    ScriptChangeHandSize handSize = ScriptableObject.CreateInstance<ScriptChangeHandSize>();
                    handSize.name = "Reduce Hand Size By 2";
                    handSize.set = false;
                    handSize.value = -2;
                    data.startScripts = new Script[] { handSize };
                })
            );
            */

            assets.Add(TribeCopy("Clunk", "Draw")
                .WithFlag("Images/DrawFlag.png")
                .WithSelectSfxEvent(FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/card/draw_multi"))
                .SubscribeToAfterAllBuildEvent(
                (data) =>
                {
                    GameObject gameObject = data.characterPrefab.gameObject.InstantiateKeepName();
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    gameObject.name = "Player (Tutorial.Draw)";
                    data.id = "Tutorial.Draw";
                    data.characterPrefab = gameObject.GetComponent<Character>();

                    data.leaders = DataList<CardData>("Pimento2", "ShellWitch2", "Tusk2");

                    Inventory inventory = ScriptableObject.CreateInstance<Inventory>();
                    inventory.deck.list = DataList<CardData>("Dart", "Dart", "Dart", "Dart", "ShellShield", "BerryBasket", "SunlightDrum", "TigrisMask", "SnowCannon").ToList();
                    inventory.upgrades.Add(TryGet<CardUpgradeData>("Crown"));
                    data.startingInventory = inventory;

                    RewardPool unitPool = CreateRewardPool("SpiceUnitPool", "Units", DataList<CardData>(
                        "Firefist", "Pyra", "Fulbert",
                        "PepperWitch", "Kernel", "Shelly",
                        "Chompom", "Pecan", "Kokonut",
                        "Prickle", "Noggin", "BerryMonster",
                        "Dimona", "Pygmy", "Minimoko",
                        "JabJoat", "Wally"));

                    RewardPool itemPool = CreateRewardPool("SpiceItemPool", "Items", DataList<CardData>(
                        "Heartforge", "DragonflamePepper", "Peppereaper", "Peppering", "PepperFlag",
                        "SpiceStones", "Peppermaton", "MonkeyWorshipTotem", "NutshellCake", "Demonheart", "TotemOfTheGoat",
                        "Dittostone", "BlazeTea", "SunRod", "Putty", "EnemyCloner",
                        "SunRod", "Plum", "BerryBasket", "BerryBlade", "SnowStick"));

                    RewardPool charmPool = CreateRewardPool("SpiceCharmPool", "Charms", DataList<CardUpgradeData>(
                        "Crown", "CardUpgradeSun",
                        "CardUpgradeSpice", "CardUpgradeDemonize",
                        "CardUpgradeShellOnKill", "CardUpgradeShellBecomesSpice",
                        "CardUpgradeAcorn", "CardUpgradeSpiky",
                        "CardUpgradeTeethWhenHit",
                        "CardUpgradeBoost", "CardUpgradePunchfist",
                        "CardUpgradeHeart", "CardUpgradeBarrage"));

                    data.rewardPools = new RewardPool[]
                    {
                        unitPool,
                        itemPool,
                        charmPool,
                        //Extensions.GetRewardPool("GeneralUnitPool"),
                        //Extensions.GetRewardPool("GeneralItemPool"),
                        //Extensions.GetRewardPool("GeneralCharmPool"),
                        Extensions.GetRewardPool("GeneralModifierPool"),
                        //Extensions.GetRewardPool("SnowUnitPool"),
                        //Extensions.GetRewardPool("SnowItemPool"),
                        //Extensions.GetRewardPool("SnowCharmPool"),
                    };

                })
            );

            assets.Add(TribeCopy("Clunk", "Draw2")
                .WithFlag("Images/DrawFlag.png")
                .WithSelectSfxEvent(FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/card/draw_multi"))
                .SubscribeToAfterAllBuildEvent(
                (data) =>
                {
                    GameObject gameObject = data.characterPrefab.gameObject.InstantiateKeepName();
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    gameObject.name = "Player (Tutorial.Draw2)";
                    data.id = "Tutorial.Draw2";
                    data.characterPrefab = gameObject.GetComponent<Character>();

                    data.leaders = DataList<CardData>("Numskull2", "Witch2", "Frosty2");

                    Inventory inventory = ScriptableObject.CreateInstance<Inventory>();
                    inventory.deck.list = DataList<CardData>("Sword", "Sword", "Sword", "Sword", "FrostBloom", "BerryBasket", "SunlightDrum", "Vox", "Bumblebee").ToList();
                    inventory.upgrades.Add(TryGet<CardUpgradeData>("Crown"));
                    data.startingInventory = inventory;

                    RewardPool unitPool = CreateRewardPool("BomUnitPool", "Units", DataList<CardData>(
                        "Blue", "Blunky", "Icemason",
                        "Frostinger", "Grink", "Gnomlings",
                        "Kalamari", "Waddlegoons", "Wally",
                        "Pootie", "MagmaBlacksmith", "TinyTyko",
                        "Bombom", "Chungoon", "Foxee"));

                    RewardPool itemPool = CreateRewardPool("BomItemPool", "Items", DataList<CardData>(
                        "IceDice", "FrostBell", "EnergyDart", "Nullifier", "Voidstone",
                        "IceShard", "MoltenDip", "Crowbar", "SunburstDart", "TigrisMask", "TotemOfTheGoat",
                        "Dittostone", "BlazeTea", "SunRod", "Putty", "EnemyCloner",
                        "SunRod", "Plum", "BerryBasket", "BerryBlade", "SnowStick"));

                    RewardPool charmPool = CreateRewardPool("BomharmPool", "Charms", DataList<CardUpgradeData>(
                        "Crown", "CardUpgradeSun",
                        "CardUpgradeBlock", "CardUpgradeBalanced",
                        "CardUpgradeFrosthand", "CardUpgradeBom",
                        "CardUpgradeInk", "CardUpgradeFlameberry",
                        "CardUpgradeBoost",
                        "CardUpgradeMime", "CardUpgradePunchfist",
                        "CardUpgradeHeart", "CardUpgradeBarrage"));

                    data.rewardPools = new RewardPool[]
                    {
                        unitPool,
                        itemPool,
                        charmPool,
                        //Extensions.GetRewardPool("GeneralUnitPool"),
                        //Extensions.GetRewardPool("GeneralItemPool"),
                        //Extensions.GetRewardPool("GeneralCharmPool"),
                        Extensions.GetRewardPool("GeneralModifierPool"),
                        //Extensions.GetRewardPool("SnowUnitPool"),
                        //Extensions.GetRewardPool("SnowItemPool"),
                        //Extensions.GetRewardPool("SnowCharmPool"),
                    };

                })
            );

            preLoaded = true;
        }

        public override void Load()
        {
            instance = this;
            if (!preLoaded) { CreateModAssets(); }
            base.Load();
            CreateLocalizedStrings();
            GameMode gameMode = TryGet<GameMode>("GameModeNormal");
            gameMode.classes = gameMode.classes.Append(TryGet<ClassData>("Draw")).ToArray();
            gameMode.classes = gameMode.classes.Append(TryGet<ClassData>("Draw2")).ToArray();

            Events.OnEntityCreated += FixImage;
        }

        public override void Unload()
        {
            base.Unload();
            GameMode gameMode = TryGet<GameMode>("GameModeNormal");
            UnloadFromClasses();
            gameMode.classes = RemoveNulls(gameMode.classes);
            UnloadFromClasses();

            Events.OnEntityCreated -= FixImage;
        }

        private void FixImage(Entity entity)
        {
            Card card = entity.display as Card;
            if (card.hasScriptableImage)
                card.mainImage.gameObject.SetActive(true);
        }

        internal T[] RemoveNulls<T>(T[] data) where T : DataFile
        {
            List<T> list = data.ToList();
            list.RemoveAll(x => x == null || x.ModAdded == this);
            return list.ToArray();
        }

        //Credits to Hopeful for this method
        public override List<T> AddAssets<T, Y>()
        {
            if (assets.OfType<T>().Any())
                Debug.LogWarning($"[{Title}] adding {typeof(Y).Name}s: {assets.OfType<T>().Select(a => a._data.name).Join()}");
            return assets.OfType<T>().ToList();
        }

        public void UnloadFromClasses()
        {
            List<ClassData> tribes = AddressableLoader.GetGroup<ClassData>("ClassData");
            foreach (ClassData tribe in tribes)
            {
                if (tribe == null || tribe.rewardPools == null) { continue; } //This isn't even a tribe; skip it.

                foreach (RewardPool pool in tribe.rewardPools)
                {
                    if (pool == null) { continue; }; //This isn't even a reward pool; skip it.

                    pool.list.RemoveAllWhere((item) => item == null || item.ModAdded == this); //Find and remove everything that needs to be removed.
                }
            }
        }

        public string TribeTitleKey => GUID + ".TribeTitle";
        public string TribeDescKey => GUID + ".TribeDesc";

        //Call this method in Load()
        private void CreateLocalizedStrings()
        {
            StringTable uiText = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English);
            uiText.SetString(TribeTitleKey, "The Collectors");                                       //Create the title
            uiText.SetString(TribeDescKey, "Needle and the Frost Muncher realized they both held a strong desire of consuming cards. " +
                "They banded together to form a new clan over this hobby. " +
                "\n\n" +
                "The tribe is an assortment of odds and ends that Needle found \"useful\". " +
                "There is a strange fixation with drawing cards.");                                  //Create the description.

        }
    }
   
    /*
    public class DrawToAmountModifierSystem : GameSystem
    {
        private void OnEnable()
        {
            Events.OnBattleTurnEnd += BattleTurnEnd;
        }

        private void OnDisable()
        {
            Events.OnBattleTurnEnd -= BattleTurnEnd;
        }

        private void BattleTurnEnd(int turn)
        {
            int amount = Events.GetHandSize(References.PlayerData.handSize);
            if (!Battle.instance.ended && References.Player.handContainer.Count < amount && turn != 0)
            {
                int amountToDraw = amount - References.Player.handContainer.Count;
                ActionQueue.Stack(new ActionDraw(References.Player, amountToDraw));
            }
        }
    
    }
    */

    [HarmonyPatch(typeof(References), nameof(References.Classes), MethodType.Getter)]
    static class FixClassesGetter
    {
        static void Postfix(ref ClassData[] __result) => __result = AddressableLoader.GetGroup<ClassData>("ClassData").ToArray();
    }

    //Scrapped GameModifier Patch. Maybe will be used in a future tutorial.
    /*
    [HarmonyPatch(typeof(GameObjectExt), "AddComponentByName")]
    class PatchAddComponent
    {
        static string assem => typeof(PatchAddComponent).Assembly.GetName().Name;
        static string namesp => typeof(PatchAddComponent).Namespace;

        static Component Postfix(Component __result, GameObject gameObject, string componentName)
        {
            if (__result == null)
            {
                Type type = Type.GetType(namesp + "." + componentName + "," + assem);
                if (type != null)
                {
                    return gameObject.AddComponent(type);
                }
            }
            return __result;
        }
    }
    */

    [HarmonyPatch(typeof(TribeHutSequence), "SetupFlags")]
    class PatchTribeHut
    {
        static string TribeName = "Draw";
        static void Postfix(TribeHutSequence __instance)
        {
            GameObject gameObject = GameObject.Instantiate(__instance.flags[0].gameObject);
            gameObject.transform.SetParent(__instance.flags[0].gameObject.transform.parent, false);
            TribeFlagDisplay flagDisplay = gameObject.GetComponent<TribeFlagDisplay>();
            ClassData tribe = Tutorial5.instance.TryGet<ClassData>(TribeName);
            flagDisplay.flagSprite = tribe.flag;
            __instance.flags = __instance.flags.Append(flagDisplay).ToArray();
            flagDisplay.SetAvailable();
            flagDisplay.SetUnlocked();

            TribeDisplaySequence sequence2 = GameObject.FindObjectOfType<TribeDisplaySequence>(true);
            GameObject gameObject2 = GameObject.Instantiate(sequence2.displays[1].gameObject);
            gameObject2.transform.SetParent(sequence2.displays[2].gameObject.transform.parent, false);
            sequence2.tribeNames = sequence2.tribeNames.Append(TribeName).ToArray();
            sequence2.displays = sequence2.displays.Append(gameObject2).ToArray();

            Button button = flagDisplay.GetComponentInChildren<Button>();
            button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            button.onClick.AddListener(() => { sequence2.Run(TribeName); });

            //(SfxOneShot)
            gameObject2.GetComponent<SfxOneshot>().eventRef = FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/card/draw_multi");

            //0: Flag (ImageSprite)
            gameObject2.transform.GetChild(0).GetComponent<ImageSprite>().SetSprite(tribe.flag);

            //1: Left (ImageSprite)
            Sprite needle = Tutorial5.instance.TryGet<CardData>("needleLeader").mainSprite;
            gameObject2.transform.GetChild(1).GetComponent<ImageSprite>().SetSprite(needle);

            //2: Right (ImageSprite)
            Sprite muncher = Tutorial5.instance.TryGet<CardData>("muncherLeader").mainSprite;
            gameObject2.transform.GetChild(2).GetComponent<ImageSprite>().SetSprite(muncher);
            gameObject2.transform.GetChild(2).localScale *= 1.2f;

            //3: Textbox (Image)
            gameObject2.transform.GetChild(3).GetComponent<Image>().color = new Color(0.12f, 0.47f, 0.57f);

            //3-0: Text (LocalizedString)
            StringTable collection = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English);
            gameObject2.transform.GetChild(3).GetChild(0).GetComponent<LocalizeStringEvent>().StringReference = collection.GetString(Tutorial5.instance.TribeDescKey);

            //4:Title Ribbon (Image)
            //4-0: Text (LocalizedString)
            gameObject2.transform.GetChild(4).GetChild(0).GetComponent<LocalizeStringEvent>().StringReference = collection.GetString(Tutorial5.instance.TribeTitleKey);
        }
    }
}
