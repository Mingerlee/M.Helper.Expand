# API 与源码索引

本页根据当前构建程序集反射和编译器 XML 文档生成，仅列出导出的公共类型及其声明的公共成员。继承成员和 internal 辅助类型不列入；属性访问器合并为属性。

签名中的 Int32、String 等为 CLR 类型名称，泛型参数用反引号或方括号表示。用途描述来自原始 XML 注释；行为差异和边界以功能手册为准。

## M.Simple.Expand.AppSetting

M.Simple.Expand扩展 配置文件读取辅助类

### ConfigExist

```text
Boolean ConfigExist(System.String)
```

M.Simple.Expand扩展节点是否存在

### GetConfig

```text
System.String GetConfig(System.String)
```

M.Simple.Expand扩展读取配置文件value

- name：eg:ConnectionStrings:DefaultConnection

### GetConfigBoolean

```text
Boolean GetConfigBoolean(System.String)
```

M.Simple.Expand扩展bool

### GetConfigInt32

```text
Int32 GetConfigInt32(System.String)
```

M.Simple.Expand扩展Int32

### GetConfigSection

```text
T GetConfigSection[T](System.String)
```

M.Simple.Expand扩展获取节点下所有内容

## M.Simple.Expand.Attributes.AppServiceAttribute

M.Simple.Expand扩展AppService特性

### AppServiceAttribute

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### InterfaceServiceType

```text
Boolean InterfaceServiceType
```

是否可以从第一个接口获取服务类型

### Lifetime

```text
Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime
```

生命周期

### ServiceType

```text
System.Type ServiceType
```

指定服务类型

## M.Simple.Expand.Attributes.AutowiredAttribute

M.Simple.Expand扩展属性注入--自动注入特性

### AutowiredAttribute

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.BetweenValue

M.Simple.Expand扩展两者之间

### BetweenValue

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### IsEqualMaxValue

```text
Boolean IsEqualMaxValue
```

是否可以等于最大值

### IsEqualMinValue

```text
Boolean IsEqualMinValue
```

是否可以等于最小值

### MaxValue

```text
Int32 MaxValue
```

最大值

### MinValue

```text
Int32 MinValue
```

最小值

## M.Simple.Expand.Attributes.CompareValueAttribute

M.Simple.Expand扩展需要对比的属性

### CompareValueAttribute

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.GreaterThanMinValue

M.Simple.Expand扩展大于最小值

### GreaterThanMinValue

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### IsEqualMinValue

```text
Boolean IsEqualMinValue
```

是否可以等于最小值

### MinValue

```text
Int32 MinValue
```

最小值

## M.Simple.Expand.Attributes.IsDateTime

M.Simple.Expand扩展验证是否为日期格式

### IsDateTime

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.IsEmail

M.Simple.Expand扩展验证是否为电子邮件格式：test@example.com

### IsEmail

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.IsIDcard

M.Simple.Expand扩展验证是否为身份证号码格式：11010120250101223X

### IsIDcard

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.IsInt

M.Simple.Expand扩展验证是否为Int类型

### IsInt

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.IsMobile

M.Simple.Expand扩展验证是否为手机号码格式：18888888888

### IsMobile

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.IsTelephone

M.Simple.Expand扩展验证是否为电话号码格式：010-12345678

### IsTelephone

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.IsUrl

M.Simple.Expand扩展验证是否为网址格式：https://www.baidu.com

### IsUrl

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.LessThanMaxValue

M.Simple.Expand扩展小于最大值

### LessThanMaxValue

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### IsEqualMaxValue

```text
Boolean IsEqualMaxValue
```

是否可以等于最大值

### MaxValue

```text
Int32 MaxValue
```

最大值

## M.Simple.Expand.Attributes.ModifyValueAttribute

M.Simple.Expand扩展需要修改的属性

- flidName：属性名称

### ModifyValueAttribute

```text
Void .ctor(System.String)
```

M.Simple.Expand扩展需要修改的属性

- flidName：属性名称

### FlidName

```text
System.String FlidName
```

字段名称

## M.Simple.Expand.Attributes.Password

M.Simple.Expand扩展验证密码格式：必须包含字母(大写或者小写)、数字和特殊符号，长度8~20

### Password

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Attributes.RegexMatch

M.Simple.Expand扩展正则表达式验证

### RegexMatch

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### RegexStr

```text
System.String RegexStr
```

正则字符串

## M.Simple.Expand.Attributes.UserId

M.Simple.Expand扩展验证用户名格式：A123456_B必须以字母开头，长度为4~19，可以包含字母、数字和_

### UserId

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Enums.DiffMonthWayEnum

### value__

```text
Int32 value__
```

重载或注释详情请对照功能手册与源码。

### 简单

```text
M.Simple.Expand.Enums.DiffMonthWayEnum 简单
```

自然月差，忽略日；方向为结束日期减开始日期

### 精确

```text
M.Simple.Expand.Enums.DiffMonthWayEnum 精确
```

完整月差，按较早日期AddMonths确定边界，缺失对应日时取月末

### 跨月

```text
M.Simple.Expand.Enums.DiffMonthWayEnum 跨月
```

跨过的自然月边界数；同月为0，与简单模式一致

## M.Simple.Expand.Enums.FormatMoneyEnum

M.Simple.Expand扩展金额格式化枚举

### C0

```text
M.Simple.Expand.Enums.FormatMoneyEnum C0
```

eg:10000000.51=>10,000,001eg:10000000.49=>10,000,000

### C0_en_US

```text
M.Simple.Expand.Enums.FormatMoneyEnum C0_en_US
```

eg:10000000.51=>$10,000,001eg:10000000.49=>$10,000,000

### C0_zh_CN

```text
M.Simple.Expand.Enums.FormatMoneyEnum C0_zh_CN
```

eg:10000000.51=>￥10,000,001eg:10000000.49=>￥10,000,000

### C1

```text
M.Simple.Expand.Enums.FormatMoneyEnum C1
```

eg:10000000.51=>10,000,000.5eg:10000000.49=>10,000,000.5

### C1_en_US

```text
M.Simple.Expand.Enums.FormatMoneyEnum C1_en_US
```

eg:10000000.51=>$10,000,000.5eg:10000000.49=>$10,000,000.5

### C1_zh_CN

```text
M.Simple.Expand.Enums.FormatMoneyEnum C1_zh_CN
```

eg:10000000.51=>￥10,000,000.5eg:10000000.49=>￥10,000,000.5

### C2

```text
M.Simple.Expand.Enums.FormatMoneyEnum C2
```

eg:10000000.515=>10,000,000.52eg:10000000.511=>10,000,000.51

### C2_en_US

```text
M.Simple.Expand.Enums.FormatMoneyEnum C2_en_US
```

eg:10000000.515=>$10,000,000.52eg:10000000.511=>$10,000,000.51

### C2_zh_CN

```text
M.Simple.Expand.Enums.FormatMoneyEnum C2_zh_CN
```

eg:10000000.515=>￥10,000,000.52eg:10000000.511=>￥10,000,000.51

### C3

```text
M.Simple.Expand.Enums.FormatMoneyEnum C3
```

eg:10000000.5151=>10,000,000.515eg:10000000.5155=>10,000,000.516

### C3_en_US

```text
M.Simple.Expand.Enums.FormatMoneyEnum C3_en_US
```

eg:10000000.5151=>$10,000,000.515eg:10000000.5155=>$10,000,000.516

### C3_zh_CN

```text
M.Simple.Expand.Enums.FormatMoneyEnum C3_zh_CN
```

eg:10000000.5151=>￥10,000,000.515eg:10000000.5155=>￥10,000,000.516

### C4

```text
M.Simple.Expand.Enums.FormatMoneyEnum C4
```

eg:10000000.51515=>10,000,000.5152eg:10000000.51511=>10,000,000.5151

### C4_en_US

```text
M.Simple.Expand.Enums.FormatMoneyEnum C4_en_US
```

eg:10000000.51515=>$10,000,000.5152eg:10000000.51511=>$10,000,000.5151

### C4_zh_CN

```text
M.Simple.Expand.Enums.FormatMoneyEnum C4_zh_CN
```

eg:10000000.51515=>￥10,000,000.5152eg:10000000.51511=>￥10,000,000.5151

### value__

```text
Int32 value__
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Enums.LanguageEnum

M.Simple.Expand扩展语言

### en_US

```text
M.Simple.Expand.Enums.LanguageEnum en_US
```

英文

### value__

```text
Int32 value__
```

重载或注释详情请对照功能手册与源码。

### zh_CN

```text
M.Simple.Expand.Enums.LanguageEnum zh_CN
```

中文

## M.Simple.Expand.Extensions

### AddAppServices

```text
Void AddAppServices(Microsoft.Extensions.DependencyInjection.IServiceCollection)
```

M.Simple.Expand扩展 注册应用程序域中所有有AppService特性的服务 使用该属性注入的时候需要注意，Program(Startup)配置中使用 builder.Services.AddAppServices();//注册应用服务 Autofac容器注入时，需要添加.PropertiesAutowired() 允许属性注入

### AddEMailSetup

```text
Void AddEMailSetup(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Func`2[System.IServiceProvider,M.Simple.Expand.Services.MailSettings])
```

注入邮件配置

- mailSettingsFunc：邮件服务器配置

### AddFtpClientSetup

```text
Void AddFtpClientSetup(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Func`2[System.IServiceProvider,M.Simple.Expand.Services.FtpClientSettings])
```

注入ftp配置

- ftpClientSettingsFunc：ftp配置

### AddJsonFileWithBasePath

```text
Microsoft.Extensions.Configuration.IConfigurationBuilder AddJsonFileWithBasePath(Microsoft.Extensions.Configuration.IConfigurationBuilder, System.String, Boolean, Boolean)
```

M.Simple.Expand扩展自定义 AddJsonFile 重载，自动使用已设置的基础路径

### AesDecrypt

```text
System.String AesDecrypt(System.String, Byte[], Byte[])
```

重载或注释详情请对照功能手册与源码。

### AesDecrypt

```text
System.String AesDecrypt(System.String, System.String, System.String)
```

重载或注释详情请对照功能手册与源码。

### AesEncrypt

```text
M.Simple.Expand.Models.AESEncryptResultModel AesEncrypt(System.String)
```

重载或注释详情请对照功能手册与源码。

### AesEncrypt

```text
System.String AesEncrypt(System.String, Byte[], Byte[])
```

重载或注释详情请对照功能手册与源码。

### AesEncrypt

```text
System.String AesEncrypt(System.String, System.String, System.String)
```

重载或注释详情请对照功能手册与源码。

### ByProperty

```text
System.Collections.Generic.IEqualityComparer`1[T] ByProperty[T,TKey](T, System.Func`2[T,TKey], System.Collections.Generic.IEqualityComparer`1[TKey])
```

创建按属性比较的比较器

### CalculateAge

```text
Int32 CalculateAge(System.DateTime)
```

M.Simple.Expand扩展依据出生日期计算年龄

### CamelToUnderline

```text
System.String CamelToUnderline(System.String)
```

M.Simple.Expand扩展驼峰命名转下划线命名（如：userName → user_name）

### CeilingSpecial

```text
System.Decimal CeilingSpecial(System.Decimal, Int32)
```

按规则向上取整：保留两位小数，第三位及以后向上进一 212.0001 → 212.00 212.001 → 212.01 212.000 → 212.00

### CompareModel

```text
System.Collections.Generic.List`1[M.Simple.Expand.Models.ComparesResults] CompareModel[T](T, T, Boolean)
```

M.Simple.Expand扩展 比较两个实体值是否有修改过，返回修改结果集合

- newModel：新实体

- oldModel：对比的旧实体

- isAttr：是否只比较有CompareValueAttribute特性标记的属性

返回：修改结果集合

### ContainsAll

```text
Boolean ContainsAll(System.String, System.StringComparison, System.String[])
```

重载或注释详情请对照功能手册与源码。

### ContainsAll

```text
Boolean ContainsAll(System.String, System.String[])
```

重载或注释详情请对照功能手册与源码。

### ContainsAny

```text
Boolean ContainsAny(System.String, System.StringComparison, System.String[])
```

重载或注释详情请对照功能手册与源码。

### ContainsAny

```text
Boolean ContainsAny(System.String, System.String[])
```

重载或注释详情请对照功能手册与源码。

### CreateByProperty

```text
System.Collections.Generic.IEqualityComparer`1[T] CreateByProperty[T,TKey](System.Func`2[T,TKey], System.Collections.Generic.IEqualityComparer`1[TKey])
```

创建按属性比较的比较器

### DefaultEmpty

```text
System.Collections.Generic.IEnumerable`1[T] DefaultEmpty[T](System.Collections.Generic.IEnumerable`1[T])
```

M.Simple.Expand扩展如果集合为 null，返回空集合（避免空引用异常）

### DiffDays

```text
Int32 DiffDays(System.DateTime, System.DateTime, Boolean)
```

M.Simple.Expand扩展 计算日期差

- endTime：结束日期

- includeToday：是否包含当天

返回：天

### DiffMonth

```text
Int32 DiffMonth(System.DateTime, System.DateTime, M.Simple.Expand.Enums.DiffMonthWayEnum)
```

M.Simple.Expand扩展 计算月份差

- startTime：开始日期，忽略时分秒

- endTime：结束日期，忽略时分秒

- diffMonthWay：计算方式：简单、精确、跨月

返回：有符号的月份差

### ExceptItem

```text
System.Collections.Generic.IEnumerable`1[T] ExceptItem[T](System.Collections.Generic.IEnumerable`1[T], T, System.Collections.Generic.IEqualityComparer`1[T])
```

M.Simple.Expand扩展排除指定元素

### ExceptItems

```text
System.Collections.Generic.IEnumerable`1[T] ExceptItems[T](System.Collections.Generic.IEnumerable`1[T], System.Collections.Generic.IEnumerable`1[T], System.Collections.Generic.IEqualityComparer`1[T])
```

M.Simple.Expand扩展排除指定的多个元素（支持自定义相等比较）

- source：源集合

- itemsToExclude：要排除的元素集合

- comparer：自定义相等比较器

### FirstDayOfWeek

```text
System.DateTime FirstDayOfWeek(System.DateTime)
```

M.Simple.Expand扩展取本周的第一天（周一）

### FirstLetterToUpper

```text
System.String FirstLetterToUpper(System.String)
```

M.Simple.Expand扩展首字母大写

### Flatten

```text
System.Collections.Generic.IEnumerable`1[T] Flatten[T](System.Collections.Generic.IEnumerable`1[System.Collections.Generic.IEnumerable`1[T]])
```

M.Simple.Expand扩展扁平化嵌套集合

### GetAttributeOfType

```text
T GetAttributeOfType[T](System.Enum)
```

M.Simple.Expand扩展直接获取特性

### GetDate

```text
System.DateTime GetDate(System.Nullable`1[System.DateTime], System.Nullable`1[System.DateTime])
```

M.Simple.Expand扩展获取日期（不含时分秒）

- defaultDateTime：默认日期

### GetDescription

```text
System.String GetDescription(System.Enum)
```

M.Simple.Expand扩展获取枚举值的描述信息

- source：枚举值

### GetDisplayDescription

```text
System.String GetDisplayDescription(System.Enum)
```

M.Simple.Expand扩展获取特性 (DisplayAttribute) 的说明(Description)

返回：如果未使用该特性，则返回枚举的名称

### GetDisplayName

```text
System.String GetDisplayName(System.Enum)
```

M.Simple.Expand扩展 获取特性 (DisplayAttribute) 的名称(Name)

返回：如果未使用该特性，则返回枚举的名称

### GetEMailAddressArray

```text
System.String[] GetEMailAddressArray(System.String)
```

M.Simple.Expand扩展拆分邮件地址

### GetEncryptSecret

```text
System.String GetEncryptSecret(System.Guid)
```

M.Simple.Expand扩展随机秘钥(Guid)

### GetFirstDayOfMonth

```text
System.DateTime GetFirstDayOfMonth(System.DateTime)
```

M.Simple.Expand扩展 获取当月第一天

### GetLastDayOfMonth

```text
System.DateTime GetLastDayOfMonth(System.DateTime)
```

M.Simple.Expand扩展 获取当月最后一天

### GetListHashCode

```text
Int32 GetListHashCode(System.Collections.Generic.IEnumerable`1[System.Int32])
```

重载或注释详情请对照功能手册与源码。

### GetListHashCode

```text
Int32 GetListHashCode(System.Collections.Generic.IEnumerable`1[System.Nullable`1[System.Int32]])
```

重载或注释详情请对照功能手册与源码。

### GetListHashCode

```text
Int32 GetListHashCode(System.Collections.Generic.IEnumerable`1[System.String])
```

重载或注释详情请对照功能手册与源码。

### GetListHashCode

```text
Int32 GetListHashCode(System.Collections.Generic.IList`1[System.Int32])
```

重载或注释详情请对照功能手册与源码。

### GetListHashCode

```text
Int32 GetListHashCode(System.Collections.Generic.IList`1[System.Nullable`1[System.Int32]])
```

重载或注释详情请对照功能手册与源码。

### GetListHashCode

```text
Int32 GetListHashCode(System.Collections.Generic.IList`1[System.String])
```

重载或注释详情请对照功能手册与源码。

### GetNextGuid

```text
System.Guid GetNextGuid(System.Guid)
```

M.Simple.Expand扩展获取Guid

### GetNoHyphenString

```text
System.String GetNoHyphenString(System.Guid)
```

M.Simple.Expand扩展转换为无横线的GUID字符串

### GetRealLength

```text
Int32 GetRealLength(System.String)
```

M.Simple.Expand扩展获取字节长度

### GetStringValue

```text
System.String GetStringValue(System.Reflection.PropertyInfo, System.Object)
```

M.Simple.Expand扩展获取属性的值

### GetValue

```text
TValue GetValue[TKey,TValue](System.Collections.Generic.Dictionary`2[TKey,TValue], TKey)
```

M.Simple.Expand扩展获取Value值

### GetValueOrDefault

```text
System.String GetValueOrDefault(System.String, System.String)
```

M.Simple.Expand扩展自身值为null或者空字符，则取默认值

### GroupBySelectFirst

```text
System.Collections.Generic.IEnumerable`1[T] GroupBySelectFirst[T,TKey](System.Collections.Generic.IEnumerable`1[T], System.Func`2[T,TKey])
```

M.Simple.Expand扩展分组后取每组的第一条数据

### GroupByThenSortTakeFirst

```text
System.Collections.Generic.IEnumerable`1[T] GroupByThenSortTakeFirst[T,TGroupKey,TSortKey](System.Collections.Generic.IEnumerable`1[T], System.Func`2[T,TGroupKey], System.Func`2[T,TSortKey], Boolean)
```

M.Simple.Expand扩展分组后排序，取每组排序后的第一条数据

- source：源集合

- groupKeySelector：分组字段选择器（按什么分组）

- sortKeySelector：排序字段选择器（分组内按什么排序）

- isDescending：是否降序排序（默认true）

返回：每组排序后的第一条数据组成的集合

### HasAtLeast

```text
Boolean HasAtLeast[T](System.Collections.Generic.IEnumerable`1[T], Int32)
```

M.Simple.Expand扩展判断集合长度是否 >= 指定值（无需全遍历，性能优化）

### IsModify

```text
Boolean IsModify[T](T, T, Boolean)
```

M.Simple.Expand扩展比较两个实体值是否有修改过

- compareModel：对比的model实体

- isAttr：是否有CompareValueAttribute特性标记

返回：有修改返回true，否则返回false

### IsNullOrEmpty

```text
Boolean IsNullOrEmpty(System.Data.DataTable)
```

M.Simple.Expand扩展是否为NULL或者为空

### IsNullOrEmpty

```text
Boolean IsNullOrEmpty[T](System.Collections.Generic.IEnumerable`1[T])
```

M.Simple.Expand扩展安全判断集合是否为空（null/无元素）

### IsNullOrEmptyOrWhiteSpace

```text
Boolean IsNullOrEmptyOrWhiteSpace(System.String)
```

M.Simple.Expand扩展判断字符串是否为 null/空/空白字符

### IsOverlap

```text
Boolean IsOverlap(System.Collections.Generic.List`1[M.Simple.Expand.Models.DatePeriod])
```

重载或注释详情请对照功能手册与源码。

### IsOverlap

```text
Boolean IsOverlap(System.Collections.Generic.List`1[M.Simple.Expand.Models.DateTimePeriod])
```

重载或注释详情请对照功能手册与源码。

### IsToday

```text
Boolean IsToday(System.DateTime)
```

M.Simple.Expand扩展 判断是否为今天

### IsTomorrow

```text
Boolean IsTomorrow(System.DateTime)
```

M.Simple.Expand扩展 判断是否为明天

### IsWeekend

```text
Boolean IsWeekend(System.DateTime)
```

M.Simple.Expand扩展 判断是否为周末（周六/周日）

### IsWorkday

```text
Boolean IsWorkday(System.DateTime)
```

M.Simple.Expand扩展判断是否为工作日（周一至周五）

### IsYesterday

```text
Boolean IsYesterday(System.DateTime)
```

M.Simple.Expand扩展 判断是否为昨天

### LastDayOfWeek

```text
System.DateTime LastDayOfWeek(System.DateTime)
```

M.Simple.Expand扩展取本周的最后一天（周日）

### MD5Encrypt

```text
System.String MD5Encrypt(System.String)
```

M.Simple.Expand扩展MD5加密

- plainText：明文

返回：密文

### MD5SaltEncrypt

```text
M.Simple.Expand.Models.MD5EncryptResultModel MD5SaltEncrypt(System.String)
```

重载或注释详情请对照功能手册与源码。

### MD5SaltEncrypt

```text
System.String MD5SaltEncrypt(System.String, System.String)
```

重载或注释详情请对照功能手册与源码。

### MD5SaltVerify

```text
Boolean MD5SaltVerify(System.String, System.String, System.String)
```

M.Simple.Expand扩展MD5验证

- plainText：明文

- cipherText：密文

- encryptSecret：盐值

返回：bool

### MD5Verify

```text
Boolean MD5Verify(System.String, System.String)
```

M.Simple.Expand扩展MD5验证

- plainText：明文

- cipherText：密文

### Merge

```text
System.Collections.Generic.Dictionary`2[TKey,TValue] Merge[TKey,TValue](System.Collections.Generic.Dictionary`2[TKey,TValue], TKey, TValue)
```

M.Simple.Expand扩展存在时更改,不存在时添加

- dictionary：之前字典

- key：Key

- value：Value

返回：添加后的字典

### MergeIgnore

```text
System.Collections.Generic.Dictionary`2[TKey,TValue] MergeIgnore[TKey,TValue](System.Collections.Generic.Dictionary`2[TKey,TValue], TKey, TValue)
```

M.Simple.Expand扩展存在时忽略,不存在时添加

- dictionary：之前字典

- key：Key

- value：Value

返回：添加后的字典

### MergeIgnores

```text
System.Collections.Generic.Dictionary`2[TKey,TValue] MergeIgnores[TKey,TValue](System.Collections.Generic.Dictionary`2[TKey,TValue], System.Collections.Generic.Dictionary`2[TKey,TValue])
```

M.Simple.Expand扩展存在时忽略,不存在时添加

- targetDictionary：之前字典

- sourceDictionary：Key-Value需要添加的字典

返回：添加后的字典

### Merges

```text
System.Collections.Generic.Dictionary`2[TKey,TValue] Merges[TKey,TValue](System.Collections.Generic.Dictionary`2[TKey,TValue], System.Collections.Generic.Dictionary`2[TKey,TValue])
```

M.Simple.Expand扩展存在时更改,不存在时添加

- targetDictionary：之前字典

- sourceDictionary：Key-Value需要添加的字典

返回：添加后的字典

### ModifyField

```text
Void ModifyField[T,T2](T, T2, Boolean)
```

M.Simple.Expand扩展修改实体指定字段

### MoneyStringToDecimal

```text
System.Decimal MoneyStringToDecimal(System.String, Int32)
```

M.Simple.Expand扩展金额格式字符串转decimal，默认保留两位小数

- input：金额格式字符串

- digits：精度(保留的几位小数)

### NextDay

```text
System.DateTime NextDay(System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### NextDay

```text
System.DateTime NextDay(System.Nullable`1[System.DateTime])
```

重载或注释详情请对照功能手册与源码。

### NextDay

```text
System.DateTime NextDay(System.Nullable`1[System.DateTime], System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### PervDay

```text
System.DateTime PervDay(System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### PervDay

```text
System.DateTime PervDay(System.Nullable`1[System.DateTime])
```

重载或注释详情请对照功能手册与源码。

### PervDay

```text
System.DateTime PervDay(System.Nullable`1[System.DateTime], System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### RandomTake

```text
T RandomTake[T](System.Collections.Generic.IEnumerable`1[T])
```

M.Simple.Expand扩展随机获取集合中的一个元素

### Remove

```text
Void Remove[TKey,TValue](System.Collections.Generic.Dictionary`2[TKey,TValue], TKey)
```

M.Simple.Expand扩展删除

- dictionary：字典

- key：Key

返回：Value

### RemovePop

```text
TValue RemovePop[TKey,TValue](System.Collections.Generic.Dictionary`2[TKey,TValue], TKey)
```

M.Simple.Expand扩展获取值之后，再删除

- dictionary：字典

- key：Key

返回：删除的值

### Removes

```text
Void Removes[TKey,TValue](System.Collections.Generic.Dictionary`2[TKey,TValue], System.Collections.Generic.IEnumerable`1[TKey])
```

M.Simple.Expand扩展批量删除

- dictionary：字典

- keys：Key集合

返回：Value

### ReverseString

```text
System.String ReverseString(System.String)
```

字符串反转

### RsaDecrypt

```text
System.String RsaDecrypt(System.String, System.String)
```

M.Simple.Expand扩展RSA解密

- cipherText：密文

- privateKey：私钥

返回：明文

### RsaDecryptWithBlock

```text
System.String RsaDecryptWithBlock(System.String, System.String)
```

M.Simple.Expand扩展RSA解密(分段)

- cipherText：密文

- privateKey：私钥

返回：明文

### RsaEncrypt

```text
M.Simple.Expand.Models.RSAEncryptResultModel RsaEncrypt(System.String)
```

重载或注释详情请对照功能手册与源码。

### RsaEncrypt

```text
System.String RsaEncrypt(System.String, System.String)
```

重载或注释详情请对照功能手册与源码。

### RsaEncryptWithBlock

```text
M.Simple.Expand.Models.RSAEncryptResultModel RsaEncryptWithBlock(System.String)
```

重载或注释详情请对照功能手册与源码。

### RsaEncryptWithBlock

```text
System.String RsaEncryptWithBlock(System.String, System.String)
```

重载或注释详情请对照功能手册与源码。

### RsaSignData

```text
System.String RsaSignData(System.String, System.String)
```

M.Simple.Expand扩展RSA生成签名

- plainText：明文

- encryptKey：秘钥

返回：签名密文

### RsaVerifyData

```text
Boolean RsaVerifyData(System.String, System.String, System.String)
```

M.Simple.Expand扩展RSA生成签名

- plainText：明文

- autograph：签名密文

- publickey：公钥

返回：签名密文

### SetConfigurationBasePath

```text
Microsoft.Extensions.Configuration.IConfigurationBuilder SetConfigurationBasePath(Microsoft.Extensions.Configuration.IConfigurationBuilder, System.String)
```

M.Simple.Expand扩展自定义扩展方法，模拟 SetBasePath 的功能

### SetStringValue

```text
Void SetStringValue(System.Reflection.PropertyInfo, System.Object, System.String)
```

M.Simple.Expand扩展设置属性的值

### Shuffle

```text
System.Collections.Generic.IEnumerable`1[T] Shuffle[T](System.Collections.Generic.IEnumerable`1[T])
```

M.Simple.Expand扩展随机打乱集合元素顺序

### SplitBatch

```text
System.Collections.Generic.IEnumerable`1[System.Collections.Generic.IEnumerable`1[T]] SplitBatch[T](System.Collections.Generic.IEnumerable`1[T], Int32)
```

M.Simple.Expand扩展按批次处理集合

- batchSize：每批次大小

### SwapCase

```text
System.String SwapCase(System.String)
```

M.Simple.Expand扩展大小写字母互转（大写转小写，小写转大写）

- str：任意字符串

返回：大小写互换后的字符串

### TakeLast

```text
System.Collections.Generic.IEnumerable`1[T] TakeLast[T](System.Collections.Generic.IEnumerable`1[T], Int32)
```

M.Simple.Expand扩展取集合最后 N 条元素（无需全遍历，性能优化）

### ToChinese

```text
System.String ToChinese(System.Decimal)
```

M.Simple.Expand扩展数据转中文大写：四舍五入后保留两位小数

### ToChineseDateString

```text
System.String ToChineseDateString(System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### ToChineseDateString

```text
System.String ToChineseDateString(System.Nullable`1[System.DateTime])
```

重载或注释详情请对照功能手册与源码。

### ToChineseDateString

```text
System.String ToChineseDateString(System.Nullable`1[System.DateTime], System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### ToChineseDateTimeString

```text
System.String ToChineseDateTimeString(System.DateTime, Boolean)
```

重载或注释详情请对照功能手册与源码。

### ToChineseDateTimeString

```text
System.String ToChineseDateTimeString(System.Nullable`1[System.DateTime], Boolean)
```

重载或注释详情请对照功能手册与源码。

### ToChineseDateTimeString

```text
System.String ToChineseDateTimeString(System.Nullable`1[System.DateTime], System.DateTime, Boolean)
```

重载或注释详情请对照功能手册与源码。

### ToComboBoxs

```text
M.Simple.Expand.Models.ComboBoxIntViewModel ToComboBoxs[T](M.Simple.Expand.Models.ComboBoxIntViewModel, System.Collections.Generic.IEnumerable`1[System.Int32])
```

M.Simple.Expand扩展将枚举类转换成下拉列表

- values：指定值集合

### ToCover

```text
System.String ToCover(System.String, Char)
```

M.Simple.Expand扩展隐藏部分字符

### ToDBC

```text
System.String ToDBC(System.String)
```

M.Simple.Expand扩展全角转半角的函数(DBC case)

- input：任意字符串

返回：半角字符串

### ToDataTable

```text
System.Data.DataTable ToDataTable(System.Collections.IList)
```

M.Simple.Expand扩展将List数据转换为DataTable

### ToDefaultDate

```text
System.String ToDefaultDate(System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### ToDefaultDate

```text
System.String ToDefaultDate(System.Nullable`1[System.DateTime])
```

重载或注释详情请对照功能手册与源码。

### ToDefaultDate

```text
System.String ToDefaultDate(System.Nullable`1[System.DateTime], System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### ToDictionarySafe

```text
System.Collections.Generic.Dictionary`2[TKey,T] ToDictionarySafe[T,TKey](System.Collections.Generic.IEnumerable`1[T], System.Func`2[T,TKey], System.Collections.Generic.IEqualityComparer`1[TKey])
```

M.Simple.Expand扩展转换为字典（重复键时取最后一个值）

### ToDictionarys

```text
System.Collections.Generic.Dictionary`2[TKey,TValue] ToDictionarys[TModel,TKey,TValue](System.Collections.Generic.IEnumerable`1[TModel], System.Func`2[TModel,TKey], System.Func`2[TModel,TValue])
```

M.Simple.Expand扩展列表转换成字典(自动忽略重复数据)

- list：列表

- keySelector：用于从每个元素中提取键的函数

- elementSelector：用于从每个元素产生结果元素值的转换函数

返回：Value

### ToEnum

```text
T ToEnum[T](System.String)
```

M.Simple.Expand扩展根据字符串转成指定枚举值

返回：返回指定枚举值

### ToEnumText

```text
System.String ToEnumText[T](Int32)
```

重载或注释详情请对照功能手册与源码。

### ToEnumText

```text
System.String ToEnumText[T](System.Nullable`1[System.Int32])
```

重载或注释详情请对照功能手册与源码。

### ToFirstPinyin

```text
System.String ToFirstPinyin(System.String)
```

M.Simple.Expand扩展汉字转化为拼音首字母

- chineseStr：汉字

返回：首字母

### ToFormatMoney

```text
System.String ToFormatMoney(System.Decimal, M.Simple.Expand.Enums.FormatMoneyEnum)
```

重载或注释详情请对照功能手册与源码。

### ToFormatMoney

```text
System.String ToFormatMoney(System.Nullable`1[System.Decimal], M.Simple.Expand.Enums.FormatMoneyEnum)
```

重载或注释详情请对照功能手册与源码。

### ToHashCode

```text
Int32 ToHashCode(System.String, System.Object[])
```

M.Simple.Expand扩展获取hashCode

### ToInt32

```text
Int32 ToInt32(System.Enum)
```

M.Simple.Expand扩展将枚举值转换为Int32

- source：枚举值

### ToLists

```text
System.Collections.Generic.List`1[T] ToLists[T](System.Data.DataTable)
```

M.Simple.Expand扩展将DataTable数据转换成List

### ToLunarCalendarDate

```text
System.String ToLunarCalendarDate(System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### ToLunarCalendarDate

```text
System.String ToLunarCalendarDate(System.Nullable`1[System.DateTime])
```

重载或注释详情请对照功能手册与源码。

### ToLunarCalendarDate

```text
System.String ToLunarCalendarDate(System.Nullable`1[System.DateTime], System.DateTime)
```

重载或注释详情请对照功能手册与源码。

### ToPastCurrentTime

```text
System.String ToPastCurrentTime(System.DateTime, System.Nullable`1[System.DateTime], M.Simple.Expand.Enums.LanguageEnum)
```

M.Simple.Expand扩展距离当前时间过去多久

- language：语言：中文；英文

### ToPinYin

```text
System.String ToPinYin(System.String)
```

M.Simple.Expand扩展汉字转化为拼音

- chineseStr：汉字

返回：全拼

### ToRound

```text
System.Decimal ToRound(System.Decimal, Int32)
```

重载或注释详情请对照功能手册与源码。

### ToRound

```text
System.Decimal ToRound(System.Nullable`1[System.Decimal], Int32)
```

重载或注释详情请对照功能手册与源码。

### ToSBC

```text
System.String ToSBC(System.String)
```

M.Simple.Expand扩展半角转全角的函数(SBC case)

- input：任意字符串

返回：全角字符串

### ToTextDescriptions

```text
System.Collections.Generic.IDictionary`2[System.String,System.String] ToTextDescriptions(System.Type)
```

M.Simple.Expand扩展 转成枚举字符串值(Text)及描述值的字典

返回：枚举字符串值(Text)及描述值的字典

### ToTimesTampCode

```text
System.String ToTimesTampCode(System.DateTime)
```

M.Simple.Expand扩展时间戳编码

返回：yyyyMMddHHmmssfff

### ToValueDescriptions

```text
System.Collections.Generic.IDictionary`2[System.Int32,System.String] ToValueDescriptions(System.Type)
```

M.Simple.Expand扩展转成枚举值(Value)及描述值的字典

返回：枚举值(Value)及描述值的字典

### TryToEnum

```text
Boolean TryToEnum[T](System.String, T ByRef)
```

M.Simple.Expand扩展根据字符串转成指定枚举值

返回：返回指定枚举值

### UnderlineToCamel

```text
System.String UnderlineToCamel(System.String)
```

M.Simple.Expand扩展下划线命名转驼峰命名（如：user_name → userName）

### Where

```text
System.Data.DataTable Where(System.Data.DataTable, System.Func`2[System.Data.DataRow,System.Boolean])
```

M.Simple.Expand扩展根据条件筛选DataTable的行（类似Linq的Where）

- dt：源DataTable

- predicate：筛选条件

返回：筛选后的新DataTable

### WhereNotNull

```text
System.Collections.Generic.IEnumerable`1[T] WhereNotNull[T](System.Collections.Generic.IEnumerable`1[System.Nullable`1[T]])
```

重载或注释详情请对照功能手册与源码。

### WhereNotNull

```text
System.Collections.Generic.IEnumerable`1[T] WhereNotNull[T](System.Collections.Generic.IEnumerable`1[T])
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.FastAllPropertiesEqualityComparer`1

通过预编译委托比较实体的全部公共可读非索引属性；值类型、字符串及无可比较属性的类型使用自身相等规则。

### FastAllPropertiesEqualityComparer`1

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### Equals

```text
Boolean Equals(T, T)
```

### GetHashCode

```text
Int32 GetHashCode(T)
```

### Instance

```text
M.Simple.Expand.FastAllPropertiesEqualityComparer`1[T] Instance
```

## M.Simple.Expand.Models.AESEncryptResultModel

AES加密结果模型

### AESEncryptResultModel

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### CipherText

```text
System.String CipherText
```

密文

### IV

```text
Byte[] IV
```

初始化向量

### Key

```text
Byte[] Key
```

秘钥

## M.Simple.Expand.Models.ComboBoxIntModel

M.Simple.Expand扩展下拉框 数值类型选项

### ComboBoxIntModel

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### Text

```text
System.String Text
```

文本

### Value

```text
Int32 Value
```

值

## M.Simple.Expand.Models.ComboBoxIntViewModel

M.Simple.Expand扩展下拉框 数值类型选项

### ComboBoxIntViewModel

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### SelectOptions

```text
System.Collections.Generic.IEnumerable`1[M.Simple.Expand.Models.ComboBoxIntModel] SelectOptions
```

数值下拉框选项集

## M.Simple.Expand.Models.ComparesResults

M.Simple.Expand扩展实体对比结果模型

### ComparesResults

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### FiledName

```text
System.String FiledName
```

字段名称

### FiledNewValue

```text
System.String FiledNewValue
```

修改后的值

### FiledOldValue

```text
System.String FiledOldValue
```

修改前的值

### IsAdded

```text
System.Nullable`1[System.Boolean] IsAdded
```

是否新增

### IsDelete

```text
System.Nullable`1[System.Boolean] IsDelete
```

是否删除

### IsModify

```text
System.Nullable`1[System.Boolean] IsModify
```

是否修改

## M.Simple.Expand.Models.DatePeriod

M.Simple.Expand扩展日期集合

### DatePeriod

```text
Void .ctor(System.DateTime, System.DateTime)
```

构造函数

- startDate：开始日期

- endDate：结束日期

### EndDate

```text
System.DateTime EndDate
```

结束日期

### StartDate

```text
System.DateTime StartDate
```

开始日期

## M.Simple.Expand.Models.DateTimePeriod

M.Simple.Expand扩展日期时间集合

### DateTimePeriod

```text
Void .ctor(System.DateTime, System.DateTime)
```

构造

### EndTime

```text
System.DateTime EndTime
```

结束时间

### StartTime

```text
System.DateTime StartTime
```

开始时间

## M.Simple.Expand.Models.MD5EncryptResultModel

MD5加密结果模型

### MD5EncryptResultModel

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### CipherText

```text
System.String CipherText
```

密文

### EncryptSecret

```text
System.String EncryptSecret
```

秘钥

## M.Simple.Expand.Models.RSAEncryptResultModel

AES加密结果模型

### RSAEncryptResultModel

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### CipherText

```text
System.String CipherText
```

密文

### PrivateKey

```text
System.String PrivateKey
```

私钥

### PublicKey

```text
System.String PublicKey
```

公钥

## M.Simple.Expand.Models.RSAKeyModel

密钥类

- publicKey：共有key

- privateKey：私有key

### RSAKeyModel

```text
Void .ctor(System.String, System.String)
```

密钥类

- publicKey：共有key

- privateKey：私有key

### PrivateKey

```text
System.String PrivateKey
```

私有Key--可以加密也可以加密

### PublicKey

```text
System.String PublicKey
```

公有Key---加密key

## M.Simple.Expand.PropertyEqualityComparer`2

通用按属性比较的比较器（适用于任何类型按指定属性判断相等）

### PropertyEqualityComparer`2

```text
Void .ctor(System.Func`2[T,TKey], System.Collections.Generic.IEqualityComparer`1[TKey])
```

构造函数

- keySelector：要比较的属性（如 u => u.Id）

- keyComparer：属性的比较器（默认EqualityComparer.Default）

### Equals

```text
Boolean Equals(T, T)
```

### GetHashCode

```text
Int32 GetHashCode(T)
```

## M.Simple.Expand.Services.AutowiredService

M.Simple.Services服务类从容器装配service

### AutowiredService

```text
Void .ctor(System.IServiceProvider)
```

M.Simple.Services服务类从容器装配service

### Autowired

```text
Void Autowired(System.Object)
```

重载或注释详情请对照功能手册与源码。

### Autowired

```text
Void Autowired(System.Object, System.IServiceProvider)
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Services.EMailService

M.Simple.Services服务类邮件服务

### EMailService

```text
Void .ctor(M.Simple.Expand.Services.MailSettings)
```

构造

### GetSmtpClient

```text
System.Net.Mail.SmtpClient GetSmtpClient(M.Simple.Expand.Services.MailSettings)
```

获取客户端

### SendMail

```text
M.Simple.Expand.Services.MailResult SendMail(M.Simple.Expand.Services.MailContent)
```

发送邮件

### SendMailAsync

```text
System.Threading.Tasks.Task`1[M.Simple.Expand.Services.MailResult] SendMailAsync(M.Simple.Expand.Services.MailContent)
```

发送邮件

### Settings

```text
M.Simple.Expand.Services.MailSettings Settings
```

邮件服务配置

## M.Simple.Expand.Services.FtpClientService

M.Simple.Services服务类ftp服务

### FtpClientService

```text
Void .ctor(M.Simple.Expand.Services.FtpClientSettings)
```

构造

### FtpDelete

```text
Boolean FtpDelete(System.String)
```

M.Simple.Services服务类ftp：删除当前目录下的指定文件

- fileName：文件名

### FtpDeleteAsync

```text
System.Threading.Tasks.Task`1[System.Boolean] FtpDeleteAsync(System.String, System.Threading.CancellationToken)
```

M.Simple.Services服务类ftp：删除当前目录下的指定文件

- fileName：文件名

- cancellationToken：取消网络等待和数据传输的令牌

### FtpDirectoryExist

```text
Boolean FtpDirectoryExist(System.String)
```

M.Simple.Services服务类 判断当前目录下指定的子目录是否存在

- RemoteDirectoryName：指定的目录名

### FtpDownload

```text
Boolean FtpDownload(System.String, System.String)
```

M.Simple.Services服务类ftp：下载文件

- filePath：保存路径

- fileName：文件名称

### FtpDownloadAsync

```text
System.Threading.Tasks.Task`1[System.Boolean] FtpDownloadAsync(System.String, System.String, System.Threading.CancellationToken)
```

M.Simple.Services服务类ftp：下载文件

- filePath：保存路径

- fileName：文件名称

- cancellationToken：取消网络等待和数据传输的令牌

### FtpFileExist

```text
Boolean FtpFileExist(System.String)
```

M.Simple.Services服务类判断当前目录下指定的文件是否存在

- RemoteFileName：远程文件名

### FtpMakeDirectory

```text
Boolean FtpMakeDirectory(System.String)
```

M.Simple.Services服务类创建文件夹

### FtpReName

```text
Boolean FtpReName(System.String, System.String)
```

M.Simple.Services服务类重命名

- currentFilename：当前文件名

- newFilename：新文件名

### FtpRemoveDirectory

```text
Boolean FtpRemoveDirectory(System.String)
```

M.Simple.Services服务类删除文件夹

- folderName：文件夹名称

### FtpRemoveDirectoryAsync

```text
System.Threading.Tasks.Task`1[System.Boolean] FtpRemoveDirectoryAsync(System.String, System.Threading.CancellationToken)
```

M.Simple.Services服务类删除文件夹

- folderName：文件夹名称

- cancellationToken：取消网络等待和数据传输的令牌

### FtpUpload

```text
Boolean FtpUpload(System.String)
```

M.Simple.Services服务类ftp：上传文件到当前目录

- filename：文件名(绝对路径)

### FtpUploadAsync

```text
System.Threading.Tasks.Task`1[System.Boolean] FtpUploadAsync(System.String, System.Threading.CancellationToken)
```

M.Simple.Services服务类ftp：上传文件到当前目录

- filename：文件名(绝对路径)

- cancellationToken：取消网络等待和数据传输的令牌

### GetFtpDirectoryList

```text
System.String[] GetFtpDirectoryList()
```

M.Simple.Services服务类 获取当前目录下所有的文件夹列表(仅文件夹)

### GetFtpFileList

```text
System.Collections.Generic.List`1[System.String] GetFtpFileList(System.String, System.String)
```

M.Simple.Services服务类获取当前目录下文件列表(仅文件)

- mask：目标文件后缀名：eg:".xlsx,.xls"

- fileReadingRules：目标文件读取规则：eg:"_cg,_sx,_kc，只读取文件名中包含这些的文件"

### GetFtpFileSize

```text
Int64 GetFtpFileSize(System.String)
```

M.Simple.Services服务类获取指定文件大小

### GetFtpListDirectoryDetails

```text
System.String[] GetFtpListDirectoryDetails()
```

M.Simple.Services服务类获取当前目录下明细(包含文件和文件夹)

### GotoFtpDirectory

```text
Void GotoFtpDirectory(System.String, Boolean)
```

M.Simple.Services服务类切换当前实例目录；并行的目录操作流程应分别使用独立实例

- IsRoot：true 绝对路径 false 相对路径

### ResetFtpClientSettings

```text
Void ResetFtpClientSettings(M.Simple.Expand.Services.FtpClientSettings)
```

用配置副本替换当前实例配置；不影响其他实例或已构造的请求

### Settings

```text
M.Simple.Expand.Services.FtpClientSettings Settings
```

ftp服务配置快照；修改后需通过ResetFtpClientSettings应用

## M.Simple.Expand.Services.FtpClientSettings

M.Simple.Services服务类ftp配置

### FtpClientSettings

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### FtpPassword

```text
System.String FtpPassword
```

ftp密码

### FtpRemotePath

```text
System.String FtpRemotePath
```

ftp根目录（指定FTP连接成功后的当前目录, 如果不指定即默认为根目录）

### FtpServerIP

```text
System.String FtpServerIP
```

ftp服务地址

### FtpUrl

```text
System.String FtpUrl
```

ftpUrl

### FtpUserID

```text
System.String FtpUserID
```

ftp用户名

## M.Simple.Expand.Services.IEMailService

M.Simple.Services服务类邮件服务接口

### GetSmtpClient

```text
System.Net.Mail.SmtpClient GetSmtpClient(M.Simple.Expand.Services.MailSettings)
```

M.Simple.Services服务类获取邮件服务客户端

### SendMail

```text
M.Simple.Expand.Services.MailResult SendMail(M.Simple.Expand.Services.MailContent)
```

M.Simple.Services服务类发送邮件

### SendMailAsync

```text
System.Threading.Tasks.Task`1[M.Simple.Expand.Services.MailResult] SendMailAsync(M.Simple.Expand.Services.MailContent)
```

M.Simple.Services服务类发送邮件（异步）

## M.Simple.Expand.Services.IFtpClientService

M.Simple.Services服务类ftp服务接口

### FtpDelete

```text
Boolean FtpDelete(System.String)
```

M.Simple.Services服务类ftp：删除当前目录下的指定文件

- fileName：文件名

### FtpDeleteAsync

```text
System.Threading.Tasks.Task`1[System.Boolean] FtpDeleteAsync(System.String, System.Threading.CancellationToken)
```

M.Simple.Services服务类ftp：删除当前目录下的指定文件

- fileName：文件名

- cancellationToken：取消网络等待和数据传输的令牌

### FtpDirectoryExist

```text
Boolean FtpDirectoryExist(System.String)
```

M.Simple.Services服务类 判断当前目录下指定的子目录是否存在

- RemoteDirectoryName：指定的目录名

### FtpDownload

```text
Boolean FtpDownload(System.String, System.String)
```

M.Simple.Services服务类ftp：下载文件

- filePath：保存路径

- fileName：文件名称

### FtpDownloadAsync

```text
System.Threading.Tasks.Task`1[System.Boolean] FtpDownloadAsync(System.String, System.String, System.Threading.CancellationToken)
```

M.Simple.Services服务类ftp：下载文件

- filePath：保存路径

- fileName：文件名称

- cancellationToken：取消网络等待和数据传输的令牌

### FtpFileExist

```text
Boolean FtpFileExist(System.String)
```

M.Simple.Services服务类判断当前目录下指定的文件是否存在

- RemoteFileName：远程文件名

### FtpMakeDirectory

```text
Boolean FtpMakeDirectory(System.String)
```

M.Simple.Services服务类创建文件夹

### FtpReName

```text
Boolean FtpReName(System.String, System.String)
```

M.Simple.Services服务类重命名

- currentFilename：当前文件名

- newFilename：新文件名

### FtpRemoveDirectory

```text
Boolean FtpRemoveDirectory(System.String)
```

M.Simple.Services服务类删除文件夹

- folderName：文件夹名称

### FtpRemoveDirectoryAsync

```text
System.Threading.Tasks.Task`1[System.Boolean] FtpRemoveDirectoryAsync(System.String, System.Threading.CancellationToken)
```

M.Simple.Services服务类删除文件夹

- folderName：文件夹名称

- cancellationToken：取消网络等待和数据传输的令牌

### FtpUpload

```text
Boolean FtpUpload(System.String)
```

M.Simple.Services服务类ftp：上传文件到当前目录

- filename：文件名(绝对路径)

### FtpUploadAsync

```text
System.Threading.Tasks.Task`1[System.Boolean] FtpUploadAsync(System.String, System.Threading.CancellationToken)
```

M.Simple.Services服务类ftp：上传文件到当前目录

- filename：文件名(绝对路径)

- cancellationToken：取消网络等待和数据传输的令牌

### GetFtpDirectoryList

```text
System.String[] GetFtpDirectoryList()
```

M.Simple.Services服务类 获取当前目录下所有的文件夹列表(仅文件夹)

### GetFtpFileList

```text
System.Collections.Generic.List`1[System.String] GetFtpFileList(System.String, System.String)
```

M.Simple.Services服务类获取当前目录下文件列表(仅文件)

- mask：目标文件后缀名：eg:".xlsx,.xls"

- fileReadingRules：目标文件读取规则：eg:"_cg,_sx,_kc，只读取文件名中包含这些的文件"

### GetFtpFileSize

```text
Int64 GetFtpFileSize(System.String)
```

M.Simple.Services服务类获取指定文件大小

### GetFtpListDirectoryDetails

```text
System.String[] GetFtpListDirectoryDetails()
```

M.Simple.Services服务类获取当前目录下明细(包含文件和文件夹)

### GotoFtpDirectory

```text
Void GotoFtpDirectory(System.String, Boolean)
```

M.Simple.Services服务类切换当前目录

- IsRoot：true 绝对路径 false 相对路径

### ResetFtpClientSettings

```text
Void ResetFtpClientSettings(M.Simple.Expand.Services.FtpClientSettings)
```

用配置副本替换当前实例配置；不影响其他实例或已构造的请求

## M.Simple.Expand.Services.MailContent

M.Simple.Services服务类邮件内容

### MailContent

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### AttachmentPaths

```text
System.Collections.Generic.List`1[System.String] AttachmentPaths
```

附件

### BccAddresses

```text
System.String BccAddresses
```

密送地址

### Body

```text
System.String Body
```

内容

### CcAddresses

```text
System.String CcAddresses
```

抄送地址

### IsHTML

```text
Boolean IsHTML
```

是否Html格式

### ReplyToAddresses

```text
System.String ReplyToAddresses
```

ReplyToAddresses

### Subject

```text
System.String Subject
```

主题

### ToAddresses

```text
System.String ToAddresses
```

收件人邮件地址

## M.Simple.Expand.Services.MailDeliveryOptions

M.Simple.Services服务类邮件发送选项

### SpecifiedPickUpDirectory

```text
M.Simple.Expand.Services.MailDeliveryOptions SpecifiedPickUpDirectory
```

指定PickUp目录

### UseIISPickUpDirectory

```text
M.Simple.Expand.Services.MailDeliveryOptions UseIISPickUpDirectory
```

使用IIS目录

### UseNetworkSmtp

```text
M.Simple.Expand.Services.MailDeliveryOptions UseNetworkSmtp
```

使用网络Smtp

### value__

```text
Int32 value__
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.Services.MailResult

M.Simple.Services服务类邮件发送接口模型

### MailResult

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### MailResult

```text
Void .ctor(System.String)
```

重载或注释详情请对照功能手册与源码。

### ErrorMsg

```text
System.String ErrorMsg
```

错误消息

### Status

```text
M.Simple.Expand.Services.MailStatus Status
```

邮件发送状态

## M.Simple.Expand.Services.MailSettings

M.Simple.Services服务类邮件服务配置

### MailSettings

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### DeliveryOption

```text
M.Simple.Expand.Services.MailDeliveryOptions DeliveryOption
```

邮件发送选项

### EnableSSL

```text
Boolean EnableSSL
```

是否启用SSL验证

### FromAddress

```text
System.String FromAddress
```

发件人邮箱地址

### GetCredential

```text
System.Net.NetworkCredential GetCredential
```

获得凭证

### Password

```text
System.String Password
```

密码

### PortNumber

```text
Int32 PortNumber
```

端口

### RequireAuth

```text
Boolean RequireAuth
```

验证授权

### SpecifiedPickUpDirectoryPath

```text
System.String SpecifiedPickUpDirectoryPath
```

指定目录路径

### StmpHostServer

```text
System.String StmpHostServer
```

smtp主机服务器

### Username

```text
System.String Username
```

用户名

## M.Simple.Expand.Services.MailStatus

M.Simple.Services服务类邮件状态

### Failure

```text
M.Simple.Expand.Services.MailStatus Failure
```

失败

### Success

```text
M.Simple.Expand.Services.MailStatus Success
```

成功

### value__

```text
Int32 value__
```

重载或注释详情请对照功能手册与源码。

## M.Simple.Expand.ShortCodeGenerator

短编码帮助

### GenerateShortCode

```text
System.String GenerateShortCode()
```

生成Base62短编码（基于雪花ID） 进程内复用固定节点号1的生成器，不保证跨进程或进程重启后的唯一性

返回：11位短编码，同一进程内共享生成器的序列状态

## M.Simple.Expand.SnowCodeGenerator

雪花编码帮助

### SnowCodeGenerator

```text
Void .ctor()
```

重载或注释详情请对照功能手册与源码。

### GetSnowCode

```text
Int64 GetSnowCode()
```

生成雪花编码

### GetTimeStampSnowCode

```text
System.String GetTimeStampSnowCode()
```

生成雪花编码(包含时间戳)

## M.Simple.Expand.SnowflakeIdGenerator

### SnowflakeIdGenerator

```text
Void .ctor(Int64)
```

初始化雪花算法生成器

- workerId：机器ID（0-1023），分布式环境需保证唯一

### NextId

```text
Int64 NextId()
```

生成雪花ID（64位长整型） 性能：单机每秒可生成数百万个ID，线程安全

返回：唯一雪花ID

### NextIdString

```text
System.String NextIdString()
```

转换雪花ID为字符串（可选）

返回：字符串格式的雪花ID

## M.Simple.Expand.UniqueCodeGenerator

唯一编码帮助

### GenerateGuidCode

```text
System.String GenerateGuidCode()
```

生成高性能唯一编码（Guid优化版） 格式：32位大写无分隔符（如：550E8400E29B41D4A716446655440000） 性能：单机每秒可生成百万级编码

返回：32位唯一编码

### GenerateTimeStampGuidCode

```text
System.String GenerateTimeStampGuidCode()
```

生成带时间戳的唯一编码（可排序） 格式：UTC时间自0001-01-01起的毫秒数 + 完整32位大写无分隔符GUID 时间戳仅提供毫秒级时间顺序，同一毫秒内不保证生成顺序；唯一性依赖完整GUID

返回：带时间戳的唯一编码

## M.Simple.Helper.FtpClient

M.Simple.Helper帮助类FTP 帮助类

### FtpClient

```text
Void .ctor(System.String, System.String, System.String, System.String)
```

M.Simple.Helper帮助类连接FTP

- FtpServerIP：FTP连接地址

- FtpUserID：FTP用户名

- FtpPassword：FTP密码

- FtpRemotePath：指定FTP连接成功后的当前目录, 如果不指定即默认为根目录

### Delete

```text
Void Delete(System.String)
```

M.Simple.Helper帮助类FTP：删除当前目录下的指定文件

- fileName：文件名

### DirectoryExist

```text
Boolean DirectoryExist(System.String)
```

M.Simple.Helper帮助类 判断当前目录下指定的子目录是否存在

- RemoteDirectoryName：指定的目录名

### Download

```text
Void Download(System.String, System.String)
```

M.Simple.Helper帮助类FTP：下载文件

- filePath：保存路径

- fileName：文件名称

### FileExist

```text
Boolean FileExist(System.String)
```

M.Simple.Helper帮助类判断当前目录下指定的文件是否存在

- RemoteFileName：远程文件名

### GetDirectoryList

```text
System.String[] GetDirectoryList()
```

M.Simple.Helper帮助类 获取当前目录下所有的文件夹列表(仅文件夹)

### GetFileList

```text
System.Collections.Generic.IEnumerable`1[System.String] GetFileList(System.String, System.String)
```

M.Simple.Helper帮助类获取当前目录下文件列表(仅文件)

- mask：目标文件后缀名：eg:".xlsx,.xls"

- fileReadingRules：目标文件读取规则：eg:"_cg,_sx,_kc，只读取文件名中包含这些的文件"

### GetFileSize

```text
Int64 GetFileSize(System.String)
```

M.Simple.Helper帮助类获取指定文件大小

### GetFilesDetailList

```text
System.String[] GetFilesDetailList()
```

M.Simple.Helper帮助类获取当前目录下明细(包含文件和文件夹)

### GotoDirectory

```text
Void GotoDirectory(System.String, Boolean)
```

M.Simple.Helper帮助类切换当前目录

- IsRoot：true 绝对路径 false 相对路径

### MakeDir

```text
Void MakeDir(System.String)
```

M.Simple.Helper帮助类创建文件夹

### MovieFile

```text
Void MovieFile(System.String, System.String)
```

M.Simple.Helper帮助类移动文件

### ReName

```text
Void ReName(System.String, System.String)
```

M.Simple.Helper帮助类重命名

### RemoveDirectory

```text
Void RemoveDirectory(System.String)
```

M.Simple.Helper帮助类删除文件夹

- folderName：文件夹名称

### Upload

```text
Void Upload(System.String)
```

M.Simple.Helper帮助类FTP：上传文件到当前目录

- filename：文件名

## 源码目录索引

### Extensions

- `Extensions/Extensions.ConfigurationBuilder.cs`

- `Extensions/Extensions.DataTable.cs`

- `Extensions/Extensions.DateTime.cs`

- `Extensions/Extensions.Dictionary.cs`

- `Extensions/Extensions.Encrypt.cs`

- `Extensions/Extensions.EntityCompare.cs`

- `Extensions/Extensions.Enum.cs`

- `Extensions/Extensions.Guid.cs`

- `Extensions/Extensions.IList.cs`

- `Extensions/Extensions.Money.cs`

- `Extensions/Extensions.PropertyInjection.cs`

- `Extensions/Extensions.String.cs`

- `Extensions/Extensions.cs`

### Attributes

- `Attributes/AppServiceAttribute.cs`

- `Attributes/AutowiredAttribute.cs`

- `Attributes/CompareValueAttribute.cs`

- `Attributes/ModifyValueAttribute.cs`

- `Attributes/MyValidationAttribute.cs`

### Comparers

- `Comparers/FastAllPropertiesEqualityComparer.cs`

- `Comparers/PropertyEqualityComparer.cs`

### Models

- `Models/AESEncryptResultModel.cs`

- `Models/ComboBoxIntModel.cs`

- `Models/ComparesResults.cs`

- `Models/DateTimePeriod.cs`

### Services

- `Services/AutowiredService.cs`

- `Services/EMailManager/EMailService.cs`

- `Services/EMailManager/IEMailService.cs`

- `Services/EMailManager/MailContent.cs`

- `Services/EMailManager/MailEnum.cs`

- `Services/EMailManager/MailResult.cs`

- `Services/EMailManager/MailSettings.cs`

- `Services/FtpClientManager/FtpClientService.cs`

- `Services/FtpClientManager/FtpClientSettings.cs`

- `Services/FtpClientManager/IFtpClientService.cs`

### Helper

- `Helper/ChineseCalendarHelper.cs`

- `Helper/EncryptHelper.cs`

- `Helper/FtpClient.cs`

- `Helper/Public/AppSetting.cs`

- `Helper/Public/SnowCodeGenerator.cs`
