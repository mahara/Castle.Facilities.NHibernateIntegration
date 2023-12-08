# Castle.Facilities.NHibernateIntegration

Castle.Facilities.NHibernateIntegration

## License

Castle.Facilities.NHibernateIntegration is &copy; 2004-2024 Castle Project. It is free software, and may be redistributed under the terms of the [Apache 2.0](http://opensource.org/licenses/Apache-2.0) license.

## NuGet Preview Feed

If you would like to use preview NuGet's from our CI builds on AppVeyor, you can add the following NuGet source to your project:

```
https://ci.appveyor.com/nuget/windsor-qkry8n2r6yak
```

## Tests

The tests run against a MSSQL Server 2008 by default.
But due to NHibernate's features, you can change the database server to any of your choice.

1. Create two databases on a database server, e.g.: "test" and "test2" on a MSSQL Server.

2. Modify the connection information properties in "Castle.Facilities.NHibernateIntegration.Tests" project to use the databases created.
   See "App.config" and all "\*Configuration\*.xml" files for full details.



