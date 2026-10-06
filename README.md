<p align="center">
<img src="https://i.loli.net/2020/08/30/sn8ov9cYDCGeWPk.png"/>
</p>

<div align="center">

# 哔哩

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/flulwh/Bili.Uwp)](https://github.com/flulwh/Bili.Uwp/releases) ![GitHub Release Date](https://img.shields.io/github/release-date/flulwh/Bili.Uwp) ![GitHub All Releases](https://img.shields.io/github/downloads/flulwh/Bili.Uwp/total) ![GitHub stars](https://img.shields.io/github/stars/flulwh/Bili.Uwp?style=flat) ![GitHub forks](https://img.shields.io/github/forks/Richasy/Bili.Uwp)

`哔哩` 现在为 Windows 11 设计！
  
[![Release Builder](https://github.com/flulwh/Bili.Uwp/actions/workflows/release-builder.yml/badge.svg)](https://github.com/flulwh/Bili.Uwp/actions/workflows/release-builder.yml)

</div>

---

### [哔哩助理](https://github.com/Richasy/Bili.Copilot) 已经上线，将逐步替代现在的哔哩 UWP

---

`哔哩` 是一款 [哔哩哔哩](https://www.bilibili.com) 的第三方应用，使用 UWP 框架开发，是原生的 Windows 应用，支持 Windows 10/11 桌面系统。主打设计和易用性，~~广受用户好评~~。

## 维护与署名

本仓库是由 **flulwh** 维护的第三方分支，与原作者及哔哩哔哩官方无隶属关系。原项目由 **Richasy** 创建，原作者署名及 [MIT 许可证](./LICENSE) 均予以保留。第三方维护内容、发布包和签名由 flulwh 独立负责。

原项目：[Richasy/Bili.Uwp](https://github.com/Richasy/Bili.Uwp)。本分支的安装包请从 [flulwh/Bili.Uwp Releases](https://github.com/flulwh/Bili.Uwp/releases) 获取。由于包 Publisher 已变更，原版与本分支的安装包使用不同的签名身份，不能通过本分支安装包覆盖升级原版。

## 开发与签名

为保护签名私钥，仓库不分发 `.pfx` 文件。首次本地打包时，在 Visual Studio 的 `Package.appxmanifest` → `Packaging` → `Choose Certificate` 中创建测试证书，Publisher 必须填写 `CN=flulwh`，并将证书保存为 `src\App\App_TemporaryKey_flulwh.pfx`。安装自签名包前，将对应的公有 `.cer` 安装到当前用户的“受信任的人”证书存储区。不要提交 `.pfx` 或其他私钥文件。

Release Builder 工作流使用仓库 Actions Secret `SIGNING_CERTIFICATE_BASE64` 还原签名证书。仓库所有者需自行配置该 Secret；私钥不要提交到 Git。

## 🙌 简单的开始

~~### 从商店安装 (不可用)~~

~~将链接 `ms-windows-store://pdp/?productid=9mvn4nslt150` 复制到浏览器地址栏打开，从 Microsoft Store 下载。~~

由于名称、功能、图像与官方应用重合度太高，哔哩 UWP 的后续更新已经被商店阻止。目前仅提供 Github 下载

### 侧加载 (Sideload)

如果你想本地安装哔哩，或者尝试当月的最新功能，请打开 [本分支的 Releases](https://github.com/flulwh/Bili.Uwp/releases)，找到最新版本，并选择适用于当前系统的安装包下载。

然后打开 [系统设置](ms-settings:developers)，打开 `开发者模式` ，并等待系统安装一些必要的扩展项。

在应用压缩包下载完成后，解压压缩包，并在管理员模式下，使用 **Windows PowerShell** *(不是PowerShell Core)* 运行解压后的 `install.ps1` 脚本，根据提示进行安装。

**Watch** 项目，以获取应用的更新动态。

关于如何一步步地使用侧加载 (Sideload) 方式安装 UWP 应用及订阅应用更新，请参见 [下载并安装哔哩的详细说明](https://github.com/Richasy/Bili.Uwp/wiki/%E4%B8%8B%E8%BD%BD%E5%B9%B6%E5%AE%89%E8%A3%85%E5%93%94%E5%93%A9%E7%9A%84%E8%AF%A6%E7%BB%86%E8%AF%B4%E6%98%8E) 。

## ❓ 常见问题

在应用的安装使用过程中，你可能会碰到一些问题，这篇文档也许可以帮助你解决遇到的困难：[常见问题](https://github.com/Richasy/Bili.Uwp/wiki/%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98)

## 📃 文档

原项目的架构和使用说明仍可在 [Richasy/Bili.Uwp Wiki](https://github.com/Richasy/Bili.Uwp/wiki) 查阅；本分支的问题与维护建议请提交到 [flulwh/Bili.Uwp Issues](https://github.com/flulwh/Bili.Uwp/issues/new/choose)。

## 🚀 协作

非常感谢有兴趣的开发者或爱好者参与 `哔哩` 项目，分享你的见解与思路。对于任何有兴趣想为 `哔哩` 做出贡献的小伙伴，请参见我们的 [哔哩 Wiki](https://github.com/Richasy/Bili.Uwp/wiki) 了解更多有关协作的内容和引导知识。

## 💬 讨论

借助 Github 平台提供的 Discussions 功能，对于一般讨论、提议或分享，我们都可以在 [哔哩论坛](https://github.com/Richasy/Bili.Uwp/discussions) 中进行，欢迎来这里进行讨论。

## ~~🌏 路线图~~

~~哔哩会逐步完善，请查看 [哔哩里程碑](https://github.com/Richasy/Bili.Uwp/milestones) 来了解哔哩下一步打算做的事情。与此同时，欢迎各位开发者加入，让我们一起打造哔哩的未来。~~

## 🧩 截图

*桌面*
![桌面截图](./assets/screenshot_desktop.png)

*XBOX*
![XBOX截图](./assets/screenshot_xbox.png)
