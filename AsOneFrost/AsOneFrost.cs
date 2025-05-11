using Deadpan.Enums.Engine.Components.Modding;
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

namespace AsOneFrost_Classes
{
    public class AsOneFrost : WildfrostMod
    {
        public override string GUID => "fg.wildfrost.asonefrost";

        public override string[] Depends => new string[0];

        public override string Title => "AsOneFrost";

        public override string Description => "Mods that mix some particular units as same tribes.";

        internal static AsOneFrost instance;

        public static void AddComponent(string name)
        {
            Campaign.instance.gameObject.AddComponentByName(name);
        }

        public AsOneFrost(string baseDirectory) : base(baseDirectory) { }

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
        private ClassDataBuilder TribeCopy(string oldName, string newName) => DataCopy<ClassData, ClassDataBuilder>(oldName, newName);
        private T DataCopy<Y, T>(string oldName, string newName) where Y : DataFile where T : DataFileBuilder<Y, T>, new()
        {
            Y data = Get<Y>(oldName).InstantiateKeepName();
            data.name = GUID + "." + newName;
            T builder = data.Edit<Y, T>();
            builder.Mod = this;
            return builder;
        }

        private T[] DataList<T>(params string[] names) where T : DataFile => names.Select((s) => TryGet<T>(s)).ToArray();

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

            assets.Add(new CardUpgradeDataBuilder(this)
                .Create("CardUpgradeSuperDraw")
                .WithTitle("Quickdraw Charm")
                .WithText($"Gain <keyword=draw> <2> and <keyword=zoomlin>")
                .WithType(CardUpgradeData.Type.Charm)
                .WithImage("blueDraw.png")
                .SetTraits(TStack("Draw", 2), TStack("Consume", 1))
                .FreeModify(
                (data) =>
                {
                    TargetConstraintIsItem item = ScriptableObject.CreateInstance<TargetConstraintIsItem>();
                    item.name = "Is Item";
                    TargetConstraintHasTrait consume = ScriptableObject.CreateInstance<TargetConstraintHasTrait>();
                    consume.name = "Does Not Have Consume";
                    consume.trait = TryGet<TraitData>("Consume");
                    consume.not = true;
                    data.targetConstraints = new TargetConstraint[] { item, consume };
                })
            );

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

            assets.Add(TribeCopy("Clunk", "Spice")
                .WithFlag("Images/DrawFlag.png")
                .WithSelectSfxEvent(FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/card/draw_multi"))
                .SubscribeToAfterAllBuildEvent(
                (data) =>
                {
                    GameObject gameObject = data.characterPrefab.gameObject.InstantiateKeepName();
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    gameObject.name = "Player (AsOneFrost.Spice)";
                    data.id = "AsOneFrost.Spice";
                    data.characterPrefab = gameObject.GetComponent<Character>();

                    data.leaders = DataList<CardData>("Pimento", "ShellWitch", "Tusk");

                    Inventory inventory = ScriptableObject.CreateInstance<Inventory>();
                    inventory.deck.list = DataList<CardData>("Dart", "Dart", "Dart", "Dart", "ShellShield", "BerryBasket", "SunlightDrum", "TigrisMask", "SnowCannon").ToList();
                    inventory.upgrades.Add(TryGet<CardUpgradeData>("Crown"));
                    data.startingInventory = inventory;

                    RewardPool unitPool = CreateRewardPool("SpiceUnitPool", "Units", DataList<CardData>(
                        "Firefist", "Pyra", "Fulbert",
                        "PepperWitch", "Kernel", "Shelly",
                        "Chompom", "Pecan", "Kokonut",
                        "Prickle", "Noggin", "BerryMonster",
                        "Dimona", "Pygmy", "Minimoko"));

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
                        "CardUpgradeTeethWhenHit", "CardUpgradePunchfist",
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
            GameMode gameMode = TryGet<GameMode>("GameModeNormal");
            gameMode.classes = gameMode.classes.Append(TryGet<ClassData>("Spice")).ToArray();

            //Events.OnEntityCreated += FixImage;
        }

        public override void Unload()
        {
            base.Unload();
            GameMode gameMode = TryGet<GameMode>("GameModeNormal");
            UnloadFromClasses();
            gameMode.classes = RemoveNulls(gameMode.classes);
            UnloadFromClasses();

            //Events.OnEntityCreated -= FixImage;
        }

        //private void FixImage(Entity entity)
        //{
            //Card card = entity.display as Card;
            //if (card.hasScriptableImage)
                //card.mainImage.gameObject.SetActive(true);
        //}

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
    }
}