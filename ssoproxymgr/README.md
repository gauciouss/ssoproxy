# SSOProxyMgr 系統設計文件

## 專案名稱：sso proxy 管理介面

## 1. 系統概述
ssoproxy的管理介面

## 2. 使用技術
    1. .NET10
    2. dotnet mvc
    3. bootstrap
    4. jQuery

### 3. 系統架構



## 4. 功能說明

* 登入

* sso_proxy_target 管理

* reids token 管理

 redis format: 
    {
        key: string,
        value: string (trust accounts format value),
        exipire_time: yyyyMdd HHmmss
    }
    