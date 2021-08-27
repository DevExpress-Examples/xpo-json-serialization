<!-- default badges list -->
![](https://img.shields.io/endpoint?url=https://codecentral.devexpress.com/api/v1/VersionRange/148645646/21.1.3%2B)
[![](https://img.shields.io/badge/Open_in_DevExpress_Support_Center-FF7200?style=flat-square&logo=DevExpress&logoColor=white)](https://supportcenter.devexpress.com/ticket/details/T830519)
[![](https://img.shields.io/badge/📖_How_to_use_DevExpress_Examples-e9f6fc?style=flat-square)](https://docs.devexpress.com/GeneralInformation/403183)
<!-- default badges end -->
# An ASP.NET Core Web API CRUD Service

When building a [Web API service](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-3.1), it is convenient to separate your Controller's code from the data access logic. Don't deal with SQL, DB connections, etc. each time you need to extend your API, but use the database-independent object-oriented approach to retrieve data with sorting, filtering and complex data shaping.

The persistent object JSON serialization feature makes it easy to use XPO as a Data Access layer in an ASP.NET Core Web API service. You no longer need to manually format JSON responses or make a POCO copy of each persistent class. This tutorial demonstrates how to enable this feature and implement a few Controllers.

You may also be interested in the following examples:
* [How to Implement OData v4 Service with XPO (.NET Core)](https://github.com/DevExpress-Examples/XPO_how-to-implement-odata4-service-with-xpo-netcore)
* [How to Implement OData v4 Service with XPO (.NET Framework)](https://github.com/DevExpress-Examples/XPO_how-to-implement-odata4-service-with-xpo)


## Prerequisites
 Visual Studio 2019 with the following workloads:
 * ASP.NET and web development
 * .NET Core cross-platform development
 * [.NET Core 3.1 SDK or later](https://www.microsoft.com/net/download)
 
## Create the project.
Use the following steps to create a project or refer to the [original tutorial](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-3.1) in the Microsoft documentation.
* From the **File** menu, select **New** > **Project**.
* Select the **ASP.NET Core Web Application** template and click **Next**. Fill in the **Name** field and click the **Create** button.
* In the **Create a new ASP.NET Core web application** dialog, choose the ASP.NET Core version. Select the **API** template and click **Create**.

## Configure XPO
* Install [DevExpress.XPO](https://www.nuget.org/packages/DevExpress.Xpo/) Nuget package.  
   `Install-Package DevExpress.Xpo`
* Use the [ORM Data Model Wizard](https://documentation.devexpress.com/CoreLibraries/14810) to create the data model or generate it from the existing database.
* Add the connection string to the *appsettings.json* file.  
   ```json
   "ConnectionStrings": {
      "SQLite": "XpoProvider=SQLite;Data Source=demo.db",
      "MSSqlServer": "XpoProvider=MSSqlServer;data source=(local);user id=sa;password=;initial catalog=XpoASPNETCoreDemo;Persist Security Info=true"
   }
   ```
* Open the *Startup.cs* file and register the UnitOfWork Service as described in [ASP.NET Core Dependency Injection in XPO](https://www.devexpress.com/Support/Center/Question/Details/T637597).  
   ```cs
   public void ConfigureServices(IServiceCollection services) {
            services.AddControllersWithViews();
            services.AddCors();
            services.AddXpoDefaultUnitOfWork(true, (DataLayerOptionsBuilder options) =>
                options.UseConnectionString(Configuration.GetConnectionString("MSSqlServer"))
                 //.UseAutoCreationOption(AutoCreateOption.DatabaseAndSchema) // debug only
                .UseEntityTypes(ConnectionHelper.GetPersistentTypes()));
            //...
   }

   ``` 

* In the *Startup.cs* also register custom JSON converter and Metadata provider Services. 
    ```cs

            //...
            services.AddHttpContextAccessor();
            services.ConfigureOptions<ConfigureJsonOptions>();
            services.AddSingleton(typeof(IModelMetadataProvider), typeof(XpoMetadataProvider));
            //...

   ```

* Add [JsonConverters.cs](CS/XpoSerialization/JsonConverters.cs), [XPOJsonOptions.cs](CS/XpoSerialization/XPOJsonOptions.cs) files to your project. 
## Create a Controller
* Declare a local variable to store the [UnitOfWork](https://documentation.devexpress.com/CoreLibraries/2138) instance passed as a constructor parameter.
   ```cs
   [ApiController]
   [Route("api/[controller]")]
   public class CustomersController : ControllerBase {
      private UnitOfWork uow;
      public CustomersController(UnitOfWork uow) {
         this.uow = uow;
      }
   ```
* GET methods implementation is simple and straightforward. Load object(s) from the database and return the result.  
   ```cs
   [HttpGet]
   public IEnumerable Get() {
      return uow.Query<Customer>();
   } 
   [HttpGet("{id}")]
   public Customer Get(int id) {
     return uow.GetObjectByKey<Customer>(id);
   }
   ```
* The POST method creates a new persistent object and saves it to the database.
   ```cs
   [HttpPost]
   public IActionResult Post([FromBody] Customer customer) {
      try {
         uow.CommitChanges();
         return NoContent();
      } catch(Exception exception) {
         return BadRequest(exception);
      }
   } 
 
   ```
* The PUT and DELETE methods do not require any special remarks.

   ```cs

   [HttpPut("{id}")]
   public IActionResult Put(int id, [FromBody] Customer customer) {
      if(id != customer.Oid)
         return NotFound();
      try {
         uow.CommitChanges();
         return NoContent();
      } catch(Exception exception) {
         return BadRequest(exception);
      }
   }
   [HttpDelete("{id}")]
   public IActionResult Delete(int id) {
      try {
         Customer customer = uow.GetObjectByKey<Customer>(id);
         uow.Delete(customer);
         uow.CommitChanges();
         return NoContent();
      } catch(Exception exception) {
         return BadRequest(exception);
      }
   } 

   ```
