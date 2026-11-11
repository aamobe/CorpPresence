# 液体容器：仅独立开发参考

液体容器不等于普通箱体增加一个容量字段。初始化、当前成分量、温度、鉴定、分类、定价、倾倒和保存相互耦合；禁止工具生成或应用这类能力。完整边界见[液体系统参考](liquid-systems.md)。

[容器专题](liquid-containers.md)解释工厂与恢复的差别，[保存交易专题](liquid-persistence-trade.md)解释空满分类与顾客拒收。已有LiquidShell或BuildLegacyOilBottle只能作为原版扩展路线的现有案例，不能当作独立调酒容器的模板。

[拒绝输入](../../../tools/ItemAuthoring/examples/liquid-container.json)只有类别，不提供容量、配方或默认参数。即使声明kind=container或ordinary，只要声明液体能力也整单拒绝；没有“仅壳体”“强制生成”例外。

独立开发时需逐项人工明确：状态所有者、空壳价、容量单位、混合/排空规则、分类/价格刷新、保存恢复、互倒协议、失败保留与历史迁移。不默认与原版水互通，不修改现有银血ID或已发布存档。
