# Sparc.Storage.Azure
[![Nuget](https://img.shields.io/nuget/v/Sparc.Storage.Azure?label=Sparc.Storage.Azure)](https://www.nuget.org/packages/Sparc.Storage.Azure/)

The `Sparc.Storage.Azure` library enables the use of Azure Storage solution in Sparc projects.

Getting Started

1. Add the Sparc.Storage.Azure Nuget package: [![Nuget](https://img.shields.io/nuget/v/Sparc.Storage.Azure?label=Sparc.Storage.Azure)](https://www.nuget.org/packages/Sparc.Storage.Azure/)
2. Add your Storage string at the `appsettings.json` file
```json
 "ConnectionStrings": { 
    "Storage": "",
     ...
  }
```
3. At your `Program.cs` file add
```csharp
  builder.Services.AddAzureStorage(builder.Configuration.GetConnectionString("Storage");
  ```
This way anytime you inject the `IFileRepository` and use it in a Feature it will be actually be implemented as a [AzureBlobRepository](AzureBlobRepository.cs)

You can check an example at the [UserPhotoUpload feature](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Users/UserPhotoUpload.cs)
