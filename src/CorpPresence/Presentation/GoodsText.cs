using System.Collections.Generic;

namespace CorpPresence;

// Explicit presentation metadata. Dictionary identifiers remain unchanged.
internal static class GoodsText
{
    internal sealed class Keys
    {
        internal readonly string Name, Flavor, Description, Manual, Effects;
        internal Keys(string name, string flavor, string description, string manual, string effects)
        { Name = name; Flavor = flavor; Description = description; Manual = manual; Effects = effects; }
    }
    private static readonly Dictionary<string, Keys> Definitions = new()
    {
        ["agent_cert"] = new Keys("items.definitions.corporateAgentCertificate.name", "items.definitions.corporateAgentCertificate.flavor", "items.definitions.corporateAgentCertificate.description", null, null),
        ["anchor_cell"] = new Keys("items.definitions.anchorHighCapacityCell.name", "items.definitions.anchorHighCapacityCell.flavor", "items.definitions.anchorHighCapacityCell.description", null, null),
        ["anon_parcel"] = new Keys("items.definitions.anonymousParcel.name", "items.definitions.anonymousParcel.flavor", "items.definitions.anonymousParcel.description", null, null),
        ["blood_drawer"] = new Keys("items.definitions.bloodDrawer.name", "items.definitions.bloodDrawer.flavor", "items.definitions.bloodDrawer.description", null, null),
        ["candle"] = new Keys("items.definitions.solanaScentedCandle.name", "items.definitions.solanaScentedCandle.flavor", null, null, null),
        ["canning_machine"] = new Keys("items.definitions.canningMachine.name", "items.definitions.canningMachine.flavor", "items.definitions.canningMachine.description", "items.definitions.canningMachine.manual", null),
        ["carapace_full"] = new Keys("items.definitions.isomerCarapaceFull.name", "items.definitions.isomerCarapaceFull.flavor", "items.definitions.isomerCarapaceFull.description", null, null),
        ["carapace_medium"] = new Keys("items.definitions.isomerCarapaceMedium.name", "items.definitions.isomerCarapaceMedium.flavor", "items.definitions.isomerCarapaceMedium.description", null, null),
        ["carapace_small"] = new Keys("items.definitions.isomerCarapaceSmall.name", "items.definitions.isomerCarapaceSmall.flavor", "items.definitions.isomerCarapaceSmall.description", null, null),
        ["coarse_mince"] = new Keys("items.definitions.scrapMince.name", "items.definitions.scrapMince.flavor", "items.definitions.scrapMince.description", null, null),
        ["crab_stick"] = new Keys("items.definitions.crabFlavoredStick.name", "items.definitions.crabFlavoredStick.flavor", "items.definitions.crabFlavoredStick.description", null, null),
        ["crate_carmicheal"] = new Keys("items.definitions.carmichealDeliveryCrate.name", "items.definitions.carmichealDeliveryCrate.flavor", "items.definitions.carmichealDeliveryCrate.description", null, null),
        ["crate_davre"] = new Keys("items.definitions.davreDeliveryCrate.name", "items.definitions.davreDeliveryCrate.flavor", "items.definitions.davreDeliveryCrate.description", null, null),
        ["crate_jackson_private"] = new Keys("items.definitions.drJacksonSDeliveryCrate.name", "items.definitions.drJacksonSDeliveryCrate.flavor", "items.definitions.drJacksonSDeliveryCrate.description", null, null),
        ["crate_meridian"] = new Keys("items.definitions.meridianDeliveryCrate.name", "items.definitions.meridianDeliveryCrate.flavor", "items.definitions.meridianDeliveryCrate.description", null, null),
        ["crate_pastel"] = new Keys("items.definitions.pastelDeliveryCrate.name", "items.definitions.pastelDeliveryCrate.flavor", "items.definitions.pastelDeliveryCrate.description", null, null),
        ["crate_solana"] = new Keys("items.definitions.solanaDeliveryCrate.name", "items.definitions.solanaDeliveryCrate.flavor", "items.definitions.solanaDeliveryCrate.description", null, null),
        ["crate_spacer"] = new Keys("items.definitions.spacerAllianceCrate.name", "items.definitions.spacerAllianceCrate.flavor", "items.definitions.spacerAllianceCrate.description", null, null),
        ["custom_dagger"] = new Keys("items.definitions.customDagger.name", "items.definitions.customDagger.flavor", "items.definitions.customDagger.description", null, null),
        ["custom_machete"] = new Keys("items.definitions.customMachete.name", "items.definitions.customMachete.flavor", "items.definitions.customMachete.description", null, null),
        ["custom_saber"] = new Keys("items.definitions.customSaber.name", "items.definitions.customSaber.flavor", "items.definitions.customSaber.description", null, null),
        ["elixir"] = new Keys("items.definitions.socialiteEssence.name", "items.definitions.socialiteEssence.flavor", null, null, null),
        ["fate_coin_carmicheal"] = new Keys("items.definitions.carmichealSCoinOfFate.name", "items.definitions.carmichealSCoinOfFate.flavor", "items.definitions.carmichealSCoinOfFate.description", null, null),
        ["fate_coin_davre"] = new Keys("items.definitions.davreSCoinOfFate.name", "items.definitions.davreSCoinOfFate.flavor", "items.definitions.davreSCoinOfFate.description", null, null),
        ["fate_coin_meridian"] = new Keys("items.definitions.meridianSCoinOfFate.name", "items.definitions.meridianSCoinOfFate.flavor", "items.definitions.meridianSCoinOfFate.description", null, null),
        ["fate_coin_pastel"] = new Keys("items.definitions.pastelSCoinOfFate.name", "items.definitions.pastelSCoinOfFate.flavor", "items.definitions.pastelSCoinOfFate.description", null, null),
        ["fate_coin_solana"] = new Keys("items.definitions.solanaSCoinOfFate.name", "items.definitions.solanaSCoinOfFate.flavor", "items.definitions.solanaSCoinOfFate.description", null, null),
        ["mango_warehouse"] = new Keys("mango.warehouse.name", "mango.warehouse.flavor", "mango.warehouse.description", null, null),
        ["fufu_doll"] = new Keys("fufuDoll.name", "fufuDoll.visitor.thanks", "fufuDoll.description", null, null),
        ["ufo_backpack"] = new Keys("ufoBackpack.name", "ufoBackpack.visitor.thanks", "ufoBackpack.description", null, null),
        ["mango_backpack"] = new Keys("mango.backpack.name", "mango.backpack.flavor", "mango.backpack.description", null, null),
        ["waste_battery"] = new Keys("wasteBattery.item.name", "wasteBattery.visitor.thanks", "wasteBattery.item.description", null, null),
        ["fine_mince"] = new Keys("items.definitions.primeMince.name", "items.definitions.primeMince.flavor", "items.definitions.primeMince.description", null, null),
        ["fish_can"] = new Keys("items.definitions.davreCannedFish.name", "items.definitions.davreCannedFish.flavor", "items.definitions.davreCannedFish.description", null, null),
        ["fish_mince"] = new Keys("items.definitions.fishMince.name", "items.definitions.fishMince.flavor", "items.definitions.fishMince.description", null, null),
        ["forge_anvil"] = new Keys("items.definitions.forgingAnvil.name", "items.definitions.forgingAnvil.flavor", "items.definitions.forgingAnvil.description", "items.definitions.forgingAnvil.manual", null),
        ["forge_furnace"] = new Keys("items.definitions.precisionForge.name", "items.definitions.precisionForge.flavor", "items.definitions.precisionForge.description", "items.definitions.precisionForge.manual", null),
        ["forged_billet"] = new Keys("items.definitions.forgedBillet.name", "items.definitions.forgedBillet.flavor", "items.definitions.forgedBillet.description", null, null),
        ["freedom_walkman"] = new Keys("items.definitions.freeFrequencyWalkman.name", "items.definitions.freeFrequencyWalkman.flavor", "items.definitions.freeFrequencyWalkman.description", null, null),
        ["fresh_meat_can"] = new Keys("items.definitions.davreFreshMeat.name", "items.definitions.davreFreshMeat.flavor", "items.definitions.davreFreshMeat.description", null, null),
        ["friend_medal"] = new Keys("items.definitions.friendOfTheCorporations.name", "items.definitions.friendOfTheCorporations.flavor", "items.definitions.friendOfTheCorporations.description", null, null),
        ["fruit_paste"] = new Keys("items.definitions.nutrifruitPaste.name", "items.definitions.nutrifruitPaste.flavor", "items.definitions.nutrifruitPaste.description", null, null),
        ["gloves"] = new Keys("items.definitions.kottonWorkGloves.name", "items.definitions.kottonWorkGloves.flavor", null, null, null),
        ["hemingway_stamp"] = new Keys("items.definitions.hemingwayLabelStamp.name", "items.definitions.hemingwayLabelStamp.flavor", "items.definitions.hemingwayLabelStamp.description", null, null),
        ["implant_compat"] = new Keys("items.definitions.implantCompatibilityAgent.name", "items.definitions.implantCompatibilityAgent.flavor", "items.definitions.implantCompatibilityAgent.description", null, null),
        ["isomer_gel"] = new Keys("items.definitions.isomerRepairGel.name", "items.definitions.isomerRepairGel.flavor", "items.definitions.isomerRepairGel.description", null, null),
        ["isomer_reactor"] = new Keys("items.definitions.isomerReactor.name", "items.definitions.isomerReactor.flavor", "items.definitions.isomerReactor.description", "items.definitions.isomerReactor.manual", null),
        ["isomer_vial"] = new Keys("items.definitions.isomerSolution.name", "items.definitions.isomerSolution.flavor", "items.definitions.isomerSolution.description", null, null),
        ["leviathan"] = new Keys("items.definitions.leviathanWaterPurifier.name", "items.definitions.leviathanWaterPurifier.flavor", "items.definitions.leviathanWaterPurifier.description", null, null),
        ["loose_cigarette"] = new Keys("items.definitions.looseCigarettes.name", "items.definitions.looseCigarettes.flavor", "items.definitions.looseCigarettes.description", null, null),
        ["lower_rep_cert"] = new Keys("items.definitions.lowerLevelsRepresentative.name", "items.definitions.lowerLevelsRepresentative.flavor", "items.definitions.lowerLevelsRepresentative.description", null, null),
        ["mask"] = new Keys("items.definitions.eternalNightRepairMask.name", "items.definitions.eternalNightRepairMask.flavor", null, null, null),
        ["meat_grinder"] = new Keys("items.definitions.meatGrinder.name", "items.definitions.meatGrinder.flavor", "items.definitions.meatGrinder.description", "items.definitions.meatGrinder.manual", null),
        ["meridian_cert"] = new Keys("items.definitions.meridianAuthorizedDealerCertificate.name", "items.definitions.meridianAuthorizedDealerCertificate.flavor", "items.definitions.meridianAuthorizedDealerCertificate.description", null, null),
        ["mutual_medkit"] = new Keys("items.definitions.mutualAidMedkit.name", "items.definitions.mutualAidMedkit.flavor", "items.definitions.mutualAidMedkit.description", null, null),
        ["oilflower"] = new Keys("items.definitions.oilflowerBlossoms.name", null, "items.definitions.oilflowerBlossoms.description", null, null),
        ["oilflower_seed"] = new Keys("items.definitions.oilflowerSeeds.name", null, "items.definitions.oilflowerSeeds.description", null, null),
        ["packaging_machine"] = new Keys("items.definitions.packagingMachine.name", "items.definitions.packagingMachine.flavor", "items.definitions.packagingMachine.description", "items.definitions.packagingMachine.manual", null),
        ["painblock"] = new Keys("items.definitions.painBlocker.name", "items.definitions.painBlocker.flavor", null, null, null),
        ["patch"] = new Keys("items.definitions.hemingwayQuitPatch.name", "items.definitions.hemingwayQuitPatch.flavor", null, null, null),
        ["perfume_base_oil"] = new Keys("items.definitions.legacyBaseOilSampleBottle.name", null, "items.definitions.legacyBaseOilSampleBottle.description", null, null),
        ["perfume_base_oil_material"] = new Keys("items.definitions.perfumeBaseOil.name", null, "items.definitions.perfumeBaseOil.description", null, null),
        ["perfume_blood_collector"] = new Keys("items.definitions.bloodCollector.name", null, "items.definitions.bloodCollector.description", "items.definitions.bloodCollector.manual", "items.definitions.bloodCollector.effects"),
        ["perfume_blood_extractor"] = new Keys("items.definitions.pheromoneExtractor.name", null, "items.definitions.pheromoneExtractor.description", "items.definitions.pheromoneExtractor.manual", "items.definitions.pheromoneExtractor.effects"),
        ["perfume_blue_extract"] = new Keys("items.definitions.pheromoneExtract.name", null, "items.definitions.pheromoneExtract.description", null, null),
        ["perfume_extract"] = new Keys("items.definitions.neuroactiveExtract.name", null, "items.definitions.neuroactiveExtract.description", null, null),
        ["perfume_mixer"] = new Keys("items.definitions.perfumeSynthesizer.name", null, "items.definitions.perfumeSynthesizer.description", "items.definitions.perfumeSynthesizer.manual", "items.definitions.perfumeSynthesizer.effects"),
        ["perfume_oil_extractor"] = new Keys("items.definitions.baseOilExtractor.name", null, "items.definitions.baseOilExtractor.description", "items.definitions.baseOilExtractor.manual", "items.definitions.baseOilExtractor.effects"),
        ["production_work_bay"] = new Keys("production.workBay.item.name", null, "production.workBay.item.description", null, null),
        [CargoWarehouse.GoodsKey] = new Keys("production.cargoWarehouse.item.name", null, "production.cargoWarehouse.item.description", null, null),
        [CargoWarehouse.ModuleKey] = new Keys("production.cargoWarehouse.module.name", null, "production.cargoWarehouse.module.description", null, null),
        [CargoWarehouse.ControllerKey] = new Keys("production.cargoWarehouse.controller.name", null, "production.cargoWarehouse.controller.description", null, null),
        ["rat_wheel_generator"] = new Keys("items.definitions.ratWheelGenerator.name", "items.definitions.ratWheelGenerator.flavor", "items.definitions.ratWheelGenerator.description", null, null),
        ["rebel_token"] = new Keys("items.definitions.resistanceWaypointToken.name", "items.definitions.resistanceWaypointToken.flavor", "items.definitions.resistanceWaypointToken.description", null, null),
        ["rejuvenation"] = new Keys("items.definitions.rejuvenationSerum.name", "items.definitions.rejuvenationSerum.flavor", "items.definitions.rejuvenationSerum.description", null, null),
        ["rolling_machine"] = new Keys("items.definitions.cigaretteRoller.name", "items.definitions.cigaretteRoller.flavor", "items.definitions.cigaretteRoller.description", "items.definitions.cigaretteRoller.manual", null),
        ["rosen_revolver"] = new Keys("items.definitions.rosenSHandmadeRevolver.name", "items.definitions.rosenSHandmadeRevolver.flavor", "items.definitions.rosenSHandmadeRevolver.description", null, null),
        ["rosen_rifle"] = new Keys("items.definitions.rosenSHandmadeRifle.name", "items.definitions.rosenSHandmadeRifle.flavor", "items.definitions.rosenSHandmadeRifle.description", null, null),
        ["sb_handbook"] = new Keys("items.definitions.silverBloodRefiningHandbook.name", "items.definitions.silverBloodRefiningHandbook.flavor", "items.definitions.silverBloodRefiningHandbook.description", null, null),
        ["seafood_sealer"] = new Keys("items.definitions.sealer.name", "items.definitions.sealer.flavor", "items.definitions.sealer.description", null, null),
        ["sign"] = new Keys("items.definitions.pastelPartnerStoreSign.name", "items.definitions.pastelPartnerStoreSign.flavor", null, null, null),
        ["silver_blood"] = new Keys("items.definitions.silverBlood.name", "items.definitions.silverBlood.flavor", "items.definitions.silverBlood.description", null, null),
        ["silverblood"] = new Keys("items.definitions.silverbloodTrilobite.name", "items.definitions.silverbloodTrilobite.flavor", "items.definitions.silverbloodTrilobite.description", null, null),
        ["stamina"] = new Keys("items.definitions.staminaBooster.name", "items.definitions.staminaBooster.flavor", null, null, null),
        ["tobacco_leaf"] = new Keys("items.definitions.tobaccoLeaves.name", "items.definitions.tobaccoLeaves.flavor", "items.definitions.tobaccoLeaves.description", null, null),
        ["tobacco_seed"] = new Keys("items.definitions.tobaccoSeeds.name", "items.definitions.tobaccoSeeds.flavor", "items.definitions.tobaccoSeeds.description", null, null),
        ["zhuoyue_battery"] = new Keys("items.definitions.eninenceEnergyCell.name", "items.definitions.eninenceEnergyCell.flavor", "items.definitions.eninenceEnergyCell.description", null, null),
    };
    internal static Keys ForId(string id) => id != null && Definitions.TryGetValue(id, out var keys) ? keys : null;
    internal static void Bind(IReadOnlyDictionary<string, Goods.Def> definitions)
    {
        foreach (var pair in definitions) pair.Value.TextKeys = ForId(pair.Key);
    }
}
