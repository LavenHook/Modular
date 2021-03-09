This project is designated for components that are necessary for functionality across multiple modules, but are 
*not specific* to the domain/application itself. These componenets are domain/application agnostic. This is more about 
the infrastructure that can be reused by each project/module, regradless of whether it is DAL, Domain, Application, UI, etc. 
(no APB analog)
-examples to include: 
  - IModule/Module definitions
  - general authentication and authorization (properties, configurations, extensions) that are not application specific
  - INavigationProvider definitions that can be injected/configured by other projects
  - options for configuring any of the objects already included in this project
  - components that are not stored/hydrated with domain models