﻿using Harmony;
using Oculus.Newtonsoft.Json;
using Oculus.Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UWE;

namespace UnKnownName
{
    [HarmonyPatch(typeof(MainMenuRightSide), nameof(MainMenuRightSide.Awake))]
    public class UnknownNameConfig
    {
        public static readonly string config = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
        public static string UnKnownLabel = "";
        public static string UnKnownTitle = "Unanalyzed Item";
        public static string UnKnownDescription = "This item has not been analyzed yet. \nTo discover its usefulness please scan it at your earliest convenience.";
        public static bool ScanOnPickup = false;
        public static bool Hardcore = false;

        [HarmonyPostfix]
        public static void Postfix()
        {
            Inventory_Pickup.newgame = true;
            if(File.Exists(config))
            {
                using(StreamReader reader = new StreamReader(config))
                {
                    JObject json = JsonConvert.DeserializeObject<JObject>(reader.ReadToEnd());
                    if(json["UnKnownLabel"] != null)
                    {
                        UnKnownLabel = json["UnKnownLabel"].ToString();
                    }
                    if(json["UnKnownTitle"] != null)
                    {
                        UnKnownTitle = json["UnKnownTitle"].ToString();
                    }
                    if(json["UnKnownDescription"] != null)
                    {
                        UnKnownDescription = json["UnKnownDescription"].ToString();
                    }
                    if(json["ScanOnPickup"] != null)
                    {
                        ScanOnPickup = json["ScanOnPickup"].ToString().ToLower() == "true";
                    }
                    if(json["Hardcore"] != null)
                    {
                        Hardcore = json["Hardcore"].ToString().ToLower() == "true";
                    }
                }
            }
            else
            {
                using(StreamWriter writer = new StreamWriter(config))
                {
                    JObject json = new JObject
                    {
                        ["UnKnownLabel"] = UnKnownLabel,
                        ["UnKnownTitle"] = UnKnownTitle,
                        ["UnKnownDescription"] = UnKnownDescription,
                        ["ScanOnPickup"] = ScanOnPickup,
                        ["Hardcore"] = Hardcore
                    };
                    writer.Write(json.ToString());
                }
            }
        }
    }

    [HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.IsCraftRecipeUnlocked))]
    public class CrafterLogic_IsCraftRecipeUnlocked
    {
        [HarmonyPostfix]
        public static void Postfix(TechType techType, ref bool __result)
        {
            if(UnknownNameConfig.Hardcore && GameModeUtils.RequiresBlueprints() && __result)
            {
#if SUBNAUTICA
                ITechData data = CraftData.Get(techType, true);
                if(techType != TechType.Titanium && data != null)
                {
                    int ingredientCount = data.ingredientCount;
                    for(int i = 0; i < ingredientCount; i++)
                    {
                        IIngredient ingredient = data.GetIngredient(i);
                        if(!CrafterLogic.IsCraftRecipeUnlocked(ingredient.techType))
                        {
                            __result = false;
                            return;
                        }
                    }
                }
#elif BELOWZERO
                if(techType != TechType.Titanium)
                {
                    List<Ingredient> data = TechData.GetIngredients(techType)?.ToList() ?? new List<Ingredient>();
                    int ingredientCount = data.Count;
                    for(int i = 0; i < ingredientCount; i++)
                    {
                        Ingredient ingredient = data[i];
                        if(!CrafterLogic.IsCraftRecipeUnlocked(ingredient.techType))
                        {
                            __result = false;
                            return;
                        }
                    }
                }
#endif
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public class Player_Start
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance)
        {
            if(File.Exists(UnknownNameConfig.config))
            {
                using(StreamReader reader = new StreamReader(UnknownNameConfig.config))
                {
                    JObject json = JsonConvert.DeserializeObject<JObject>(reader.ReadToEnd());
                    if(json["UnKnownLabel"] != null)
                    {
                        UnknownNameConfig.UnKnownLabel = json["UnKnownLabel"].ToString();
                    }
                    if(json["UnKnownTitle"] != null)
                    {
                        UnknownNameConfig.UnKnownTitle = json["UnKnownTitle"].ToString();
                    }
                    if(json["UnKnownDescription"] != null)
                    {
                        UnknownNameConfig.UnKnownDescription = json["UnKnownDescription"].ToString();
                    }
                    if(json["ScanOnPickup"] != null)
                    {
                        UnknownNameConfig.ScanOnPickup = json["ScanOnPickup"].ToString().ToLower() == "true";
                    }
                    if(json["Hardcore"] != null)
                    {
                        UnknownNameConfig.Hardcore = json["Hardcore"].ToString().ToLower() == "true";
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.WriteIngredients))]
    public class TooltipFactory_WriteIngredients
    {
#if SUBNAUTICA

        [HarmonyPostfix]
        public static void Postfix(ITechData data, ref List<TooltipIcon> icons)
        {
            int ingredientCount = data.ingredientCount;
            for(int i = 0; i < ingredientCount; i++)
            {
                IIngredient ingredient = data.GetIngredient(i);
                TechType techType = ingredient.techType;
                if(!KnownTech.Contains(techType) && PDAScanner.ContainsCompleteEntry(techType))
                {
                    KnownTech.Add(techType);
                }
                if(!KnownTech.Contains(techType) && GameModeUtils.RequiresBlueprints())
                {
                    TooltipIcon icon = icons.Find((TooltipIcon) => TooltipIcon.sprite == SpriteManager.Get(techType) && TooltipIcon.text.Contains(Language.main.GetOrFallback(TooltipFactory.techTypeIngredientStrings.Get(techType), techType)));
                    if(icons.Contains(icon))
                    {
                        icons.Remove(icon);
                        TooltipIcon tooltipIcon = new TooltipIcon() { sprite = SpriteManager.Get(TechType.None), text = UnknownNameConfig.UnKnownTitle };
                        icons.Add(tooltipIcon);
                    }
                }
            }
        }

#elif BELOWZERO

        [HarmonyPostfix]
        public static void Postfix(IList<Ingredient> ingredients, ref List<TooltipIcon> icons)
        {
            int ingredientCount = ingredients.Count;
            for(int i = 0; i < ingredientCount; i++)
            {
                TechType techType = ingredients[i].techType;
                if(!KnownTech.Contains(techType) && PDAScanner.ContainsCompleteEntry(techType))
                {
                    KnownTech.Add(techType, true);
                }
                if(!KnownTech.Contains(techType) && GameModeUtils.RequiresBlueprints())
                {
                    TooltipIcon icon = icons.Find((TooltipIcon) => TooltipIcon.sprite == SpriteManager.Get(techType) && TooltipIcon.text.Contains(Language.main.GetOrFallback(TooltipFactory.techTypeIngredientStrings.Get(techType), techType)));
                    if(icons.Contains(icon))
                    {
                        icons.Remove(icon);
                        TooltipIcon tooltipIcon = new TooltipIcon() { sprite = SpriteManager.Get(TechType.None), text = UnknownNameConfig.UnKnownTitle };
                        icons.Add(tooltipIcon);
                    }
                }
            }
        }

#endif
    }

    [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.Recipe))]
    public class TooltipFactory_Recipe
    {
        [HarmonyPostfix]
        public static void Postfix(bool locked, ref string tooltipText)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if(locked && GameModeUtils.RequiresBlueprints())
            {
                TooltipFactory.WriteTitle(stringBuilder, UnknownNameConfig.UnKnownTitle);
                TooltipFactory.WriteDescription(stringBuilder, UnknownNameConfig.UnKnownDescription);
                tooltipText = stringBuilder.ToString();
            }
        }
    }

    [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.ItemCommons))]
    public class TooltipFactory_InventoryItem
    {
        [HarmonyPostfix]
        public static void Postfix(ref StringBuilder sb, TechType techType, GameObject obj)
        {
            if(!PDAScanner.CanScan(obj) || PDAScanner.ContainsCompleteEntry(techType) || KnownTech.Contains(techType))
            {
                return;
            }
            sb.Clear();
            TooltipFactory.WriteTitle(sb, UnknownNameConfig.UnKnownTitle);
            TooltipFactory.WriteDescription(sb, UnknownNameConfig.UnKnownDescription);
        }
    }

    [HarmonyPatch(typeof(LanguageCache), nameof(LanguageCache.GetPickupText))]
    public class LanguageCache_GetPickupText
    {
        [HarmonyPostfix]
        public static void Postfix(ref string __result, TechType techType)
        {
            if(!KnownTech.Contains(techType) && GameModeUtils.RequiresBlueprints())
            {
                __result = UnknownNameConfig.UnKnownLabel;
            }
        }
    }

    [HarmonyPatch(typeof(ScannerTool), nameof(ScannerTool.OnHover))]
    public class ScannerTool_OnHover
    {
        [HarmonyPostfix]
        public static void Postfix(ScannerTool __instance)
        {
            PDAScanner.ScanTarget scanTarget = PDAScanner.scanTarget;
            PDAScanner.Result result = PDAScanner.CanScan();
            PDAScanner.EntryData entryData = PDAScanner.GetEntryData(PDAScanner.scanTarget.techType);

            if((entryData != null && (KnownTech.Contains(entryData.blueprint) || KnownTech.Contains(entryData.key))) || PDAScanner.ContainsCompleteEntry(scanTarget.techType) || __instance.energyMixin.charge <= 0f || !scanTarget.isValid || result != PDAScanner.Result.Scan || !GameModeUtils.RequiresBlueprints())
            {
                return;
            }
#if SUBNAUTICA
            HandReticle.main.SetInteractText(UnknownNameConfig.UnKnownLabel, false, HandReticle.Hand.None);
#elif BELOWZERO
            HandReticle.main.SetText(HandReticle.TextType.Hand, UnknownNameConfig.UnKnownLabel, true, GameInput.Button.None);
#endif
        }

    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Pickup))]
    public class Inventory_Pickup
    {
        public static bool newgame = true;

        [HarmonyPostfix]
        public static void Postfix(Inventory __instance, Pickupable pickupable)
        {
            if(newgame && UnknownNameConfig.Hardcore && !Utils.GetContinueMode() && pickupable.GetTechType() != TechType.FireExtinguisher)
            {
                Pickupable pickupable1 = CraftData.InstantiateFromPrefab(TechType.Scanner).GetComponent<Pickupable>();
                ScannerTool scannerTool = pickupable1.GetComponent<ScannerTool>();
                Pickupable pickupable2 = CraftData.InstantiateFromPrefab(TechType.Battery).GetComponent<Pickupable>();
                pickupable1.Pickup(false);
                pickupable2.Pickup(false);
                scannerTool.energyMixin.batterySlot.AddItem(pickupable2);
                Inventory.main.container.AddItem(pickupable1);
                newgame = false;
            }

            TechType techType = pickupable.GetTechType();
            PDAScanner.EntryData entryData = PDAScanner.GetEntryData(techType);
            GameObject gameObject = pickupable.gameObject;
            if(UnknownNameConfig.ScanOnPickup && Inventory.main.container.Contains(TechType.Scanner) && entryData != null)
            {
                if(!PDAScanner.GetPartialEntryByKey(techType, out PDAScanner.Entry entry))
                {
                    entry = PDAScanner.Add(techType, 1);
                }
                if(entry != null)
                {
                    PDAScanner.partial.Remove(entry);
                    PDAScanner.complete.Add(entry.techType);
                    PDAScanner.NotifyRemove(entry);
                    PDAScanner.Unlock(entryData, true, true, true);
                    KnownTech.Add(techType, false);
                    if(gameObject != null)
                    {
                        gameObject.SendMessage("OnScanned", null, SendMessageOptions.DontRequireReceiver);
                    }
                    ResourceTracker.UpdateFragments();
                }
            }
        }
    }

    [HarmonyPatch(typeof(KnownTech), nameof(KnownTech.Analyze))]
    public class KnownTech_Analyze
    {
        [HarmonyPrefix]
        public static bool Prefix(TechType techType)
        {
            PDAScanner.EntryData entryData = PDAScanner.GetEntryData(techType);
            return techType == TechType.None
                ? true
                : entryData == null
                ? true
                : PDAScanner.ContainsCompleteEntry(techType)
                ? true
                : string.IsNullOrEmpty(entryData.encyclopedia) || PDAEncyclopedia.ContainsEntry(entryData.encyclopedia);
        }
    }

    [HarmonyPatch(typeof(GUIHand), nameof(GUIHand.OnUpdate))]
    public class GUIHand_OnUpdate
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            PDAScanner.ScanTarget scanTarget = PDAScanner.scanTarget;
            PDAScanner.EntryData entryData = PDAScanner.GetEntryData(scanTarget.techType);
            TechType key = entryData?.key ?? TechType.None;
            TechType blueprint = entryData?.blueprint ?? TechType.None;

            if((entryData != null && ((blueprint!= TechType.None && KnownTech.Contains(entryData.blueprint)) || (key != TechType.None && KnownTech.Contains(entryData.key)))) || !scanTarget.isValid || PDAScanner.CanScan() != PDAScanner.Result.Scan || !GameModeUtils.RequiresBlueprints())
            {
                return;
            }
#if SUBNAUTICA
            HandReticle.main.SetInteractText(UnknownNameConfig.UnKnownLabel, false, HandReticle.Hand.None);
#elif BELOWZERO
            HandReticle.main.SetText(HandReticle.TextType.Hand, UnknownNameConfig.UnKnownLabel, true, GameInput.Button.None);
#endif
        }
    }

    [HarmonyPatch(typeof(PDAScanner), nameof(PDAScanner.Scan), new Type[] { })]
    public class PDAScanner_Scan
    {
        public static TechType techType;
        [HarmonyPrefix]
        public static void Prefix()
        {
            if(PDAScanner.scanTarget.techType != TechType.None)
            {
                techType = PDAScanner.scanTarget.techType;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(ref PDAScanner.Result __result)
        {
            if(__result != PDAScanner.Result.Scan && !KnownTech.Contains(techType))
            {
                KnownTech.Add(techType);
#if SUBNAUTICA
                TechType techType2 = CraftData.GetHarvestOutputData(techType);
#elif BELOWZERO
                TechType techType2 = TechData.GetHarvestOutput(techType);
#endif
                if(techType2 != TechType.None)
                {
                    KnownTech.Add(techType2);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Builder),nameof(Builder.UpdateAllowed))]
    public class Builder_UpdateAllowed
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            if(UnknownNameConfig.Hardcore && __result && Builder.prefab != null)
            {
                TechType techType = CraftData.GetTechType(Builder.prefab);
                __result = CrafterLogic.IsCraftRecipeUnlocked(techType);
            }
        }
    }

    [HarmonyPatch(typeof(PDAScanner), nameof(PDAScanner.Initialize))]
    public class PDAScanner_Iniialize
    {
        [HarmonyPrefix]
        public static void Prefix(PDAData pdaData)
        {
            if(UnknownNameConfig.Hardcore)
            {
                List<PDAScanner.EntryData> scanner = pdaData.scanner;
                List<TechType> haveEntries = new List<TechType>();
                List<TechType> missingEntries = new List<TechType>();

                foreach(PDAScanner.EntryData data in scanner)
                {
                    if(!haveEntries.Contains(data.key))
                    {
                        haveEntries.Add(data.key);
                    }
                }

                foreach(TechType techType in Enum.GetValues(typeof(TechType)))
                {
                    if(!haveEntries.Contains(techType))
                    {
                        missingEntries.Add(techType);
                    }
                }

                foreach(TechType tech in missingEntries)
                {
#if SUBNAUTICA
                    if(CraftData.Get(tech, true) == null || tech == TechType.Titanium)
                    {
#elif BELOWZERO
                    if((TechData.GetIngredients(tech) == null || tech == TechType.Titanium))
                    {
#endif
                        PDAScanner.EntryData entryData = new PDAScanner.EntryData()
                        {
                            key = tech,
                            destroyAfterScan = false,
                            isFragment = false,
                            locked = true,
                            scanTime = 2f,
                            totalFragments = 1
                        };
                        pdaData.scanner.Add(entryData);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(KnownTech), nameof(KnownTech.Initialize))]
    public class KnownTech_Initialize
    {
        [HarmonyPrefix]
        public static void Prefix(PDAData data)
        {
            List<TechType> types = data.defaultTech;
            List<TechType> removals = new List<TechType>();

            foreach(TechType techType in types)
            {
#if SUBNAUTICA
                if(techType == TechType.Titanium || CraftData.Get(techType, true) == null)
                {
#elif BELOWZERO
                if(techType == TechType.Titanium || TechData.GetIngredients(techType) == null)
                {
#endif
                    removals.Add(techType);
                }
            }

            foreach(TechType tech in removals)
            {
                data.defaultTech.Remove(tech);
            }
        }
    }
}