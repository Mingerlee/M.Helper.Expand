# 容器性能基准

比较 `M.Simple.Container`、`Microsoft.Extensions.DependencyInjection 8.0.1`、`Autofac 8.1.0`（官方 DI 适配器 `10.0.0`）和手写对象创建。

当前包使用四段版本号 `1.0.0.1`。下文 `1.0.0`、`1.0.1` 是测试时的原版本标记；当前代码相对于已测 `1.0.1` 仅调整版本号，保留历史报告及目录名称以便追溯。

## 1.0.1 启动优化结果（2026-11-23）

默认构建和作用域校验均保持开启。以下为进程已预热后的基准均值，完整误差和分配量见原始报告。

| 场景 | Simple 1.0.1 | Autofac | 微软 DI |
| --- | ---: | ---: | ---: |
| 容器构建、释放 | 9.575 μs | 64.259 μs | 10.354 μs |
| 容器构建、首次解析、释放 | 9.999 μs | 83.449 μs | 11.605 μs |
| 单例缓存解析 | 138.79 ns | 665.19 ns | 48.54 ns |
| 瞬态依赖图解析 | 372.25 ns | 3,886.89 ns | 110.86 ns |
| 作用域缓存解析 | 144.38 ns | 668.51 ns | 98.94 ns |
| 作用域创建、依赖图解析、释放 | 1,201.66 ns | 8,280.26 ns | 616.08 ns |

原先关注的“构建、首次解析、释放”从 `1.0.0` 的 `1,311.792 μs` 降至本轮 `9.999 μs`；每次分配从 `36.01 KB` 降至 `7.84 KB`。前后两轮环境状态存在波动，不将此比值作为固定加速倍数。当前解析场景仍慢于微软 DI，不能据此宣称全场景领先。

主要改动：

- 用 .NET 8 `ConstructorInvoker` 替代逐容器的表达式编译，保持完整构建校验。
- 使用弱键缓存复用构造函数和参数元数据；缓存不持有任何容器、业务实例或注册键。每个容器单独绑定依赖和生命周期。
- 零至四参数构造函数直接调用 Invoker；更多参数使用池化数组并清除引用。
- 使用泛型数组创建器替代集合解析的表达式编译。
- 对注册建立索引，避免依赖查找时反复线性扫描所有描述符。

原始报告：

- [1.0.1 构建与首次解析](results/1.0.1/StartupBenchmarks-report-github.md)
- [1.0.1 解析与作用域](results/1.0.1/ResolutionBenchmarks-report-github.md)

本轮预热基准为 1 次进程启动、3 次预热、6 次测量，注册集合和首次解析目标与原基线一致。元数据和 Invoker 可在进程内跨容器复用，所以上述微秒级结果不等于应用新进程的第一次启动。独立冷启动另用 `ColdStartupBenchmarks`，每个提供程序启动 5 个新进程，每个进程只测一次，不预热。

独立新进程首次构建、解析、释放的 Simple 均值由 `67.52 ms` 变为 `53.33 ms`，中位数由 `73.14 ms` 变为 `52.80 ms`，首次分配由 `36.94 KB` 变为 `12.58 KB`。两轮耗时置信区间存在重叠，5 个样本不足以承诺稳定的冷启动加速倍数；其中还包含首次执行相关方法的 JIT 和反射初始化。这与预热后重复构建的量级改善是两个不同指标。

- [优化前独立冷启动](results/ColdStartupBenchmarks-before-report.md)
- [1.0.1 独立冷启动](results/1.0.1/ColdStartupBenchmarks-report-github.md)

## 1.0.0 基线结果（2026-11-23）

下表为平均耗时。越低越好；完整误差、标准差和分配量保留在原始报告中。

| 场景 | Simple | Autofac | 微软 DI |
| --- | ---: | ---: | ---: |
| 单例缓存解析 | 77.90 ns | 337.05 ns | 48.43 ns |
| 瞬态依赖图解析 | 223.69 ns | 2,136.27 ns | 84.14 ns |
| 作用域缓存解析 | 98.52 ns | 673.23 ns | 65.58 ns |
| 作用域创建、依赖图解析、释放 | 631.49 ns | 8,055.79 ns | 310.52 ns |
| 容器构建、首次解析、释放 | 1,311.792 μs | 54.287 μs | 9.324 μs |

手写相同瞬态依赖图为 `25.55 ns`，分配 `112 B`；Simple 与微软 DI 的该图分配同为 `112 B`，Autofac 为 `2632 B`。Simple 的单例和作用域缓存解析没有观察到每次解析的托管分配。

在这四个解析场景中，本次 Simple 平均耗时低于 Autofac，高于微软 DI。Simple 的构建及首次解析明显慢于两者，目前主要成本是构建阶段的表达式编译。此结果不支持“全面优于 Autofac”的结论，也不代表业务 API 吞吐量。

原始 BenchmarkDotNet 报告：

- [解析与作用域](results/ResolutionBenchmarks-report-github.md)
- [构建与首次解析](results/StartupBenchmarks-report-github.md)
- [手写创建基线](results/HandWrittenBenchmarks-report-github.md)

## 测量方式与限制

- Windows 11 `10.0.26300.9974`、SDK `8.0.101`、运行时 `.NET 8.0.1`、X64 RyuJIT、Workstation GC；BenchmarkDotNet `0.13.12`。工具未识别处理器型号。
- `1.0.0` 基线每个场景 1 次进程启动、5 次预热、8 次测量；工具默认异常值处理规则可能使最终保留样本少于 8 个。
- 均使用标准 `IServiceProvider` 入口，Autofac 经官方适配器；没有测量 Autofac 的原生 `Resolve<T>()` 入口。
- Simple 和微软 DI 均开启构建与作用域校验；Autofac 使用默认构建选项，校验能力并不完全对等。
- 稳定解析预先创建计划；完整作用域场景每次创建、解析、同步释放一个作用域，包含可释放服务。
- 构建场景在已启动的基准进程内重复创建容器，并首次解析该容器的瞬态图，再释放容器；不等于新进程启动或整个 Web 应用启动。
- 本次不同场景及前后轮次存在明显波动，部分置信区间较宽；原始报告中的 `Error` 是 99.9% 置信区间的半宽。数据适合确定下一步优化方向，不宜作为稳定倍率承诺。
- 尚未测量 BAMS 真实依赖图、高并发吞吐、多实现集合和大规模泛型注册。

## 复现

从仓库根目录运行，尽量保持机器空闲。预热后的构建及解析：

```powershell
dotnet run --project M.Simple.Container.Benchmarks -c Release -- --filter 'StartupBenchmarks.*' 'ResolutionBenchmarks.*' --job short --warmupCount 3 --iterationCount 6 --artifacts artifacts/container-optimized-benchmarks
```

新进程冷启动使用类型上定义的 ColdStart 作业，不额外指定 `--job short`：

```powershell
dotnet run --project M.Simple.Container.Benchmarks -c Release -- --filter '*ColdStartupBenchmarks*' --artifacts artifacts/container-cold-after
```

快速分阶段诊断（构建、首次解析、释放）使用 `--startup-profile`，结果仅用于定位，不替代 BenchmarkDotNet。数据：[优化前](results/startup-phases-before.csv)、[优化后](results/1.0.1/startup-phases-after.csv)。手写创建基线可用 `--filter '*HandWrittenBenchmarks*' --job short` 单独运行。

后续优先改进每次解析的锁与委托调用成本，再扩大真实依赖图与并发基准。
