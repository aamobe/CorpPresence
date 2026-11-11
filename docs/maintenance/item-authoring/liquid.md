# 液体成分与混合物：仅独立开发参考

本类不接受生成，连需求设计、默认配方和实现骨架也不输出。完整原则、两条路线、历史银血教训见[液体系统参考](liquid-systems.md)。原版水系统按本项目最高兼容风险对待。

按具体问题阅读[成分与路线](liquid-components.md)、[容器状态](liquid-containers.md)、[保存与交易](liquid-persistence-trade.md)。这些文档服务于人工独立流程，不是工具输入定义。

[拒绝输入](../../../tools/ItemAuthoring/examples/liquid.json)只声明被禁止的类别，不包含成分或配方；Preview/Apply/Check必须返回LIQUID_REQUIRES_SEPARATE_DEVELOPMENT，目标仓库字节不变。任何植物、机器、固体原料混合请求触及此边界也整单拒绝。

源码事实与未核验项以[原版契约](native-contracts.md)和主参考的证据等级为准。独立开发后的代码/实机项目由液体专案确定，不由生成工具调用或生成。
