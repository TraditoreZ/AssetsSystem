# 打包

## 打包入口
```mermaid
工具栏 - AssetSystem - AssetBundle
```
## 配置

### 母包配置规则/分包配置规则
bundle系统需指定母包配置规则与分包配置规则,母包配置规则是会将bundle打入StreamingAssets下随包导出. 分包的配置规则将生成可更新bundle包. 具体的配置方法请参考 Assetbundle规则文档

## 分包根目录
指定一个目录用于记录存放完整的bundle以及更新配置文件, 可用于内网更新测试以及上传CDN使用

## 构建参数
指定bundle构建形式, 压缩格式。 非必要需求建议不要修改

## 平台选择
选择生成bundle文件的运行环境


## 构建形式

母包:使用 [母包配置规则] 构建跟随本体的bundle文件,

分包:使用 [分包配置规则] 构建更新包, 分包需指定基于哪个母包版本做更新差分

## 版本号规则

例:x.xx.xx
版本号第一位无实际意义, 可根据项目自行管理
版本号第二位为强制更新位, 用户必须在更新和退出游戏间做出选择。 适用于lua等重要的更新
版本号第三位为非强制更新位, 用户可以选择不更新直接进入游戏, 适用于资源优化以及非阻断性修复


## Bundle源

生成Bundle后, 会在根目录下存在一个AssetBundles的文件夹, 用于进行差分增量以及增量打包。 如果该文件夹被修改或删除, 则系统无法正常导出基于母包的更新包.  如果有重大更新需要替换母包的资源,则该文件夹允许手动删除降低冗余(生成环境禁止修改或删除该文件夹)


## 更新配置

每次bundle的更新都依赖于 [version]_manifest 以及 [version]_modify.json 文件.
modify记录了 自母包以来所有发生变化bundle的name、bundleHash以及size


# 接口

## AssetSystem.Asset.Initialize(string root, LoadType _loadType, bool simulateIODelay)

#### 接口功能

> Asset初始化

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|root |ture |string|资源根目录 |
|_loadType |true |enum:LoadType |加载资源方式。 AssetDatabase, AssetBundle, Resource|
|simulateIODelay |false |bool |编辑器下是否模拟异步加载延迟|



## AssetSystem.Asset.Load(string path)

#### 接口功能

> 同步加载一个资源

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|path |ture |string|请求加载资源的路径 |


#### 返回字段

|返回字段|字段类型|说明 |
|:----- |:------|:----------------------------- |
|object | Object |返回资源Object, 未找到资源时返回Null|


## AssetSystem.Asset.LoadAll(string path)

#### 接口功能

> 同步加载该bundle下所有资源

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|path |ture |string|请求加载bundle的路径 |


#### 返回字段

|返回字段|字段类型|说明 |
|:----- |:------|:----------------------------- |
|object | Object[] |返回资源Object数组, 未找到资源时返回Null|


## AssetSystem.Asset.LoadAsync(string path, Action<\Object> callback)

#### 接口功能

> 异步请求加载资源

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|path |ture |string|请求加载资源的路径 |
|callback |ture |Action<\Object>|资源加载完成时回调函数 |

#### 异步回调

|返回字段|字段类型|说明 |
|:----- |:------|:----------------------------- |
|object | Object[] |返回资源Object, 未找到资源时返回Null|


## AssetSystem.Asset.LoadAllAsync(string path, Action<\Object[]> callback)

#### 接口功能

> 异步加载该bundle下所有资源

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|path |ture |string|请求加载bundle的路径 |
|callback |ture |Action<\Object[]>|资源加载完成时回调函数 |

#### 异步回调

|返回字段|字段类型|说明 |
|:----- |:------|:----------------------------- |
|object | Object[] |返回资源Object数组, 未找到资源时返回Null|


## AssetSystem.Asset.LoadPackage(string packagePath)

#### 接口功能

> 同步加载一个bundle文件

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|packagePath |ture |string|请求加载bundle的路径 |


#### 返回字段

|返回字段|字段类型|说明 |
|:----- |:------|:----------------------------- |
|result | bool |返回是否将bundle加载到内存中|





## AssetSystem.Asset.LoadPackageAsync(string packagePath, Action<\bool> callback)

#### 接口功能

> 异步加载该bundle文件

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|packagePath |ture |string|请求加载bundle文件的路径 |
|callback |ture |Action<\Object[]>|bundle加载到内存中完成时回调函数 |

#### 异步回调

|返回字段|字段类型|说明 |
|:----- |:------|:----------------------------- |
|result | bool |返回是否将bundle加载到内存中|






## AssetSystem.Asset.LoadScene(string scenePath)

#### 接口功能

> 同步加载场景资源

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|scenePath |ture |string|请求加载场景的路径 |


#### 返回字段

|返回字段|字段类型|说明 |
|:----- |:------|:----------------------------- |
|sceneName | string |返回场景名称,加载失败时为空|



## AssetSystem.Asset.LoadSceneAsync(string scenePath, Action<\string> callback)

#### 接口功能

> 异步加载场景资源

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|scenePath |ture |string|请求加载场景的路径 |
|callback |ture |Action<\Object[]>|场景资源加载完成回调 |

#### 异步回调

|返回字段|字段类型|说明 |
|:----- |:------|:----------------------------- |
|sceneName | string |返回场景名称,加载失败时为空|







## AssetSystem.Asset.ExistAsset(string path)

#### 接口功能

> 请求判断资源是否存在

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|path |ture |string|请求资源路径 |


#### 返回字段

|返回字段|字段类型|说明 |
|:----- |:------|:----------------------------- |
|exit | bool |返回资源检查结果, 存在为True, 不存在为False|




## AssetSystem.Asset.Unload(string path)

#### 接口功能

> 请求卸载资源

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|path |ture |string|请求资源路径 |

## AssetSystem.Asset.Unload(Object obj)

#### 接口功能

> 请求卸载资源

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|obj |ture |Object|请求资源Obj |



## AssetSystem.Asset.UnloadAll(string path)

#### 接口功能

> 请求卸载bundle文件

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|path |ture |string|请求卸载bundle文件路径 |


## AssetSystem.Asset.UnloadScene(string scenePath)

#### 接口功能

> 请求卸载场景资源

#### 请求参数

|参数|必选|类型|说明|
|:----- |:-------|:-----|----- |
|scenePath |ture |string|请求卸载场景路径 |


