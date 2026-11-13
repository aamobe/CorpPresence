# 原版机器资源清单

来源：本机 `原版参考-100-Rev13`；扫描 Machine*.cs（排除 Machinery UI 类），补查动态状态、叠加层和同名前缀。机器工厂 26 个，其中两种分配器共用 bottle_printer；3 个辅助类不另算机器。原文件只读。

图板每格上方 1×、下方 3×最近邻；色板按主体使用量排序，主体阈值 Alpha≥128。阅读器缩放会影响实际显示倍率。无源码行号的同名前缀图仅为补充资源，不据名称推断当前游戏会使用。

|资源|类别|尺寸|主体色数|代码行号|状态|
|---|---|---|---:|---|---|
|purifier|MachinePurifier|48×64|16|42,109|visible|
|purifier_w|MachinePurifier|48×64|17|105|visible|
|purifier_on|MachinePurifier|48×64|22||visible|
|furnace|MachineFurnace|48×48|9|31|visible|
|furnace_on|MachineFurnace|48×48|11||visible|
|gibber|MachineAutoButcher|48×48|14|126,308,764|visible|
|blender|MachineBlender|48×48|12|10,17,33,104|visible|
|blender_b|MachineBlender|48×48|17|13|visible|
|bottle_printer|MachineBottlePrinter|32×48|23|25,126,150|visible|
|chem_finisher|MachineChemFinisher|48×48|11|19,26,95|visible|
|chem_finisher_b|MachineChemFinisher|48×48|12|22|visible|
|evaporator|MachineEvaporator|48×48|7|15,33|visible|
|evaporator_b|MachineEvaporator|48×48|16|29|visible|
|heating_plate|MachineHeatingPlate|48×48|10|32,39,138|visible|
|heating_plate_b|MachineHeatingPlate|48×48|17|35|visible|
|printer_off|MachinePrinter|48×48|26|81,249|visible|
|printer_on|MachinePrinter|48×48|27|154,165|visible|
|alarm_system|MachineAlarm|48×48|7|16,63,68,88,93|visible|
|alarm_system_r|MachineAlarm|48×48|10|59,84|visible|
|alarm_system_b|MachineAlarm|48×48|0||no opaque pixels|
|moisture_farm|MachineMoistureFarm|48×48|9|32,82,86,96,107,127,281|visible|
|moisture_farm_w|MachineMoistureFarm|48×48|12|92,103|visible|
|moisture_farm_r|MachineMoistureFarm|48×48|0||no opaque pixels|
|dealmaker|MachineDealMaker|48×32|7|17|visible|
|desequencer|MachineDesequencer|48×48|12|48,159,164|visible|
|desequencer_r|MachineDesequencer|48×48|14|155|visible|
|dna_manipulator|MachineDnaManipulator|48×48|18|53,114,181|visible|
|dna_manipulator_working|MachineDnaManipulator|48×48|20|55,181|visible|
|animal_feeder|MachineFeedDispenser|48×48|12|67|visible|
|fridge|MachineFridge|48×48|14|65|visible|
|hydroponic|MachineHydroponic|48×64|14|33,151|visible|
|hydroponic_off|MachineHydroponic|48×64|0|151|no opaque pixels|
|meat_culture_machine|MachineMeatCulture|×|0|12|missing export|
|projector|MachineProjector|48×48|10|16|visible|
|ras_empty|MachineRAS|64×48|16|6,101,434|visible|
|ras|MachineRAS|64×48|19|8,225,430|visible|
|ras_full|MachineRAS|64×48|21|10,430|visible|
|ras_tank|MachineRAS|48×48|8|12,122,240|visible|
|recharger_base|MachineRecharger|64×32|6|20|visible|
|recharger_top|MachineRecharger|64×32|4|77|visible|
|recharger_g|MachineRecharger|3×3|1|87|visible|
|recharger_o|MachineRecharger|3×3|1|91|visible|
|storage_coordinator|MachineStorageCoordinator|48×48|9|68,153,573,597,612,629,701,755|visible|
|turbo_booster_adv|MachineTurboBoosterAdv|48×32|11|188|visible|
|turbo_booster_adv_ready|MachineTurboBoosterAdv|48×32|11|180|visible|
|turbo_booster_adv_charging|MachineTurboBoosterAdv|48×32|10|184|visible|
|wine_rack|MachineWineRack|48×48|8|27|visible|
|wine_bottom|MachineWineRack|10×8|6|85|visible|
|cassette_player|MachineCassettePlayer|80×80|23|40|visible|
|broken_alarm|MachineBrokenHelper|×|0|19|missing export|
|broken_furnace|MachineBrokenHelper|×|0|23|missing export|
|broken_moisture_farm|MachineBrokenHelper|×|0|27|missing export|
|portable_water_purifier|Comparison|32×32|13||visible|
|animal_cage|Comparison|64×48|8||visible|
