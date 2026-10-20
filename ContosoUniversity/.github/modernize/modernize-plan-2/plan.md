# Modernization Plan: ContosoUniversity

**Project**: ContosoUniversity

---

## Technical Framework

- **Language**: C#
- **Framework**: .NET Framework 4.8, ASP.NET MVC 5
- **Build Tool**: MSBuild with a legacy, non-SDK-style project
- **Database**: SQL Server with Entity Framework Core 3.1
- **Key Dependencies**: Entity Framework Core, MSMQ, local file storage

---

## Overview

> Modernize ContosoUniversity from its on-premises Windows application to a
> supported .NET LTS runtime and Azure-hosted architecture. The application
> currently uses ASP.NET MVC 5, SQL Server, MSMQ, and local disk for uploads.
> The modernized application will:
>
> - Run on the latest .NET LTS release to remain supported and maintainable.
> - Use Azure SQL Database, Azure Service Bus, and Azure Blob Storage for
>   persistence, notifications, and teaching-material uploads.
> - Run on Azure Container Apps, as described in the project documentation.
> - Have known dependency CVEs remediated before deployment.
>
> Modernization proceeds from the runtime upgrade to Azure service migrations,
> followed by vulnerability remediation and deployment.

---

## Migration Impact Summary

| App | Original service | Azure service | Auth | Notes |
|-----|------------------|---------------|------|-------|
| ContosoUniversity | .NET Framework 4.8 | .NET 10 LTS | — | Runtime |
| ContosoUniversity | SQL Server | Azure SQL Database | MI | Data |
| ContosoUniversity | MSMQ | Azure Service Bus | MI | Notifications |
| ContosoUniversity | Local files | Azure Blob Storage | MI | Uploads |
| ContosoUniversity | On-prem Windows | Azure Container Apps | MI | Hosting |

---
