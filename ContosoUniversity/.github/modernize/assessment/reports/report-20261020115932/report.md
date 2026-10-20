# ContosoUniversity

## Summary

| Metric | Value |
|--------|-------|
| Total Issues | 32 |
| Mandatory Blockers | 16 |
| Potential Issues | 8 |

## Component Information

| Property | Value |
|----------|-------|
| Language | C# |
| Frameworks | .NETFramework,Version=v4.8 |
| Build tools | MSBuild |

## Cloud Readiness Issues

| Issue Name | Criticality | Story Points | Occurrences |
|------------|-------------|--------------|-------------|
| MSMQ usage is detected | Mandatory | 3 | [12](#MSMQ_usage_is_detected) |
| Windows authentication detected | Mandatory | 3 | [1](#Windows_authentication_detected) |
| Local or network IO operations detected | Potential | 3 | [8](#Local_or_network_IO_operations_detected) |
| SQL database connection detected | Potential | 3 | [1](#SQL_database_connection_detected) |
| Connection strings without configuration builders detected | Optional | 3 | [2](#Connection_strings_without_configuration_builders_detected) |
| Static content detected | Optional | 3 | [1](#Static_content_detected) |

### Issue Details

<details id="MSMQ_usage_is_detected">
<summary><b>MSMQ usage is detected</b> — affected files</summary>

- `Services\NotificationService.cs (line 21)`
- `Services\NotificationService.cs (line 26)`
- `Services\NotificationService.cs (line 30)`
- `Services\NotificationService.cs (line 22)`
- `Services\NotificationService.cs (line 19)`
- `Services\NotificationService.cs (line 11)`
- `Services\NotificationService.cs (line 77)`
- `Services\NotificationService.cs (line 77)`
- `Services\NotificationService.cs (line 73)`
- `Services\NotificationService.cs (line 54)`
- `Services\NotificationService.cs (line 54)`
- `Services\NotificationService.cs (line 57)`

</details>

<details id="Windows_authentication_detected">
<summary><b>Windows authentication detected</b> — affected files</summary>

- `Web.config`

</details>

<details id="Local_or_network_IO_operations_detected">
<summary><b>Local or network IO operations detected</b> — affected files</summary>

- `Controllers\CoursesController.cs (line 161)`
- `Controllers\CoursesController.cs (line 78)`
- `Controllers\CoursesController.cs (line 159)`
- `Controllers\CoursesController.cs (line 76)`
- `Controllers\CoursesController.cs (line 229)`
- `Controllers\CoursesController.cs (line 172)`
- `Controllers\CoursesController.cs (line 233)`
- `Controllers\CoursesController.cs (line 174)`

</details>

<details id="SQL_database_connection_detected">
<summary><b>SQL database connection detected</b> — affected files</summary>

- `Web.config`

</details>

<details id="Connection_strings_without_configuration_builders_detected">
<summary><b>Connection strings without configuration builders detected</b> — affected files</summary>

- `Web.config`
- `Web.config`

</details>

<details id="Static_content_detected">
<summary><b>Static content detected</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

## DotNET Upgrade Issues [View Details](scenarios/dotnet-version-upgrade/assessment.md)

| Issue Category | Criticality | Story Points | Occurrences |
|----------------|-------------|--------------|-------------|
| Binary incompatible for selected .NET version | Mandatory | 1 | [63](#Binary_incompatible_for_selected_NET_version) |
| NuGet package functionality is included with framework reference | Mandatory | 1 | [11](#NuGet_package_functionality_is_included_with_framework_reference) |
| System.Web.Optimization bundling and minification is not supported in .NET Core and should be replaced with actual html tags pointing to content files | Mandatory | 1 | [9](#System_Web_Optimization_bundling_and_minification_is_not_supported_in_NET_Core_and_should_be_replaced_with_actual_html_tags_pointing_to_content_files) |
| Manual redirect conflicts with auto-generated version | Mandatory | 1 | [6](#Manual_redirect_conflicts_with_auto-generated_version) |
| Routes registration via RouteCollection is not supported in .NET Core and needs to be converted to the route mappings on the application object | Mandatory | 1 | [2](#Routes_registration_via_RouteCollection_is_not_supported_in_NET_Core_and_needs_to_be_converted_to_the_route_mappings_on_the_application_object) |
| NuGet package is incompatible | Mandatory | 1 | [1](#NuGet_package_is_incompatible) |
| Project file needs to be converted to SDK-style | Mandatory | 1 | [1](#Project_file_needs_to_be_converted_to_SDK-style) |
| Project's target framework(s) needs to be changed | Mandatory | 1 | [1](#Project_s_target_framework_s_needs_to_be_changed) |
| Convert application initialization code from Global.asax.cs to .NET Core and clean up Global.asax.cs | Mandatory | 1 | [1](#Convert_application_initialization_code_from_Global_asax_cs_to_NET_Core_and_clean_up_Global_asax_cs) |
| Convert System.Messaging to MSMQ in .NET Core | Mandatory | 1 | [1](#Convert_System_Messaging_to_MSMQ_in_NET_Core) |
| MSMQ & Message Queuing | Mandatory | 2 | 0 |
| Legacy Configuration System | Mandatory | 2 | 0 |
| ASP.NET Framework (System.Web) | Mandatory | 4 | 0 |
| Source incompatible for selected .NET version | Potential | 1 | [29](#Source_incompatible_for_selected_NET_version) |
| NuGet package upgrade is recommended | Potential | 1 | [24](#NuGet_package_upgrade_is_recommended) |
| Binding redirect forces version downgrade | Potential | 1 | [6](#Binding_redirect_forces_version_downgrade) |
| NuGet package is deprecated | Optional | 1 | [4](#NuGet_package_is_deprecated) |
| NuGet package contains security vulnerability | Optional | 1 | [1](#NuGet_package_contains_security_vulnerability) |

### Issue Details

<details id="Binary_incompatible_for_selected_NET_version">
<summary><b>Binary incompatible for selected .NET version</b> — affected files</summary>

- `Services\NotificationService.cs (line 116, col 12)`
- `Services\NotificationService.cs (line 71, col 12)`
- `Services\NotificationService.cs (line 74, col 16)`
- `Services\NotificationService.cs (line 73, col 16)`
- `Services\NotificationService.cs (line 60, col 16)`
- `Services\NotificationService.cs (line 54, col 16)`
- `Services\NotificationService.cs (line 30, col 12)`
- `Services\NotificationService.cs (line 26, col 16)`
- `Services\NotificationService.cs (line 22, col 16)`
- `Services\NotificationService.cs (line 21, col 16)`
- `Services\NotificationService.cs (line 19, col 12)`
- `Services\NotificationService.cs (line 11, col 38)`
- `Global.asax.cs (line 17, col 12)`
- `App_Start\RouteConfig.cs (line 7, col 8)`

</details>

<details id="NuGet_package_functionality_is_included_with_framework_reference">
<summary><b>NuGet package functionality is included with framework reference</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details id="System_Web_Optimization_bundling_and_minification_is_not_supported_in_NET_Core_and_should_be_replaced_with_actual_html_tags_pointing_to_content_files">
<summary><b>System.Web.Optimization bundling and minification is not supported in .NET Core and should be replaced with actual html tags pointing to content files</b> — affected files</summary>

- `Views\Courses\Create.cshtml`
- `Views\Courses\Edit.cshtml`
- `Views\Departments\Create.cshtml`
- `Views\Departments\Edit.cshtml`
- `Views\Instructors\Create.cshtml`
- `Views\Instructors\Edit.cshtml`
- `Views\Shared\_Layout.cshtml`
- `Views\Students\Create.cshtml`
- `Views\Students\Edit.cshtml`

</details>

<details id="Manual_redirect_conflicts_with_auto-generated_version">
<summary><b>Manual redirect conflicts with auto-generated version</b> — affected files</summary>

- `Web.config`

</details>

<details id="Routes_registration_via_RouteCollection_is_not_supported_in_NET_Core_and_needs_to_be_converted_to_the_route_mappings_on_the_application_object">
<summary><b>Routes registration via RouteCollection is not supported in .NET Core and needs to be converted to the route mappings on the application object</b> — affected files</summary>

- `Global.asax.cs`
- `App_Start\RouteConfig.cs`

</details>

<details id="NuGet_package_is_incompatible">
<summary><b>NuGet package is incompatible</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details id="Project_file_needs_to_be_converted_to_SDK-style">
<summary><b>Project file needs to be converted to SDK-style</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details id="Project_s_target_framework_s_needs_to_be_changed">
<summary><b>Project's target framework(s) needs to be changed</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details id="Convert_application_initialization_code_from_Global_asax_cs_to_NET_Core_and_clean_up_Global_asax_cs">
<summary><b>Convert application initialization code from Global.asax.cs to .NET Core and clean up Global.asax.cs</b> — affected files</summary>

- `Global.asax.cs`

</details>

<details id="Convert_System_Messaging_to_MSMQ_in_NET_Core">
<summary><b>Convert System.Messaging to MSMQ in .NET Core</b> — affected files</summary>

- `Services\NotificationService.cs`

</details>

<details id="Source_incompatible_for_selected_NET_version">
<summary><b>Source incompatible for selected .NET version</b> — affected files</summary>

- `Services\NotificationService.cs (line 73, col 16)`
- `Services\NotificationService.cs (line 16, col 12)`
- `Data\SchoolContextFactory.cs (line 9, col 12)`
- `Controllers\CoursesController.cs (line 127, col 8)`
- `Controllers\CoursesController.cs (line 179, col 24)`
- `Controllers\CoursesController.cs (line 148, col 20)`
- `Controllers\CoursesController.cs (line 138, col 20)`
- `Controllers\CoursesController.cs (line 134, col 16)`
- `Controllers\CoursesController.cs (line 44, col 8)`
- `Controllers\CoursesController.cs (line 86, col 24)`
- `Controllers\CoursesController.cs (line 65, col 20)`
- `Controllers\CoursesController.cs (line 55, col 20)`
- `Controllers\CoursesController.cs (line 51, col 16)`
- `Global.asax.cs (line 26, col 12)`
- `Global.asax.cs (line 11, col 34)`

</details>

<details id="NuGet_package_upgrade_is_recommended">
<summary><b>NuGet package upgrade is recommended</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details id="Binding_redirect_forces_version_downgrade">
<summary><b>Binding redirect forces version downgrade</b> — affected files</summary>

- `Web.config`

</details>

<details id="NuGet_package_is_deprecated">
<summary><b>NuGet package is deprecated</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

<details id="NuGet_package_contains_security_vulnerability">
<summary><b>NuGet package contains security vulnerability</b> — affected files</summary>

- `ContosoUniversity.csproj`

</details>

## Security Issues

> **Note:** These issues were generated by AI and may contain inaccuracies or incomplete information. Please review carefully.

| Issue Name | Criticality | Story Points | Files |
|------------|-------------|--------------|-------|
| CVE-2024-0056: Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass  | Mandatory | 1 | [1](#CVE-2024-0056_Microsoft_Data_SqlClient_and_System_Data_SqlClient_vulnerable_to_SQL_Data_Provider_Security_Feature_Bypass) |
| CWE-662: Improper Synchronization | Potential | 8 | [2](#CWE-662_Improper_Synchronization) |
| CWE-820: Missing Synchronization | Potential | 8 | [2](#CWE-820_Missing_Synchronization) |
| CWE-99: Improper Control of Resource Identifiers ('Resource Injection') | Potential | 3 | [2](#CWE-99_Improper_Control_of_Resource_Identifiers_Resource_Injection) |
| CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal') | Optional | 8 | [2](#CWE-22_Improper_Limitation_of_a_Pathname_to_a_Restricted_Directory_Path_Traversal) |
| CWE-79: Improper Neutralization of Input During Web Page Generation ('Cross-site Scripting') | Optional | 8 | [3](#CWE-79_Improper_Neutralization_of_Input_During_Web_Page_Generation_Cross-site_Scripting) |
| CWE-732: Incorrect Permission Assignment for Critical Resource | Optional | 5 | [1](#CWE-732_Incorrect_Permission_Assignment_for_Critical_Resource) |
| CWE-23: Relative Path Traversal | Optional | 5 | [2](#CWE-23_Relative_Path_Traversal) |

### Security Issue Details

<details id="CVE-2024-0056_Microsoft_Data_SqlClient_and_System_Data_SqlClient_vulnerable_to_SQL_Data_Provider_Security_Feature_Bypass">
<summary><b>CVE-2024-0056: Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass </b> — affected files</summary>

- `packages.config:14`

</details>

<details id="CWE-662_Improper_Synchronization">
<summary><b>CWE-662: Improper Synchronization</b> — affected files</summary>

- `Services/NotificationService.cs:20-23`
- `Controllers/BaseController.cs:12`

</details>

<details id="CWE-820_Missing_Synchronization">
<summary><b>CWE-820: Missing Synchronization</b> — affected files</summary>

- `Services/NotificationService.cs:20-23`
- `Controllers/BaseController.cs:12`

</details>

<details id="CWE-99_Improper_Control_of_Resource_Identifiers_Resource_Injection">
<summary><b>CWE-99: Improper Control of Resource Identifiers ('Resource Injection')</b> — affected files</summary>

- `Controllers/CoursesController.cs:130`
- `Controllers/CoursesController.cs:172`

</details>

<details id="CWE-22_Improper_Limitation_of_a_Pathname_to_a_Restricted_Directory_Path_Traversal">
<summary><b>CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')</b> — affected files</summary>

- `Controllers/CoursesController.cs:130`
- `Controllers/CoursesController.cs:172`

</details>

<details id="CWE-79_Improper_Neutralization_of_Input_During_Web_Page_Generation_Cross-site_Scripting">
<summary><b>CWE-79: Improper Neutralization of Input During Web Page Generation ('Cross-site Scripting')</b> — affected files</summary>

- `Scripts/notifications.js:80-84`
- `Services/NotificationService.cs:39-49`
- `Controllers/CoursesController.cs:102`

</details>

<details id="CWE-732_Incorrect_Permission_Assignment_for_Critical_Resource">
<summary><b>CWE-732: Incorrect Permission Assignment for Critical Resource</b> — affected files</summary>

- `Services/NotificationService.cs:20-23`

</details>

<details id="CWE-23_Relative_Path_Traversal">
<summary><b>CWE-23: Relative Path Traversal</b> — affected files</summary>

- `Controllers/CoursesController.cs:130`
- `Controllers/CoursesController.cs:172`

</details>

---

## Codebase Insights

> **Note:** These documents are generated by AI and may contain inaccuracies or incomplete information. Please review carefully.

> **Codebase Insights aren't available yet.**
>
> These documents are generated when assessment runs with **Full analysis** coverage. Re-run the assessment and set `analysisCoverage: full` to enable them.

[Share feedback](https://aka.ms/ghcp-appmod/feedback)
