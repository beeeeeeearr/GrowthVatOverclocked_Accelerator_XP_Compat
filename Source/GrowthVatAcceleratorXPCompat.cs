using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowthVatAcceleratorXPCompat
{
    public enum AcceleratorMode
    {
        Default = 0,
        Efficient = 1,
        Biodrive = 2,
        Powerdrive = 3,
        MaximumOverdrive = 4,
        MassProduce = 5
    }

    public struct AcceleratorModeValues
    {
        public float growth;
        public float nutrientUse;
        public float storage;
        public float power;

        public AcceleratorModeValues(float growth, float nutrientUse, float storage, float power)
        {
            this.growth = growth;
            this.nutrientUse = nutrientUse;
            this.storage = storage;
            this.power = power;
        }
    }

    public sealed class CompatSettings : ModSettings
    {
        // Efficient retains the old setting names for save compatibility with v13.
        public float growthSpeedPerAccelerator = 0.50f;
        public float nutrientUsePerAccelerator = 0.60f;
        public float nutrientStoragePerAccelerator = 1.00f;
        public float acceleratorPowerWatts = 400f;

        public float biodriveGrowth = 0.90f;
        public float biodriveNutrients = 1.50f;
        public float biodriveStorage = 2.00f;
        public float biodrivePower = 400f;

        public float powerdriveGrowth = 0.90f;
        public float powerdriveNutrients = 0.70f;
        public float powerdriveStorage = 1.25f;
        public float powerdrivePower = 1200f;

        public float maximumGrowth = 1.50f;
        public float maximumNutrients = 1.80f;
        public float maximumStorage = 2.50f;
        public float maximumPower = 1800f;

        public float massProduceGrowth = 1.50f;
        public float massProduceNutrients = 1.00f;
        public float massProduceStorage = 2.00f;
        public float massProducePower = 800f;
        public float massProduceSkillFactor = 0.25f;
        public float massProduceGrowthPointFactor = 0.20f;

        public AcceleratorMode newAcceleratorMode = AcceleratorMode.Efficient;
        public bool compensateSkillXp = true;
        public bool repairAndScaleGrowthPoints = true;
        public bool ignoreVatLearningSaturation = true;
        public bool showFacingIndicator = true;
        public bool enableBiodroneEmbryoCompat = true;
        public bool verboseDevLogging = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref growthSpeedPerAccelerator, "growthSpeedPerAccelerator", 0.50f);
            Scribe_Values.Look(ref nutrientUsePerAccelerator, "nutrientUsePerAccelerator", 0.60f);
            Scribe_Values.Look(ref nutrientStoragePerAccelerator, "nutrientStoragePerAccelerator", 1.00f);
            Scribe_Values.Look(ref acceleratorPowerWatts, "acceleratorPowerWatts", 400f);

            Scribe_Values.Look(ref biodriveGrowth, "biodriveGrowth", 0.90f);
            Scribe_Values.Look(ref biodriveNutrients, "biodriveNutrients", 1.50f);
            Scribe_Values.Look(ref biodriveStorage, "biodriveStorage", 2.00f);
            Scribe_Values.Look(ref biodrivePower, "biodrivePower", 400f);

            Scribe_Values.Look(ref powerdriveGrowth, "powerdriveGrowth", 0.90f);
            Scribe_Values.Look(ref powerdriveNutrients, "powerdriveNutrients", 0.70f);
            Scribe_Values.Look(ref powerdriveStorage, "powerdriveStorage", 1.25f);
            Scribe_Values.Look(ref powerdrivePower, "powerdrivePower", 1200f);

            Scribe_Values.Look(ref maximumGrowth, "maximumGrowth", 1.50f);
            Scribe_Values.Look(ref maximumNutrients, "maximumNutrients", 1.80f);
            Scribe_Values.Look(ref maximumStorage, "maximumStorage", 2.50f);
            Scribe_Values.Look(ref maximumPower, "maximumPower", 1800f);

            Scribe_Values.Look(ref massProduceGrowth, "massProduceGrowth", 1.50f);
            Scribe_Values.Look(ref massProduceNutrients, "massProduceNutrients", 1.00f);
            Scribe_Values.Look(ref massProduceStorage, "massProduceStorage", 2.00f);
            Scribe_Values.Look(ref massProducePower, "massProducePower", 800f);
            Scribe_Values.Look(ref massProduceSkillFactor, "massProduceSkillFactor", 0.25f);
            Scribe_Values.Look(ref massProduceGrowthPointFactor, "massProduceGrowthPointFactor", 0.20f);

            Scribe_Values.Look(ref newAcceleratorMode, "newAcceleratorMode", AcceleratorMode.Efficient);
            Scribe_Values.Look(ref compensateSkillXp, "compensateSkillXp", true);
            Scribe_Values.Look(ref repairAndScaleGrowthPoints, "repairAndScaleGrowthPoints", true);
            Scribe_Values.Look(ref ignoreVatLearningSaturation, "ignoreVatLearningSaturation", true);
            Scribe_Values.Look(ref showFacingIndicator, "showFacingIndicator", true);
            Scribe_Values.Look(ref enableBiodroneEmbryoCompat, "enableBiodroneEmbryoCompat", true);
            Scribe_Values.Look(ref verboseDevLogging, "verboseDevLogging", true);
        }

        public AcceleratorModeValues ValuesFor(AcceleratorMode mode)
        {
            switch (mode)
            {
                case AcceleratorMode.Default: return DefAdjuster.OriginalValues;
                case AcceleratorMode.Biodrive: return new AcceleratorModeValues(biodriveGrowth, biodriveNutrients, biodriveStorage, biodrivePower);
                case AcceleratorMode.Powerdrive: return new AcceleratorModeValues(powerdriveGrowth, powerdriveNutrients, powerdriveStorage, powerdrivePower);
                case AcceleratorMode.MaximumOverdrive: return new AcceleratorModeValues(maximumGrowth, maximumNutrients, maximumStorage, maximumPower);
                case AcceleratorMode.MassProduce: return new AcceleratorModeValues(massProduceGrowth, massProduceNutrients, massProduceStorage, massProducePower);
                default: return new AcceleratorModeValues(growthSpeedPerAccelerator, nutrientUsePerAccelerator, nutrientStoragePerAccelerator, acceleratorPowerWatts);
            }
        }

        public void ResetBalancedPresets()
        {
            growthSpeedPerAccelerator = 0.50f;
            nutrientUsePerAccelerator = 0.60f;
            nutrientStoragePerAccelerator = 1.00f;
            acceleratorPowerWatts = 400f;
            biodriveGrowth = 0.90f;
            biodriveNutrients = 1.50f;
            biodriveStorage = 2.00f;
            biodrivePower = 400f;
            powerdriveGrowth = 0.90f;
            powerdriveNutrients = 0.70f;
            powerdriveStorage = 1.25f;
            powerdrivePower = 1200f;
            maximumGrowth = 1.50f;
            maximumNutrients = 1.80f;
            maximumStorage = 2.50f;
            maximumPower = 1800f;
            massProduceGrowth = 1.50f;
            massProduceNutrients = 1.00f;
            massProduceStorage = 2.00f;
            massProducePower = 800f;
            massProduceSkillFactor = 0.25f;
            massProduceGrowthPointFactor = 0.20f;
        }
    }

    public sealed class CompatMod : Mod
    {
        internal static CompatSettings Settings;
        private Vector2 scroll;
        private float viewHeight = 1050f;
        private static readonly Dictionary<AcceleratorMode, string> powerBuffers = new Dictionary<AcceleratorMode, string>();

        public CompatMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CompatSettings>();
        }

        public override string SettingsCategory() { return "Growth Vat Accelerator Compat"; }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect outRect = inRect;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);
            Widgets.BeginScrollView(outRect, ref scroll, viewRect);
            Listing_Standard list = new Listing_Standard();
            list.Begin(viewRect);

            list.Label("Preset used by newly built accelerators: " + ModeLabel(Settings.newAcceleratorMode));
            Rect presetRow = list.GetRect(68f);
            float bw = (presetRow.width - 8f) / 3f;
            DrawPresetButton(new Rect(presetRow.x, presetRow.y, bw, 30f), AcceleratorMode.Default);
            DrawPresetButton(new Rect(presetRow.x + (bw + 4f), presetRow.y, bw, 30f), AcceleratorMode.Efficient);
            DrawPresetButton(new Rect(presetRow.x + 2f * (bw + 4f), presetRow.y, bw, 30f), AcceleratorMode.Biodrive);
            DrawPresetButton(new Rect(presetRow.x, presetRow.y + 34f, bw, 30f), AcceleratorMode.Powerdrive);
            DrawPresetButton(new Rect(presetRow.x + (bw + 4f), presetRow.y + 34f, bw, 30f), AcceleratorMode.MaximumOverdrive);
            DrawPresetButton(new Rect(presetRow.x + 2f * (bw + 4f), presetRow.y + 34f, bw, 30f), AcceleratorMode.MassProduce);
            list.Label("Preset buttons set the default for new accelerators and immediately apply that mode to every existing accelerator on loaded maps. Default restores Growth Accelerator's original values and is not shown on individual-building gizmos.");
            list.GapLine();

            DrawModeEditor(list, AcceleratorMode.Efficient, "Efficient", ref Settings.growthSpeedPerAccelerator, ref Settings.nutrientUsePerAccelerator, ref Settings.nutrientStoragePerAccelerator, ref Settings.acceleratorPowerWatts);
            DrawModeEditor(list, AcceleratorMode.Biodrive, "Biodrive", ref Settings.biodriveGrowth, ref Settings.biodriveNutrients, ref Settings.biodriveStorage, ref Settings.biodrivePower);
            DrawModeEditor(list, AcceleratorMode.Powerdrive, "Powerdrive", ref Settings.powerdriveGrowth, ref Settings.powerdriveNutrients, ref Settings.powerdriveStorage, ref Settings.powerdrivePower);
            DrawModeEditor(list, AcceleratorMode.MaximumOverdrive, "MAXIMUM OVERDRIVE", ref Settings.maximumGrowth, ref Settings.maximumNutrients, ref Settings.maximumStorage, ref Settings.maximumPower);
            DrawModeEditor(list, AcceleratorMode.MassProduce, "Mass Produce", ref Settings.massProduceGrowth, ref Settings.massProduceNutrients, ref Settings.massProduceStorage, ref Settings.massProducePower);
            list.Label("Mass Produce skill XP earned: " + Settings.massProduceSkillFactor.ToString("P0"));
            Settings.massProduceSkillFactor = list.Slider(Settings.massProduceSkillFactor, 0f, 1f);
            list.Label("Mass Produce growth points earned: " + Settings.massProduceGrowthPointFactor.ToString("P0"));
            Settings.massProduceGrowthPointFactor = list.Slider(Settings.massProduceGrowthPointFactor, 0f, 1f);
            list.Label("When a vat occupant is younger than age 3, every linked accelerator temporarily operates as Efficient. The accelerator retains its selected mode and automatically resumes it when growth points and vat skill XP become available.");
            list.GapLine();

            if (list.ButtonText("Restore balanced preset values"))
            {
                Settings.ResetBalancedPresets();
                powerBuffers.Clear();
                DefAdjuster.RefreshAllAccelerators();
            }
            list.GapLine();
            list.CheckboxLabeled("Compensate Growth Vats: Overclocked skill XP for actual vat speed", ref Settings.compensateSkillXp,
                "Uses the XP configured for the active Overclocked mode, then multiplies it by the vat's live growth-speed multiplier, including mixed per-accelerator drive modes.");
            list.CheckboxLabeled("Repair and scale child growth points with actual vat speed", ref Settings.repairAndScaleGrowthPoints,
                "Growth points use the vat's live combined accelerator speed, including mixed per-accelerator drive modes.");
            list.CheckboxLabeled("Ignore daily skill-learning saturation for vat XP", ref Settings.ignoreVatLearningSaturation);
            list.CheckboxLabeled("Show accelerator facing tile while placing", ref Settings.showFacingIndicator);
            list.CheckboxLabeled("Enable Biodrone embryo growth-vat compatibility", ref Settings.enableBiodroneEmbryoCompat);
            list.CheckboxLabeled("Verbose Developer Mode logging", ref Settings.verboseDevLogging);
            list.Gap();
            if (list.ButtonText("Apply edited mode values now"))
            {
                DefAdjuster.RefreshAllAccelerators();
                Messages.Message("Accelerator mode values refreshed on all loaded maps.", MessageTypeDefOf.NeutralEvent, false);
            }

            viewHeight = Math.Max(1000f, list.CurHeight + 30f);
            list.End();
            Widgets.EndScrollView();
        }

        private static void DrawPresetButton(Rect rect, AcceleratorMode mode)
        {
            if (Widgets.ButtonText(rect, ModeLabel(mode)))
            {
                Settings.newAcceleratorMode = mode;
                DefAdjuster.ApplyModeToAll(mode);
            }
        }

        private static void DrawModeEditor(Listing_Standard list, AcceleratorMode mode, string title,
            ref float growth, ref float nutrients, ref float storage, ref float power)
        {
            list.Label(title + " mode");
            list.Label("Growth added per accelerator: " + growth.ToString("P0"));
            growth = list.Slider(growth, 0f, 3f);
            list.Label("Nutrient use added per accelerator: " + nutrients.ToString("P0"));
            nutrients = list.Slider(nutrients, 0f, 4f);
            list.Label("Nutrient storage added per accelerator: " + storage.ToString("0.00"));
            storage = list.Slider(storage, 0f, 10f);

            Rect row = list.GetRect(30f);
            Widgets.Label(new Rect(row.x, row.y, row.width * 0.52f, row.height), "Power per accelerator (W):");
            string buffer;
            if (!powerBuffers.TryGetValue(mode, out buffer)) buffer = Math.Round(power).ToString();
            Rect textRect = new Rect(row.x + row.width * 0.53f, row.y, row.width * 0.20f, 28f);
            string controlName = "GVA_Power_" + mode;
            GUI.SetNextControlName(controlName);
            buffer = Widgets.TextField(textRect, buffer);
            powerBuffers[mode] = buffer;
            float parsed;
            if (float.TryParse(buffer, out parsed)) power = Mathf.Clamp(parsed, 0f, 10000f);
            Rect sliderRect = new Rect(row.x + row.width * 0.75f, row.y, row.width * 0.25f, 28f);
            float sliderValue = Widgets.HorizontalSlider(sliderRect, power, 0f, 10000f, true, Math.Round(power) + " W");
            bool textFocused = GUI.GetNameOfFocusedControl() == controlName;
            if (!textFocused && Math.Abs(sliderValue - power) > 0.001f) power = sliderValue;
            if (!textFocused) powerBuffers[mode] = Math.Round(power).ToString();
            list.GapLine();
        }

        internal static string ModeLabel(AcceleratorMode mode)
        {
            switch (mode)
            {
                case AcceleratorMode.Default: return "Default";
                case AcceleratorMode.Biodrive: return "Biodrive";
                case AcceleratorMode.Powerdrive: return "Powerdrive";
                case AcceleratorMode.MaximumOverdrive: return "MAXIMUM OVERDRIVE";
                case AcceleratorMode.MassProduce: return "Mass Produce";
                default: return "Efficient";
            }
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            DefAdjuster.RefreshAllAccelerators();
        }
    }

    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            if (CompatMod.Settings == null) CompatMod.Settings = new CompatSettings();
            DefAdjuster.InstallModeCompAndCaptureOriginals();
            DefAdjuster.ApplyBiodroneEmbryoCompatibility();
            new Harmony("bk.gvo.growthaccelerator.xpcompat").PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[GVO + Growth Accelerator XP Compat] Loaded v15 with Mass Produce mode and automatic newborn Efficient override.");
        }
    }

    public sealed class CompProperties_AcceleratorDriveMode : CompProperties
    {
        public CompProperties_AcceleratorDriveMode() { compClass = typeof(CompAcceleratorDriveMode); }
    }

    public sealed class CompAcceleratorDriveMode : ThingComp
    {
        private AcceleratorMode mode = AcceleratorMode.Efficient;
        private AcceleratorMode appliedMode = (AcceleratorMode)(-1);
        private bool initialized;

        public AcceleratorMode Mode { get { return mode; } }
        public AcceleratorMode EffectiveMode { get { return DetermineEffectiveMode(); } }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && !initialized)
                mode = CompatMod.Settings == null ? AcceleratorMode.Efficient : CompatMod.Settings.newAcceleratorMode;
            initialized = true;
            ApplyMode(true);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref mode, "acceleratorDriveMode", AcceleratorMode.Efficient);
            Scribe_Values.Look(ref initialized, "acceleratorDriveModeInitialized", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                appliedMode = (AcceleratorMode)(-1);
                ApplyMode(true);
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent != null && parent.IsHashIntervalTick(60)) ApplyMode(false);
        }

        public void SetMode(AcceleratorMode newMode)
        {
            mode = newMode;
            initialized = true;
            ApplyMode(true);
            Messages.Message(parent.LabelCap + " set to " + CompatMod.ModeLabel(mode) + ".", parent, MessageTypeDefOf.NeutralEvent, false);
        }

        public void ApplyMode() { ApplyMode(true); }

        private void ApplyMode(bool force)
        {
            if (parent == null || CompatMod.Settings == null) return;
            AcceleratorMode effective = DetermineEffectiveMode();
            if (!force && effective == appliedMode) return;
            appliedMode = effective;
            DefAdjuster.ApplyValuesToInstance(parent, CompatMod.Settings.ValuesFor(effective));
        }

        private AcceleratorMode DetermineEffectiveMode()
        {
            if (parent == null) return mode;
            Building_GrowthVat vat = AcceleratorRuntime.FindLinkedVat(parent);
            Pawn occupant = AcceleratorRuntime.FindVatOccupant(vat);
            if (occupant != null && occupant.ageTracker != null && occupant.ageTracker.AgeBiologicalYearsFloat < 3f)
                return AcceleratorMode.Efficient;
            return mode;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
            yield return MakeCommand(AcceleratorMode.Efficient, "Balanced baseline: +50% growth, +60% nutrients, 400 W by default.");
            yield return MakeCommand(AcceleratorMode.Biodrive, "Favors power efficiency and pays primarily in nutrient throughput.");
            yield return MakeCommand(AcceleratorMode.Powerdrive, "Favors nutrient efficiency and pays primarily in electrical power.");
            yield return MakeCommand(AcceleratorMode.MaximumOverdrive, "Highest quality growth, with slightly worse efficiency in both resources.");
            yield return MakeCommand(AcceleratorMode.MassProduce, "Rapid low-cost aging: +150% growth, +100% nutrients, 800 W, but only 25% skill XP and 20% growth points. Newborn occupants temporarily use Efficient.");
        }

        private Command_Action MakeCommand(AcceleratorMode target, string description)
        {
            Command_Action command = new Command_Action();
            command.defaultLabel = CompatMod.ModeLabel(target);
            command.defaultDesc = description;
            command.action = delegate { SetMode(target); };
            if (mode == target) command.Disable("Already selected");
            return command;
        }

        public override string CompInspectStringExtra()
        {
            AcceleratorMode effective = DetermineEffectiveMode();
            AcceleratorModeValues v = CompatMod.Settings.ValuesFor(effective);
            string text = "Selected mode: " + CompatMod.ModeLabel(mode);
            if (effective != mode)
                text += "\nEffective mode: " + CompatMod.ModeLabel(effective)
                    + "\nReason: occupant is not yet eligible for growth points or vat skill XP";
            text += "\nGrowth: +" + v.growth.ToString("P0")
                + " | Nutrients: +" + v.nutrientUse.ToString("P0")
                + " | Storage: +" + v.storage.ToString("0.##")
                + " | Power: " + Math.Round(v.power) + " W";
            if (effective == AcceleratorMode.MassProduce)
                text += "\nSkill XP: " + CompatMod.Settings.massProduceSkillFactor.ToString("P0")
                    + " | Growth points: " + CompatMod.Settings.massProduceGrowthPointFactor.ToString("P0");
            return text;
        }
    }

    internal static class AcceleratorRuntime
    {
        private const string AcceleratorDefName = "kathanon_GrowthAccelerator_GrowthVatAccelerator";

        internal static Building_GrowthVat FindLinkedVat(ThingWithComps accelerator)
        {
            if (accelerator == null) return null;
            CompFacility facility = accelerator.AllComps.OfType<CompFacility>().FirstOrDefault();
            if (facility == null) return null;
            try
            {
                PropertyInfo property = AccessTools.Property(facility.GetType(), "LinkedBuildings")
                    ?? AccessTools.Property(typeof(CompFacility), "LinkedBuildings");
                object linked = property == null ? null : property.GetValue(facility, null);
                IEnumerable enumerable = linked as IEnumerable;
                if (enumerable != null)
                    foreach (object item in enumerable)
                    {
                        Building_GrowthVat vat = item as Building_GrowthVat;
                        if (vat != null) return vat;
                    }
            }
            catch { }
            return null;
        }

        internal static Pawn FindVatOccupant(Building_GrowthVat vat)
        {
            if (vat == null) return null;
            try
            {
                MethodInfo directlyHeld = AccessTools.Method(vat.GetType(), "GetDirectlyHeldThings");
                object owner = directlyHeld == null ? null : directlyHeld.Invoke(vat, null);
                Pawn pawn = FindPawnInObject(owner, 0);
                if (pawn != null) return pawn;

                foreach (string name in new[] { "SelectedPawn", "ContainedPawn", "Occupant", "ContainedThing" })
                {
                    PropertyInfo property = AccessTools.Property(vat.GetType(), name);
                    pawn = property == null ? null : FindPawnInObject(property.GetValue(vat, null), 0);
                    if (pawn != null) return pawn;
                    FieldInfo field = AccessTools.Field(vat.GetType(), name) ?? AccessTools.Field(vat.GetType(), char.ToLowerInvariant(name[0]) + name.Substring(1));
                    pawn = field == null ? null : FindPawnInObject(field.GetValue(vat), 0);
                    if (pawn != null) return pawn;
                }
            }
            catch { }
            return null;
        }

        private static Pawn FindPawnInObject(object value, int depth)
        {
            if (value == null || depth > 3) return null;
            Pawn pawn = value as Pawn;
            if (pawn != null) return pawn;
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null && !(value is string))
            {
                foreach (object item in enumerable)
                {
                    pawn = FindPawnInObject(item, depth + 1);
                    if (pawn != null) return pawn;
                }
            }
            return null;
        }

        internal static Building_GrowthVat FindGrowthVat(Pawn pawn)
        {
            if (pawn == null) return null;
            object holder = pawn.ParentHolder;
            for (int depth = 0; depth < 8 && holder != null; depth++)
            {
                Building_GrowthVat vat = holder as Building_GrowthVat;
                if (vat != null) return vat;
                object next = null;
                try
                {
                    PropertyInfo ownerProperty = AccessTools.Property(holder.GetType(), "Owner");
                    if (ownerProperty != null) next = ownerProperty.GetValue(holder, null);
                    if (next == null)
                    {
                        PropertyInfo parentProperty = AccessTools.Property(holder.GetType(), "ParentHolder");
                        if (parentProperty != null) next = parentProperty.GetValue(holder, null);
                    }
                    if (next == null)
                    {
                        FieldInfo ownerField = AccessTools.Field(holder.GetType(), "owner");
                        if (ownerField != null) next = ownerField.GetValue(holder);
                    }
                }
                catch { }
                if (ReferenceEquals(next, holder)) break;
                holder = next;
            }
            return null;
        }

        internal static float QualityFactor(Building_GrowthVat vat, bool forGrowthPoints)
        {
            if (vat == null || vat.Map == null || CompatMod.Settings == null) return 1f;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(AcceleratorDefName);
            if (def == null) return 1f;
            float totalGrowth = 0f;
            float weightedQuality = 0f;
            foreach (Thing thing in vat.Map.listerThings.ThingsOfDef(def))
            {
                ThingWithComps accelerator = thing as ThingWithComps;
                if (accelerator == null || !IsOperational(accelerator) || FindLinkedVat(accelerator) != vat) continue;
                CompAcceleratorDriveMode comp = accelerator.GetComp<CompAcceleratorDriveMode>();
                if (comp == null) continue;
                AcceleratorMode effective = comp.EffectiveMode;
                float growth = Math.Max(0f, CompatMod.Settings.ValuesFor(effective).growth);
                float quality = effective == AcceleratorMode.MassProduce
                    ? (forGrowthPoints ? CompatMod.Settings.massProduceGrowthPointFactor : CompatMod.Settings.massProduceSkillFactor)
                    : 1f;
                totalGrowth += growth;
                weightedQuality += growth * quality;
            }
            return totalGrowth <= 0.0001f ? 1f : Mathf.Clamp01(weightedQuality / totalGrowth);
        }

        private static bool IsOperational(ThingWithComps accelerator)
        {
            CompPowerTrader power = accelerator.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn) return false;
            CompFlickable flick = accelerator.GetComp<CompFlickable>();
            if (flick != null && !flick.SwitchIsOn) return false;
            return true;
        }
    }

    internal static class DefAdjuster
    {
        private const string AcceleratorDefName = "kathanon_GrowthAccelerator_GrowthVatAccelerator";
        private const string GrowthSpeedStat = "kathanon_GrowthAccelerator_GrowthVatSpeed";
        private const string NutrientUseStat = "kathanon_GrowthAccelerator_GrowthVatNutrientUse";
        private const string NutrientStorageStat = "kathanon_GrowthAccelerator_GrowthVatNutrientStorage";
        private static AcceleratorModeValues originalValues = new AcceleratorModeValues(0.50f, 0f, 0f, 0f);
        private static bool originalsCaptured;

        internal static AcceleratorModeValues OriginalValues { get { return originalValues; } }

        internal static void InstallModeCompAndCaptureOriginals()
        {
            ThingDef accelerator = DefDatabase<ThingDef>.GetNamedSilentFail(AcceleratorDefName);
            if (accelerator == null) return;
            if (accelerator.comps == null) accelerator.comps = new List<CompProperties>();

            CompProperties_Facility facility = accelerator.comps.OfType<CompProperties_Facility>().FirstOrDefault();
            CompProperties_Power power = accelerator.comps.OfType<CompProperties_Power>().FirstOrDefault();
            if (!originalsCaptured)
            {
                originalValues = new AcceleratorModeValues(
                    GetOffset(facility, GrowthSpeedStat), GetOffset(facility, NutrientUseStat),
                    GetOffset(facility, NutrientStorageStat), power == null ? 0f : Math.Max(0f, GetPowerConsumption(power)));
                originalsCaptured = true;
            }

            if (!accelerator.comps.OfType<CompProperties_AcceleratorDriveMode>().Any())
                accelerator.comps.Add(new CompProperties_AcceleratorDriveMode());

            // Def-level values are only a safe fallback. Spawned buildings receive cloned per-instance properties.
            ApplyValuesToDef(accelerator, CompatMod.Settings.ValuesFor(CompatMod.Settings.newAcceleratorMode));
        }

        internal static void ApplyValuesToInstance(ThingWithComps accelerator, AcceleratorModeValues values)
        {
            if (accelerator == null) return;
            CompFacility facility = accelerator.AllComps.OfType<CompFacility>().FirstOrDefault();
            if (facility != null)
            {
                CompProperties_Facility clone = CloneProps(facility.props as CompProperties_Facility);
                if (clone != null)
                {
                    clone.statOffsets = clone.statOffsets == null ? new List<StatModifier>() : clone.statOffsets.Select(CloneModifier).ToList();
                    SetOffset(clone.statOffsets, GrowthSpeedStat, values.growth);
                    SetOffset(clone.statOffsets, NutrientUseStat, values.nutrientUse);
                    SetOffset(clone.statOffsets, NutrientStorageStat, values.storage);
                    facility.props = clone;
                }
            }

            CompPowerTrader power = accelerator.AllComps.OfType<CompPowerTrader>().FirstOrDefault();
            if (power != null)
            {
                CompProperties_Power clone = CloneProps(power.props as CompProperties_Power);
                if (clone != null)
                {
                    SetPowerConsumption(clone, Math.Max(0f, values.power));
                    power.props = clone;
                    power.PowerOutput = -Math.Max(0f, values.power);
                }
            }
        }

        internal static void ApplyModeToAll(AcceleratorMode mode)
        {
            if (Find.Maps == null) return;
            foreach (Map map in Find.Maps)
            {
                if (map == null || map.listerThings == null) continue;
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(AcceleratorDefName);
                if (def == null) continue;
                foreach (Thing thing in map.listerThings.ThingsOfDef(def).ToList())
                {
                    ThingWithComps thingWithComps = thing as ThingWithComps;
                    CompAcceleratorDriveMode comp = thingWithComps == null ? null : thingWithComps.GetComp<CompAcceleratorDriveMode>();
                    if (comp != null) comp.SetMode(mode);
                }
            }
            ApplyValuesToDef(DefDatabase<ThingDef>.GetNamedSilentFail(AcceleratorDefName), CompatMod.Settings.ValuesFor(mode));
        }

        internal static void RefreshAllAccelerators()
        {
            InstallModeCompAndCaptureOriginals();
            if (Find.Maps == null) return;
            foreach (Map map in Find.Maps)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(AcceleratorDefName);
                if (map == null || def == null) continue;
                foreach (Thing thing in map.listerThings.ThingsOfDef(def).ToList())
                {
                    ThingWithComps thingWithComps = thing as ThingWithComps;
                    CompAcceleratorDriveMode comp = thingWithComps == null ? null : thingWithComps.GetComp<CompAcceleratorDriveMode>();
                    if (comp != null) comp.ApplyMode();
                }
            }
        }

        private static void ApplyValuesToDef(ThingDef accelerator, AcceleratorModeValues values)
        {
            if (accelerator == null || accelerator.comps == null) return;
            CompProperties_Facility facility = accelerator.comps.OfType<CompProperties_Facility>().FirstOrDefault();
            if (facility != null)
            {
                if (facility.statOffsets == null) facility.statOffsets = new List<StatModifier>();
                SetOffset(facility.statOffsets, GrowthSpeedStat, values.growth);
                SetOffset(facility.statOffsets, NutrientUseStat, values.nutrientUse);
                SetOffset(facility.statOffsets, NutrientStorageStat, values.storage);
            }
            CompProperties_Power power = accelerator.comps.OfType<CompProperties_Power>().FirstOrDefault();
            if (power != null) SetPowerConsumption(power, Math.Max(0f, values.power));
        }

        private static float GetPowerConsumption(CompProperties_Power props)
        {
            if (props == null) return 0f;
            try
            {
                FieldInfo field = AccessTools.Field(props.GetType(), "basePowerConsumption")
                    ?? AccessTools.Field(props.GetType(), "powerConsumption");
                if (field != null) return Convert.ToSingle(field.GetValue(props));
                PropertyInfo property = AccessTools.Property(props.GetType(), "basePowerConsumption")
                    ?? AccessTools.Property(props.GetType(), "PowerConsumption")
                    ?? AccessTools.Property(props.GetType(), "powerConsumption");
                if (property != null && property.CanRead) return Convert.ToSingle(property.GetValue(props, null));
            }
            catch { }
            return 0f;
        }

        private static void SetPowerConsumption(CompProperties_Power props, float watts)
        {
            if (props == null) return;
            try
            {
                FieldInfo field = AccessTools.Field(props.GetType(), "basePowerConsumption")
                    ?? AccessTools.Field(props.GetType(), "powerConsumption");
                if (field != null)
                {
                    field.SetValue(props, watts);
                    return;
                }
                PropertyInfo property = AccessTools.Property(props.GetType(), "basePowerConsumption")
                    ?? AccessTools.Property(props.GetType(), "PowerConsumption")
                    ?? AccessTools.Property(props.GetType(), "powerConsumption");
                if (property != null && property.CanWrite) property.SetValue(props, watts, null);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Could not update accelerator power properties: " + ex, 198740252);
            }
        }

        private static T CloneProps<T>(T source) where T : CompProperties
        {
            if (source == null) return null;
            try { return (T)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(source, null); }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Could not clone per-instance comp properties: " + ex, 198740251);
                return null;
            }
        }

        private static StatModifier CloneModifier(StatModifier source)
        {
            return source == null ? null : new StatModifier { stat = source.stat, value = source.value };
        }

        private static float GetOffset(CompProperties_Facility facility, string statDefName)
        {
            if (facility == null || facility.statOffsets == null) return 0f;
            StatModifier modifier = facility.statOffsets.FirstOrDefault(x => x != null && x.stat != null && x.stat.defName == statDefName);
            return modifier == null ? 0f : modifier.value;
        }

        private static void SetOffset(List<StatModifier> offsets, string statDefName, float value)
        {
            StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(statDefName);
            if (stat == null) return;
            StatModifier modifier = offsets.FirstOrDefault(x => x != null && x.stat == stat);
            if (modifier == null)
            {
                modifier = new StatModifier { stat = stat };
                offsets.Add(modifier);
            }
            modifier.value = value;
        }

        internal static bool IsBiodroneEmbryo(Thing thing)
        {
            return thing != null && IsBiodroneEmbryoDef(thing.def);
        }

        internal static bool IsBiodroneEmbryoDef(ThingDef def)
        {
            if (def == null) return false;
            string text = ((def.defName ?? "") + " " + (def.label ?? "")).ToLowerInvariant();
            return text.Contains("embryo") || (text.Contains("biodrone") && text.Contains("ovum"));
        }

        private static FieldInfo humanEmbryoGeneSetField;
        private static FieldInfo geneTrackerPawnField;

        internal static bool EnsureBiodroneEmbryoGeneSet(Thing thing)
        {
            if (!CompatMod.Settings.enableBiodroneEmbryoCompat || !IsBiodroneEmbryo(thing)) return false;
            HumanEmbryo embryo = thing as HumanEmbryo;
            if (embryo == null) return false;

            try
            {
                if (humanEmbryoGeneSetField == null)
                    humanEmbryoGeneSetField = AccessTools.Field(typeof(HumanEmbryo), "geneSet");
                if (humanEmbryoGeneSetField == null)
                {
                    Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] HumanEmbryo.geneSet was not found; Biodrone embryo genes cannot be initialized.", 198740231);
                    return false;
                }

                if (humanEmbryoGeneSetField.GetValue(embryo) != null) return false;
                XenotypeDef biodrone = DefDatabase<XenotypeDef>.GetNamedSilentFail("CYB_Biodrone");
                if (biodrone == null) return false;

                GeneSet geneSet = new GeneSet();
                foreach (GeneDef gene in biodrone.AllGenes)
                    geneSet.AddGene(gene);
                humanEmbryoGeneSetField.SetValue(embryo, geneSet);

                if (Prefs.DevMode && CompatMod.Settings.verboseDevLogging)
                    Log.Message("[GVO + Growth Accelerator XP Compat] Initialized Biodrone embryo gene set with " + biodrone.AllGenes.Count + " genes.");
                return true;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Could not initialize Biodrone embryo genes: " + ex, 198740232);
                return false;
            }
        }

        internal static Pawn PawnForGeneTracker(Pawn_GeneTracker tracker)
        {
            if (tracker == null) return null;
            try
            {
                if (geneTrackerPawnField == null)
                    geneTrackerPawnField = AccessTools.Field(typeof(Pawn_GeneTracker), "pawn");
                return geneTrackerPawnField == null ? null : geneTrackerPawnField.GetValue(tracker) as Pawn;
            }
            catch { return null; }
        }

        internal static int EnsureBiodronePawnGenes(Pawn pawn)
        {
            if (!CompatMod.Settings.enableBiodroneEmbryoCompat || pawn == null || pawn.genes == null) return 0;
            XenotypeDef biodrone = DefDatabase<XenotypeDef>.GetNamedSilentFail("CYB_Biodrone");
            if (biodrone == null || pawn.genes.Xenotype != biodrone) return 0;

            int added = 0;
            foreach (GeneDef geneDef in biodrone.AllGenes)
            {
                bool present = pawn.genes.GenesListForReading.Any(g => g != null && g.def == geneDef);
                if (!present)
                {
                    pawn.genes.AddGene(geneDef, false);
                    added++;
                }
            }

            if (added > 0 && Prefs.DevMode && CompatMod.Settings.verboseDevLogging)
                Log.Message("[GVO + Growth Accelerator XP Compat] Restored " + added + " missing Biodrone germline genes on " + pawn.LabelShortCap + ".");
            return added;
        }

        internal static void ApplyBiodroneEmbryoCompatibility()
        {
            if (!CompatMod.Settings.enableBiodroneEmbryoCompat) return;

            ThingDef vanilla = DefDatabase<ThingDef>.GetNamedSilentFail("HumanEmbryo");
            if (vanilla == null)
            {
                vanilla = DefDatabase<ThingDef>.AllDefsListForReading.FirstOrDefault(d =>
                    d != null && ((d.defName ?? "").Equals("HumanEmbryo", StringComparison.OrdinalIgnoreCase) ||
                    ((d.label ?? "").ToLowerInvariant().Contains("human embryo"))));
            }
            if (vanilla == null)
            {
                Log.Warning("[GVO + Growth Accelerator XP Compat] Vanilla HumanEmbryo def was not found; Biodrone embryo compatibility could not be applied.");
                return;
            }

            List<ThingDef> candidates = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d != vanilla && IsBiodroneEmbryoDef(d)).ToList();
            foreach (ThingDef candidate in candidates)
            {
                if (candidate.thingCategories == null) candidate.thingCategories = new List<ThingCategoryDef>();
                if (vanilla.thingCategories != null)
                {
                    foreach (ThingCategoryDef category in vanilla.thingCategories)
                        if (!candidate.thingCategories.Contains(category)) candidate.thingCategories.Add(category);
                }

                if (candidate.comps == null) candidate.comps = new List<CompProperties>();
                if (vanilla.comps != null)
                {
                    foreach (CompProperties vanillaComp in vanilla.comps)
                    {
                        if (vanillaComp == null) continue;
                        Type compType = vanillaComp.GetType();
                        if (!candidate.comps.Any(c => c != null && c.GetType() == compType))
                            candidate.comps.Add(vanillaComp);
                    }
                }

                if (Prefs.DevMode && CompatMod.Settings.verboseDevLogging)
                    Log.Message("[GVO + Growth Accelerator XP Compat] Patched embryo-like def for vat compatibility: " + candidate.defName);
            }

            if (candidates.Count == 0)
                Log.Warning("[GVO + Growth Accelerator XP Compat] No non-vanilla embryo-like ThingDef was detected. Dev-spawn one and report its exact defName from the log/inspector if it still cannot enter a vat.");
        }
    }

    [HarmonyPatch]
    internal static class Patch_OverclockedLearnMode_Transpiler
    {
        private static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("GrowthVatsOverclocked.VatExtensions.HediffComp_OverclockedVatLearning");
            MethodInfo method = type == null ? null : AccessTools.Method(type, "LearnMode");
            if (method == null)
                Log.Error("[GVO + Growth Accelerator XP Compat] Could not find LearnMode; XP compensation cannot run.");
            else
                Log.Message("[GVO + Growth Accelerator XP Compat] XP transpiler targeting the exact installed LearnMode method.");
            return method;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = instructions.ToList();
            MethodInfo wrapper = AccessTools.Method(typeof(Patch_OverclockedLearnMode_Transpiler), "SkillLearnAdjusted");
            bool patched = false;

            for (int i = 0; i < list.Count; i++)
            {
                MethodInfo called = list[i].operand as MethodInfo;
                if (called == null || called.DeclaringType != typeof(SkillRecord) || called.Name != "Learn") continue;

                ParameterInfo[] parameters = called.GetParameters();
                if (parameters.Length != 3 || parameters[0].ParameterType != typeof(float)
                    || parameters[1].ParameterType != typeof(bool) || parameters[2].ParameterType != typeof(bool)) continue;

                // RimWorld 1.6 stack immediately before the call is:
                // SkillRecord, xp, direct, ignoreLearningRate.
                // Add the current Overclocked learning-comp instance and call a matching static wrapper.
                CodeInstruction original = list[i];
                list.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                i++;

                CodeInstruction replacement = new CodeInstruction(OpCodes.Call, wrapper);
                replacement.labels.AddRange(original.labels);
                replacement.blocks.AddRange(original.blocks);
                list[i] = replacement;
                patched = true;
                break;
            }

            if (!patched)
                Log.Error("[GVO + Growth Accelerator XP Compat] LearnMode was found, but its RimWorld 1.6 SkillRecord.Learn(float,bool,bool) call was not found.");
            return list;
        }

        private static FieldInfo xpSinceMidnightField;
        private static PropertyInfo xpSinceMidnightProperty;
        private static bool dailyXpAccessorResolved;

        public static void SkillLearnAdjusted(SkillRecord skill, float xp, bool direct, bool ignoreLearningRate, object learningComp)
        {
            float adjusted = AdjustAward(xp, learningComp);
            float previousDailyXp = 0f;
            bool saturationBypassed = false;

            if (CompatMod.Settings.ignoreVatLearningSaturation)
                saturationBypassed = TryClearDailySkillXp(skill, out previousDailyXp);

            try
            {
                // Keep RimWorld's normal passion and pawn learning-rate behavior.
                // Only the per-day saturation counter is temporarily cleared for this vat award.
                skill.Learn(adjusted, direct, ignoreLearningRate);
            }
            finally
            {
                // Restoring the original value means vat XP neither suffers from nor contributes
                // to daily saturation. Ordinary non-vat learning continues to use the cap normally.
                if (saturationBypassed)
                    RestoreDailySkillXp(skill, previousDailyXp);
            }
        }

        private static void ResolveDailyXpAccessor()
        {
            if (dailyXpAccessorResolved) return;
            dailyXpAccessorResolved = true;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            xpSinceMidnightField = typeof(SkillRecord).GetFields(flags)
                .FirstOrDefault(f => f.FieldType == typeof(float)
                    && f.Name.IndexOf("xpSinceMidnight", StringComparison.OrdinalIgnoreCase) >= 0);

            if (xpSinceMidnightField == null)
            {
                xpSinceMidnightProperty = typeof(SkillRecord).GetProperties(flags)
                    .FirstOrDefault(prop => prop.PropertyType == typeof(float)
                        && prop.Name.IndexOf("xpSinceMidnight", StringComparison.OrdinalIgnoreCase) >= 0
                        && prop.CanRead && prop.GetSetMethod(true) != null);
            }
        }

        private static bool TryClearDailySkillXp(SkillRecord skill, out float previous)
        {
            previous = 0f;
            if (skill == null) return false;

            try
            {
                ResolveDailyXpAccessor();
                if (xpSinceMidnightField != null)
                {
                    previous = (float)xpSinceMidnightField.GetValue(skill);
                    xpSinceMidnightField.SetValue(skill, 0f);
                    return true;
                }

                if (xpSinceMidnightProperty != null)
                {
                    previous = (float)xpSinceMidnightProperty.GetValue(skill, null);
                    xpSinceMidnightProperty.SetValue(skill, 0f, null);
                    return true;
                }

                Log.WarningOnce("[GVO + Growth Accelerator XP Compat] SkillRecord daily-XP counter was not found; vat XP saturation could not be bypassed.", 198740233);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Could not temporarily clear the daily skill-XP counter: " + ex, 198740234);
            }
            return false;
        }

        private static void RestoreDailySkillXp(SkillRecord skill, float previous)
        {
            try
            {
                if (xpSinceMidnightField != null)
                    xpSinceMidnightField.SetValue(skill, previous);
                else if (xpSinceMidnightProperty != null)
                    xpSinceMidnightProperty.SetValue(skill, previous, null);

                if (Prefs.DevMode && CompatMod.Settings.verboseDevLogging)
                    Log.Message("[GVO + Growth Accelerator XP Compat] Vat XP ignored daily learning saturation; restored prior daily XP counter of " + previous.ToString("0.##") + ".");
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Could not restore the daily skill-XP counter after vat learning: " + ex, 198740235);
            }
        }

        private static float AdjustAward(float xp, object learningComp)
        {
            if (!CompatMod.Settings.compensateSkillXp) return xp;

            float multiplier = 1f;
            Building_GrowthVat vat = null;
            try
            {
                Pawn pawn = GetPawn(learningComp);
                vat = AcceleratorRuntime.FindGrowthVat(pawn);
                StatDef speedStat = DefDatabase<StatDef>.GetNamedSilentFail("kathanon_GrowthAccelerator_GrowthVatSpeed");
                if (vat != null && speedStat != null)
                    multiplier = Math.Max(1f, vat.GetStatValue(speedStat));
                else
                    Log.WarningOnce("[GVO + Growth Accelerator XP Compat] LearnMode ran, but its pawn growth vat or GrowthVatSpeed stat was not found; XP remained 1x.", 198740225);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] XP multiplier lookup failed; XP remained 1x. " + ex, 198740226);
            }

            float qualityFactor = AcceleratorRuntime.QualityFactor(vat, false);
            float result = xp * multiplier * qualityFactor;
            if (Prefs.DevMode && CompatMod.Settings.verboseDevLogging)
                Log.Message("[GVO + Growth Accelerator XP Compat] Skill award: " + xp.ToString("0.##") + " XP x speed " + multiplier.ToString("0.###")
                    + " x quality " + qualityFactor.ToString("0.###") + " = " + result.ToString("0.##") + " XP.");
            return result;
        }

        private static Pawn GetPawn(object learningComp)
        {
            if (learningComp == null) return null;
            try
            {
                PropertyInfo pawnProperty = AccessTools.Property(typeof(HediffComp), "Pawn");
                Pawn pawn = pawnProperty == null ? null : pawnProperty.GetValue(learningComp, null) as Pawn;
                if (pawn != null) return pawn;

                FieldInfo parentField = AccessTools.Field(typeof(HediffComp), "parent");
                Hediff parent = parentField == null ? null : parentField.GetValue(learningComp) as Hediff;
                return parent == null ? null : parent.pawn;
            }
            catch { return null; }
        }
    }


    // Repairs growth points after each growth-vat age update. This version reads the
    // Pawn_AgeTracker.GrowthPointsPerDay value that Growth Vats: Overclocked itself patches,
    // rather than trying to read the same value as a normal pawn stat. It also resolves vats
    // through nested ThingOwner holders and reports every failed prerequisite in Developer Mode.
    [HarmonyPatch]
    internal static class Patch_PawnAgeTracker_NotifyTickedInGrowthVat_GrowthPointRepair
    {
        private sealed class GrowthState
        {
            public int lastGameTick;
            public float stageStartPoints;
            public float expectedStageGain;
            public float lastPointsAfterRepair;
            public int lastLogTick;
            public bool loggedSuccess;
        }

        private static readonly Dictionary<int, GrowthState> states = new Dictionary<int, GrowthState>();
        private static FieldInfo ageTrackerPawnField;
        private static FieldInfo growthPointsField;
        private static PropertyInfo growthPointsProperty;
        private static PropertyInfo growthPointsPerDayProperty;
        private static MethodInfo growthPointsPerDayGetter;
        private static StatDef fallbackGrowthRateStat;
        private static StatDef vatSpeedStat;
        private static bool growthAccessorResolved;
        private static bool rateAccessorResolved;

        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(Pawn_AgeTracker), "Notify_TickedInGrowthVat");
            if (method == null)
                Log.Error("[GVO + Growth Accelerator XP Compat] Pawn_AgeTracker.Notify_TickedInGrowthVat was not found; growth-point repair cannot run.");
            else
                Log.Message("[GVO + Growth Accelerator XP Compat] v15 growth-point repair attached after Pawn_AgeTracker.Notify_TickedInGrowthVat.");
            return method;
        }

        private static void Postfix(Pawn_AgeTracker __instance, object[] __args)
        {
            if (!CompatMod.Settings.repairAndScaleGrowthPoints || __instance == null || Current.ProgramState != ProgramState.Playing) return;

            Pawn pawn = GetPawn(__instance);
            if (pawn == null)
            {
                DevWarningOnce("Notify_TickedInGrowthVat ran, but the Pawn_AgeTracker pawn field could not be resolved.", 198740241);
                return;
            }
            if (pawn.Dead || pawn.ageTracker == null) return;

            float age = pawn.ageTracker.AgeBiologicalYearsFloat;
            if (age < 3f || age >= 13f)
            {
                states.Remove(pawn.thingIDNumber);
                return;
            }

            Building_GrowthVat vat = FindGrowthVat(pawn);
            if (vat == null)
            {
                DevWarningOnce("Notify_TickedInGrowthVat ran for " + pawn.LabelShortCap + ", but its growth vat could not be resolved from the holder chain.", 198740242 + pawn.thingIDNumber);
                states.Remove(pawn.thingIDNumber);
                return;
            }

            float currentPoints;
            if (!TryGetGrowthPoints(__instance, out currentPoints))
            {
                DevWarningOnce("Notify_TickedInGrowthVat ran for " + pawn.LabelShortCap + ", but current growth points could not be read.", 198740243 + pawn.thingIDNumber);
                return;
            }

            int now = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            GrowthState state;
            if (!states.TryGetValue(pawn.thingIDNumber, out state))
            {
                state = new GrowthState
                {
                    lastGameTick = now,
                    stageStartPoints = currentPoints,
                    expectedStageGain = 0f,
                    lastPointsAfterRepair = currentPoints,
                    lastLogTick = now,
                    loggedSuccess = false
                };
                states[pawn.thingIDNumber] = state;
                if (Prefs.DevMode && CompatMod.Settings.verboseDevLogging)
                    Log.Message("[GVO + Growth Accelerator XP Compat] v15 began tracking " + pawn.LabelShortCap
                        + " at " + currentPoints.ToString("0.###") + " growth points; holder=" + vat.LabelShortCap + ".");
                return;
            }

            int elapsedGameTicks = now - state.lastGameTick;
            if (elapsedGameTicks <= 0) return;

            // Growth moments reset the stage's point total. Start a new cumulative target.
            if (currentPoints + 0.001f < state.lastPointsAfterRepair)
            {
                state.lastGameTick = now;
                state.stageStartPoints = currentPoints;
                state.expectedStageGain = 0f;
                state.lastPointsAfterRepair = currentPoints;
                state.lastLogTick = now;
                state.loggedSuccess = false;
                return;
            }

            float basePointsPerDay = GetBasePointsPerDay(__instance, pawn);
            if (basePointsPerDay <= 0f)
            {
                DevWarningOnce("Growth-point rate resolved to zero for " + pawn.LabelShortCap
                    + ". Pawn_AgeTracker.GrowthPointsPerDay and the fallback stat were both unavailable or zero.", 198740244 + pawn.thingIDNumber);
                state.lastGameTick = now;
                state.lastPointsAfterRepair = currentPoints;
                return;
            }

            float vatSpeed = GetVatSpeed(vat);
            float qualityFactor = AcceleratorRuntime.QualityFactor(vat, true);
            state.expectedStageGain += basePointsPerDay * vatSpeed * qualityFactor * elapsedGameTicks / 60000f;

            // Enforce the cumulative target exactly. Positive correction repairs missing accelerated
            // points; negative correction is required for Mass Produce when its quality penalty
            // is lower than the source mod's unmodified award rate.
            float targetPoints = state.stageStartPoints + state.expectedStageGain;
            float correction = targetPoints - currentPoints;
            if (Math.Abs(correction) > 0.00001f)
            {
                currentPoints = Math.Max(state.stageStartPoints, targetPoints);
                SetGrowthPoints(__instance, currentPoints);
            }

            state.lastGameTick = now;
            state.lastPointsAfterRepair = currentPoints;

            if (Prefs.DevMode && CompatMod.Settings.verboseDevLogging
                && (!state.loggedSuccess || now - state.lastLogTick >= 2500))
            {
                int biologicalTicks = ExtractBiologicalTicks(__args);
                Log.Message("[GVO + Growth Accelerator XP Compat] v15 growth repair for " + pawn.LabelShortCap
                    + ": base " + basePointsPerDay.ToString("0.###") + "/day x accelerator " + vatSpeed.ToString("0.###")
                    + " x quality " + qualityFactor.ToString("0.###")
                    + " = " + (basePointsPerDay * vatSpeed * qualityFactor).ToString("0.###") + "/day; "
                    + correction.ToString("+0.####;-0.####;0") + " correction this update; stage points " + currentPoints.ToString("0.###")
                    + "; vat biological ticks argument=" + biologicalTicks + ".");
                state.lastLogTick = now;
                state.loggedSuccess = true;
            }
        }

        private static int ExtractBiologicalTicks(object[] args)
        {
            if (args == null) return 0;
            for (int i = 0; i < args.Length; i++)
                if (args[i] is int) return (int)args[i];
            return 0;
        }

        private static Building_GrowthVat FindGrowthVat(Pawn pawn)
        {
            if (pawn == null) return null;

            object holder = pawn.ParentHolder;
            for (int depth = 0; depth < 8 && holder != null; depth++)
            {
                Building_GrowthVat vat = holder as Building_GrowthVat;
                if (vat != null) return vat;

                object next = null;
                try
                {
                    PropertyInfo ownerProperty = AccessTools.Property(holder.GetType(), "Owner");
                    if (ownerProperty != null) next = ownerProperty.GetValue(holder, null);
                    if (next == null)
                    {
                        PropertyInfo parentProperty = AccessTools.Property(holder.GetType(), "ParentHolder");
                        if (parentProperty != null) next = parentProperty.GetValue(holder, null);
                    }
                    if (next == null)
                    {
                        FieldInfo ownerField = AccessTools.Field(holder.GetType(), "owner");
                        if (ownerField != null) next = ownerField.GetValue(holder);
                    }
                }
                catch { }

                if (ReferenceEquals(next, holder)) break;
                holder = next;
            }
            return null;
        }

        private static Pawn GetPawn(Pawn_AgeTracker tracker)
        {
            try
            {
                if (ageTrackerPawnField == null)
                    ageTrackerPawnField = typeof(Pawn_AgeTracker).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(f => f.FieldType == typeof(Pawn));
                return ageTrackerPawnField == null ? null : ageTrackerPawnField.GetValue(tracker) as Pawn;
            }
            catch { return null; }
        }

        private static void ResolveGrowthAccessor()
        {
            if (growthAccessorResolved) return;
            growthAccessorResolved = true;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            growthPointsField = typeof(Pawn_AgeTracker).GetFields(flags)
                .FirstOrDefault(f => f.FieldType == typeof(float)
                    && f.Name.IndexOf("growthPoints", StringComparison.OrdinalIgnoreCase) >= 0);

            if (growthPointsField == null)
            {
                growthPointsProperty = typeof(Pawn_AgeTracker).GetProperties(flags)
                    .FirstOrDefault(p => p.PropertyType == typeof(float)
                        && p.Name.IndexOf("growthPoints", StringComparison.OrdinalIgnoreCase) >= 0
                        && p.Name.IndexOf("PerDay", StringComparison.OrdinalIgnoreCase) < 0
                        && p.CanRead && p.GetSetMethod(true) != null);
            }

            if (growthPointsField == null && growthPointsProperty == null)
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Pawn_AgeTracker growth-points field/property was not found; growth repair cannot run.", 198740236);
        }

        private static bool TryGetGrowthPoints(Pawn_AgeTracker tracker, out float points)
        {
            points = 0f;
            try
            {
                ResolveGrowthAccessor();
                if (growthPointsField != null)
                {
                    points = (float)growthPointsField.GetValue(tracker);
                    return true;
                }
                if (growthPointsProperty != null)
                {
                    points = (float)growthPointsProperty.GetValue(tracker, null);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Could not read child growth points: " + ex, 198740237);
            }
            return false;
        }

        private static void SetGrowthPoints(Pawn_AgeTracker tracker, float points)
        {
            try
            {
                if (growthPointsField != null) growthPointsField.SetValue(tracker, points);
                else if (growthPointsProperty != null) growthPointsProperty.SetValue(tracker, points, null);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Could not write child growth points: " + ex, 198740238);
            }
        }

        private static void ResolveRateAccessor()
        {
            if (rateAccessorResolved) return;
            rateAccessorResolved = true;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            growthPointsPerDayProperty = typeof(Pawn_AgeTracker).GetProperties(flags)
                .FirstOrDefault(p => p.PropertyType == typeof(float)
                    && p.Name.Equals("GrowthPointsPerDay", StringComparison.OrdinalIgnoreCase)
                    && p.CanRead);

            if (growthPointsPerDayProperty == null)
            {
                growthPointsPerDayGetter = typeof(Pawn_AgeTracker).GetMethods(flags)
                    .FirstOrDefault(m => m.ReturnType == typeof(float)
                        && m.GetParameters().Length == 0
                        && (m.Name.Equals("get_GrowthPointsPerDay", StringComparison.OrdinalIgnoreCase)
                            || m.Name.Equals("GrowthPointsPerDay", StringComparison.OrdinalIgnoreCase)));
            }
        }

        private static float GetBasePointsPerDay(Pawn_AgeTracker tracker, Pawn pawn)
        {
            try
            {
                ResolveRateAccessor();
                if (growthPointsPerDayProperty != null)
                    return Math.Max(0f, (float)growthPointsPerDayProperty.GetValue(tracker, null));
                if (growthPointsPerDayGetter != null)
                    return Math.Max(0f, (float)growthPointsPerDayGetter.Invoke(tracker, null));

                // Compatibility fallback for builds that expose the displayed rate as a stat.
                if (fallbackGrowthRateStat == null)
                    fallbackGrowthRateStat = DefDatabase<StatDef>.GetNamedSilentFail("GrowthPointsPerDayAtLearningLevel");
                return fallbackGrowthRateStat == null ? 0f : Math.Max(0f, pawn.GetStatValue(fallbackGrowthRateStat));
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Could not read Pawn_AgeTracker.GrowthPointsPerDay: " + ex, 198740240);
                return 0f;
            }
        }

        private static float GetVatSpeed(Building_GrowthVat vat)
        {
            try
            {
                if (vatSpeedStat == null)
                    vatSpeedStat = DefDatabase<StatDef>.GetNamedSilentFail("kathanon_GrowthAccelerator_GrowthVatSpeed");
                if (vatSpeedStat == null)
                {
                    DevWarningOnce("Growth Accelerator's GrowthVatSpeed stat was not found; growth-point scaling remained at 1x.", 198740245);
                    return 1f;
                }
                return Math.Max(1f, vat.GetStatValue(vatSpeedStat));
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GVO + Growth Accelerator XP Compat] Could not read vat accelerator speed; using 1x. " + ex, 198740246);
                return 1f;
            }
        }

        private static void DevWarningOnce(string message, int key)
        {
            if (Prefs.DevMode && CompatMod.Settings.verboseDevLogging)
                Log.WarningOnce("[GVO + Growth Accelerator XP Compat] " + message, key);
        }
    }

    [HarmonyPatch(typeof(ThingFilter), "Allows", new Type[] { typeof(Thing) })]
    internal static class Patch_ThingFilter_Allows_BiodroneEmbryo
    {
        private static void Postfix(Thing t, ThingFilter __instance, ref bool __result)
        {
            if (!CompatMod.Settings.enableBiodroneEmbryoCompat || !DefAdjuster.IsBiodroneEmbryo(t)) return;
            DefAdjuster.EnsureBiodroneEmbryoGeneSet(t);
            if (__result) return;

            ThingDef vanilla = DefDatabase<ThingDef>.GetNamedSilentFail("HumanEmbryo");
            if (vanilla == null) return;
            try
            {
                MethodInfo allowsDef = AccessTools.Method(typeof(ThingFilter), "Allows", new Type[] { typeof(ThingDef) });
                if (allowsDef != null && (bool)allowsDef.Invoke(__instance, new object[] { vanilla }))
                    __result = true;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(ListerThings), "ThingsOfDef")]
    internal static class Patch_ListerThings_ThingsOfDef_Biodrone
    {
        private static void Postfix(ThingDef def, ListerThings __instance, ref List<Thing> __result)
        {
            if (!CompatMod.Settings.enableBiodroneEmbryoCompat || def == null || def.defName != "HumanEmbryo" || __result == null) return;
            ThingDef biodrone = DefDatabase<ThingDef>.GetNamedSilentFail("HumanEmbryo_CYB_Biodrone");
            if (biodrone == null) return;
            List<Thing> extras = __instance.ThingsOfDef(biodrone);
            if (extras == null || extras.Count == 0) return;
            List<Thing> combined = new List<Thing>(__result);
            foreach (Thing thing in extras)
            {
                DefAdjuster.EnsureBiodroneEmbryoGeneSet(thing);
                if (!combined.Contains(thing)) combined.Add(thing);
            }
            __result = combined;
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), "SetXenotypeDirect", new Type[] { typeof(XenotypeDef) })]
    internal static class Patch_PawnGeneTracker_SetXenotypeDirect_Biodrone
    {
        private static void Postfix(Pawn_GeneTracker __instance, object[] __args)
        {
            if (!CompatMod.Settings.enableBiodroneEmbryoCompat || __args == null) return;
            XenotypeDef xenotype = __args.OfType<XenotypeDef>().FirstOrDefault();
            if (xenotype == null || xenotype.defName != "CYB_Biodrone") return;
            DefAdjuster.EnsureBiodronePawnGenes(DefAdjuster.PawnForGeneTracker(__instance));
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), "ExposeData")]
    internal static class Patch_PawnGeneTracker_ExposeData_RepairBiodrone
    {
        private static void Postfix(Pawn_GeneTracker __instance)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                DefAdjuster.EnsureBiodronePawnGenes(DefAdjuster.PawnForGeneTracker(__instance));
        }
    }

    public sealed class PlaceWorker_ShowAcceleratorFacing : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            if (!CompatMod.Settings.showFacingIndicator) return;
            Map map = Find.CurrentMap;
            if (map == null) return;
            IntVec3 facingCell = center + rot.FacingCell;
            if (facingCell.InBounds(map)) GenDraw.DrawFieldEdges(new List<IntVec3> { facingCell }, Color.cyan);
        }
    }
}
