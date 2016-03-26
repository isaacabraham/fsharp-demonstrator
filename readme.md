# Hosting F# in a web application on Azure

This repo is designed to showcase some F# features whilst at the same time also giving some examples of how to host your F# applications within a web-facing application and also to host it within Azure. Examples are provided for both ASP .NET Web API as well as Suave.

## F# Web Applications

### ASP .NET Web API
Example shown illustrates how to host some self-contained F# business logic assembly within a simple OWIN-based ASP .NET Web API project.

### Suave
Example shown illustrates how to host the same F# business logic assembly within a Suave application and routing.

## Azure Hosting

### Azure Resource Manager (ARM) Template
ARM templates allow you to specify declaratively how to create an application. A resource template is included to allow you to deploy all infrastructure from a single script: -

* App Hosting Plan
* App Service
* App Insights
* Configuration Settings for App Service based on App Insights

### Suave integration
A web.config file is provided within the Suave application which is used when hosting Suave executables within the Azure App Service. Essentially IIS acts as a pass-thru to allow you to forward all requests directly to the Suave application.

### App Insights integration
Some code has been added to the Suave application to allow easy App Insights integration, which captures e.g. requests, events, tracing and even dependency tracking e.g. HTTP, Azure and SQL calls.

### Source Code deployment
Support is provided for direct deployment from source code e.g. GitHub directly to Azure App Service.  This is enabled through the ``build.cmd`` file, which utilises Azure's Kudu framework to perform builds via calling: -

* ``Paket bootstrapper`` (download Paket)
* ``Paket restore`` (download Nuget dependencies)
* ``MSBuild`` (build source code)
* ``Kudu Sync`` (diff MSBuild outputs with previous deployment)
    
Note that the ``.deployment`` file is used to tell Kudu which command to run whenever a push occurs to e.g. the Git repository.
    
### Web Jobs
A simple example of a Web Job running directly via an F# script is supplied (``sample.fsx``). This script is copied as part of the ``build.cmd`` process to the webjobs folder within the App Service (e.g. ``app_data\jobs\continuous\Sample``). The web job simply runs every 30 seconds and writes to a local file. 